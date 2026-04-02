using UnityEngine;
using Clout.Core;
using Clout.Player;
using Clout.Empire.Dealing;

namespace Clout.World.NPCs
{
    /// <summary>
    /// IInteractable component attached to CustomerAI NPCs.
    /// When the player presses interact near a customer in "Seeking" state,
    /// this initiates a deal through the DealManager.
    ///
    /// Customers only show the interact prompt when they're looking to buy.
    /// Visual indicator (question mark/exclamation) shows their state.
    /// </summary>
    [RequireComponent(typeof(CustomerAI))]
    public class DealInteraction : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [SerializeField] private string _seekingPrompt = "Deal";
        [SerializeField] private string _busyPrompt = "";

        [Header("Visual Indicator")]
        public GameObject seekingIndicator;       // ? icon above head
        public GameObject buyingIndicator;        // $ icon during deal

        private CustomerAI _customer;

        public string InteractionPrompt
        {
            get
            {
                if (_customer == null) return "";
                return _customer.currentState == CustomerState.Seeking
                    ? _seekingPrompt
                    : _busyPrompt;
            }
        }

        private void Awake()
        {
            _customer = GetComponent<CustomerAI>();
        }

        private void Update()
        {
            // Update visual indicators
            if (seekingIndicator != null)
                seekingIndicator.SetActive(_customer.currentState == CustomerState.Seeking);
            if (buyingIndicator != null)
                buyingIndicator.SetActive(_customer.currentState == CustomerState.Buying);
        }

        public bool CanInteract(CharacterStateManager character)
        {
            if (_customer == null) return false;

            // Customer must be seeking product
            if (_customer.currentState != CustomerState.Seeking &&
                _customer.currentState != CustomerState.Wandering)
                return false;

            // Player must have product
            ProductInventory productInv = character.GetComponent<ProductInventory>();
            if (productInv == null || productInv.Products.Count == 0)
                return false;

            // DealManager must not be busy
            if (DealManager.Instance != null && DealManager.Instance.IsDealActive)
                return false;

            return true;
        }

        public void OnInteract(CharacterStateManager character)
        {
            PlayerStateManager player = character as PlayerStateManager;
            if (player == null) return;

            if (!CanInteract(character))
            {
                Debug.Log("[DealInteraction] Can't deal right now.");
                return;
            }

            // Start the deal
            if (DealManager.Instance != null)
            {
                bool started = DealManager.Instance.StartDeal(player, _customer);
                if (started)
                {
                    Debug.Log($"[DealInteraction] Deal started with {_customer.customerName}");
                }
            }
            else
            {
                Debug.LogWarning("[DealInteraction] No DealManager found in scene!");
            }
        }
    }
}
