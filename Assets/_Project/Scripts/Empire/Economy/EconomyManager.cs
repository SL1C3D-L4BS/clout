using UnityEngine;
using System.Collections.Generic;
using System;

namespace Clout.Empire.Economy
{
    /// <summary>
    /// Economy simulation — Phase 2 singleplayer.
    /// FishNet server-authoritative logic will be restored in Phase 4.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        [Header("Market Config")]
        public float priceUpdateInterval = 300f;     // 5 minutes between market shifts
        public float maxPriceSwing = 0.3f;           // 30% max price change per interval
        public float demandDecayRate = 0.01f;        // Natural demand decrease

        [Header("Money Laundering")]
        public float launderingRate = 0.1f;          // 10% fee to launder
        public float maxDailyLaunder = 5000f;        // Per business per day

        // Market state
        private Dictionary<string, MarketData> _marketPrices = new Dictionary<string, MarketData>();
        private float _lastPriceUpdate;

        public event Action<string, float> OnPriceChanged;  // productId, newPrice

        private void Start()
        {
            _lastPriceUpdate = Time.time;
        }

        /// <summary>
        /// Get current street price for a product in a specific zone.
        /// </summary>
        public float GetStreetPrice(string productId, string zoneId)
        {
            if (_marketPrices.TryGetValue($"{productId}_{zoneId}", out var data))
                return data.currentPrice;

            return 50f; // Default fallback
        }

        /// <summary>
        /// Record a sale — affects supply/demand calculations.
        /// </summary>
        public void RecordSale(string productId, string zoneId, int quantity, float quality)
        {
            string key = $"{productId}_{zoneId}";
            if (!_marketPrices.ContainsKey(key))
            {
                _marketPrices[key] = new MarketData { productId = productId, zoneId = zoneId, currentPrice = 50f };
            }

            var data = _marketPrices[key];
            data.totalSoldThisCycle += quantity;

            // Oversupply reduces price
            if (data.totalSoldThisCycle > data.demandThisCycle)
            {
                float excess = (data.totalSoldThisCycle - data.demandThisCycle) / Mathf.Max(1f, data.demandThisCycle);
                data.currentPrice *= (1f - excess * 0.1f);
            }

            // Quality affects repeat business
            data.averageQuality = Mathf.Lerp(data.averageQuality, quality, 0.1f);

            _marketPrices[key] = data;
        }

        /// <summary>
        /// Launder cash through a business front. Returns laundered amount (minus fee).
        /// </summary>
        public float LaunderCash(float cashAmount, string businessId)
        {
            float fee = cashAmount * launderingRate;
            float laundered = cashAmount - fee;
            Debug.Log($"[Economy] Laundered ${cashAmount:F0} through {businessId} -> ${laundered:F0} clean (${fee:F0} fee)");
            return laundered;
        }

        private void Update()
        {
            if (Time.time - _lastPriceUpdate >= priceUpdateInterval)
            {
                UpdateMarketPrices();
                _lastPriceUpdate = Time.time;
            }
        }

        private void UpdateMarketPrices()
        {
            foreach (var key in new List<string>(_marketPrices.Keys))
            {
                var data = _marketPrices[key];

                // Natural demand recovery
                data.demandThisCycle = Mathf.Max(10f, data.baseDemand * (1f + UnityEngine.Random.Range(-0.2f, 0.2f)));

                // Reset cycle
                data.totalSoldThisCycle = 0;

                // Price drift toward base
                data.currentPrice = Mathf.Lerp(data.currentPrice, data.basePrice, 0.1f);

                _marketPrices[key] = data;
                OnPriceChanged?.Invoke(data.productId, data.currentPrice);
            }
        }
    }

    [System.Serializable]
    public struct MarketData
    {
        public string productId;
        public string zoneId;
        public float currentPrice;
        public float basePrice;
        public float baseDemand;
        public float demandThisCycle;
        public int totalSoldThisCycle;
        public float averageQuality;
    }
}
