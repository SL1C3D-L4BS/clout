using UnityEngine;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Crafting;

namespace Clout.Empire.Properties
{
    /// <summary>
    /// Runtime property instance — attached to the property GameObject in the world.
    /// Tracks stash (stored product), applied upgrades, condition, and employee slots.
    ///
    /// Created by PropertyManager.BuyProperty() or PropertySystemFactory at edit time.
    /// Implements IInteractable so the player can walk up and interact.
    /// </summary>
    public class Property : MonoBehaviour, IInteractable
    {
        [Header("Definition")]
        [SerializeField] private PropertyDefinition _definition;

        // ─── Runtime State ──────────────────────────────────
        private readonly HashSet<int> _appliedUpgrades = new HashSet<int>();
        private readonly List<StashSlot> _stash = new List<StashSlot>();
        private float _condition = 1f; // 1.0 = perfect, 0 = condemned
        private bool _isOwned;

        // Upgrade bonuses (cached from applied upgrades)
        private float _bonusStorage;
        private int _bonusEmployeeSlots;
        private float _bonusCraftingSpeed;
        private float _bonusCraftingQuality;
        private float _bonusSecurityLevel;
        private float _bonusPoliceVisibility; // Negative = less visible
        private float _bonusDailyRevenue;
        private float _bonusUpkeepReduction;

        // ─── Properties ─────────────────────────────────────
        public PropertyDefinition Definition => _definition;
        public bool IsOwned => _isOwned;
        public float Condition => _condition;
        public IReadOnlyList<StashSlot> Stash => _stash;
        public int StashCount => _stash.Count;

        public string InteractionPrompt => _isOwned
            ? $"Enter: {_definition.propertyName}"
            : $"Buy: {_definition.propertyName} (${_definition.purchasePrice:F0})";

        // ─── Setup ───────────────────────────────────────────

        /// <summary>
        /// Set the definition at edit time (called by PropertySystemFactory).
        /// </summary>
        public void SetDefinition(PropertyDefinition definition)
        {
            _definition = definition;
        }

        // ─── Factory ────────────────────────────────────────

        /// <summary>
        /// Create a new property instance from a definition.
        /// Called by PropertyManager.BuyProperty().
        /// </summary>
        public static Property Create(PropertyDefinition definition, Vector3 position, Transform parent = null)
        {
            // Try to find existing property GameObject at this location
            Property existing = FindExistingProperty(definition);
            if (existing != null)
            {
                existing._isOwned = true;
                return existing;
            }

            // Create new GameObject
            GameObject propObj = new GameObject($"Property_{definition.propertyName.Replace(" ", "_")}");
            propObj.transform.position = position;
            if (parent != null) propObj.transform.SetParent(parent);

            Property prop = propObj.AddComponent<Property>();
            prop._definition = definition;
            prop._isOwned = true;

            return prop;
        }

        private static Property FindExistingProperty(PropertyDefinition definition)
        {
            var all = FindObjectsByType<Property>(FindObjectsInactive.Exclude);
            foreach (var p in all)
            {
                if (p._definition == definition) return p;
            }
            return null;
        }

        // ─── IInteractable ──────────────────────────────────

        public bool CanInteract(CharacterStateManager character)
        {
            return !character.isDead;
        }

        public void OnInteract(CharacterStateManager character)
        {
            if (_isOwned)
            {
                // Open property management UI
                Debug.Log($"[Property] Entering {_definition.propertyName}");
                PropertyManager mgr = PropertyManager.Instance;
                // PropertyUI will handle display via events
            }
            else
            {
                // Trigger purchase flow
                Debug.Log($"[Property] Viewing {_definition.propertyName} for sale");
            }
        }

        // ─── Stash Management ───────────────────────────────

        /// <summary>
        /// Store product in this property's stash. Returns quantity stored.
        /// </summary>
        public int StoreProduct(string productId, int quantity, float quality)
        {
            float maxStorage = GetMaxStorage();
            int currentStored = GetTotalStashed();
            int canStore = Mathf.Min(quantity, (int)(maxStorage - currentStored));
            if (canStore <= 0) return 0;

            // Try stacking
            for (int i = 0; i < _stash.Count; i++)
            {
                if (_stash[i].productId == productId && Mathf.Abs(_stash[i].quality - quality) < 0.05f)
                {
                    var slot = _stash[i];
                    slot.quantity += canStore;
                    _stash[i] = slot;
                    return canStore;
                }
            }

            // New slot
            _stash.Add(new StashSlot
            {
                productId = productId,
                quantity = canStore,
                quality = quality
            });

            return canStore;
        }

        /// <summary>
        /// Retrieve product from stash. Returns quantity retrieved.
        /// </summary>
        public int RetrieveProduct(string productId, int quantity)
        {
            int retrieved = 0;
            for (int i = _stash.Count - 1; i >= 0 && retrieved < quantity; i--)
            {
                if (_stash[i].productId != productId) continue;

                var slot = _stash[i];
                int take = Mathf.Min(slot.quantity, quantity - retrieved);
                slot.quantity -= take;
                retrieved += take;

                if (slot.quantity <= 0)
                    _stash.RemoveAt(i);
                else
                    _stash[i] = slot;
            }
            return retrieved;
        }

        public int GetTotalStashed()
        {
            int total = 0;
            foreach (var s in _stash) total += s.quantity;
            return total;
        }

        /// <summary>
        /// Police raid — confiscate all stashed product. Returns total units seized.
        /// </summary>
        public int ConfiscateStash()
        {
            int total = GetTotalStashed();
            _stash.Clear();
            _condition = Mathf.Max(0.3f, _condition - 0.15f); // Raids damage property
            return total;
        }

        // ─── Upgrades ───────────────────────────────────────

        public bool HasUpgrade(int upgradeIndex) => _appliedUpgrades.Contains(upgradeIndex);

        public void ApplyUpgrade(int upgradeIndex)
        {
            if (_definition.availableUpgrades == null) return;
            if (upgradeIndex >= _definition.availableUpgrades.Length) return;
            if (_appliedUpgrades.Contains(upgradeIndex)) return;

            _appliedUpgrades.Add(upgradeIndex);
            PropertyUpgrade upgrade = _definition.availableUpgrades[upgradeIndex];

            // Cache upgrade effects
            if (upgrade.effects != null)
            {
                foreach (var effect in upgrade.effects)
                {
                    switch (effect.type)
                    {
                        case PropertyUpgradeType.StorageCapacity:
                            _bonusStorage += effect.value;
                            break;
                        case PropertyUpgradeType.EmployeeSlots:
                            _bonusEmployeeSlots += (int)effect.value;
                            break;
                        case PropertyUpgradeType.CraftingSpeed:
                            _bonusCraftingSpeed += effect.value;
                            break;
                        case PropertyUpgradeType.CraftingQuality:
                            _bonusCraftingQuality += effect.value;
                            break;
                        case PropertyUpgradeType.SecurityLevel:
                            _bonusSecurityLevel += effect.value;
                            break;
                        case PropertyUpgradeType.PoliceVisibility:
                            _bonusPoliceVisibility += effect.value;
                            break;
                        case PropertyUpgradeType.DailyRevenue:
                            _bonusDailyRevenue += effect.value;
                            break;
                        case PropertyUpgradeType.UpkeepReduction:
                            _bonusUpkeepReduction += effect.value;
                            break;
                    }
                }
            }
        }

        // ─── Computed Stats ─────────────────────────────────

        public float GetMaxStorage() => _definition.maxStorage + _bonusStorage;
        public int GetMaxEmployeeSlots() => _definition.maxEmployeeSlots + _bonusEmployeeSlots;
        public float GetCraftingSpeedMultiplier() => 1f + _bonusCraftingSpeed;
        public float GetCraftingQualityBonus() => _bonusCraftingQuality;
        public float GetSecurityLevel() => Mathf.Clamp01(_bonusSecurityLevel);
        public float GetPoliceVisibility() => Mathf.Clamp01(_definition.policeVisibility + _bonusPoliceVisibility);

        public float GetDailyRevenue()
        {
            return (_definition.dailyRevenue + _bonusDailyRevenue) * _condition;
        }

        public float GetDailyUpkeep()
        {
            float upkeep = _definition.dailyUpkeep * (1f - Mathf.Clamp01(_bonusUpkeepReduction));
            return upkeep;
        }

        /// <summary>
        /// Total property value = purchase price + all applied upgrade costs.
        /// </summary>
        public float GetTotalValue()
        {
            float value = _definition.purchasePrice;
            if (_definition.availableUpgrades != null)
            {
                foreach (int idx in _appliedUpgrades)
                {
                    if (idx < _definition.availableUpgrades.Length)
                        value += _definition.availableUpgrades[idx].cost;
                }
            }
            return value * _condition;
        }

        // ─── Condition / Degradation ────────────────────────

        /// <summary>
        /// Called when upkeep can't be paid — property degrades.
        /// </summary>
        public void DegradeFromNonPayment()
        {
            _condition = Mathf.Max(0.1f, _condition - 0.05f);
        }

        /// <summary>
        /// Repair property condition. Costs money.
        /// </summary>
        public float Repair(float amount)
        {
            float repairCost = (1f - _condition) * _definition.purchasePrice * 0.1f * amount;
            _condition = Mathf.Min(1f, _condition + amount);
            return repairCost;
        }
    }

    // ─── Data Structures ─────────────────────────────────────

    [System.Serializable]
    public struct StashSlot
    {
        public string productId;
        public int quantity;
        public float quality;
    }
}
