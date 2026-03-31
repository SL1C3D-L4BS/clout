using UnityEngine;
using UnityEngine.InputSystem;
using Clout.Empire.Dealing;
using Clout.World.NPCs;

namespace Clout.UI.Dealing
{
    /// <summary>
    /// Deal negotiation UI — screen-space panel for player ↔ customer transactions.
    ///
    /// Shows:
    /// - Customer info (name, preferred product, addiction level)
    /// - Product selection from player's ProductInventory
    /// - Quantity slider
    /// - Price negotiation slider (suggested price ± 30%)
    /// - Accept/Cancel buttons
    /// - Risk indicator
    ///
    /// Uses OnGUI for rapid prototyping — will migrate to UI Toolkit for production.
    /// </summary>
    public class DealUI : MonoBehaviour
    {
        [Header("References")]
        public DealManager dealManager;

        // State
        private bool _visible;
        private DealContext _context;
        private int _selectedProductIndex;
        private int _quantity = 1;
        private float _priceMultiplier = 1f;    // 0.7 to 1.3 range
        private string _resultMessage = "";
        private float _resultTimer;
        private Vector2 _scrollPosition;

        // Cached data
        private System.Collections.Generic.List<ProductStack> _availableProducts;
        private float _suggestedPrice;

        private void Start()
        {
            if (dealManager == null)
                dealManager = DealManager.Instance;

            if (dealManager != null)
            {
                dealManager.OnDealStarted += OnDealStarted;
                dealManager.OnDealEnded += OnDealEnded;
                dealManager.OnDealCompleted += OnDealCompleted;
            }
        }

        private void OnDestroy()
        {
            if (dealManager != null)
            {
                dealManager.OnDealStarted -= OnDealStarted;
                dealManager.OnDealEnded -= OnDealEnded;
                dealManager.OnDealCompleted -= OnDealCompleted;
            }
        }

        private void OnDealStarted(DealContext context)
        {
            _context = context;
            _visible = true;
            _selectedProductIndex = 0;
            _quantity = 1;
            _priceMultiplier = 1f;
            _resultMessage = "";
            _availableProducts = context.inventory.GetAllProducts();

            // Auto-select preferred product if available
            for (int i = 0; i < _availableProducts.Count; i++)
            {
                if (_availableProducts[i].product != null &&
                    _availableProducts[i].product.productType == context.preferredProduct)
                {
                    _selectedProductIndex = i;
                    break;
                }
            }

            RecalculatePrice();
        }

        private void OnDealEnded()
        {
            _visible = false;
        }

        private void OnDealCompleted(DealResult result)
        {
            _resultMessage = result.message;
            _resultTimer = 3f;
        }

        private void Update()
        {
            if (_resultTimer > 0)
            {
                _resultTimer -= Time.deltaTime;
                if (_resultTimer <= 0) _resultMessage = "";
            }

            // ESC to cancel
            if (_visible && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                dealManager?.CancelDeal("Player cancelled.");
            }
        }

        private void OnGUI()
        {
            // Result message (persists after deal closes)
            if (!string.IsNullOrEmpty(_resultMessage))
            {
                GUIStyle resultStyle = new GUIStyle(GUI.skin.box);
                resultStyle.fontSize = 18;
                resultStyle.alignment = TextAnchor.MiddleCenter;
                resultStyle.normal.textColor = Color.yellow;
                GUI.Box(new Rect(Screen.width / 2 - 200, 20, 400, 40), _resultMessage, resultStyle);
            }

            if (!_visible) return;
            if (_availableProducts == null || _availableProducts.Count == 0)
            {
                dealManager?.CancelDeal("No product available.");
                return;
            }

            // Dark overlay
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Main panel
            float panelW = 500;
            float panelH = 480;
            float panelX = (Screen.width - panelW) / 2;
            float panelY = (Screen.height - panelH) / 2;
            Rect panelRect = new Rect(panelX, panelY, panelW, panelH);

            GUI.Box(panelRect, "");

            GUILayout.BeginArea(new Rect(panelX + 15, panelY + 10, panelW - 30, panelH - 20));

            // === HEADER ===
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 20;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("MAKE A DEAL", headerStyle);
            GUILayout.Space(5);

            // === CUSTOMER INFO ===
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Customer: {_context.customer.customerName}", GUILayout.Width(200));
            string wantsStr = _context.preferredProduct.ToString();
            GUILayout.Label($"Wants: {wantsStr}");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Budget: ${_context.customerBudget:F0}", GUILayout.Width(200));
            GUILayout.Label($"Addiction: {_context.customer.addictionLevel:P0}");
            GUILayout.EndHorizontal();

            if (_context.loyaltyBonus > 0)
                GUILayout.Label($"★ Loyal customer (bonus: +{_context.loyaltyBonus:P0})");

            GUILayout.Space(10);

            // === PRODUCT SELECTION ===
            GUILayout.Label("Select Product:");
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(100));
            for (int i = 0; i < _availableProducts.Count; i++)
            {
                var stack = _availableProducts[i];
                string label = $"{stack.productId} x{stack.quantity} (Q:{stack.quality:P0})";
                bool isPreferred = stack.product != null &&
                    stack.product.productType == _context.preferredProduct;

                GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
                if (i == _selectedProductIndex)
                    btnStyle.normal.textColor = Color.green;
                else if (isPreferred)
                    btnStyle.normal.textColor = Color.yellow;

                if (GUILayout.Button(label, btnStyle))
                {
                    _selectedProductIndex = i;
                    _quantity = 1;
                    RecalculatePrice();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(5);

            // === QUANTITY ===
            var selected = _availableProducts[_selectedProductIndex];
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Quantity: {_quantity}", GUILayout.Width(100));
            _quantity = Mathf.RoundToInt(GUILayout.HorizontalSlider(_quantity, 1, selected.quantity));
            GUILayout.EndHorizontal();

            // === PRICE NEGOTIATION ===
            GUILayout.Space(5);
            float totalPrice = _suggestedPrice * _priceMultiplier * _quantity;
            GUILayout.Label($"Price per unit: ${_suggestedPrice * _priceMultiplier:F0}  |  Total: ${totalPrice:F0}");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Low", GUILayout.Width(30));
            _priceMultiplier = GUILayout.HorizontalSlider(_priceMultiplier, 0.7f, 1.3f);
            GUILayout.Label("High", GUILayout.Width(30));
            GUILayout.EndHorizontal();

            // Acceptance likelihood
            float acceptChance = EstimateAcceptance();
            GUIStyle chanceStyle = new GUIStyle(GUI.skin.label);
            chanceStyle.normal.textColor = acceptChance > 0.7f ? Color.green :
                acceptChance > 0.4f ? Color.yellow : Color.red;
            GUILayout.Label($"Acceptance chance: {acceptChance:P0}", chanceStyle);

            // Risk indicator
            if (_context.isPublicLocation)
            {
                GUIStyle riskStyle = new GUIStyle(GUI.skin.label);
                riskStyle.normal.textColor = Color.red;
                GUILayout.Label("⚠ PUBLIC LOCATION — higher heat!", riskStyle);
            }

            GUILayout.Space(10);

            // === BUTTONS ===
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("  DEAL  ", GUILayout.Height(35), GUILayout.Width(120)))
            {
                float agreedPricePerUnit = _suggestedPrice * _priceMultiplier;
                dealManager.ExecuteDeal(selected, _quantity, agreedPricePerUnit);
            }

            GUILayout.Space(20);

            if (GUILayout.Button(" Walk Away ", GUILayout.Height(35), GUILayout.Width(120)))
            {
                dealManager.CancelDeal("Player walked away.");
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void RecalculatePrice()
        {
            if (_availableProducts == null || _selectedProductIndex >= _availableProducts.Count) return;
            var stack = _availableProducts[_selectedProductIndex];
            _suggestedPrice = DealManager.CalculateSuggestedPrice(stack, _context.customer);
        }

        private float EstimateAcceptance()
        {
            if (_availableProducts == null || _selectedProductIndex >= _availableProducts.Count) return 0;
            var stack = _availableProducts[_selectedProductIndex];
            float pricePerUnit = _suggestedPrice * _priceMultiplier;
            bool wouldAccept = _context.customer.EvaluateDeal(pricePerUnit, stack.quality, 0);
            // Fuzzy estimate — adjust based on how close to threshold
            float budget = _context.customerBudget;
            float ratio = pricePerUnit / budget;
            float estimate = wouldAccept ? Mathf.Lerp(0.6f, 1f, 1f - ratio) : Mathf.Lerp(0f, 0.3f, 1f - ratio);
            return Mathf.Clamp01(estimate);
        }
    }
}
