using UnityEngine;
using System;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Economy;
using Clout.Empire.Reputation;
using Clout.Empire.Territory;
using Clout.World.Police;
using Clout.Utils;

namespace Clout.Empire.Properties
{
    /// <summary>
    /// Manages all player-owned properties — purchases, upgrades, daily revenue/upkeep,
    /// stash access, raid events, and territory influence from ownership.
    ///
    /// Ticks on TransactionLedger.OnDayEnd for daily economics.
    /// Phase 2 singleplayer — FishNet authority restored in Phase 4.
    /// </summary>
    public class PropertyManager : MonoBehaviour
    {
        public static PropertyManager Instance { get; private set; }

        [Header("Config")]
        public int maxOwnedProperties = 10;
        public float raidHeatThreshold = 60f; // Heat above this enables raid rolls

        // Owned properties — runtime state
        private readonly List<Property> _ownedProperties = new List<Property>();

        // ─── Events ──────────────────────────────────────────
        public event Action<Property> OnPropertyPurchased;
        public event Action<Property> OnPropertySold;
        public event Action<Property> OnPropertyUpgraded;
        public event Action<Property> OnPropertyRaided;
        public event Action OnDailyTick;

        // ─── Properties ──────────────────────────────────────
        public IReadOnlyList<Property> OwnedProperties => _ownedProperties;
        public int PropertyCount => _ownedProperties.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Subscribe to day cycle for revenue/upkeep
            if (TransactionLedger.Instance != null)
                TransactionLedger.Instance.OnDayEnd += ProcessDailyTick;
        }

        private void OnDestroy()
        {
            if (TransactionLedger.Instance != null)
                TransactionLedger.Instance.OnDayEnd -= ProcessDailyTick;
        }

        // ─── Purchase / Sell ─────────────────────────────────

        /// <summary>
        /// Buy a property. Requires clean cash (legal transaction for real estate).
        /// Returns the Property instance if successful, null if failed.
        /// </summary>
        public Property BuyProperty(PropertyDefinition definition, Vector3 worldPosition, Transform parent = null)
        {
            if (definition == null) return null;
            if (_ownedProperties.Count >= maxOwnedProperties)
            {
                Debug.Log("[Property] Max properties reached.");
                return null;
            }

            // Already own this exact property?
            foreach (var owned in _ownedProperties)
            {
                if (owned.Definition == definition)
                {
                    Debug.Log($"[Property] Already own {definition.propertyName}.");
                    return null;
                }
            }

            // Check funds — properties require clean cash for large purchases,
            // but allow dirty for cheap safehouses (under $5000)
            CashManager cash = CashManager.Instance;
            if (cash == null) return null;

            bool isSmallPurchase = definition.purchasePrice < 5000f;
            if (isSmallPurchase)
            {
                if (!cash.CanAfford(definition.purchasePrice))
                {
                    Debug.Log($"[Property] Can't afford {definition.propertyName} (${definition.purchasePrice:F0}).");
                    return null;
                }
                cash.Spend(definition.purchasePrice, $"Property: {definition.propertyName}");
            }
            else
            {
                if (!cash.CanAffordClean(definition.purchasePrice))
                {
                    Debug.Log($"[Property] Need ${definition.purchasePrice:F0} clean cash for {definition.propertyName}.");
                    return null;
                }
                cash.SpendClean(definition.purchasePrice, $"Property: {definition.propertyName}");
            }

            // Create property instance
            Property property = Property.Create(definition, worldPosition, parent);
            _ownedProperties.Add(property);

            // Territory influence
            TerritoryManager territory = FindAnyObjectByType<TerritoryManager>();
            if (territory != null && !string.IsNullOrEmpty(definition.districtName))
                territory.AddInfluence(definition.districtName, 0, 15f);

            // Reputation
            ReputationManager rep = FindAnyObjectByType<ReputationManager>();
            if (rep != null)
                rep.AddClout(ReputationManager.CloutValues.BuyProperty, $"bought {definition.propertyName}");

            // Events
            EventBus.Publish(new PropertyPurchasedEvent
            {
                propertyId = definition.propertyName,
                districtId = definition.districtName ?? "",
                price = definition.purchasePrice
            });

            OnPropertyPurchased?.Invoke(property);

            Debug.Log($"[Property] Purchased {definition.propertyName} for ${definition.purchasePrice:F0} " +
                      $"at {worldPosition}");
            return property;
        }

        /// <summary>
        /// Sell a property back for 60% of current value (purchase + upgrades).
        /// </summary>
        public bool SellProperty(Property property)
        {
            if (property == null || !_ownedProperties.Contains(property)) return false;

            float sellValue = property.GetTotalValue() * 0.6f;

            CashManager cash = CashManager.Instance;
            if (cash != null)
                cash.EarnClean(sellValue, $"Sold property: {property.Definition.propertyName}");

            _ownedProperties.Remove(property);
            OnPropertySold?.Invoke(property);

            // Destroy the property GameObject
            if (property.gameObject != null)
                Destroy(property.gameObject);

            Debug.Log($"[Property] Sold {property.Definition.propertyName} for ${sellValue:F0}.");
            return true;
        }

        // ─── Upgrades ────────────────────────────────────────

        /// <summary>
        /// Apply an upgrade to a property. Deducts clean cash.
        /// </summary>
        public bool UpgradeProperty(Property property, int upgradeIndex)
        {
            if (property == null || !_ownedProperties.Contains(property)) return false;

            PropertyDefinition def = property.Definition;
            if (def.availableUpgrades == null || upgradeIndex >= def.availableUpgrades.Length)
                return false;

            PropertyUpgrade upgrade = def.availableUpgrades[upgradeIndex];
            if (property.HasUpgrade(upgradeIndex)) return false; // Already applied

            CashManager cash = CashManager.Instance;
            if (cash == null || !cash.CanAfford(upgrade.cost)) return false;

            cash.Spend(upgrade.cost, $"Upgrade: {upgrade.upgradeName} @ {def.propertyName}");
            property.ApplyUpgrade(upgradeIndex);

            OnPropertyUpgraded?.Invoke(property);
            Debug.Log($"[Property] Upgraded {def.propertyName}: {upgrade.upgradeName} (${upgrade.cost:F0})");
            return true;
        }

        // ─── Daily Tick ──────────────────────────────────────

        private void ProcessDailyTick()
        {
            CashManager cash = CashManager.Instance;
            if (cash == null) return;

            float totalRevenue = 0;
            float totalUpkeep = 0;

            for (int i = _ownedProperties.Count - 1; i >= 0; i--)
            {
                Property prop = _ownedProperties[i];
                if (prop == null) { _ownedProperties.RemoveAt(i); continue; }

                // Revenue (clean — from cover business)
                float revenue = prop.GetDailyRevenue();
                if (revenue > 0)
                {
                    cash.EarnClean(revenue, $"Revenue: {prop.Definition.propertyName}");
                    totalRevenue += revenue;
                }

                // Upkeep (clean — operating costs)
                float upkeep = prop.GetDailyUpkeep();
                if (upkeep > 0)
                {
                    if (cash.CanAfford(upkeep))
                    {
                        cash.Spend(upkeep, $"Upkeep: {prop.Definition.propertyName}");
                        totalUpkeep += upkeep;
                    }
                    else
                    {
                        // Can't pay upkeep — property degrades
                        prop.DegradeFromNonPayment();
                        Debug.LogWarning($"[Property] Can't afford upkeep for {prop.Definition.propertyName}!");
                    }
                }

                // Raid check
                ProcessRaidCheck(prop);
            }

            if (totalRevenue > 0 || totalUpkeep > 0)
            {
                Debug.Log($"[Property] Daily tick — Revenue: ${totalRevenue:F0}, Upkeep: ${totalUpkeep:F0}, " +
                          $"Net: ${totalRevenue - totalUpkeep:F0}");
            }

            OnDailyTick?.Invoke();
        }

        private void ProcessRaidCheck(Property property)
        {
            // Only raid if heat is above threshold
            WantedSystem wanted = FindAnyObjectByType<WantedSystem>();
            if (wanted == null) return;

            float heat = wanted.CurrentHeat;
            if (heat < raidHeatThreshold) return;

            // Raid probability scales with heat and property visibility
            float heatFactor = Mathf.InverseLerp(raidHeatThreshold, 100f, heat);
            float raidRoll = UnityEngine.Random.value;
            float raidChance = property.Definition.raidChance * heatFactor *
                               (1f + property.Definition.policeVisibility);

            // Security upgrades reduce raid chance
            raidChance *= (1f - property.GetSecurityLevel() * 0.5f);

            if (raidRoll < raidChance)
            {
                ExecuteRaid(property);
            }
        }

        private void ExecuteRaid(Property property)
        {
            Debug.Log($"[Property] RAID on {property.Definition.propertyName}!");

            // Confiscate stashed product
            int confiscated = property.ConfiscateStash();

            // Cash confiscation
            CashManager cash = CashManager.Instance;
            float cashTaken = 0;
            if (cash != null)
            {
                cashTaken = cash.Confiscate(
                    UnityEngine.Random.Range(500f, 2000f),
                    $"Raid: {property.Definition.propertyName}");
            }

            // Heat spike
            WantedSystem wanted = FindAnyObjectByType<WantedSystem>();
            if (wanted != null)
                wanted.AddHeat(WantedSystem.HeatValues.DealingNearPolice, "property raid");

            // Reputation hit
            ReputationManager rep = FindAnyObjectByType<ReputationManager>();
            if (rep != null && confiscated > 0)
                rep.RemoveClout(Mathf.Abs(ReputationManager.CloutValues.ProductSeized), "raid seized product");

            // Survive bonus if player is at property
            if (rep != null)
                rep.AddClout(ReputationManager.CloutValues.SurviveRaid, "survived raid");

            OnPropertyRaided?.Invoke(property);

            EventBus.Publish(new PropertyRaidedEvent
            {
                propertyId = property.Definition.propertyName,
                productConfiscated = confiscated,
                cashConfiscated = cashTaken
            });
        }

        // ─── Queries ─────────────────────────────────────────

        public Property GetProperty(string propertyName)
        {
            foreach (var p in _ownedProperties)
            {
                if (p.Definition.propertyName == propertyName) return p;
            }
            return null;
        }

        public List<Property> GetPropertiesByType(PropertyType type)
        {
            var result = new List<Property>();
            foreach (var p in _ownedProperties)
            {
                if (p.Definition.propertyType == type) result.Add(p);
            }
            return result;
        }

        public bool OwnsPropertyOfType(PropertyType type)
        {
            foreach (var p in _ownedProperties)
            {
                if (p.Definition.propertyType == type) return true;
            }
            return false;
        }

        /// <summary>
        /// Total daily net income from all properties.
        /// </summary>
        public float GetTotalDailyNet()
        {
            float net = 0;
            foreach (var p in _ownedProperties)
                net += p.GetDailyRevenue() - p.GetDailyUpkeep();
            return net;
        }

        /// <summary>
        /// Total value of all owned properties (purchase + upgrades).
        /// </summary>
        public float GetTotalPortfolioValue()
        {
            float total = 0;
            foreach (var p in _ownedProperties)
                total += p.GetTotalValue();
            return total;
        }
    }
}
