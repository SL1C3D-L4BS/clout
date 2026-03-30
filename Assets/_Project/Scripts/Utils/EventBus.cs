using System;
using System.Collections.Generic;
using UnityEngine;

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

    public struct WorkerHiredEvent
    {
        public string workerId;
        public string workerType;
        public string assignedPropertyId;
    }

    public struct MoneyChangedEvent
    {
        public float dirtyMoney;
        public float cleanMoney;
        public float changeAmount;
        public string reason;
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
}
