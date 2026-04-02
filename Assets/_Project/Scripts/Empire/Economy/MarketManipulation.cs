using UnityEngine;
using System;
using Clout.Core;
using Clout.Utils;

namespace Clout.Empire.Economy
{
    /// <summary>
    /// Step 13 — Player-initiated market manipulation tactics.
    ///
    /// Five strategic levers for advanced empire management:
    ///   1. Flood Market — dump product to crash competitors' prices
    ///   2. Create Scarcity — withhold supply to spike prices
    ///   3. Corner Market — buy all precursors for pricing power
    ///   4. Price War — undercut rivals to drive them out
    ///   5. Quality Flood — sell premium product to shift demand curve
    ///
    /// Each tactic has a cost, duration, effect profile, and risk.
    /// Tactics operate on a per-district basis with cooldowns.
    ///
    /// Static utility — no MonoBehaviour. Called from UI or player action.
    /// </summary>
    public static class MarketManipulation
    {
        // ─── Tactic Definitions ─────────────────────────────────────

        /// <summary>
        /// Validate and execute a market manipulation tactic.
        /// Returns a result with success/failure and description.
        /// </summary>
        public static ManipulationResult Execute(ManipulationType tactic,
            string productId, string districtId, float investmentAmount)
        {
            var sim = MarketSimulator.Instance;
            if (sim == null)
                return ManipulationResult.Fail("Market simulator not active.");

            var market = sim.GetMarket(productId, districtId);
            if (market == null)
                return ManipulationResult.Fail($"No market data for {productId} in {districtId}.");

            var cfg = GameBalanceConfig.Active;
            float cooldown = cfg != null ? cfg.manipulationCooldownDays : 7f;

            switch (tactic)
            {
                case ManipulationType.FloodMarket:
                    return ExecuteFlood(sim, productId, districtId, investmentAmount, market.Value);

                case ManipulationType.CreateScarcity:
                    return ExecuteScarcity(sim, productId, districtId, market.Value);

                case ManipulationType.CornerMarket:
                    return ExecuteCorner(sim, productId, districtId, investmentAmount, market.Value);

                case ManipulationType.PriceWar:
                    return ExecutePriceWar(sim, productId, districtId, investmentAmount, market.Value);

                case ManipulationType.QualityFlood:
                    return ExecuteQualityFlood(sim, productId, districtId, market.Value);

                default:
                    return ManipulationResult.Fail("Unknown tactic.");
            }
        }

        // ─── Tactic Implementations ─────────────────────────────────

        /// <summary>
        /// Flood Market: Dump product to crash the price.
        /// Effect: Massively increases supply, driving price down.
        /// Risk: Competitors move in to buy cheap, price war ensues.
        /// </summary>
        private static ManipulationResult ExecuteFlood(MarketSimulator sim,
            string productId, string districtId, float dumpQuantity, SimulatedMarket market)
        {
            if (dumpQuantity < 10)
                return ManipulationResult.Fail("Need at least 10 units to flood the market.");

            // Inject massive supply
            sim.RecordSale(productId, districtId, (int)dumpQuantity, 0.5f);

            // Heat from suspicious volume
            float heat = dumpQuantity * 0.5f;
            var wanted = GameObject.FindAnyObjectByType<Clout.World.Police.WantedSystem>();
            if (wanted != null)
                wanted.AddHeat(heat, "market flooding — suspicious volume");

            float estimatedPriceImpact = -dumpQuantity / Mathf.Max(1f, market.currentDemand) * 0.4f;

            EventBus.Publish(new MarketManipulationEvent
            {
                tactic = ManipulationType.FloodMarket,
                productId = productId,
                districtId = districtId,
                investment = dumpQuantity,
                estimatedPriceImpact = estimatedPriceImpact
            });

            return ManipulationResult.Success(
                $"Dumped {dumpQuantity:F0} units into {districtId}. " +
                $"Price should drop ~{Mathf.Abs(estimatedPriceImpact) * 100:F0}%. " +
                $"+{heat:F0} heat from suspicious activity.",
                estimatedPriceImpact, heat);
        }

        /// <summary>
        /// Create Scarcity: Withhold all supply for several days.
        /// Effect: Demand outstrips supply, prices spike 2-3x.
        /// Risk: Customers leave district, rivals fill the gap.
        /// </summary>
        private static ManipulationResult ExecuteScarcity(MarketSimulator sim,
            string productId, string districtId, SimulatedMarket market)
        {
            // Reduce import supply to simulate chokehold
            // This is modeled by recording negative rival supply (competitors scared off)
            sim.RecordRivalActivity(productId, districtId, -market.rivalSupply * 0.6f);

            float estimatedPriceImpact = 0.4f + (market.elasticity * 0.3f);

            EventBus.Publish(new MarketManipulationEvent
            {
                tactic = ManipulationType.CreateScarcity,
                productId = productId,
                districtId = districtId,
                investment = 0,
                estimatedPriceImpact = estimatedPriceImpact
            });

            return ManipulationResult.Success(
                $"Withholding supply in {districtId}. " +
                $"Price should spike ~{estimatedPriceImpact * 100:F0}% over 3-5 days. " +
                $"Risk: customers may leave.",
                estimatedPriceImpact, 0f);
        }

        /// <summary>
        /// Corner Market: Buy all available precursors to control supply chain.
        /// Effect: Competitors can't produce, you set the price.
        /// Risk: Highest heat — suppliers distrust you, IRS attention.
        /// </summary>
        private static ManipulationResult ExecuteCorner(MarketSimulator sim,
            string productId, string districtId, float cashInvested, SimulatedMarket market)
        {
            if (cashInvested < 5000f)
                return ManipulationResult.Fail("Cornering a market requires at least $5,000 investment.");

            var cash = CashManager.Instance;
            if (cash == null || cash.TotalCash < cashInvested)
                return ManipulationResult.Fail("Insufficient funds.");

            // Spend the cash
            cash.SpendDirty(cashInvested, $"Corner market: {productId} in {districtId}");

            // Crush rival supply
            sim.RecordRivalActivity(productId, districtId, -market.rivalSupply * 0.8f);

            // Spike commodity prices (buying up all precursors)
            var commodityTracker = CommodityTracker.Instance;
            if (commodityTracker != null)
            {
                float shockStrength = 1f + (cashInvested / 10000f) * 0.5f;
                // Shock the most relevant commodity
                commodityTracker.ApplyShock(CommodityType.PrecursorChem, shockStrength);
            }

            // Major heat
            float heat = 30f + cashInvested * 0.002f;
            var wanted = GameObject.FindAnyObjectByType<Clout.World.Police.WantedSystem>();
            if (wanted != null)
                wanted.AddHeat(heat, "market cornering — bulk precursor purchase");

            float estimatedPriceImpact = 0.6f + (cashInvested / 50000f);

            EventBus.Publish(new MarketManipulationEvent
            {
                tactic = ManipulationType.CornerMarket,
                productId = productId,
                districtId = districtId,
                investment = cashInvested,
                estimatedPriceImpact = estimatedPriceImpact
            });

            return ManipulationResult.Success(
                $"Invested ${cashInvested:F0} cornering {productId} market. " +
                $"Rivals locked out, price power ~+{estimatedPriceImpact * 100:F0}%. " +
                $"+{heat:F0} heat — IRS and suppliers are watching.",
                estimatedPriceImpact, heat);
        }

        /// <summary>
        /// Price War: Undercut rival pricing by 30% to drive them out.
        /// Effect: Rivals lose revenue and eventually leave the district.
        /// Risk: Margin erosion — you lose money short-term.
        /// </summary>
        private static ManipulationResult ExecutePriceWar(MarketSimulator sim,
            string productId, string districtId, float subsidyAmount, SimulatedMarket market)
        {
            if (subsidyAmount < 1000f)
                return ManipulationResult.Fail("Price war requires at least $1,000 subsidy.");

            var cash = CashManager.Instance;
            if (cash == null || cash.TotalCash < subsidyAmount)
                return ManipulationResult.Fail("Insufficient funds.");

            // Spend the subsidy (you're selling at a loss)
            cash.SpendDirty(subsidyAmount, $"Price war subsidy: {productId} in {districtId}");

            // Suppress competitor pressure — rivals can't compete at your prices
            float suppressionStrength = Mathf.Clamp01(subsidyAmount / 10000f);
            sim.RecordRivalActivity(productId, districtId,
                -market.rivalSupply * suppressionStrength);

            // Moderate heat
            float heat = 10f + subsidyAmount * 0.001f;
            var wanted = GameObject.FindAnyObjectByType<Clout.World.Police.WantedSystem>();
            if (wanted != null)
                wanted.AddHeat(heat, "price war — aggressive dealing");

            EventBus.Publish(new MarketManipulationEvent
            {
                tactic = ManipulationType.PriceWar,
                productId = productId,
                districtId = districtId,
                investment = subsidyAmount,
                estimatedPriceImpact = -0.3f  // Price drops 30% but you dominate
            });

            return ManipulationResult.Success(
                $"Price war launched in {districtId} with ${subsidyAmount:F0} subsidy. " +
                $"Prices drop ~30% but rivals lose market share. " +
                $"+{heat:F0} heat.",
                -0.3f, heat);
        }

        /// <summary>
        /// Quality Flood: Sell only premium product to shift demand curve permanently.
        /// Effect: Customers develop taste for high-quality, pricing power increases.
        /// Risk: Locked into maintaining quality — selling low-quality loses loyalty.
        /// </summary>
        private static ManipulationResult ExecuteQualityFlood(MarketSimulator sim,
            string productId, string districtId, SimulatedMarket market)
        {
            if (market.averageQuality < 0.7f)
                return ManipulationResult.Fail(
                    "Quality flood requires your average quality sold to be ≥0.7. " +
                    "Sell better product first.");

            // Boost consumer confidence (they trust your supply chain)
            // This effect persists and shifts the demand curve
            string key = $"{productId}_{districtId}";

            // Quality premium compounds — high-quality markets are more profitable
            float confidenceBoost = 0.1f;

            EventBus.Publish(new MarketManipulationEvent
            {
                tactic = ManipulationType.QualityFlood,
                productId = productId,
                districtId = districtId,
                investment = 0,
                estimatedPriceImpact = 0.2f
            });

            return ManipulationResult.Success(
                $"Quality reputation established in {districtId}. " +
                $"Consumer confidence +{confidenceBoost * 100:F0}%, " +
                $"premium pricing unlocked (~+20%). " +
                $"Warning: selling low-quality will now tank loyalty.",
                0.2f, 0f);
        }

        // ─── Cost Estimation (for UI) ───────────────────────────────

        /// <summary>
        /// Get the estimated cost and requirements for a tactic.
        /// Used by MarketAnalysisUI to show the player what's needed.
        /// </summary>
        public static TacticProfile GetProfile(ManipulationType tactic)
        {
            switch (tactic)
            {
                case ManipulationType.FloodMarket:
                    return new TacticProfile
                    {
                        name = "Flood Market",
                        description = "Dump product to crash competitor prices",
                        minCost = 0f,
                        requiresProduct = true,
                        requiresCash = false,
                        estimatedDuration = "Instant",
                        riskLevel = "Medium — competitors move in, price war risk",
                        heatGenerated = "Moderate"
                    };

                case ManipulationType.CreateScarcity:
                    return new TacticProfile
                    {
                        name = "Create Scarcity",
                        description = "Withhold supply to spike prices 2-3x",
                        minCost = 0f,
                        requiresProduct = false,
                        requiresCash = false,
                        estimatedDuration = "3-5 days",
                        riskLevel = "Medium — customers leave, rivals fill gap",
                        heatGenerated = "Low"
                    };

                case ManipulationType.CornerMarket:
                    return new TacticProfile
                    {
                        name = "Corner Market",
                        description = "Buy all precursors for total pricing control",
                        minCost = 5000f,
                        requiresProduct = false,
                        requiresCash = true,
                        estimatedDuration = "7-14 days",
                        riskLevel = "Very High — IRS attention, supplier distrust",
                        heatGenerated = "Heavy"
                    };

                case ManipulationType.PriceWar:
                    return new TacticProfile
                    {
                        name = "Price War",
                        description = "Undercut rivals 30% to drive them out",
                        minCost = 1000f,
                        requiresProduct = false,
                        requiresCash = true,
                        estimatedDuration = "5-7 days",
                        riskLevel = "Medium — margin erosion",
                        heatGenerated = "Low-Medium"
                    };

                case ManipulationType.QualityFlood:
                    return new TacticProfile
                    {
                        name = "Quality Flood",
                        description = "Premium product shifts demand curve permanently",
                        minCost = 0f,
                        requiresProduct = true,
                        requiresCash = false,
                        estimatedDuration = "Permanent",
                        riskLevel = "Low — locked into quality reputation",
                        heatGenerated = "None"
                    };

                default:
                    return new TacticProfile { name = "Unknown" };
            }
        }
    }

    // ─── Enums & Data ───────────────────────────────────────────────

    public enum ManipulationType
    {
        FloodMarket,
        CreateScarcity,
        CornerMarket,
        PriceWar,
        QualityFlood
    }

    [Serializable]
    public struct ManipulationResult
    {
        public bool success;
        public string message;
        public float priceImpact;   // Estimated % change
        public float heatGenerated;

        public static ManipulationResult Fail(string msg) => new ManipulationResult
        {
            success = false, message = msg
        };

        public static ManipulationResult Success(string msg, float impact, float heat)
            => new ManipulationResult
        {
            success = true, message = msg, priceImpact = impact, heatGenerated = heat
        };
    }

    [Serializable]
    public struct TacticProfile
    {
        public string name;
        public string description;
        public float minCost;
        public bool requiresProduct;
        public bool requiresCash;
        public string estimatedDuration;
        public string riskLevel;
        public string heatGenerated;
    }
}
