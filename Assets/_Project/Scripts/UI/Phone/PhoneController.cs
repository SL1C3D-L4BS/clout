using UnityEngine;
using UnityEngine.InputSystem;
using Clout.Utils;

namespace Clout.UI.Phone
{
    /// <summary>
    /// Phone Controller — empire management hub UI.
    ///
    /// Spec v2.0 Step 9: In-game smartphone with 5 tabs:
    ///   Map | Contacts | Products | Finances | Messages
    ///
    /// Architecture:
    ///   - OnGUI-based (consistent with Phase 2 prototyping pattern)
    ///   - Toggle with M key (map/phone)
    ///   - Tab switching via number keys 1-5 or mouse clicks
    ///   - Dark overlay when open (gameplay paused visual cue)
    ///   - Each tab is a separate MonoBehaviour for modularity
    ///   - Real-time data via singleton references + EventBus subscriptions
    ///
    /// The phone UI is styled as a sleek 2026 smartphone frame rendered
    /// on-screen with tab content inside. Think GTA V phone meets real-time
    /// empire dashboard.
    /// </summary>
    public class PhoneController : MonoBehaviour
    {
        public static PhoneController Instance { get; private set; }

        [Header("State")]
        public bool IsOpen { get; private set; }

        [Header("Layout")]
        [Tooltip("Phone screen width as fraction of screen height.")]
        public float phoneWidthRatio = 0.28f;
        [Tooltip("Phone screen height as fraction of screen height.")]
        public float phoneHeightRatio = 0.75f;

        // ─── Tab System ────────────────────────────────────────
        public enum PhoneTab { Map, Contacts, Products, Finances, Messages }
        private PhoneTab _currentTab = PhoneTab.Map;
        private readonly string[] _tabNames = { "MAP", "CONTACTS", "PRODUCTS", "FINANCES", "MESSAGES" };
        private readonly string[] _tabIcons = { "\u25cb", "\u263a", "\u2606", "$", "\u2709" };

        // Tab renderers (populated by child components registering themselves)
        private PhoneMapTab _mapTab;
        private PhoneContactsTab _contactsTab;
        private PhoneProductsTab _productsTab;
        private PhoneFinanceTab _financeTab;
        private PhoneMessagesTab _messagesTab;

        // ─── Notification Badge ────────────────────────────────
        private int _unreadMessages;

        // ─── Styles ────────────────────────────────────────────
        private GUIStyle _phoneFrameStyle;
        private GUIStyle _tabStyle;
        private GUIStyle _activeTabStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _badgeStyle;
        private bool _stylesInitialized;

        // ─── Colors ────────────────────────────────────────────
        private static readonly Color PHONE_BG = new Color(0.08f, 0.08f, 0.12f);
        private static readonly Color TAB_BG = new Color(0.12f, 0.12f, 0.18f);
        private static readonly Color TAB_ACTIVE = new Color(0.2f, 0.35f, 0.9f);
        private static readonly Color HEADER_COLOR = new Color(0.7f, 0.75f, 0.85f);
        private static readonly Color TEXT_COLOR = new Color(0.85f, 0.88f, 0.92f);
        private static readonly Color ACCENT_GREEN = new Color(0.2f, 0.85f, 0.4f);
        private static readonly Color ACCENT_RED = new Color(0.9f, 0.25f, 0.2f);
        private static readonly Color ACCENT_GOLD = new Color(0.95f, 0.8f, 0.2f);
        private static readonly Color DIM_TEXT = new Color(0.5f, 0.52f, 0.58f);

        // ─── Public Style Accessors (for tab components) ────────
        public GUIStyle HeaderStyle => _headerStyle;
        public GUIStyle BodyStyle => _bodyStyle;
        public GUIStyle ValueStyle => _valueStyle;
        public GUIStyle TitleStyle => _titleStyle;
        public static Color AccentGreen => ACCENT_GREEN;
        public static Color AccentRed => ACCENT_RED;
        public static Color AccentGold => ACCENT_GOLD;
        public static Color DimText => DIM_TEXT;
        public static Color TextCol => TEXT_COLOR;

        // ─── Lifecycle ──────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Find tab components on same or child objects
            _mapTab = GetComponentInChildren<PhoneMapTab>();
            _contactsTab = GetComponentInChildren<PhoneContactsTab>();
            _productsTab = GetComponentInChildren<PhoneProductsTab>();
            _financeTab = GetComponentInChildren<PhoneFinanceTab>();
            _messagesTab = GetComponentInChildren<PhoneMessagesTab>();

            // Subscribe to message-worthy events for notification badge
            EventBus.Subscribe<WorkerArrestedEvent>(OnWorkerArrested);
            EventBus.Subscribe<WorkerBetrayedEvent>(OnWorkerBetrayed);
            EventBus.Subscribe<PropertyRaidAlertEvent>(OnRaidAlert);
            EventBus.Subscribe<WantedLevelChangedEvent>(OnWantedChanged);
            EventBus.Subscribe<DealCompletedEvent>(OnDealCompleted);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            EventBus.Unsubscribe<WorkerArrestedEvent>(OnWorkerArrested);
            EventBus.Unsubscribe<WorkerBetrayedEvent>(OnWorkerBetrayed);
            EventBus.Unsubscribe<PropertyRaidAlertEvent>(OnRaidAlert);
            EventBus.Unsubscribe<WantedLevelChangedEvent>(OnWantedChanged);
            EventBus.Unsubscribe<DealCompletedEvent>(OnDealCompleted);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // Toggle phone with M key
            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                IsOpen = !IsOpen;
                if (IsOpen) _currentTab = PhoneTab.Map; // Default to map
            }

            // Close with Escape
            if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
                IsOpen = false;

            // Tab switching with number keys when open
            if (IsOpen)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) _currentTab = PhoneTab.Map;
                if (Keyboard.current.digit2Key.wasPressedThisFrame) _currentTab = PhoneTab.Contacts;
                if (Keyboard.current.digit3Key.wasPressedThisFrame) _currentTab = PhoneTab.Products;
                if (Keyboard.current.digit4Key.wasPressedThisFrame) _currentTab = PhoneTab.Finances;
                if (Keyboard.current.digit5Key.wasPressedThisFrame) _currentTab = PhoneTab.Messages;
            }
        }

        // ─── Notification Handlers ──────────────────────────────

        private void OnWorkerArrested(WorkerArrestedEvent e) => _unreadMessages++;
        private void OnWorkerBetrayed(WorkerBetrayedEvent e) => _unreadMessages++;
        private void OnRaidAlert(PropertyRaidAlertEvent e) => _unreadMessages++;
        private void OnWantedChanged(WantedLevelChangedEvent e) { if (e.newLevel > e.previousLevel) _unreadMessages++; }
        private void OnDealCompleted(DealCompletedEvent e) { /* No notification for deals — too frequent */ }

        public void ClearUnread() => _unreadMessages = 0;
        public void AddMessage() => _unreadMessages++;

        // ─── OnGUI ──────────────────────────────────────────────

        private void OnGUI()
        {
            if (!IsOpen) return;
            InitStyles();

            // Semi-transparent dark overlay
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Phone frame dimensions
            float phoneW = Screen.height * phoneWidthRatio;
            float phoneH = Screen.height * phoneHeightRatio;
            float phoneX = (Screen.width - phoneW) / 2f;
            float phoneY = (Screen.height - phoneH) / 2f;
            Rect phoneRect = new Rect(phoneX, phoneY, phoneW, phoneH);

            // Phone outer frame (dark, rounded corners simulated by box)
            GUI.color = PHONE_BG;
            GUI.DrawTexture(phoneRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Notch / status bar area
            float statusBarH = phoneH * 0.05f;
            Rect statusRect = new Rect(phoneX, phoneY, phoneW, statusBarH);
            DrawStatusBar(statusRect);

            // Tab bar
            float tabBarH = phoneH * 0.07f;
            Rect tabBarRect = new Rect(phoneX, phoneY + statusBarH, phoneW, tabBarH);
            DrawTabBar(tabBarRect);

            // Content area
            float contentY = phoneY + statusBarH + tabBarH;
            float contentH = phoneH - statusBarH - tabBarH;
            Rect contentRect = new Rect(phoneX + 8, contentY + 4, phoneW - 16, contentH - 8);

            // Draw active tab content
            DrawTabContent(contentRect);

            // Phone frame border (subtle glow)
            DrawPhoneBorder(phoneRect);

            // Input hint at bottom of screen
            GUI.color = DIM_TEXT;
            GUI.Label(new Rect(phoneX, phoneY + phoneH + 5, phoneW, 20),
                "[M] Close   [1-5] Switch Tab   [ESC] Close", _bodyStyle);
            GUI.color = Color.white;
        }

        private void DrawStatusBar(Rect rect)
        {
            GUI.color = new Color(0.1f, 0.1f, 0.15f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Time (real system time as in-game phone time)
            string timeStr = System.DateTime.Now.ToString("h:mm tt");
            GUI.color = TEXT_COLOR;

            GUIStyle miniStyle = new GUIStyle(GUI.skin.label);
            miniStyle.fontSize = Mathf.RoundToInt(rect.height * 0.55f);
            miniStyle.alignment = TextAnchor.MiddleCenter;
            miniStyle.normal.textColor = TEXT_COLOR;

            GUI.Label(rect, $"CLOUT          {timeStr}          \u2588\u2588\u2588", miniStyle);
            GUI.color = Color.white;
        }

        private void DrawTabBar(Rect rect)
        {
            GUI.color = TAB_BG;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            float tabW = rect.width / _tabNames.Length;

            for (int i = 0; i < _tabNames.Length; i++)
            {
                Rect tabRect = new Rect(rect.x + i * tabW, rect.y, tabW, rect.height);
                bool isActive = (int)_currentTab == i;

                // Active tab highlight
                if (isActive)
                {
                    GUI.color = TAB_ACTIVE;
                    GUI.DrawTexture(tabRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                // Tab label
                GUIStyle tabLabelStyle = new GUIStyle(GUI.skin.label);
                tabLabelStyle.fontSize = Mathf.RoundToInt(rect.height * 0.32f);
                tabLabelStyle.alignment = TextAnchor.MiddleCenter;
                tabLabelStyle.normal.textColor = isActive ? Color.white : DIM_TEXT;
                tabLabelStyle.fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal;

                GUI.Label(tabRect, $"{_tabIcons[i]}\n{_tabNames[i]}", tabLabelStyle);

                // Notification badge on Messages tab
                if (i == 4 && _unreadMessages > 0)
                {
                    Rect badgeRect = new Rect(tabRect.xMax - 14, tabRect.y + 2, 12, 12);
                    GUI.color = ACCENT_RED;
                    GUI.DrawTexture(badgeRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;

                    GUIStyle bStyle = new GUIStyle(GUI.skin.label);
                    bStyle.fontSize = 8;
                    bStyle.alignment = TextAnchor.MiddleCenter;
                    bStyle.normal.textColor = Color.white;
                    GUI.Label(badgeRect, _unreadMessages > 9 ? "9+" : _unreadMessages.ToString(), bStyle);
                }

                // Click handler
                if (GUI.Button(tabRect, "", GUIStyle.none))
                {
                    _currentTab = (PhoneTab)i;
                    if (i == 4) _unreadMessages = 0; // Clear on open
                }
            }
        }

        private void DrawTabContent(Rect rect)
        {
            switch (_currentTab)
            {
                case PhoneTab.Map:
                    if (_mapTab != null) _mapTab.DrawTab(rect);
                    else DrawPlaceholder(rect, "MAP", "District map with territory overlay");
                    break;
                case PhoneTab.Contacts:
                    if (_contactsTab != null) _contactsTab.DrawTab(rect);
                    else DrawPlaceholder(rect, "CONTACTS", "Workers, suppliers, customers");
                    break;
                case PhoneTab.Products:
                    if (_productsTab != null) _productsTab.DrawTab(rect);
                    else DrawPlaceholder(rect, "PRODUCTS", "Inventory & pricing");
                    break;
                case PhoneTab.Finances:
                    if (_financeTab != null) _financeTab.DrawTab(rect);
                    else DrawPlaceholder(rect, "FINANCES", "Income, expenses, laundering");
                    break;
                case PhoneTab.Messages:
                    if (_messagesTab != null) _messagesTab.DrawTab(rect);
                    else DrawPlaceholder(rect, "MESSAGES", "Notifications & alerts");
                    break;
            }
        }

        private void DrawPlaceholder(Rect rect, string title, string desc)
        {
            GUILayout.BeginArea(rect);
            GUILayout.Space(20);
            GUI.color = TEXT_COLOR;
            GUILayout.Label(title, _titleStyle);
            GUI.color = DIM_TEXT;
            GUILayout.Label(desc, _bodyStyle);
            GUI.color = Color.white;
            GUILayout.EndArea();
        }

        private void DrawPhoneBorder(Rect rect)
        {
            float t = 2f;
            Color borderColor = new Color(0.25f, 0.3f, 0.45f, 0.5f);
            GUI.color = borderColor;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, t), Texture2D.whiteTexture); // Top
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - t, rect.width, t), Texture2D.whiteTexture); // Bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y, t, rect.height), Texture2D.whiteTexture); // Left
            GUI.DrawTexture(new Rect(rect.xMax - t, rect.y, t, rect.height), Texture2D.whiteTexture); // Right
            GUI.color = Color.white;
        }

        // ─── Style Initialization ────────────────────────────────

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = 18;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = TEXT_COLOR;
            _titleStyle.alignment = TextAnchor.MiddleLeft;

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 14;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = HEADER_COLOR;

            _bodyStyle = new GUIStyle(GUI.skin.label);
            _bodyStyle.fontSize = 12;
            _bodyStyle.normal.textColor = TEXT_COLOR;
            _bodyStyle.wordWrap = true;

            _valueStyle = new GUIStyle(GUI.skin.label);
            _valueStyle.fontSize = 14;
            _valueStyle.fontStyle = FontStyle.Bold;
            _valueStyle.normal.textColor = ACCENT_GREEN;
            _valueStyle.alignment = TextAnchor.MiddleRight;
        }
    }
}
