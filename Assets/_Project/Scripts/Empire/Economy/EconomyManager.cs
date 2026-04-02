using UnityEngine;
using System.Collections.Generic;
using System;

namespace Clout.Empire.Economy
{
    /// <summary>
    /// Economy simulation — multi-layer market dynamics.
    ///
    /// Spec v2.0 Section 23: Full price formula:
    ///   P(t) = P_base × (D(t)/S(t)) × (1 + E_r) × (1 + R_m) × M_s
    ///
    /// Where:
    ///   D(t)/S(t) — dynamic demand/supply ratio (updated every tick)
    ///   E_r       — price elasticity per region (how sensitive price is to supply changes)
    ///   R_m       — risk modifier from heat/investigation (high heat = risk premium)
    ///   M_s       — seasonal/geopolitical multiplier
    ///
    /// Market mechanics:
    ///   - Per-district supply/demand curves
    ///   - Player actions directly move markets (flood = crash, restrict = spike)
    ///   - Addiction economics (addicted customers are price-inelastic)
    ///   - Quality premium (exponential price increase for high quality)
    ///   - Competition pricing (AI factions undercut dynamically — Phase 4)
    ///   - Cross-district arbitrage
    ///
    /// Phase 2 singleplayer — server-authoritative logic restored in Phase 4.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        // ─── Configuration ───────────────────────────────────────────

        [Header("Market Tick")]
        [Tooltip("Seconds between market price recalculations.")]
        public float priceUpdateInterval = 300f;

        [Header("Price Formula Parameters")]
        [Tooltip("Maximum D/S ratio effect. Prevents infinite price spikes.")]
        public float maxDemandSupplyRatio = 3f;

        [Tooltip("Minimum D/S ratio. Prevents prices from reaching zero.")]
        public float minDemandSupplyRatio = 0.2f;

        [Header("Elasticity (E_r) — Per Region")]
        [Tooltip("Default elasticity. 0 = inelastic (price doesn't move much). 1 = very elastic.")]
        [Range(0f, 1f)] public float defaultElasticity = 0.3f;

        [Header("Risk Modifier (R_m) — From Heat")]
        [Tooltip("Heat-to-risk conversion. At max heat (100), risk modifier = this value.")]
        [Range(0f, 1f)] public float maxRiskPremium = 0.5f;

        [Header("Seasonal Modifier (M_s)")]
        [Tooltip("Current seasonal multiplier. 1.0 = no effect. Set by world events.")]
        public float seasonalMultiplier = 1f;

        [Header("Money Laundering")]
        public float launderingFeeRate = 0.1f;
        public float maxDailyLaunderPerBusiness = 5000f;

        [Header("Quality Premium")]
        [Tooltip("Exponent for quality-based pricing. Higher = bigger premium for top-tier product.")]
        public float qualityExponent = 1.5f;

        // ─── Market State ────────────────────────────────────────────

        private Dictionary<string, MarketData> _markets = new Dictionary<string, MarketData>();
        private float _lastPriceUpdate;
        private float _currentGlobalHeat;   // Cached from WantedSystem for R_m calculation

        // ─── Events ─────────────────────────────────────────────────

        public event Action<string, float, float> OnPriceChanged;  // productId, oldPrice, newPrice

        // ─── Lifecycle ───────────────────────────────────────────────

        private void Start()
        {
            _lastPriceUpdate = Time.time;
        }

        private void Update()
        {
            if (Time.time - _lastPriceUpdate >= priceUpdateInterval)
            {
                UpdateAllMarkets();
                _lastPriceUpdate = Time.time;
            }
        }

        // ─── Core Price Formula (Spec v2.0 Section 23) ───────────────

        /// <summary>
        /// Full multi-layer price calculation.
        ///
        /// Step 13: When MarketSimulator is active, delegates to its advanced formula
        /// (competition, events, commodity costs, consumer confidence). Falls back to
        /// the legacy formula when MarketSimulator is not present.
        /// </summary>
        public float CalculatePrice(string productId, string zoneId, float quality = 0.5f)
        {
            // Step 13 — delegate to MarketSimulator when available
            var sim = MarketSimulator.Instance;
            if (sim != null)
                return sim.GetPrice(productId, zoneId, quality);

            // Legacy formula (Phase 2 fallback)
            MarketData market = GetOrCreateMarket(productId, zoneId);

            float basePrice = market.basePrice;
            float supply = Mathf.Max(1f, market.totalSoldThisCycle + 1f);
            float demand = Mathf.Max(1f, market.demandThisCycle);
            float dsRatio = Mathf.Clamp(demand / supply, minDemandSupplyRatio, maxDemandSupplyRatio);

            float elasticity = market.elasticity > 0f ? market.elasticity : defaultElasticity;
            float elasticityFactor = 1f + elasticity * (dsRatio - 1f);

            float riskModifier = 1f + (_currentGlobalHeat / 100f) * maxRiskPremium;
            float seasonal = seasonalMultiplier;
            float qualityMultiplier = Mathf.Pow(0.5f + quality, qualityExponent);

            float finalPrice = basePrice * dsRatio * elasticityFactor * riskModifier * seasonal * qualityMultiplier;
            return Mathf.Max(1f, finalPrice);
        }

        /// <summary>
        /// Convenience method — returns price for average quality product.
        /// Used by DealerAI and ShopKeeper for default pricing.
        /// </summary>
        public float GetStreetPrice(string productId, string zoneId)
        {
            return CalculatePrice(productId, zoneId, 0.5f);
        }

        // ─── Market Operations ───────────────────────────────────────

        /// <summary>
        /// Record a sale — shifts supply curve. More sales = more supply pressure = lower prices.
        /// </summary>
        public void RecordSale(string productId, string zoneId, int quantity, float quality)
        {
            MarketData market = GetOrCreateMarket(productId, zoneId);

            market.totalSoldThisCycle += quantity;

            // Running average quality (EMA with 0.1 smoothing)
            market.averageQuality = Mathf.Lerp(market.averageQuality, quality, 0.1f);

            // Track historical volume for trend analysis
            market.lifetimeVolume += quantity;

            _markets[$"{productId}_{zoneId}"] = market;
        }

        /// <summary>
        /// Record a purchase — shifts demand curve. More buying = more demand = higher prices.
        /// Called when player/NPC buys from suppliers.
        /// </summary>
        public void RecordPurchase(string productId, string zoneId, int quantity)
        {
            MarketData market = GetOrCreateMarket(productId, zoneId);
            market.demandThisCycle += quantity * 0.5f;  // Purchases indicate demand
            _markets[$"{productId}_{zoneId}"] = market;
        }

        /// <summary>
        /// Update the heat value used in risk modifier calculation.
        /// Called by WantedSystem when heat changes.
        /// </summary>
        public void UpdateHeatForPricing(float currentHeat)
        {
            _currentGlobalHeat = currentHeat;
        }

        /// <summary>
        /// Set seasonal multiplier — called by world event system or time manager.
        /// Examples: summer festival = 1.3 (demand spike), winter = 0.85 (reduced outdoor dealing)
        /// </summary>
        public void SetSeasonalMultiplier(float multiplier)
        {
            seasonalMultiplier = Mathf.Clamp(multiplier, 0.5f, 2f);
        }

        /// <summary>
        /// Register a new product in a zone with initial market conditions.
        /// </summary>
        public void InitializeMarket(string productId, string zoneId, float basePrice, float baseDemand, float elasticity = 0f)
        {
            string key = $"{productId}_{zoneId}";
            _markets[key] = new MarketData
            {
                productId = productId,
                zoneId = zoneId,
                basePrice = basePrice,
                currentPrice = basePrice,
                baseDemand = baseDemand,
                demandThisCycle = baseDemand,
                elasticity = elasticity > 0f ? elasticity : defaultElasticity,
                averageQuality = 0.5f
            };
        }

        // ─── Money Laundering ────────────────────────────────────────

        /// <summary>
        /// Launder cash through a business front. Returns clean amount (minus fee).
        /// Spec v2.0 Section 15: 5-step pipeline. Phase 2 implements step 1 (placement) only.
        /// </summary>
        public float LaunderCash(float dirtyAmount, string businessId)
        {
            float fee = dirtyAmount * launderingFeeRate;
            float clean = dirtyAmount - fee;
            Debug.Log($"[Economy] Laundered ${dirtyAmount:F0} through {businessId} -> ${clean:F0} clean (${fee:F0} fee)");
            return clean;
        }

        // ─── Market Queries ──────────────────────────────────────────

        /// <summary>
        /// Get current market data for analysis (UI, AI decision-making).
        /// </summary>
        public MarketData GetMarketData(string productId, string zoneId)
        {
            return GetOrCreateMarket(productId, zoneId);
        }

        /// <summary>
        /// Get all tracked markets for dashboard display.
        /// </summary>
        public IReadOnlyDictionary<string, MarketData> GetAllMarkets() => _markets;

        /// <summary>
        /// Get the demand/supply ratio for a product in a zone. >1 = undersupplied, <1 = oversupplied.
        /// Used by DealerAI to choose optimal dealing locations.
        /// </summary>
        public float GetDemandSupplyRatio(string productId, string zoneId)
        {
            MarketData market = GetOrCreateMarket(productId, zoneId);
            float supply = Mathf.Max(1f, market.totalSoldThisCycle + 1f);
            float demand = Mathf.Max(1f, market.demandThisCycle);
            return demand / supply;
        }

        /// <summary>
        /// Is a market oversaturated? Returns true if supply > demand × 1.5.
        /// Used by DealerAI to avoid flooded zones.
        /// </summary>
        public bool IsMarketSaturated(string productId, string zoneId)
        {
            return GetDemandSupplyRatio(productId, zoneId) < 0.67f;
        }

        // ─── Market Tick ─────────────────────────────────────────────

        private void UpdateAllMarkets()
        {
            foreach (var key in new List<string>(_markets.Keys))
            {
                var market = _markets[key];
                float oldPrice = market.currentPrice;

                // Demand recovery — drifts toward base with variance
                float demandVariance = UnityEngine.Random.Range(-0.15f, 0.15f);
                market.demandThisCycle = Mathf.Max(5f, market.baseDemand * (1f + demandVariance));

                // Reset supply tracking for new cycle
                market.totalSoldThisCycle = 0;

                // Recalculate current price using full formula
                market.currentPrice = CalculatePrice(market.productId, market.zoneId, market.averageQuality);

                // Price inertia — don't swing too fast (damped movement)
                market.currentPrice = Mathf.Lerp(oldPrice, market.currentPrice, 0.3f);

                _markets[key] = market;

                if (Mathf.Abs(oldPrice - market.currentPrice) > 0.5f)
                    OnPriceChanged?.Invoke(market.productId, oldPrice, market.currentPrice);
            }
        }

        private MarketData GetOrCreateMarket(string productId, string zoneId)
        {
            string key = $"{productId}_{zoneId}";
            if (!_markets.TryGetValue(key, out var data))
            {
                data = new MarketData
                {
                    productId = productId,
                    zoneId = zoneId,
                    basePrice = 50f,
                    currentPrice = 50f,
                    baseDemand = 20f,
                    demandThisCycle = 20f,
                    elasticity = defaultElasticity,
                    averageQuality = 0.5f
                };
                _markets[key] = data;
            }
            return data;
        }
    }

    // ─── Market Data Struct ──────────────────────────────────────────

    /// <summary>
    /// Per-product per-zone market state.
    /// Spec v2.0 Section 23: tracks all variables in the price formula.
    /// </summary>
    [System.Serializable]
    public struct MarketData
    {
        public string productId;
        public string zoneId;

        // Price
        public float currentPrice;
        public float basePrice;

        // Demand/Supply
        public float baseDemand;
        public float demandThisCycle;
        public int totalSoldThisCycle;

        // Elasticity (per-product per-zone)
        public float elasticity;

        // Quality tracking
        public float averageQuality;

        // Historical
        public long lifetimeVolume;
    }
}
