using UnityEngine;
using UnityEngine.InputSystem;
using Clout.Core;
using Clout.Empire.Economy;
using Clout.World.NPCs;
using System.Collections.Generic;

namespace Clout.UI.Economy
{
    /// <summary>
    /// Shop interaction UI — OnGUI panel for buying ingredients / selling product.
    /// Subscribes to ShopKeeper.OnShopOpened/OnShopClosed events.
    ///
    /// Uses OnGUI for rapid prototyping — will migrate to UI Toolkit for production.
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        private bool _visible;
        private ShopKeeper _shop;
        private CharacterStateManager _buyer;
        private List<ShopListingState> _listings;
        private int _selectedIndex;
        private int _buyQuantity = 1;
        private string _message = "";
        private float _messageTimer;
        private Vector2 _scrollPosition;

        private void Start()
        {
            // Auto-subscribe to all shop keepers in scene
            var shops = FindObjectsByType<ShopKeeper>();
            foreach (var shop in shops)
            {
                shop.OnShopOpened += Open;
                shop.OnShopClosed += Close;
            }
        }

        private void OnDestroy()
        {
            var shops = FindObjectsByType<ShopKeeper>();
            foreach (var shop in shops)
            {
                shop.OnShopOpened -= Open;
                shop.OnShopClosed -= Close;
            }
        }

        public void Open(ShopKeeper shop, CharacterStateManager buyer)
        {
            _shop = shop;
            _buyer = buyer;
            _visible = true;
            _selectedIndex = 0;
            _buyQuantity = 1;
            _message = "";
            RefreshListings();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            _visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void RefreshListings()
        {
            if (_shop != null)
                _listings = _shop.GetCurrentListings();
        }

        private void Update()
        {
            if (_messageTimer > 0)
            {
                _messageTimer -= Time.deltaTime;
                if (_messageTimer <= 0) _message = "";
            }

            if (_visible && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _shop?.CloseShop();
            }
        }

        private void OnGUI()
        {
            // Floating message
            if (!string.IsNullOrEmpty(_message))
            {
                GUIStyle msgStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter
                };
                msgStyle.normal.textColor = Color.yellow;
                GUI.Box(new Rect(Screen.width / 2 - 200, 20, 400, 35), _message, msgStyle);
            }

            if (!_visible || _listings == null) return;

            // Dark overlay
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Main panel
            float panelW = 520;
            float panelH = 500;
            float panelX = (Screen.width - panelW) / 2;
            float panelY = (Screen.height - panelH) / 2;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "");
            GUILayout.BeginArea(new Rect(panelX + 15, panelY + 10, panelW - 30, panelH - 20));

            // Header
            GUIStyle header = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label(_shop.shopName.ToUpper(), header);
            GUILayout.Space(3);

            // Cash display
            CashManager cash = CashManager.Instance;
            string cashStr = cash != null ? $"${cash.TotalCash:F0}" : "$???";
            string dirtyStr = cash != null ? $"(Dirty: ${cash.DirtyCash:F0} | Clean: ${cash.CleanCash:F0})" : "";

            GUIStyle cashStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            cashStyle.normal.textColor = new Color(0.4f, 1f, 0.4f);
            GUILayout.Label($"Your Cash: {cashStr}", cashStyle);

            GUIStyle subCash = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };
            subCash.normal.textColor = Color.gray;
            GUILayout.Label(dirtyStr, subCash);

            GUILayout.Space(8);

            // Shop type label
            string typeLabel = _shop.shopType switch
            {
                ShopType.IngredientSupplier => "INGREDIENTS FOR SALE",
                ShopType.FenceShop => "WE BUY PRODUCT",
                ShopType.GeneralStore => "GENERAL GOODS",
                ShopType.WeaponDealer => "WEAPONS & AMMO",
                ShopType.BlackMarket => "BLACK MARKET",
                _ => "ITEMS"
            };
            GUILayout.Label(typeLabel, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });

            GUILayout.Space(5);

            // Item list
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
            for (int i = 0; i < _listings.Count; i++)
            {
                var item = _listings[i];
                bool inStock = item.currentStock > 0;
                string stockStr = inStock ? $"x{item.currentStock}" : "OUT";

                GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
                if (i == _selectedIndex)
                    btnStyle.normal.textColor = Color.green;
                else if (!inStock)
                    btnStyle.normal.textColor = Color.gray;

                string label = $"{item.listing.displayName}  ${item.listing.price:F0}  [{stockStr}]";
                if (GUILayout.Button(label, btnStyle))
                {
                    _selectedIndex = i;
                    _buyQuantity = 1;
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(5);

            // Selected item details
            if (_selectedIndex < _listings.Count)
            {
                var selected = _listings[_selectedIndex];

                if (!string.IsNullOrEmpty(selected.listing.description))
                    GUILayout.Label(selected.listing.description, new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 });

                GUILayout.Space(5);

                // Quantity slider
                int maxBuy = Mathf.Min(selected.currentStock, 20);
                if (maxBuy > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Quantity: {_buyQuantity}", GUILayout.Width(100));
                    _buyQuantity = Mathf.RoundToInt(GUILayout.HorizontalSlider(_buyQuantity, 1, Mathf.Max(1, maxBuy)));
                    GUILayout.EndHorizontal();

                    float totalCost = selected.listing.price * _buyQuantity;
                    bool canAfford = cash != null && cash.CanAfford(totalCost);

                    GUIStyle costStyle = new GUIStyle(GUI.skin.label);
                    costStyle.normal.textColor = canAfford ? Color.white : Color.red;
                    GUILayout.Label($"Total: ${totalCost:F0}", costStyle);

                    GUILayout.Space(5);

                    // Buy button
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    GUI.enabled = canAfford && selected.currentStock > 0;
                    if (GUILayout.Button("  BUY  ", GUILayout.Height(35), GUILayout.Width(120)))
                    {
                        if (_shop.BuyFromShop(selected.listing.itemId, _buyQuantity, _buyer))
                        {
                            _message = $"Bought {selected.listing.displayName} x{_buyQuantity}!";
                            _messageTimer = 2f;
                            RefreshListings();
                            _buyQuantity = 1;
                        }
                        else
                        {
                            _message = "Purchase failed!";
                            _messageTimer = 2f;
                        }
                    }
                    GUI.enabled = true;

                    GUILayout.Space(20);

                    if (GUILayout.Button(" CLOSE ", GUILayout.Height(35), GUILayout.Width(120)))
                    {
                        _shop?.CloseShop();
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUIStyle oos = new GUIStyle(GUI.skin.label);
                    oos.normal.textColor = Color.red;
                    GUILayout.Label("OUT OF STOCK", oos);
                }
            }

            GUILayout.EndArea();
        }
    }
}
