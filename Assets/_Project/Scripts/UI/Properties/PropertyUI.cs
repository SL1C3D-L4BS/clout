using UnityEngine;
using UnityEngine.InputSystem;
using Clout.Core;
using Clout.Empire.Properties;
using Clout.Empire.Economy;
using System.Collections.Generic;

namespace Clout.UI.Properties
{
    /// <summary>
    /// Property management UI — OnGUI panel for browsing available properties,
    /// purchasing, viewing owned properties, managing stash, and applying upgrades.
    ///
    /// Two modes:
    /// 1. BROWSE — view all properties for sale in the current area
    /// 2. MANAGE — view owned property details, stash, upgrades
    ///
    /// Uses OnGUI for rapid prototyping — will migrate to UI Toolkit for production.
    /// </summary>
    public class PropertyUI : MonoBehaviour
    {
        private bool _visible;
        private Property _selectedProperty;
        private PropertyDefinition _selectedForSale;
        private Vector3 _purchasePosition;

        // Available properties for sale (set by PropertySystemFactory at scene build)
        [HideInInspector] public List<PropertyForSale> propertiesForSale = new List<PropertyForSale>();

        // UI state
        private int _selectedTab; // 0=ForSale, 1=Owned
        private int _selectedIndex;
        private Vector2 _scrollPos;
        private Vector2 _detailScroll;
        private string _message = "";
        private float _messageTimer;

        // ─── Open / Close ───────────────────────────────────

        public void OpenBrowse()
        {
            _visible = true;
            _selectedTab = 0;
            _selectedIndex = 0;
            _message = "";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void OpenManage(Property property)
        {
            _visible = true;
            _selectedProperty = property;
            _message = "";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            _visible = false;
            _selectedProperty = null;
            _selectedForSale = null;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (_messageTimer > 0)
            {
                _messageTimer -= Time.deltaTime;
                if (_messageTimer <= 0) _message = "";
            }

            if (Keyboard.current == null) return;

            // P toggles property browser
            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                if (_visible)
                    Close();
                else
                    OpenBrowse();
            }

            if (_visible && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        // ─── OnGUI ──────────────────────────────────────────

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

            if (!_visible) return;

            // Dark overlay
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float panelW = 620;
            float panelH = 550;
            float panelX = (Screen.width - panelW) / 2;
            float panelY = (Screen.height - panelH) / 2;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "");
            GUILayout.BeginArea(new Rect(panelX + 15, panelY + 10, panelW - 30, panelH - 20));

            // Header
            GUIStyle header = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("PROPERTY EMPIRE", header);
            GUILayout.Space(3);

            // Cash display
            DrawCashBar();

            GUILayout.Space(5);

            // Tab bar
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_selectedTab == 0, "FOR SALE", GUI.skin.button, GUILayout.Height(28)))
                _selectedTab = 0;
            if (GUILayout.Toggle(_selectedTab == 1, "OWNED", GUI.skin.button, GUILayout.Height(28)))
                _selectedTab = 1;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (_selectedTab == 0)
                DrawForSaleTab();
            else
                DrawOwnedTab();

            GUILayout.Space(5);

            // Close button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(" CLOSE ", GUILayout.Height(30), GUILayout.Width(120)))
                Close();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        // ─── Cash Bar ───────────────────────────────────────

        private void DrawCashBar()
        {
            CashManager cash = CashManager.Instance;
            if (cash == null) return;

            GUIStyle cashStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            cashStyle.normal.textColor = new Color(0.4f, 1f, 0.4f);
            GUILayout.Label($"Cash: ${cash.TotalCash:F0}  (Clean: ${cash.CleanCash:F0}  |  Dirty: ${cash.DirtyCash:F0})", cashStyle);
        }

        // ─── For Sale Tab ───────────────────────────────────

        private void DrawForSaleTab()
        {
            if (propertiesForSale == null || propertiesForSale.Count == 0)
            {
                GUILayout.Label("No properties available for sale in this area.",
                    new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                return;
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));
            for (int i = 0; i < propertiesForSale.Count; i++)
            {
                var listing = propertiesForSale[i];
                if (listing.definition == null) continue;

                // Check if already owned
                bool owned = PropertyManager.Instance != null &&
                             PropertyManager.Instance.GetProperty(listing.definition.propertyName) != null;

                GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
                if (i == _selectedIndex)
                    btnStyle.normal.textColor = Color.cyan;
                else if (owned)
                    btnStyle.normal.textColor = Color.gray;

                string typeStr = listing.definition.propertyType.ToString();
                string label = owned
                    ? $"[OWNED] {listing.definition.propertyName}  ({typeStr})"
                    : $"{listing.definition.propertyName}  ({typeStr})  ${listing.definition.purchasePrice:F0}";

                if (GUILayout.Button(label, btnStyle))
                {
                    _selectedIndex = i;
                    _selectedForSale = listing.definition;
                    _purchasePosition = listing.worldPosition;
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(5);

            // Selected property details
            if (_selectedIndex < propertiesForSale.Count)
            {
                var selected = propertiesForSale[_selectedIndex];
                DrawPropertyDetails(selected.definition, false, selected.worldPosition);
            }
        }

        // ─── Owned Tab ──────────────────────────────────────

        private void DrawOwnedTab()
        {
            PropertyManager mgr = PropertyManager.Instance;
            if (mgr == null || mgr.PropertyCount == 0)
            {
                GUILayout.Label("You don't own any properties yet.",
                    new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                return;
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(150));
            var owned = mgr.OwnedProperties;
            for (int i = 0; i < owned.Count; i++)
            {
                Property prop = owned[i];
                if (prop == null || prop.Definition == null) continue;

                GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
                if (prop == _selectedProperty)
                    btnStyle.normal.textColor = Color.green;

                float netDaily = prop.GetDailyRevenue() - prop.GetDailyUpkeep();
                string netStr = netDaily >= 0 ? $"+${netDaily:F0}/day" : $"-${Mathf.Abs(netDaily):F0}/day";
                string condStr = prop.Condition >= 0.9f ? "" : $"  [{prop.Condition:P0}]";

                if (GUILayout.Button($"{prop.Definition.propertyName}  ({prop.Definition.propertyType})  {netStr}{condStr}", btnStyle))
                    _selectedProperty = prop;
            }
            GUILayout.EndScrollView();

            GUILayout.Space(5);

            // Selected owned property details
            if (_selectedProperty != null)
                DrawOwnedPropertyDetails(_selectedProperty);
        }

        // ─── Property Details (For Sale) ────────────────────

        private void DrawPropertyDetails(PropertyDefinition def, bool owned, Vector3 position)
        {
            if (def == null) return;

            _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUILayout.Height(180));

            // Description
            if (!string.IsNullOrEmpty(def.description))
            {
                GUILayout.Label(def.description, new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 12 });
                GUILayout.Space(3);
            }

            // Stats grid
            GUILayout.Label($"Type: {def.propertyType}", SmallLabel());
            GUILayout.Label($"Price: ${def.purchasePrice:F0}", SmallLabel());
            GUILayout.Label($"Daily Revenue: ${def.dailyRevenue:F0}  |  Upkeep: ${def.dailyUpkeep:F0}  |  Net: ${def.dailyRevenue - def.dailyUpkeep:F0}/day", SmallLabel());
            GUILayout.Label($"Storage: {def.maxStorage} units  |  Employee Slots: {def.maxEmployeeSlots}", SmallLabel());
            GUILayout.Label($"Visibility: {def.policeVisibility:P0}  |  Raid Risk: {def.raidChance:P0}", SmallLabel());
            if (!string.IsNullOrEmpty(def.districtName))
                GUILayout.Label($"District: {def.districtName}", SmallLabel());
            if (def.requiresCoverBusiness)
            {
                GUIStyle warn = SmallLabel();
                warn.normal.textColor = Color.yellow;
                GUILayout.Label("Requires cover business to reduce police attention.", warn);
            }

            // Available upgrades preview
            if (def.availableUpgrades != null && def.availableUpgrades.Length > 0)
            {
                GUILayout.Space(3);
                GUILayout.Label($"Upgrades Available: {def.availableUpgrades.Length}", SmallLabel());
            }

            GUILayout.EndScrollView();

            // Buy button
            bool alreadyOwned = PropertyManager.Instance?.GetProperty(def.propertyName) != null;
            if (!alreadyOwned)
            {
                CashManager cash = CashManager.Instance;
                bool isSmall = def.purchasePrice < 5000f;
                bool canAfford = cash != null && (isSmall ? cash.CanAfford(def.purchasePrice) : cash.CanAffordClean(def.purchasePrice));

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUI.enabled = canAfford;
                string buyLabel = isSmall ? "  BUY  " : "  BUY (Clean $)  ";
                if (GUILayout.Button(buyLabel, GUILayout.Height(32), GUILayout.Width(160)))
                {
                    Property bought = PropertyManager.Instance?.BuyProperty(def, position);
                    if (bought != null)
                    {
                        _message = $"Purchased {def.propertyName}!";
                        _messageTimer = 3f;
                        _selectedProperty = bought;
                        _selectedTab = 1; // Switch to owned tab
                    }
                    else
                    {
                        _message = "Purchase failed!";
                        _messageTimer = 2f;
                    }
                }
                GUI.enabled = true;

                if (!canAfford && cash != null)
                {
                    GUIStyle cantAfford = SmallLabel();
                    cantAfford.normal.textColor = Color.red;
                    string reason = isSmall ? $"Need ${def.purchasePrice:F0}" : $"Need ${def.purchasePrice:F0} CLEAN cash";
                    GUILayout.Label(reason, cantAfford);
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        // ─── Owned Property Details ─────────────────────────

        private void DrawOwnedPropertyDetails(Property prop)
        {
            if (prop == null || prop.Definition == null) return;

            _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUILayout.Height(200));

            GUILayout.Label($"Condition: {prop.Condition:P0}", SmallLabel());
            GUILayout.Label($"Revenue: ${prop.GetDailyRevenue():F0}/day  |  Upkeep: ${prop.GetDailyUpkeep():F0}/day", SmallLabel());
            GUILayout.Label($"Storage: {prop.GetTotalStashed()}/{prop.GetMaxStorage():F0} units", SmallLabel());
            GUILayout.Label($"Employees: 0/{prop.GetMaxEmployeeSlots()}", SmallLabel());
            GUILayout.Label($"Security: {prop.GetSecurityLevel():P0}  |  Visibility: {prop.GetPoliceVisibility():P0}", SmallLabel());
            GUILayout.Label($"Value: ${prop.GetTotalValue():F0}", SmallLabel());

            // Stash contents
            if (prop.StashCount > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("─── STASH ───", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 12 });
                foreach (var slot in prop.Stash)
                    GUILayout.Label($"  {slot.productId} x{slot.quantity} (Q:{slot.quality:P0})", SmallLabel());
            }

            // Upgrades
            PropertyDefinition def = prop.Definition;
            if (def.availableUpgrades != null && def.availableUpgrades.Length > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("─── UPGRADES ───", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 12 });
                for (int i = 0; i < def.availableUpgrades.Length; i++)
                {
                    var upgrade = def.availableUpgrades[i];
                    bool applied = prop.HasUpgrade(i);

                    if (applied)
                    {
                        GUIStyle doneStyle = SmallLabel();
                        doneStyle.normal.textColor = Color.green;
                        GUILayout.Label($"  [DONE] {upgrade.upgradeName}", doneStyle);
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"  {upgrade.upgradeName} — ${upgrade.cost:F0}", SmallLabel(), GUILayout.Width(300));

                        CashManager cash = CashManager.Instance;
                        GUI.enabled = cash != null && cash.CanAfford(upgrade.cost);
                        if (GUILayout.Button("UPGRADE", GUILayout.Width(80), GUILayout.Height(20)))
                        {
                            if (PropertyManager.Instance?.UpgradeProperty(prop, i) == true)
                            {
                                _message = $"Upgraded: {upgrade.upgradeName}!";
                                _messageTimer = 2f;
                            }
                        }
                        GUI.enabled = true;
                        GUILayout.EndHorizontal();
                    }
                }
            }

            GUILayout.EndScrollView();

            // Sell button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUIStyle sellBtn = new GUIStyle(GUI.skin.button);
            sellBtn.normal.textColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button($"  SELL (${prop.GetTotalValue() * 0.6f:F0})  ", sellBtn, GUILayout.Height(28), GUILayout.Width(180)))
            {
                string name = prop.Definition.propertyName;
                if (PropertyManager.Instance?.SellProperty(prop) == true)
                {
                    _message = $"Sold {name}!";
                    _messageTimer = 2f;
                    _selectedProperty = null;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static GUIStyle SmallLabel()
        {
            return new GUIStyle(GUI.skin.label) { fontSize = 12 };
        }
    }

    // ─── Data ────────────────────────────────────────────────

    [System.Serializable]
    public struct PropertyForSale
    {
        public PropertyDefinition definition;
        public Vector3 worldPosition;
    }
}
