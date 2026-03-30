using UnityEngine;
using Clout.Core;
using Clout.Empire.Crafting;

namespace Clout.World.NPCs
{
    /// <summary>
    /// NPC customer — roams the city, has preferences, buys product from dealers.
    ///
    /// Customer behavior:
    /// 1. Wanders their territory zone
    /// 2. Periodically needs product (addiction timer)
    /// 3. Seeks out the nearest available dealer
    /// 4. Evaluates price vs quality vs loyalty
    /// 5. Makes purchase or walks away
    /// 6. Repeat with increasing addiction/loyalty
    ///
    /// Customers generate the demand that drives the entire economy.
    /// Their preferences and addiction create emergent market dynamics.
    /// </summary>
    public class CustomerAI : MonoBehaviour
    {
        [Header("Customer Profile")]
        public string customerName;
        public ProductType preferredProduct;
        [Range(0f, 1f)] public float qualityPreference = 0.5f;   // How much they care about quality
        [Range(0f, 1f)] public float pricesSensitivity = 0.5f;    // How much they care about price
        public float maxWillingToPay = 80f;

        [Header("Addiction")]
        [Range(0f, 1f)] public float addictionLevel = 0f;
        public float purchaseInterval = 300f;    // Seconds between purchases
        public float addictionGrowthRate = 0.01f; // Per purchase

        [Header("Loyalty")]
        public int preferredDealerId = -1;        // Player ID they prefer
        [Range(0f, 1f)] public float loyalty = 0f;
        public float loyaltyGrowthRate = 0.05f;   // Per successful transaction

        [Header("State")]
        public CustomerState currentState = CustomerState.Wandering;
        public float purchaseTimer;

        // Runtime
        private float _satisfactionDecay;
        private int _totalPurchases;
        private float _lastQualityReceived;

        private void Update()
        {
            purchaseTimer -= Time.deltaTime;

            switch (currentState)
            {
                case CustomerState.Wandering:
                    // Wander around zone
                    if (purchaseTimer <= 0)
                    {
                        currentState = CustomerState.Seeking;
                        purchaseTimer = GetPurchaseInterval();
                    }
                    break;

                case CustomerState.Seeking:
                    // Look for a dealer — handled by higher-level system
                    break;

                case CustomerState.Approaching:
                    // Walking toward dealer
                    break;

                case CustomerState.Buying:
                    // Transaction in progress
                    break;

                case CustomerState.Satisfied:
                    // Got what they needed — return to wandering
                    currentState = CustomerState.Wandering;
                    break;

                case CustomerState.Unsatisfied:
                    // Didn't find product — might become more desperate
                    addictionLevel = Mathf.Min(1f, addictionLevel + 0.02f);
                    currentState = CustomerState.Wandering;
                    purchaseTimer = GetPurchaseInterval() * 0.5f; // Come back sooner
                    break;
            }
        }

        /// <summary>
        /// Evaluate a deal offer. Returns true if customer accepts.
        /// </summary>
        public bool EvaluateDeal(float price, float quality, int dealerId)
        {
            // Price check
            float adjustedMax = maxWillingToPay * (1f + addictionLevel * 0.5f); // Addicts pay more
            if (price > adjustedMax) return false;

            // Quality check
            float qualityThreshold = qualityPreference * (1f - addictionLevel * 0.3f); // Addicts accept lower quality
            if (quality < qualityThreshold) return false;

            // Loyalty bonus — prefer their usual dealer
            float loyaltyBonus = (dealerId == preferredDealerId) ? loyalty * 0.3f : 0f;

            // Overall satisfaction score
            float priceScore = 1f - (price / adjustedMax);
            float qualityScore = quality;
            float totalScore = priceScore * pricesSensitivity + qualityScore * qualityPreference + loyaltyBonus;

            return totalScore > 0.3f; // Accept if score is decent
        }

        /// <summary>
        /// Complete a purchase — update addiction, loyalty, and return interval.
        /// </summary>
        public void CompletePurchase(int dealerId, float quality)
        {
            _totalPurchases++;
            _lastQualityReceived = quality;

            // Addiction grows
            addictionLevel = Mathf.Min(1f, addictionLevel + addictionGrowthRate);

            // Loyalty grows toward this dealer
            if (dealerId == preferredDealerId)
            {
                loyalty = Mathf.Min(1f, loyalty + loyaltyGrowthRate);
            }
            else if (preferredDealerId == -1 || quality > _lastQualityReceived + 0.1f)
            {
                // Switch loyalty to better dealer
                preferredDealerId = dealerId;
                loyalty = 0.1f;
            }

            currentState = CustomerState.Satisfied;
        }

        /// <summary>
        /// Purchase interval decreases with addiction (buy more often).
        /// </summary>
        private float GetPurchaseInterval()
        {
            return purchaseInterval * (1f - addictionLevel * 0.6f);
        }
    }

    public enum CustomerState
    {
        Wandering,
        Seeking,
        Approaching,
        Buying,
        Satisfied,
        Unsatisfied,
        Fleeing         // Saw police or violence
    }
}
