using UnityEngine;
using UnityEngine.UI;
using Clout.Core;
using Clout.Combat;
using Clout.Stats;
using Clout.Player;
using Clout.Empire.Reputation;
using Clout.World.Police;

namespace Clout.UI
{
    /// <summary>
    /// Combat HUD — real-time display of health, stamina, CLOUT, ammo, and wanted level.
    ///
    /// Built programmatically at runtime — no prefab dependencies.
    /// Designed for Phase 1 test arena, will evolve into full production HUD.
    ///
    /// Layout:
    /// ┌──────────────────────────────────────────────┐
    /// │  [CLOUT: 0]  [Corner Boy]      [★☆☆☆☆]     │  ← Top bar
    /// │                                              │
    /// │                    +                         │  ← Crosshair (when aiming)
    /// │                                              │
    /// │                                              │
    /// │  ████████ HP: 100/100     AMMO: 30/96       │  ← Bottom bar
    /// │  ████████ ST: 100/100     [PISTOL]          │
    /// │  [STATE: locomotion]                         │
    /// └──────────────────────────────────────────────┘
    /// </summary>
    public class CombatHUD : MonoBehaviour
    {
        [Header("References")]
        public PlayerStateManager playerStateManager;
        public RuntimeStats runtimeStats;
        public ReputationManager reputationManager;
        public WantedSystem wantedSystem;
        public AmmoCacheManager ammoCacheManager;

        // Canvas
        private Canvas _canvas;
        private CanvasScaler _scaler;

        // Health & Stamina
        private Image _healthBar;
        private Image _healthBarBG;
        private Image _staminaBar;
        private Image _staminaBarBG;
        private Text _healthText;
        private Text _staminaText;

        // Clout
        private Text _cloutScoreText;
        private Text _cloutRankText;

        // Wanted Level
        private Text _wantedText;
        private Image[] _wantedStars = new Image[5];

        // Ammo
        private Text _ammoText;
        private Text _weaponNameText;
        private GameObject _ammoPanel;

        // Crosshair
        private Image _crosshair;

        // State debug
        private Text _stateDebugText;

        // Colors
        private static readonly Color COLOR_HEALTH = new Color(0.85f, 0.15f, 0.15f);
        private static readonly Color COLOR_HEALTH_LOW = new Color(1f, 0.3f, 0.1f);
        private static readonly Color COLOR_STAMINA = new Color(0.2f, 0.75f, 0.3f);
        private static readonly Color COLOR_STAMINA_LOW = new Color(0.9f, 0.7f, 0.1f);
        private static readonly Color COLOR_CLOUT = new Color(1f, 0.84f, 0f); // Gold
        private static readonly Color COLOR_WANTED_ACTIVE = new Color(1f, 0.2f, 0.2f);
        private static readonly Color COLOR_WANTED_INACTIVE = new Color(0.3f, 0.3f, 0.3f);
        private static readonly Color COLOR_BAR_BG = new Color(0.15f, 0.15f, 0.15f, 0.7f);
        private static readonly Color COLOR_PANEL_BG = new Color(0.05f, 0.05f, 0.08f, 0.6f);

        private void Awake()
        {
            BuildCanvas();
            BuildHealthStaminaPanel();
            BuildCloutPanel();
            BuildWantedPanel();
            BuildAmmoPanel();
            BuildCrosshair();
            BuildStateDebug();
        }

        private void OnEnable()
        {
            if (runtimeStats != null)
            {
                runtimeStats.OnHealthChanged += RefreshHealth;
                runtimeStats.OnStaminaChanged += RefreshStamina;
            }

            if (reputationManager != null)
            {
                reputationManager.OnCloutChanged += OnCloutChanged;
                reputationManager.OnRankUp += OnRankUp;
            }

            if (wantedSystem != null)
            {
                wantedSystem.OnWantedLevelChanged += OnWantedChanged;
                wantedSystem.OnHeatChanged += OnHeatChanged;
            }
        }

        private void OnDisable()
        {
            if (runtimeStats != null)
            {
                runtimeStats.OnHealthChanged -= RefreshHealth;
                runtimeStats.OnStaminaChanged -= RefreshStamina;
            }

            if (reputationManager != null)
            {
                reputationManager.OnCloutChanged -= OnCloutChanged;
                reputationManager.OnRankUp -= OnRankUp;
            }

            if (wantedSystem != null)
            {
                wantedSystem.OnWantedLevelChanged -= OnWantedChanged;
                wantedSystem.OnHeatChanged -= OnHeatChanged;
            }
        }

        private void Update()
        {
            RefreshHealth();
            RefreshStamina();
            RefreshAmmo();
            RefreshCrosshair();
            RefreshStateDebug();
        }

        // ─────────────────────────────────────────────────────────
        //  BUILD UI
        // ─────────────────────────────────────────────────────────

        private void BuildCanvas()
        {
            // Canvas
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            _scaler = gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
        }

        private void BuildHealthStaminaPanel()
        {
            // Container — bottom left
            RectTransform panel = CreatePanel("HealthStaminaPanel",
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(20f, 20f), new Vector2(320f, 90f));

            // Health bar background
            _healthBarBG = CreateBar(panel, "HealthBarBG", COLOR_BAR_BG,
                new Vector2(0f, 0.55f), new Vector2(0.7f, 0.95f));

            // Health bar fill
            _healthBar = CreateBar(panel, "HealthBar", COLOR_HEALTH,
                new Vector2(0f, 0.55f), new Vector2(0.7f, 0.95f));

            // Health text
            _healthText = CreateText(panel, "HealthText", "HP: 100/100", 14,
                new Vector2(0.72f, 0.55f), new Vector2(1f, 0.95f));

            // Stamina bar background
            _staminaBarBG = CreateBar(panel, "StaminaBarBG", COLOR_BAR_BG,
                new Vector2(0f, 0.05f), new Vector2(0.7f, 0.45f));

            // Stamina bar fill
            _staminaBar = CreateBar(panel, "StaminaBar", COLOR_STAMINA,
                new Vector2(0f, 0.05f), new Vector2(0.7f, 0.45f));

            // Stamina text
            _staminaText = CreateText(panel, "StaminaText", "ST: 100/100", 14,
                new Vector2(0.72f, 0.05f), new Vector2(1f, 0.45f));
        }

        private void BuildCloutPanel()
        {
            // Container — top left
            RectTransform panel = CreatePanel("CloutPanel",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -70f), new Vector2(350f, 60f));

            // Clout score
            _cloutScoreText = CreateText(panel, "CloutScore", "CLOUT: 0", 22,
                new Vector2(0f, 0.5f), new Vector2(0.5f, 1f));
            _cloutScoreText.color = COLOR_CLOUT;
            _cloutScoreText.fontStyle = FontStyle.Bold;

            // Rank name
            _cloutRankText = CreateText(panel, "CloutRank", "Nobody", 16,
                new Vector2(0f, 0f), new Vector2(0.5f, 0.5f));
            _cloutRankText.color = new Color(0.7f, 0.7f, 0.7f);
        }

        private void BuildWantedPanel()
        {
            // Container — top right
            RectTransform panel = CreatePanel("WantedPanel",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-220f, -70f), new Vector2(200f, 60f));

            _wantedText = CreateText(panel, "WantedLabel", "WANTED", 14,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f));
            _wantedText.alignment = TextAnchor.MiddleCenter;
            _wantedText.color = COLOR_WANTED_INACTIVE;

            // 5 stars
            float starWidth = 1f / 5f;
            for (int i = 0; i < 5; i++)
            {
                GameObject starObj = new GameObject($"Star_{i}");
                starObj.transform.SetParent(panel);
                RectTransform starRect = starObj.AddComponent<RectTransform>();
                starRect.anchorMin = new Vector2(i * starWidth + 0.05f, 0.05f);
                starRect.anchorMax = new Vector2((i + 1) * starWidth - 0.05f, 0.45f);
                starRect.offsetMin = Vector2.zero;
                starRect.offsetMax = Vector2.zero;

                _wantedStars[i] = starObj.AddComponent<Image>();
                _wantedStars[i].color = COLOR_WANTED_INACTIVE;
            }
        }

        private void BuildAmmoPanel()
        {
            // Container — bottom right
            _ammoPanel = new GameObject("AmmoPanel");
            _ammoPanel.transform.SetParent(transform);
            RectTransform panel = _ammoPanel.AddComponent<RectTransform>();
            panel.anchorMin = new Vector2(1f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(1f, 0f);
            panel.anchoredPosition = new Vector2(-20f, 20f);
            panel.sizeDelta = new Vector2(250f, 80f);

            Image bg = _ammoPanel.AddComponent<Image>();
            bg.color = COLOR_PANEL_BG;

            // Ammo count
            _ammoText = CreateText(panel, "AmmoCount", "30 / 96", 24,
                new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.95f));
            _ammoText.alignment = TextAnchor.MiddleCenter;
            _ammoText.fontStyle = FontStyle.Bold;

            // Weapon name
            _weaponNameText = CreateText(panel, "WeaponName", "UNARMED", 14,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.4f));
            _weaponNameText.alignment = TextAnchor.MiddleCenter;
            _weaponNameText.color = new Color(0.6f, 0.6f, 0.6f);
        }

        private void BuildCrosshair()
        {
            GameObject crosshairObj = new GameObject("Crosshair");
            crosshairObj.transform.SetParent(transform);

            RectTransform rect = crosshairObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(4f, 4f);

            _crosshair = crosshairObj.AddComponent<Image>();
            _crosshair.color = new Color(1f, 1f, 1f, 0.8f);
            _crosshair.enabled = false; // Only show when aiming
        }

        private void BuildStateDebug()
        {
            RectTransform panel = CreatePanel("StateDebug",
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(20f, 115f), new Vector2(300f, 30f));

            _stateDebugText = CreateText(panel.GetComponent<RectTransform>(), "StateText",
                "STATE: ---", 12,
                new Vector2(0f, 0f), new Vector2(1f, 1f));
            _stateDebugText.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            _stateDebugText.fontStyle = FontStyle.Italic;
        }

        // ─────────────────────────────────────────────────────────
        //  REFRESH
        // ─────────────────────────────────────────────────────────

        private void RefreshHealth()
        {
            if (runtimeStats == null || _healthBar == null) return;

            float ratio = runtimeStats.health / (float)runtimeStats.maxHealth;
            _healthBar.rectTransform.anchorMax = new Vector2(
                Mathf.Lerp(_healthBar.rectTransform.anchorMin.x, _healthBarBG.rectTransform.anchorMax.x, ratio),
                _healthBar.rectTransform.anchorMax.y);

            _healthBar.color = ratio < 0.3f ? COLOR_HEALTH_LOW : COLOR_HEALTH;
            _healthText.text = $"HP: {runtimeStats.health}/{runtimeStats.maxHealth}";
        }

        private void RefreshStamina()
        {
            if (runtimeStats == null || _staminaBar == null) return;

            float ratio = runtimeStats.stamina / runtimeStats.maxStamina;
            _staminaBar.rectTransform.anchorMax = new Vector2(
                Mathf.Lerp(_staminaBar.rectTransform.anchorMin.x, _staminaBarBG.rectTransform.anchorMax.x, ratio),
                _staminaBar.rectTransform.anchorMax.y);

            _staminaBar.color = ratio < 0.25f ? COLOR_STAMINA_LOW : COLOR_STAMINA;
            _staminaText.text = $"ST: {Mathf.CeilToInt(runtimeStats.stamina)}/{Mathf.CeilToInt(runtimeStats.maxStamina)}";
        }

        private void RefreshAmmo()
        {
            if (playerStateManager == null || _ammoPanel == null) return;

            WeaponItem weapon = playerStateManager.weaponHolderManager?.rightItem;

            if (weapon == null)
            {
                _ammoText.text = "—";
                _weaponNameText.text = "UNARMED";
                return;
            }

            _weaponNameText.text = string.IsNullOrEmpty(weapon.itemName)
                ? weapon.weaponType.ToString().ToUpper()
                : weapon.itemName.ToUpper();

            if (weapon.HasRangedCapability && weapon.rangedWeaponHook != null)
            {
                RangedWeaponHook hook = weapon.rangedWeaponHook;
                int reserve = 0;

                if (weapon is RangedWeaponItem rangedItem && ammoCacheManager != null)
                    reserve = ammoCacheManager.GetAmmoCount(rangedItem.ammoType);

                _ammoText.text = $"{hook.CurrentAmmo} / {reserve}";
                _ammoText.color = hook.CurrentAmmo <= 0 ? Color.red : Color.white;
            }
            else
            {
                _ammoText.text = "∞";
                _ammoText.color = Color.white;
            }
        }

        private void RefreshCrosshair()
        {
            if (_crosshair == null || playerStateManager == null) return;

            bool showCrosshair = playerStateManager.isAiming ||
                (playerStateManager.weaponHolderManager?.rightItem?.HasRangedCapability ?? false);
            _crosshair.enabled = showCrosshair;

            if (showCrosshair)
            {
                float size = playerStateManager.isAiming ? 3f : 5f;
                _crosshair.rectTransform.sizeDelta = new Vector2(size, size);
                _crosshair.color = playerStateManager.isAiming
                    ? new Color(1f, 0.3f, 0.3f, 0.9f)
                    : new Color(1f, 1f, 1f, 0.5f);
            }
        }

        private void RefreshStateDebug()
        {
            if (_stateDebugText == null || playerStateManager == null) return;
            string stateId = playerStateManager.CurrentStateId ?? "none";
            _stateDebugText.text = $"STATE: {stateId}";
        }

        // ─────────────────────────────────────────────────────────
        //  EVENT CALLBACKS
        // ─────────────────────────────────────────────────────────

        private void OnCloutChanged(int oldClout, int newClout)
        {
            if (_cloutScoreText != null)
                _cloutScoreText.text = $"CLOUT: {newClout:N0}";
        }

        private void OnRankUp(int newRank, string rankName)
        {
            if (_cloutRankText != null)
                _cloutRankText.text = rankName;
        }

        private void OnWantedChanged(WantedLevel level)
        {
            if (_wantedText == null) return;

            int stars = (int)level;
            _wantedText.color = stars > 0 ? COLOR_WANTED_ACTIVE : COLOR_WANTED_INACTIVE;
            _wantedText.text = stars > 0 ? $"WANTED ★{stars}" : "CLEAN";

            for (int i = 0; i < _wantedStars.Length; i++)
            {
                if (_wantedStars[i] != null)
                    _wantedStars[i].color = i < stars ? COLOR_WANTED_ACTIVE : COLOR_WANTED_INACTIVE;
            }
        }

        private void OnHeatChanged(float heat) { }

        // ─────────────────────────────────────────────────────────
        //  UI BUILDERS
        // ─────────────────────────────────────────────────────────

        private RectTransform CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image bg = obj.AddComponent<Image>();
            bg.color = COLOR_PANEL_BG;

            return rect;
        }

        private Image CreateBar(RectTransform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private Text CreateText(RectTransform parent, string name, string defaultText,
            int fontSize, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(4f, 0f);
            rect.offsetMax = new Vector2(-4f, 0f);

            Text txt = obj.AddComponent<Text>();
            txt.text = defaultText;
            txt.fontSize = fontSize;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            return txt;
        }
    }
}
