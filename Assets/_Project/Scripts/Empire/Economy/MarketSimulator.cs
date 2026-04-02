using UnityEngine;
using System;
using System.Collections.Generic;
using Clout.Core;
using Clout.Utils;

namespace Clout.Empire.Economy
{
    /// <summary>
    /// Step 13 — Advanced market simulation engine.
    ///
    /// Wraps and extends EconomyManager with full supply/demand curves,
    /// market events, competition modeling, price history, and manipulation.
    ///
    /// Upgraded price formula:
    ///   P = P_base × (D/S)^(1/|E|) × (1 + R_heat) × M_season × Q_mod × C_comp × EVT_mod
    ///
    /// Where:
    ///   D/S    = demand-to-supply ratio (player + rival + import volume)
    ///   E      = price elasticity (-0.1 to -2.0, configurable per product/district)
    ///   R_heat = district heat → risk premium
    ///   M_s    = seasonal/calendar modifier
    ///   Q_mod  = quality premium (exponential)
    ///   C_comp = competitor pressure (1.0=monopoly, 0.6=heavy competition)
    ///   EVT    = product of all active market event modifiers
    ///
    /// Daily tick via TransactionLedger.OnDayEnd. Real-time price queries
    /// are cached and refreshed each tick to avoid per-frame computation.
    ///
    /// Singleton — attached to the same GameObject as EconomyManager.
    /// </summary>
    public class MarketSimulator : MonoBehaviour
    {
        public static MarketSimulator Instance { get; private set; }

        // ─── Configuration ──────────────────────────────────────────

        [Header("Market Events")]
        [Tooltip("All possible market event definitions.")]
        public List<MarketEvent> eventDefinitions = new List<MarketEvent>();

        [Header("Competition")]
        [Tooltip("Base rival supply contribution per contested district (units/day).")]
        public float rivalBaseSupply = 10f;

        [Tooltip("Competition price suppression floor (0.4 = competitors can suppress price to 40%).")]
        [Range(0.3f, 1f)]
        public float competitionFloor = 0.6f;

        [Header("Price History")]
        [Tooltip("Days of price history to retain per market.")]
        public int historyWindowDays = 90;

        // ─── State ──────────────────────────────────────────────────

        // Per-product per-district extended market state
        private Dictionary<string, SimulatedMarket> _markets = new Dictionary<string, SimulatedMarket>();

        // Active market events
        private List<ActiveMarketEvent> _activeEvents = new List<ActiveMarketEvent>();

        // Event cooldowns: eventName → dayLastEnded
        private Dictionary<string, int> _eventCooldowns = new Dictionary<string, int>();

        // Current game day (synced from TransactionLedger)
        private int _currentDay;

        // Cached reference
        private EconomyManager _economy;

        // ─── Properties ─────────────────────────────────────────────

        public IReadOnlyList<ActiveMarketEvent> ActiveEvents => _activeEvents;
        public int CurrentDay => _currentDay;

        // ─── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
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
            _economy = FindAnyObjectByType<EconomyManager>();

            // Late-bind
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

        // ─── Market Initialization ──────────────────────────────────

        /// <summary>
        /// Initialize a simulated market for a product in a district.
        /// Called by DistrictManager during district setup.
        /// </summary>
        public void InitializeDistrictMarket(string productId, string districtId,
            float basePrice, float baseDemand, float elasticity, float rivalPresence = 0f)
        {
            string key = MarketKey(productId, districtId);
            _markets[key] = new SimulatedMarket
            {
                productId = productId,
                districtId = districtId,
                basePrice = basePrice,
                currentPrice = basePrice,
                baseDemand = baseDemand,
                currentDemand = baseDemand,
                playerSupply = 0f,
                rivalSupply = rivalPresence * rivalBaseSupply,
                importSupply = baseDemand * 0.3f,   // NPCs fill 30% of demand by default
                elasticity = Mathf.Clamp(elasticity, 0.05f, 2f),
                competitorPressure = 1f - rivalPresence * 0.4f,
                consumerConfidence = 1f,
                priceHistory = new List<float>(historyWindowDays) { basePrice },
                dailySalesVolume = 0,
                totalLifetimeVolume = 0
            };
        }

        // ─── Core Price Calculation ─────────────────────────────────

        /// <summary>
        /// Full multi-layer price calculation with all Step 13 modifiers.
        /// This is the authoritative price source — EconomyManager.CalculatePrice
        /// delegates here when MarketSimulator is present.
        /// </summary>
        public float GetPrice(string productId, string districtId, float quality = 0.5f)
        {
            string key = MarketKey(productId, districtId);
            if (!_markets.TryGetValue(key, out var market))
            {
                // Fallback to EconomyManager for markets we don't track
                return _economy != null ? _economy.CalculatePrice(productId, districtId, quality) : 50f;
            }

            var cfg = GameBalanceConfig.Active;

            // 1. Base price
            float price = market.basePrice;

            // 2. Demand/Supply ratio with elasticity exponent
            float totalSupply = Mathf.Max(1f, market.playerSupply + market.rivalSupply + market.importSupply);
            float demand = Mathf.Max(1f, market.currentDemand);
            float dsRatio = Mathf.Clamp(demand / totalSupply,
                cfg.minDemandSupplyRatio, cfg.maxDemandSupplyRatio);

            // Elasticity: inelastic products (0.1) barely move, elastic (2.0) swing hard
            float elasticityExponent = 1f / Mathf.Max(0.05f, market.elasticity);
            price *= Mathf.Pow(dsRatio, elasticityExponent);

            // 3. Heat risk premium
            float heat = 0f;
            var wanted = FindAnyObjectByType<World.Police.WantedSystem>();
            if (wanted != null && wanted.maxHeat > 0)
                heat = wanted.CurrentHeat / wanted.maxHeat;
            float riskPremium = 1f + heat * (cfg != null ? cfg.maxRiskPremium : 0.5f);
            price *= riskPremium;

            // 4. Seasonal multiplier (from EconomyManager)
            float seasonal = _economy != null ? _economy.seasonalMultiplier : 1f;
            price *= seasonal;

            // 5. Quality premium (exponential)
            float qualExp = cfg != null ? cfg.qualityExponent : 1.5f;
            float qualityMod = Mathf.Pow(0.5f + quality, qualExp);
            price *= qualityMod;

            // 6. Competition pressure (lower = more competitors pushing price down)
            price *= Mathf.Max(competitionFloor, market.competitorPressure);

            // 7. Consumer confidence
            price *= market.consumerConfidence;

            // 8. Market event modifiers (multiplicative stack)
            float eventMod = GetEventModifier(productId, districtId);
            price *= eventMod;

            // Floor
            return Mathf.Max(1f, price);
        }

        // ─── Recording ──────────────────────────────────────────────

        /// <summary>
        /// Record a player sale — feeds into supply curve and shifts prices.
        /// Called by DealManager.ExecuteDeal().
        /// </summary>
        public void RecordSale(string productId, string districtId, int quantity, float quality)
        {
            string key = MarketKey(productId, districtId);
            if (!_markets.TryGetValue(key, out var market))
            {
                // Auto-create market entry
                InitializeDistrictMarket(productId, districtId, 50f, 20f, 0.3f);
                market = _markets[key];
            }

            market.playerSupply += quantity;
            market.dailySalesVolume += quantity;
            market.totalLifetimeVolume += quantity;

            // Running average quality
            market.averageQuality = Mathf.Lerp(market.averageQuality, quality, 0.1f);

            _markets[key] = market;

            // Also forward to legacy EconomyManager
            if (_economy != null)
                _economy.RecordSale(productId, districtId, quantity, quality);
        }

        /// <summary>
        /// Record rival faction activity in a district.
        /// Increases rival supply, which suppresses price.
        /// </summary>
        public void RecordRivalActivity(string productId, string districtId, float supplyUnits)
        {
            string key = MarketKey(productId, districtId);
            if (!_markets.TryGetValue(key, out var market)) return;

            market.rivalSupply += supplyUnits;
            market.competitorPressure = Mathf.Max(competitionFloor,
                1f - (market.rivalSupply / Mathf.Max(1f, market.currentDemand)) * 0.4f);

            _markets[key] = market;
        }

        // ─── Daily Simulation ───────────────────────────────────────

        /// <summary>
        /// Full daily market simulation tick. Called by TransactionLedger.OnDayEnd.
        /// </summary>
        public void SimulateDay()
        {
            _currentDay++;
            var cfg = GameBalanceConfig.Active;

            // 1. Update each market's supply/demand curves
            foreach (var key in new List<string>(_markets.Keys))
            {
                var market = _markets[key];
                float oldPrice = market.currentPrice;

                // Demand recovery: drift toward base with noise
                float demandVariance = UnityEngine.Random.Range(-0.1f, 0.1f);
                market.currentDemand = Mathf.Max(5f,
                    Mathf.Lerp(market.currentDemand, market.baseDemand * (1f + demandVariance), 0.2f));

                // Supply decay: yesterday's supply doesn't persist fully
                float supplyDecay = cfg != null ? cfg.marketSupplyDecayRate : 0.3f;
                market.playerSupply *= (1f - supplyDecay);
                market.rivalSupply *= (1f - supplyDecay * 0.5f);  // Rivals decay slower

                // Import supply: NPCs refill baseline supply
                float importRecovery = cfg != null ? cfg.marketImportRecoveryRate : 0.1f;
                market.importSupply = Mathf.Lerp(market.importSupply,
                    market.baseDemand * 0.3f, importRecovery);

                // Competition pressure recovery (rivals slowly re-enter cleared markets)
                market.competitorPressure = Mathf.Lerp(market.competitorPressure, 0.85f, 0.05f);

                // Consumer confidence: affected by heat, events, quality reputation
                float heatEffect = 0f;
                var wanted = FindAnyObjectByType<World.Police.WantedSystem>();
                if (wanted != null && wanted.maxHeat > 0)
                    heatEffect = wanted.CurrentHeat / wanted.maxHeat;
                market.consumerConfidence = Mathf.Lerp(
                    market.consumerConfidence,
                    1f - heatEffect * 0.3f,  // High heat scares buyers
                    0.1f);

                // Recalculate price
                market.currentPrice = GetPrice(market.productId, market.districtId, market.averageQuality);

                // Price history
                market.priceHistory.Add(market.currentPrice);
                if (market.priceHistory.Count > historyWindowDays)
                    market.priceHistory.RemoveAt(0);

                // Reset daily sales counter
                market.dailySalesVolume = 0;

                _markets[key] = market;

                // Publish significant price changes (>10%)
                float pctChange = oldPrice > 0 ? Mathf.Abs(market.currentPrice - oldPrice) / oldPrice : 0f;
                if (pctChange > 0.1f)
                {
                    EventBus.Publish(new MarketPriceChangedEvent
                    {
                        productId = market.productId,
                        districtId = market.districtId,
                        oldPrice = oldPrice,
                        newPrice = market.currentPrice,
                        percentChange = (market.currentPrice - oldPrice) / oldPrice
                    });
                }
            }

            // 2. Process market events
            ProcessMarketEvents();

            // 3. Roll for new events
            RollForNewEvents();
        }

        // ─── Market Events ──────────────────────────────────────────

        private void ProcessMarketEvents()
        {
            for (int i = _activeEvents.Count - 1; i >= 0; i--)
            {
                var evt = _activeEvents[i];
                evt.TickDay();

                if (evt.IsExpired)
                {
                    // Record cooldown
                    _eventCooldowns[evt.definition.eventName] = _currentDay;

                    EventBus.Publish(new MarketEventEndedEvent
                    {
                        eventName = evt.definition.eventName,
                        eventType = evt.definition.eventType,
                        durationDays = evt.definition.durationDays
                    });

                    Debug.Log($"[Market] Event ended: {evt.definition.eventName}");
                    _activeEvents.RemoveAt(i);
                }
            }
        }

        private void RollForNewEvents()
        {
            if (eventDefinitions == null) return;

            var cfg = GameBalanceConfig.Active;
            float globalEventChance = cfg != null ? cfg.marketEventGlobalModifier : 1f;

            foreach (var def in eventDefinitions)
            {
                if (def == null) continue;
                if (_currentDay < def.minimumDay) continue;
                if (def.dailyProbability <= 0f) continue;

                // Check cooldown
                if (_eventCooldowns.TryGetValue(def.eventName, out int lastEnded))
                {
                    if (_currentDay - lastEnded < def.cooldownDays) continue;
                }

                // Check if already active
                bool alreadyActive = false;
                foreach (var active in _activeEvents)
                {
                    if (active.definition == def) { alreadyActive = true; break; }
                }
                if (alreadyActive) continue;

                // Roll
                float chance = def.dailyProbability * globalEventChance;
                if (UnityEngine.Random.value < chance)
                {
                    TriggerEvent(def);
                }
            }
        }

        /// <summary>
        /// Manually trigger a market event (from game logic or manipulation).
        /// </summary>
        public void TriggerEvent(MarketEvent definition)
        {
            var active = new ActiveMarketEvent
            {
                definition = definition,
                startDay = _currentDay,
                remainingDays = definition.durationDays
            };
            _activeEvents.Add(active);

            // Apply commodity shocks for SupplyRouteCut events
            if (definition.eventType == MarketEventType.SupplyRouteCut)
            {
                var commodityTracker = CommodityTracker.Instance;
                if (commodityTracker != null)
                {
                    // Cut random commodity supply
                    var types = Enum.GetValues(typeof(CommodityType));
                    var targetCommodity = (CommodityType)types.GetValue(
                        UnityEngine.Random.Range(0, types.Length));
                    commodityTracker.ApplyShock(targetCommodity, 1.8f);
                }
            }

            EventBus.Publish(new MarketEventTriggeredEvent
            {
                eventName = definition.eventName,
                eventType = definition.eventType,
                durationDays = definition.durationDays,
                priceMultiplier = definition.priceMultiplier,
                demandMultiplier = definition.demandMultiplier
            });

            Debug.Log($"[Market] Event triggered: {definition.eventName} " +
                $"(price×{definition.priceMultiplier:F1}, demand×{definition.demandMultiplier:F1}, " +
                $"{definition.durationDays} days)");
        }

        /// <summary>
        /// Get the combined event modifier for a product in a district.
        /// Multiplicative stacking of all active events that affect this market.
        /// </summary>
        public float GetEventModifier(string productId, string districtId)
        {
            float modifier = 1f;
            foreach (var evt in _activeEvents)
            {
                // Check scope
                ProductType productType;
                bool validProduct = Enum.TryParse(productId, true, out productType);

                bool affectsProduct = !validProduct || evt.definition.AffectsProduct(productType);
                bool affectsDistrict = evt.definition.AffectsDistrict(districtId);

                if (affectsProduct && affectsDistrict)
                {
                    modifier *= evt.EffectivePriceMultiplier;
                }
            }
            return modifier;
        }

        /// <summary>
        /// Get combined demand modifier from active events.
        /// </summary>
        public float GetEventDemandModifier(string productId, string districtId)
        {
            float modifier = 1f;
            foreach (var evt in _activeEvents)
            {
                ProductType productType;
                bool validProduct = Enum.TryParse(productId, true, out productType);
                bool affectsProduct = !validProduct || evt.definition.AffectsProduct(productType);
                bool affectsDistrict = evt.definition.AffectsDistrict(districtId);

                if (affectsProduct && affectsDistrict)
                    modifier *= evt.EffectiveDemandMultiplier;
            }
            return modifier;
        }

        // ─── Queries ────────────────────────────────────────────────

        /// <summary>Get extended market data for UI/AI.</summary>
        public SimulatedMarket? GetMarket(string productId, string districtId)
        {
            string key = MarketKey(productId, districtId);
            return _markets.TryGetValue(key, out var m) ? m : (SimulatedMarket?)null;
        }

        /// <summary>Get all tracked simulated markets.</summary>
        public IReadOnlyDictionary<string, SimulatedMarket> AllMarkets => _markets;

        /// <summary>Get price trend over N days. Positive = bullish.</summary>
        public float GetPriceTrend(string productId, string districtId, int lookbackDays = 7)
        {
            string key = MarketKey(productId, districtId);
            if (!_markets.TryGetValue(key, out var market)) return 0f;
            var history = market.priceHistory;
            if (history.Count < 2) return 0f;

            int start = Mathf.Max(0, history.Count - lookbackDays);
            float startPrice = history[start];
            float endPrice = history[history.Count - 1];

            return startPrice > 0 ? (endPrice - startPrice) / startPrice : 0f;
        }

        /// <summary>Get price history for chart rendering.</summary>
        public IReadOnlyList<float> GetPriceHistory(string productId, string districtId)
        {
            string key = MarketKey(productId, districtId);
            return _markets.TryGetValue(key, out var m) ? m.priceHistory : null;
        }

        /// <summary>Is the market oversaturated? True = supply exceeds demand.</summary>
        public bool IsOversaturated(string productId, string districtId)
        {
            string key = MarketKey(productId, districtId);
            if (!_markets.TryGetValue(key, out var market)) return false;
            float totalSupply = market.playerSupply + market.rivalSupply + market.importSupply;
            return totalSupply > market.currentDemand * 1.5f;
        }

        // ─── Utility ────────────────────────────────────────────────

        private static string MarketKey(string productId, string districtId)
            => $"{productId}_{districtId}";

        // ─── Save / Load ────────────────────────────────────────────

        [Serializable]
        public struct MarketSaveData
        {
            public string key;
            public float currentPrice;
            public float currentDemand;
            public float playerSupply;
            public float rivalSupply;
            public float competitorPressure;
            public float consumerConfidence;
            public float averageQuality;
            public long totalLifetimeVolume;
            public float[] recentHistory;
        }

        [Serializable]
        public struct EventSaveData
        {
            public string eventName;
            public int remainingDays;
            public int startDay;
        }

        public (MarketSaveData[], EventSaveData[]) GetSaveData()
        {
            var markets = new MarketSaveData[_markets.Count];
            int i = 0;
            foreach (var kvp in _markets)
            {
                var m = kvp.Value;
                int histLen = Mathf.Min(30, m.priceHistory.Count);
                float[] hist = new float[histLen];
                for (int j = 0; j < histLen; j++)
                    hist[j] = m.priceHistory[m.priceHistory.Count - histLen + j];

                markets[i++] = new MarketSaveData
                {
                    key = kvp.Key,
                    currentPrice = m.currentPrice,
                    currentDemand = m.currentDemand,
                    playerSupply = m.playerSupply,
                    rivalSupply = m.rivalSupply,
                    competitorPressure = m.competitorPressure,
                    consumerConfidence = m.consumerConfidence,
                    averageQuality = m.averageQuality,
                    totalLifetimeVolume = m.totalLifetimeVolume,
                    recentHistory = hist
                };
            }

            var events = new EventSaveData[_activeEvents.Count];
            for (int e = 0; e < _activeEvents.Count; e++)
            {
                events[e] = new EventSaveData
                {
                    eventName = _activeEvents[e].definition.eventName,
                    remainingDays = _activeEvents[e].remainingDays,
                    startDay = _activeEvents[e].startDay
                };
            }

            return (markets, events);
        }

        public void LoadSaveData(MarketSaveData[] marketData, EventSaveData[] eventData)
        {
            if (marketData != null)
            {
                foreach (var save in marketData)
                {
                    if (!_markets.TryGetValue(save.key, out var market)) continue;
                    market.currentPrice = save.currentPrice;
                    market.currentDemand = save.currentDemand;
                    market.playerSupply = save.playerSupply;
                    market.rivalSupply = save.rivalSupply;
                    market.competitorPressure = save.competitorPressure;
                    market.consumerConfidence = save.consumerConfidence;
                    market.averageQuality = save.averageQuality;
                    market.totalLifetimeVolume = save.totalLifetimeVolume;
                    if (save.recentHistory != null)
                    {
                        market.priceHistory.Clear();
                        market.priceHistory.AddRange(save.recentHistory);
                    }
                    _markets[save.key] = market;
                }
            }

            if (eventData != null)
            {
                foreach (var save in eventData)
                {
                    foreach (var def in eventDefinitions)
                    {
                        if (def != null && def.eventName == save.eventName)
                        {
                            _activeEvents.Add(new ActiveMarketEvent
                            {
                                definition = def,
                                startDay = save.startDay,
                                remainingDays = save.remainingDays
                            });
                            break;
                        }
                    }
                }
            }
        }
    }

    // ─── Simulated Market Struct ────────────────────────────────────

    /// <summary>
    /// Extended per-product per-district market state with full simulation data.
    /// Supersedes EconomyManager.MarketData for Step 13+ systems.
    /// </summary>
    [Serializable]
    public struct SimulatedMarket
    {
        public string productId;
        public string districtId;

        // Price
        public float basePrice;
        public float currentPrice;

        // Demand
        public float baseDemand;
        public float currentDemand;

        // Supply breakdown
        public float playerSupply;      // Player's contribution this cycle
        public float rivalSupply;       // Rival faction supply
        public float importSupply;      // NPC/import baseline supply

        // Market characteristics
        public float elasticity;        // 0.05 (inelastic) to 2.0 (elastic)
        public float competitorPressure; // 1.0 = monopoly, 0.6 = heavy competition
        public float consumerConfidence; // 0-1, affected by heat and events
        public float averageQuality;    // Running average quality sold

        // Volume tracking
        public int dailySalesVolume;
        public long totalLifetimeVolume;

        // Price history (rolling window)
        public List<float> priceHistory;
    }
}
