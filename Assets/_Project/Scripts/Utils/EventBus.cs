using System;
using System.Collections.Generic;
using UnityEngine;
using Clout.Empire.Reputation;

namespace Clout.Utils
{
    /// <summary>
    /// Global event bus — ported from Sharp Accent's EventsManager.
    /// Upgraded from string-based UnityEvents to type-safe generic events.
    ///
    /// Decouples systems: combat doesn't need to know about UI, empire doesn't
    /// need to know about audio. Everything communicates through events.
    ///
    /// Usage:
    ///   // Subscribe
    ///   EventBus.Subscribe<DealCompletedEvent>(OnDealCompleted);
    ///
    ///   // Publish
    ///   EventBus.Publish(new DealCompletedEvent { productId = "og_kush", cashEarned = 500f });
    ///
    ///   // Unsubscribe
    ///   EventBus.Unsubscribe<DealCompletedEvent>(OnDealCompleted);
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers
            = new Dictionary<Type, List<Delegate>>();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _subscribers[type] = list;
            }
            list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);
            if (_subscribers.TryGetValue(type, out var list))
                list.Remove(handler);
        }

        public static void Publish<T>(T evt) where T : struct
        {
            Type type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list)) return;

            // Iterate copy to allow modification during iteration
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is Action<T> handler)
                {
                    try { handler(evt); }
                    catch (Exception e) { Debug.LogError($"[EventBus] Error in {type.Name} handler: {e}"); }
                }
            }
        }

        /// <summary>Clear all subscribers (call on scene transition if needed).</summary>
        public static void ClearAll() => _subscribers.Clear();
    }

    // ─────────────────────────────────────────────────────────
    //  GAME EVENTS — Add new event structs here as systems grow
    // ─────────────────────────────────────────────────────────

    // Combat
    public struct EnemyKilledEvent
    {
        public GameObject enemy;
        public GameObject killer;
        public bool wasPolice;
        public int cloutEarned;
        public float heatEarned;
    }

    public struct DamageTakenEvent
    {
        public GameObject target;
        public GameObject attacker;
        public int damageAmount;
        public bool wasLethal;
    }

    public struct WeaponFiredEvent
    {
        public GameObject shooter;
        public string weaponId;
        public Vector3 position;
    }

    // Empire
    public struct DealCompletedEvent
    {
        public string productId;
        public int quantity;
        public float cashEarned;
        public string customerId;
        public string districtId;
    }

    public struct ProductCookedEvent
    {
        public string productId;
        public int quantity;
        public int qualityTier;
        public string stationId;
    }

    public struct PropertyPurchasedEvent
    {
        public string propertyId;
        public string districtId;
        public float price;
    }

    public struct PropertyRaidedEvent
    {
        public string propertyId;
        public int productConfiscated;
        public float cashConfiscated;
    }

    public struct PropertyUpgradedEvent
    {
        public string propertyId;
        public string upgradeName;
        public float cost;
    }

    public struct WorkerHiredEvent
    {
        public string workerId;
        public string workerType;
        public string assignedPropertyId;
    }

    public struct MoneyChangedEvent
    {
        public float totalCash;
        public float dirtyCash;
        public float cleanCash;
        public float changeAmount;
        public string source;
    }

    // World
    public struct HeatChangedEvent
    {
        public float newHeat;
        public float changeAmount;
        public string reason;
    }

    public struct WantedLevelChangedEvent
    {
        public int newLevel;
        public int previousLevel;
    }

    public struct DistrictEnteredEvent
    {
        public string districtId;
        public string previousDistrictId;
    }

    // ─── Worker Events (Spec v2.0 Section 13) ────────────────────

    public struct WorkerFiredEvent
    {
        public string workerId;
        public string workerName;
        public string reason;
        public string assignedPropertyId;
    }

    public struct WorkerArrestedEvent
    {
        public string workerId;
        public string workerName;
        public float heatGenerated;
        public bool knewCriticalInfo;    // Compartmentalization risk — did they know enough to hurt you?
        public string assignedPropertyId;
    }

    public struct WorkerBetrayedEvent
    {
        public string workerId;
        public string workerName;
        public string betrayalType;      // "informant", "theft", "rival_defection", "independent"
        public float damageAmount;       // Cash/product stolen or intel value leaked
        public string assignedPropertyId;
    }

    public struct WorkerShiftEndEvent
    {
        public string workerId;
        public string workerName;
        public string role;              // "Dealer", "Cook", "Guard"
        public float cashEarned;         // Revenue generated this shift (dealers)
        public int unitsProduced;        // Product made this shift (cooks)
        public int dealsMade;            // Number of deals completed (dealers)
        public string assignedPropertyId;
    }

    public struct WorkerDealCompleteEvent
    {
        public string workerId;
        public string customerId;
        public string productId;
        public int quantity;
        public float cashEarned;
        public float quality;
    }

    // ─── Reputation Events (Spec v2.0 Section 36) ────────────────

    public struct ReputationChangedEvent
    {
        public ReputationDimension dimension;
        public float oldValue;
        public float newValue;
        public string reason;
        public ReputationVector vector;  // Full 4D state after change
    }

    // ─── Police Events (Spec v2.0 Sections 26-30) ──────────────

    public struct PoliceBackupRequestEvent
    {
        public GameObject requestingOfficer;
        public Vector3 location;
        public float currentHeat;
    }

    public struct PoliceSpawnedEvent
    {
        public GameObject officerObject;
        public Vector3 spawnPosition;
        public int totalOfficers;
    }

    public struct PlayerArrestedEvent
    {
        public GameObject arrestingOfficer;
        public Vector3 location;
        public float heatAtArrest;
        public bool wasLethalForce;
    }

    public struct CrimeWitnessedEvent
    {
        public string crimeType;
        public Vector3 location;
        public int witnessCount;
        public float heatGenerated;
        public float severity;
    }

    public struct PropertyRaidAlertEvent
    {
        public string propertyId;
        public float warningTime;
        public int raidStrength;
    }

    // ─── Market Events (Step 13) ────────────────────────────────

    public struct MarketPriceChangedEvent
    {
        public string productId;
        public string districtId;
        public float oldPrice;
        public float newPrice;
        public float percentChange;
    }

    public struct MarketEventTriggeredEvent
    {
        public string eventName;
        public Empire.Economy.MarketEventType eventType;
        public int durationDays;
        public float priceMultiplier;
        public float demandMultiplier;
    }

    public struct MarketEventEndedEvent
    {
        public string eventName;
        public Empire.Economy.MarketEventType eventType;
        public int durationDays;
    }

    public struct CommodityPriceShockEvent
    {
        public Empire.Economy.CommodityType commodity;
        public float oldPrice;
        public float newPrice;
        public float percentChange;
    }

    public struct MarketManipulationEvent
    {
        public Empire.Economy.ManipulationType tactic;
        public string productId;
        public string districtId;
        public float investment;
        public float estimatedPriceImpact;
    }
}
