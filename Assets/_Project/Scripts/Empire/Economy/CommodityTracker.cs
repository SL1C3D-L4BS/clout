using UnityEngine;
using System;
using System.Collections.Generic;
using Clout.Core;
using Clout.Utils;

namespace Clout.Empire.Economy
{
    /// <summary>
    /// Step 13 — Precursor commodity price simulation via geometric Brownian motion
    /// with mean reversion (Ornstein–Uhlenbeck process).
    ///
    /// Commodity prices affect production costs. When Acetone spikes, cooking meth
    /// costs more. When Coca Leaf crashes, cocaine margins improve. Players who
    /// stockpile cheap precursors gain advantage; players caught in a price spike
    /// face margin compression.
    ///
    /// Model:
    ///   dP = θ(μ - P)dt + σP·dW
    ///
    /// Where:
    ///   θ  = mean-reversion speed (how fast price returns to base)
    ///   μ  = long-run mean (base price)
    ///   σ  = volatility
    ///   dW = Wiener process increment (normal random)
    ///
    /// Price is clamped to [0.3×base, 4.0×base] to prevent degenerate states.
    ///
    /// Daily tick via TransactionLedger.OnDayEnd.
    /// </summary>
    public class CommodityTracker : MonoBehaviour
    {
        public static CommodityTracker Instance { get; private set; }

        // ─── Configuration ──────────────────────────────────────────

        [Header("Simulation")]
        [Tooltip("Mean-reversion speed. Higher = faster return to base price.")]
        [Range(0.01f, 0.2f)]
        public float meanReversionSpeed = 0.05f;

        [Tooltip("Price floor as ratio of base price.")]
        public float priceFloorRatio = 0.3f;

        [Tooltip("Price ceiling as ratio of base price.")]
        public float priceCeilingRatio = 4.0f;

        [Header("Price History")]
        [Tooltip("Rolling window of daily prices to keep per commodity.")]
        public int priceHistoryWindow = 90;

        // ─── State ──────────────────────────────────────────────────

        private Dictionary<CommodityType, CommodityState> _commodities
            = new Dictionary<CommodityType, CommodityState>();

        private System.Random _rng;

        // ─── Properties ─────────────────────────────────────────────

        public IReadOnlyDictionary<CommodityType, CommodityState> Commodities => _commodities;

        // ─── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _rng = new System.Random(Environment.TickCount);

            InitializeCommodities();
        }

        private void OnEnable()
        {
            var ledger = TransactionLedger.Instance;
            if (ledger != null) ledger.OnDayEnd += SimulateDay;
        }

        private void OnDisable()
        {
            var ledger = TransactionLedger.Instance;
            if (ledger != null) ledger.OnDayEnd -= SimulateDay;
        }

        private void Start()
        {
            // Late-bind if ledger wasn't ready in OnEnable
            if (TransactionLedger.Instance != null)
            {
                TransactionLedger.Instance.OnDayEnd -= SimulateDay;
                TransactionLedger.Instance.OnDayEnd += SimulateDay;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─── Initialization ─────────────────────────────────────────

        private void InitializeCommodities()
        {
            RegisterCommodity(CommodityType.Pseudoephedrine, "Pseudoephedrine", 50f, 0.05f);
            RegisterCommodity(CommodityType.Methylamine,     "Methylamine",     200f, 0.08f);
            RegisterCommodity(CommodityType.CocaLeaf,        "Coca Leaf",       30f,  0.12f);
            RegisterCommodity(CommodityType.Acetone,         "Acetone",         15f,  0.03f);
            RegisterCommodity(CommodityType.PrecursorChem,   "Precursor Chem",  80f,  0.06f);
            RegisterCommodity(CommodityType.CuttingAgent,    "Cutting Agent",   10f,  0.02f);
        }

        private void RegisterCommodity(CommodityType type, string name, float basePrice, float volatility)
        {
            var state = new CommodityState
            {
                type = type,
                displayName = name,
                basePrice = basePrice,
                currentPrice = basePrice,
                volatility = volatility,
                priceHistory = new List<float>(priceHistoryWindow) { basePrice },
                dailyChange = 0f,
                daysSinceShock = 0,
                shockMultiplier = 1f
            };
            _commodities[type] = state;
        }

        // ─── Daily Simulation ───────────────────────────────────────

        /// <summary>
        /// Advance all commodity prices by one game day using Ornstein–Uhlenbeck process.
        /// Called by TransactionLedger.OnDayEnd.
        /// </summary>
        public void SimulateDay()
        {
            var cfg = GameBalanceConfig.Active;
            float theta = cfg != null ? cfg.commodityMeanReversion : meanReversionSpeed;

            foreach (CommodityType type in Enum.GetValues(typeof(CommodityType)))
            {
                if (!_commodities.TryGetValue(type, out var state)) continue;

                float oldPrice = state.currentPrice;

                // Ornstein–Uhlenbeck step
                float meanReversion = theta * (state.basePrice - state.currentPrice);
                float randomShock = (float)NextGaussian() * state.volatility * state.currentPrice;

                // Apply external shock multiplier (from market events)
                float shockDecay = state.shockMultiplier > 1f ? -0.1f : (state.shockMultiplier < 1f ? 0.1f : 0f);
                state.shockMultiplier = Mathf.MoveTowards(state.shockMultiplier, 1f, Mathf.Abs(shockDecay));

                float newPrice = state.currentPrice + meanReversion + randomShock;
                newPrice *= state.shockMultiplier;

                // Clamp to bounds
                float floor = state.basePrice * priceFloorRatio;
                float ceiling = state.basePrice * priceCeilingRatio;
                newPrice = Mathf.Clamp(newPrice, floor, ceiling);

                state.dailyChange = newPrice - oldPrice;
                state.currentPrice = newPrice;
                state.daysSinceShock++;

                // Price history (rolling window)
                state.priceHistory.Add(newPrice);
                if (state.priceHistory.Count > priceHistoryWindow)
                    state.priceHistory.RemoveAt(0);

                _commodities[type] = state;

                // Publish significant moves (>5% daily change)
                float pctChange = Mathf.Abs(state.dailyChange) / oldPrice;
                if (pctChange > 0.05f)
                {
                    EventBus.Publish(new CommodityPriceShockEvent
                    {
                        commodity = type,
                        oldPrice = oldPrice,
                        newPrice = state.currentPrice,
                        percentChange = pctChange * Mathf.Sign(state.dailyChange)
                    });
                }
            }
        }

        // ─── External Shocks ────────────────────────────────────────

        /// <summary>
        /// Apply an external price shock to a commodity (from market events).
        /// Multiplier decays back to 1.0 over time.
        /// </summary>
        public void ApplyShock(CommodityType type, float multiplier)
        {
            if (!_commodities.TryGetValue(type, out var state)) return;
            state.shockMultiplier *= multiplier;
            state.daysSinceShock = 0;
            _commodities[type] = state;

            Debug.Log($"[Commodity] {state.displayName} shock: {multiplier:F2}x " +
                $"(now ${state.currentPrice:F0})");
        }

        // ─── Queries ────────────────────────────────────────────────

        /// <summary>Get current price for a commodity type.</summary>
        public float GetPrice(CommodityType type)
        {
            return _commodities.TryGetValue(type, out var state) ? state.currentPrice : 0f;
        }

        /// <summary>Get the cost modifier for a recipe based on its ingredient commodity types.</summary>
        public float GetProductionCostMultiplier(CommodityType primaryCommodity)
        {
            if (!_commodities.TryGetValue(primaryCommodity, out var state)) return 1f;
            return state.currentPrice / Mathf.Max(1f, state.basePrice);
        }

        /// <summary>Get price trend over last N days. Positive = rising, negative = falling.</summary>
        public float GetTrend(CommodityType type, int lookbackDays = 7)
        {
            if (!_commodities.TryGetValue(type, out var state)) return 0f;
            var history = state.priceHistory;
            if (history.Count < 2) return 0f;

            int start = Mathf.Max(0, history.Count - lookbackDays);
            float startPrice = history[start];
            float endPrice = history[history.Count - 1];

            return startPrice > 0 ? (endPrice - startPrice) / startPrice : 0f;
        }

        /// <summary>Get the 7-day moving average price.</summary>
        public float GetMovingAverage(CommodityType type, int window = 7)
        {
            if (!_commodities.TryGetValue(type, out var state)) return 0f;
            var history = state.priceHistory;
            if (history.Count == 0) return 0f;

            int start = Mathf.Max(0, history.Count - window);
            float sum = 0f;
            int count = 0;
            for (int i = start; i < history.Count; i++)
            {
                sum += history[i];
                count++;
            }
            return count > 0 ? sum / count : 0f;
        }

        /// <summary>Get full price history for chart rendering.</summary>
        public IReadOnlyList<float> GetPriceHistory(CommodityType type)
        {
            return _commodities.TryGetValue(type, out var state) ? state.priceHistory : null;
        }

        // ─── Gaussian Random ────────────────────────────────────────

        /// <summary>Box-Muller transform for normally distributed random values.</summary>
        private double NextGaussian()
        {
            double u1 = 1.0 - _rng.NextDouble();
            double u2 = 1.0 - _rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        // ─── Save / Load ───────────────────────────────────────────

        [Serializable]
        public struct CommoditySaveData
        {
            public CommodityType type;
            public float currentPrice;
            public float shockMultiplier;
            public int daysSinceShock;
            public float[] recentHistory;  // Last 30 days for save size
        }

        public CommoditySaveData[] GetSaveData()
        {
            var data = new CommoditySaveData[_commodities.Count];
            int i = 0;
            foreach (var kvp in _commodities)
            {
                var state = kvp.Value;
                int histLen = Mathf.Min(30, state.priceHistory.Count);
                float[] hist = new float[histLen];
                for (int j = 0; j < histLen; j++)
                    hist[j] = state.priceHistory[state.priceHistory.Count - histLen + j];

                data[i++] = new CommoditySaveData
                {
                    type = state.type,
                    currentPrice = state.currentPrice,
                    shockMultiplier = state.shockMultiplier,
                    daysSinceShock = state.daysSinceShock,
                    recentHistory = hist
                };
            }
            return data;
        }

        public void LoadSaveData(CommoditySaveData[] data)
        {
            if (data == null) return;
            foreach (var save in data)
            {
                if (!_commodities.TryGetValue(save.type, out var state)) continue;
                state.currentPrice = save.currentPrice;
                state.shockMultiplier = save.shockMultiplier;
                state.daysSinceShock = save.daysSinceShock;
                if (save.recentHistory != null)
                {
                    state.priceHistory.Clear();
                    state.priceHistory.AddRange(save.recentHistory);
                }
                _commodities[save.type] = state;
            }
        }
    }

    // ─── Commodity Types ────────────────────────────────────────────

    public enum CommodityType
    {
        Pseudoephedrine,    // Methamphetamine precursor
        Methylamine,        // Premium meth precursor
        CocaLeaf,           // Cocaine precursor
        Acetone,            // Multi-use solvent
        PrecursorChem,      // Generic synthetic precursor
        CuttingAgent        // Universal cutting/bulking agent
    }

    // ─── Commodity State ────────────────────────────────────────────

    [Serializable]
    public struct CommodityState
    {
        public CommodityType type;
        public string displayName;
        public float basePrice;
        public float currentPrice;
        public float volatility;
        public float dailyChange;
        public float shockMultiplier;
        public int daysSinceShock;
        public List<float> priceHistory;
    }
}
