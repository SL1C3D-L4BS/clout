using UnityEngine;
using Clout.World.NPCs;
using Clout.Empire.Dealing;

namespace Clout.UI.Dealing
{
    /// <summary>
    /// Supplier buy menu — screen-space catalog for purchasing wholesale product.
    ///
    /// Shows:
    /// - Supplier name and reliability
    /// - Product catalog with stock/price/quality
    /// - Quantity selector
    /// - Buy button
    /// - Player cash display
    ///
    /// Uses OnGUI for rapid prototyping — will migrate to UI Toolkit for production.
    /// </summary>
    public class SupplierUI : MonoBehaviour
    {
        private bool _visible;
        private SupplierContext _context;
        private int _selectedIndex;
        private int _buyQuantity = 1;
        private string _resultMessage = "";
        private float _resultTimer;

        /// <summary>
        /// Call this to open the supplier UI. Typically called by SupplierNPC.OnSupplierOpened.
        /// </summary>
        public void Open(SupplierContext context)
        {
            _context = context;
            _visible = true;
            _selectedIndex = 0;
            _buyQuantity = 1;
            _resultMessage = "";

            // Subscribe to close
            context.npc.OnSupplierClosed += OnClosed;
        }

        private void OnClosed()
        {
            _visible = false;
            if (_context.npc != null)
                _context.npc.OnSupplierClosed -= OnClosed;
        }

        private void Update()
        {
            if (_resultTimer > 0)
            {
                _resultTimer -= Time.deltaTime;
                if (_resultTimer <= 0) _resultMessage = "";
            }

            if (_visible && Input.GetKeyDown(KeyCode.Escape))
            {
                _context.npc?.CloseSupplier(_context.player);
            }
        }

        private void OnGUI()
        {
            // Persistent result
            if (!string.IsNullOrEmpty(_resultMessage))
            {
                GUIStyle rStyle = new GUIStyle(GUI.skin.box);
                rStyle.fontSize = 16;
                rStyle.alignment = TextAnchor.MiddleCenter;
                rStyle.normal.textColor = Color.cyan;
                GUI.Box(new Rect(Screen.width / 2 - 200, 20, 400, 35), _resultMessage, rStyle);
            }

            if (!_visible) return;

            var supplier = _context.supplier;
            if (supplier == null || supplier.catalog == null) return;

            // Dark overlay
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Panel
            float panelW = 520;
            float panelH = 440;
            float panelX = (Screen.width - panelW) / 2;
            float panelY = (Screen.height - panelH) / 2;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "");

            GUILayout.BeginArea(new Rect(panelX + 15, panelY + 10, panelW - 30, panelH - 20));

            // Header
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 20;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label($"THE CONNECT: {supplier.supplierName}", headerStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Reliability: {supplier.reliability:P0}", GUILayout.Width(150));
            GUILayout.Label($"Your Cash: ${_context.player.cash:F0}");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // === CATALOG ===
            GUILayout.Label("Available Product:");
            GUILayout.Space(3);

            for (int i = 0; i < supplier.catalog.Length; i++)
            {
                var entry = supplier.catalog[i];
                int stock = _context.currentStock != null && i < _context.currentStock.Length
                    ? _context.currentStock[i] : 0;

                string productName = entry.product != null ? entry.product.productName : "???";
                string qualityRange = $"Q:{entry.qualityFloor:P0}-{entry.qualityCeiling:P0}";
                string label = $"{productName}  |  ${entry.wholesalePrice:F0}/unit  |  Stock: {stock}  |  {qualityRange}";

                GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
                if (i == _selectedIndex)
                    btnStyle.normal.textColor = Color.green;
                if (stock <= 0)
                {
                    btnStyle.normal.textColor = Color.gray;
                    label += "  [OUT]";
                }

                if (GUILayout.Button(label, btnStyle))
                {
                    _selectedIndex = i;
                    _buyQuantity = 1;
                }
            }

            GUILayout.Space(10);

            // === SELECTED PRODUCT DETAIL ===
            if (_selectedIndex < supplier.catalog.Length)
            {
                var selected = supplier.catalog[_selectedIndex];
                int stock = _context.currentStock != null && _selectedIndex < _context.currentStock.Length
                    ? _context.currentStock[_selectedIndex] : 0;

                // Quantity
                int maxBuy = Mathf.Min(stock, Mathf.FloorToInt(_context.player.cash / Mathf.Max(1f, selected.wholesalePrice)));
                maxBuy = Mathf.Max(1, maxBuy);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Buy Quantity: {_buyQuantity}", GUILayout.Width(120));
                if (maxBuy > 1)
                    _buyQuantity = Mathf.RoundToInt(GUILayout.HorizontalSlider(_buyQuantity, 1, maxBuy));
                GUILayout.EndHorizontal();

                float totalCost = selected.wholesalePrice * _buyQuantity;
                bool canAfford = _context.player.cash >= totalCost;

                GUIStyle costStyle = new GUIStyle(GUI.skin.label);
                costStyle.fontSize = 16;
                costStyle.normal.textColor = canAfford ? Color.green : Color.red;
                GUILayout.Label($"Total Cost: ${totalCost:F0}", costStyle);

                GUILayout.Space(5);

                // === BUTTONS ===
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUI.enabled = canAfford && stock > 0;
                if (GUILayout.Button("  BUY  ", GUILayout.Height(35), GUILayout.Width(120)))
                {
                    var result = _context.npc.BuyProduct(_context.player, _selectedIndex, _buyQuantity);
                    _resultMessage = result.message;
                    _resultTimer = 3f;

                    if (result.success)
                    {
                        // Refresh stock
                        _context.currentStock = _context.npc.currentStock;
                        _buyQuantity = 1;
                    }
                }
                GUI.enabled = true;

                GUILayout.Space(20);

                if (GUILayout.Button("  Leave  ", GUILayout.Height(35), GUILayout.Width(120)))
                {
                    _context.npc.CloseSupplier(_context.player);
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }
    }
}
