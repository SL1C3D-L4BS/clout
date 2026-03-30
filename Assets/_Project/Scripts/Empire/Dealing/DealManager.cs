using UnityEngine;
using Clout.Core;
using Clout.Player;
using Clout.Empire.Crafting;
using Clout.Empire.Reputation;
using Clout.World.Police;
using Clout.World.NPCs;
using Clout.Utils;

namespace Clout.Empire.Dealing
{
    /// <summary>
    /// Orchestrates drug deals between the player and customer NPCs.
    ///
    /// Deal flow:
    /// 1. Player approaches customer → DealInteraction triggers
    /// 2. DealManager.StartDeal() → opens DealUI
    /// 3. Player selects product + quantity → negotiates price
    /// 4. DealManager.ExecuteDeal() → product/money exchange
    /// 5. Post-deal: reputation gain, heat increase, customer loyalty update
    ///
    /// Singleton — one active deal at a time.
    /// </summary>
    public class DealManager : MonoBehaviour
    {
        public static DealManager Instance { get; private set; }

        [Header("Deal Config")]
        public float negotiationRange = 0.3f;     // ±30% price negotiation
        public float dealTimeout = 30f;            // Seconds before customer walks away
        public float dealCooldown = 5f;            // Seconds between deals with same customer

        [Header("Risk")]
        [Range(0f, 1f)]
        public float baseSnitchChance = 0.05f;     // Base chance customer snitches
        public float publicDealHeatMultiplier = 1.5f;

        // Current deal state
        private PlayerStateManager _currentPlayer;
        private CustomerAI _currentCustomer;
        private bool _dealActive;
        private float _dealTimer;

        // UI callback
        public System.Action<DealContext> OnDealStarted;
        public System.Action OnDealEnded;
        public System.Action<DealResult> OnDealCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!_dealActive) return;

            _dealTimer -= Time.deltaTime;
            if (_dealTimer <= 0)
            {
                // Customer walked away — deal timed out
                CancelDeal("Customer lost patience.");
            }

            // Check if customer or player moved too far
            if (_currentPlayer != null && _currentCustomer != null)
            {
                float dist = Vector3.Distance(
                    _currentPlayer.transform.position,
                    _currentCustomer.transform.position);
                if (dist > 5f)
                {
                    CancelDeal("Too far from customer.");
                }
            }
        }

        /// <summary>
        /// Start a deal negotiation with a customer.
        /// Called by DealInteraction when player interacts with a customer NPC.
        /// </summary>
        public bool StartDeal(PlayerStateManager player, CustomerAI customer)
        {
            if (_dealActive) return false;
            if (player == null || customer == null) return false;

            // Check if player has any product
            ProductInventory inventory = player.GetComponent<ProductInventory>();
            if (inventory == null || inventory.Products.Count == 0)
            {
                Debug.Log("[DealManager] Player has no product to sell.");
                return false;
            }

            // Check if customer is in a valid state
            if (customer.currentState != CustomerState.Seeking &&
                customer.currentState != CustomerState.Wandering)
            {
                return false;
            }

            _currentPlayer = player;
            _currentCustomer = customer;
            _dealActive = true;
            _dealTimer = dealTimeout;

            // Pause customer AI
            customer.currentState = CustomerState.Buying;

            // Build deal context for UI
            DealContext context = new DealContext
            {
                player = player,
                customer = customer,
                inventory = inventory,
                preferredProduct = customer.preferredProduct,
                customerBudget = customer.maxWillingToPay * (1f + customer.addictionLevel * 0.5f),
                loyaltyBonus = (customer.preferredDealerId == 0) ? customer.loyalty : 0f,
                isPublicLocation = IsPublicLocation(player.transform.position)
            };

            // Lock player controls
            player.isInteracting = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            OnDealStarted?.Invoke(context);
            return true;
        }

        /// <summary>
        /// Execute the deal — transfer product and money.
        /// Called from DealUI when player confirms the deal.
        /// </summary>
        public DealResult ExecuteDeal(ProductStack productStack, int quantity, float agreedPrice)
        {
            if (!_dealActive || _currentPlayer == null || _currentCustomer == null)
                return DealResult.Failed("No active deal.");

            ProductInventory inventory = _currentPlayer.GetComponent<ProductInventory>();
            if (inventory == null)
                return DealResult.Failed("No product inventory.");

            // Validate product availability
            int available = inventory.GetProductCount(productStack.productId);
            if (available < quantity)
                return DealResult.Failed("Not enough product.");

            // Customer evaluates the deal
            float quality = productStack.quality;
            bool accepted = _currentCustomer.EvaluateDeal(agreedPrice, quality, 0);

            if (!accepted)
            {
                _currentCustomer.currentState = CustomerState.Unsatisfied;
                EndDeal();
                return DealResult.Failed("Customer rejected the deal.");
            }

            // === EXECUTE TRANSACTION ===
            float totalCash = agreedPrice * quantity;

            // Remove product from player
            inventory.RemoveProduct(productStack.productId, quantity);

            // Add cash to player
            _currentPlayer.cash += totalCash;

            // Update customer
            _currentCustomer.CompletePurchase(0, quality); // dealerId 0 = local player

            // === POST-DEAL EFFECTS ===

            // Reputation
            ReputationManager rep = _currentPlayer.reputationManager;
            if (rep != null)
            {
                float clout = totalCash >= 500f
                    ? ReputationManager.CloutValues.BigDeal
                    : ReputationManager.CloutValues.CompleteDeal;
                rep.AddClout(clout, "drug deal");

                if (rep.cloutRank.Value == 0) // First deal ever
                    rep.AddClout(ReputationManager.CloutValues.FirstSale, "first sale");
            }

            // Heat
            WantedSystem wanted = _currentPlayer.wantedSystem;
            bool isPublic = IsPublicLocation(_currentPlayer.transform.position);
            if (wanted != null)
            {
                float heat = WantedSystem.HeatValues.DealingInPublic;
                if (isPublic) heat *= publicDealHeatMultiplier;
                wanted.AddHeat(heat, "drug deal");
            }

            // Snitch check
            float snitchRoll = Random.value;
            float snitchChance = baseSnitchChance * (1f - quality) * (1f - _currentCustomer.loyalty);
            bool snitched = snitchRoll < snitchChance;
            if (snitched && wanted != null)
            {
                wanted.AddHeat(WantedSystem.HeatValues.DealingNearPolice, "customer snitched");
            }

            // Publish event
            EventBus.Publish(new DealCompletedEvent
            {
                productId = productStack.productId,
                quantity = quantity,
                cashEarned = totalCash,
                customerId = _currentCustomer.customerName,
                districtId = "" // TODO: territory zone
            });

            EventBus.Publish(new MoneyChangedEvent
            {
                dirtyMoney = _currentPlayer.cash,
                cleanMoney = 0,
                changeAmount = totalCash,
                reason = $"Sold {quantity}x {productStack.productId}"
            });

            DealResult result = new DealResult
            {
                success = true,
                cashEarned = totalCash,
                quantitySold = quantity,
                productName = productStack.productId,
                customerSnitched = snitched,
                message = snitched
                    ? $"Sold {quantity}x for ${totalCash:F0} — WATCH OUT, they might snitch!"
                    : $"Sold {quantity}x for ${totalCash:F0}. Easy money."
            };

            OnDealCompleted?.Invoke(result);
            EndDeal();
            return result;
        }

        /// <summary>
        /// Cancel the current deal without executing.
        /// </summary>
        public void CancelDeal(string reason = "")
        {
            if (!_dealActive) return;

            if (_currentCustomer != null)
                _currentCustomer.currentState = CustomerState.Unsatisfied;

            Debug.Log($"[DealManager] Deal cancelled: {reason}");
            EndDeal();
        }

        private void EndDeal()
        {
            _dealActive = false;

            // Restore player controls
            if (_currentPlayer != null)
            {
                _currentPlayer.isInteracting = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            _currentPlayer = null;
            _currentCustomer = null;
            OnDealEnded?.Invoke();
        }

        /// <summary>
        /// Calculate the suggested price for a product stack.
        /// Uses base value × quality multiplier × customer willingness.
        /// </summary>
        public static float CalculateSuggestedPrice(ProductStack stack, CustomerAI customer)
        {
            if (stack.product == null) return 0;

            float basePx = stack.product.baseStreetValue;
            float qualMult = ProductInventory.GetQualityMultiplier(stack.product, stack.quality);
            float addictionMult = 1f + customer.addictionLevel * 0.3f;
            float loyaltyDiscount = customer.loyalty * 0.1f; // Loyal customers expect slight discount

            return basePx * qualMult * addictionMult * (1f - loyaltyDiscount);
        }

        private bool IsPublicLocation(Vector3 position)
        {
            // Simple check — are there civilians nearby?
            Collider[] nearby = Physics.OverlapSphere(position, 15f);
            int civilianCount = 0;
            foreach (var col in nearby)
            {
                if (col.CompareTag("Civilian")) civilianCount++;
            }
            return civilianCount > 2;
        }

        public bool IsDealActive => _dealActive;
    }

    /// <summary>
    /// Context passed to DealUI when a deal begins.
    /// </summary>
    public struct DealContext
    {
        public PlayerStateManager player;
        public CustomerAI customer;
        public ProductInventory inventory;
        public ProductType preferredProduct;
        public float customerBudget;
        public float loyaltyBonus;
        public bool isPublicLocation;
    }

    /// <summary>
    /// Result of a completed or failed deal.
    /// </summary>
    public struct DealResult
    {
        public bool success;
        public float cashEarned;
        public int quantitySold;
        public string productName;
        public bool customerSnitched;
        public string message;

        public static DealResult Failed(string msg) => new DealResult
        {
            success = false,
            message = msg
        };
    }
}
