using UnityEngine;
using System;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Economy;

namespace Clout.UI.Economy
{
    /// <summary>
    /// Step 13 — Market intelligence dashboard.
    ///
    /// OnGUI overlay displaying:
    ///   - Per-product per-district price charts (sparkline)
    ///   - Demand/supply ratios with trend indicators
    ///   - Active market events with countdown
    ///   - Commodity prices (precursor tracker)
    ///   - Market manipulation panel
    ///   - Competition intelligence
    ///
    /// Toggle: M key. Phase 2-3 OnGUI; migrates to UI Toolkit in Phase 5.
    /// </summary>
    public class MarketAnalysisUI : MonoBehaviour
    {
        [Header("Toggle")]
        public KeyCode toggleKey = KeyCode.M;

        [Header("Layout")]
        public float windowWidth = 720f;
        public float windowHeight = 600f;

        // ─── State ──────────────────────────────────────────────────

        private bool _visible;
        private Vector2 _scrollPosition;
        private int _selectedTab;           // 0=Overview, 1=Commodities, 2=Events, 3=Manipulation
        private string _selectedProduct = "";
        private string _selectedDistrict = "";
        private ManipulationType _selectedTactic = ManipulationType.FloodMarket;
        private float _manipulationInput = 1000f;
        private string _lastManipulationResult = "";

        // Styles
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _tabStyle;
        private GUIStyle _tabActiveStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _sparklineStyle;
        private bool _stylesInitialized;

        private readonly string[] _tabNames = { "Market Overview", "Commodities", "Events", "Manipulation" };

        // ─── Lifecycle ──────────────────────────────────────────────

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                _visible = !_visible;
                if (_visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;
            InitStyles();

            float x = (Screen.width - windowWidth) / 2f;
            float y = (Screen.height - windowHeight) / 2f;
            Rect windowRect = new Rect(x, y, windowWidth, windowHeight);

            GUI.Box(windowRect, "", _windowStyle);
            GUILayout.BeginArea(new Rect(x + 15, y + 10, windowWidth - 30, windowHeight - 20));

            // Title
            GUILayout.Label("MARKET INTELLIGENCE", _headerStyle);
            GUILayout.Space(5);

            // Tabs
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _tabNames.Length; i++)
            {
                GUIStyle style = (i == _selectedTab) ? _tabActiveStyle : _tabStyle;
                if (GUILayout.Button(_tabNames[i], style, GUILayout.Height(28)))
                    _selectedTab = i;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            // Content
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0: DrawOverview(); break;
                case 1: DrawCommodities(); break;
                case 2: DrawEvents(); break;
                case 3: DrawManipulation(); break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ─── Tab: Market Overview ───────────────────────────────────

        private void DrawOverview()
        {
            var sim = MarketSimulator.Instance;
            if (sim == null)
            {
                GUILayout.Label("MarketSimulator not active.", _labelStyle);
                return;
            }

            GUILayout.Label($"Game Day: {sim.CurrentDay}", _subHeaderStyle);
            GUILayout.Space(5);

            GUILayout.Label("Per-Market Summary", _subHeaderStyle);
            GUILayout.Space(3);

            // Header row
            GUILayout.BeginHorizontal();
            GUILayout.Label("Product", _labelStyle, GUILayout.Width(120));
            GUILayout.Label("District", _labelStyle, GUILayout.Width(100));
            GUILayout.Label("Price", _labelStyle, GUILayout.Width(70));
            GUILayout.Label("D/S", _labelStyle, GUILayout.Width(50));
            GUILayout.Label("Trend", _labelStyle, GUILayout.Width(60));
            GUILayout.Label("Competition", _labelStyle, GUILayout.Width(80));
            GUILayout.Label("Sparkline", _labelStyle, GUILayout.Width(120));
            GUILayout.EndHorizontal();

            // Market rows
            foreach (var kvp in sim.AllMarkets)
            {
                var m = kvp.Value;
                float totalSupply = m.playerSupply + m.rivalSupply + m.importSupply;
                float dsRatio = totalSupply > 0 ? m.currentDemand / totalSupply : 99f;
                float trend = sim.GetPriceTrend(m.productId, m.districtId, 7);
                string trendArrow = trend > 0.05f ? "^" : (trend < -0.05f ? "v" : "-");
                Color trendColor = trend > 0.05f ? Color.green :
                    (trend < -0.05f ? Color.red : Color.gray);

                GUILayout.BeginHorizontal();
                GUILayout.Label(Truncate(m.productId, 14), _labelStyle, GUILayout.Width(120));
                GUILayout.Label(Truncate(m.districtId, 12), _labelStyle, GUILayout.Width(100));
                GUILayout.Label($"${m.currentPrice:F0}", _valueStyle, GUILayout.Width(70));
                GUILayout.Label($"{dsRatio:F1}", _labelStyle, GUILayout.Width(50));

                var oldColor = GUI.contentColor;
                GUI.contentColor = trendColor;
                GUILayout.Label($"{trendArrow} {trend * 100:F0}%", _labelStyle, GUILayout.Width(60));
                GUI.contentColor = oldColor;

                string compStr = m.competitorPressure >= 0.9f ? "Monopoly" :
                    (m.competitorPressure >= 0.7f ? "Low" :
                    (m.competitorPressure >= 0.5f ? "Medium" : "Heavy"));
                GUILayout.Label(compStr, _labelStyle, GUILayout.Width(80));

                // Sparkline
                DrawSparkline(m.priceHistory, 120, 20);

                GUILayout.EndHorizontal();
            }
        }

        // ─── Tab: Commodities ───────────────────────────────────────

        private void DrawCommodities()
        {
            var tracker = CommodityTracker.Instance;
            if (tracker == null)
            {
                GUILayout.Label("CommodityTracker not active.", _labelStyle);
                return;
            }

            GUILayout.Label("Precursor Commodity Prices", _subHeaderStyle);
            GUILayout.Space(5);

            // Header
            GUILayout.BeginHorizontal();
            GUILayout.Label("Commodity", _labelStyle, GUILayout.Width(140));
            GUILayout.Label("Price", _labelStyle, GUILayout.Width(70));
            GUILayout.Label("Base", _labelStyle, GUILayout.Width(60));
            GUILayout.Label("Change", _labelStyle, GUILayout.Width(70));
            GUILayout.Label("7d Trend", _labelStyle, GUILayout.Width(70));
            GUILayout.Label("Chart", _labelStyle, GUILayout.Width(140));
            GUILayout.EndHorizontal();

            foreach (var kvp in tracker.Commodities)
            {
                var state = kvp.Value;
                float pctFromBase = (state.currentPrice - state.basePrice) / state.basePrice;
                float trend7d = tracker.GetTrend(kvp.Key, 7);
                Color changeColor = state.dailyChange >= 0 ? Color.green : Color.red;
                Color trendColor = trend7d >= 0 ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);

                GUILayout.BeginHorizontal();
                GUILayout.Label(state.displayName, _labelStyle, GUILayout.Width(140));
                GUILayout.Label($"${state.currentPrice:F1}", _valueStyle, GUILayout.Width(70));
                GUILayout.Label($"${state.basePrice:F0}", _labelStyle, GUILayout.Width(60));

                var oldColor = GUI.contentColor;
                GUI.contentColor = changeColor;
                GUILayout.Label($"{(state.dailyChange >= 0 ? "+" : "")}{state.dailyChange:F1}",
                    _labelStyle, GUILayout.Width(70));

                GUI.contentColor = trendColor;
                GUILayout.Label($"{(trend7d >= 0 ? "+" : "")}{trend7d * 100:F1}%",
                    _labelStyle, GUILayout.Width(70));
                GUI.contentColor = oldColor;

                // Sparkline from price history
                var history = tracker.GetPriceHistory(kvp.Key);
                if (history != null)
                    DrawSparkline(history, 140, 20);
                else
                    GUILayout.Label("—", _labelStyle, GUILayout.Width(140));

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Label("Note: Commodity prices affect your production costs.", _labelStyle);
            GUILayout.Label("Stockpile cheap precursors to protect margins.", _labelStyle);
        }

        // ─── Tab: Events ────────────────────────────────────────────

        private void DrawEvents()
        {
            var sim = MarketSimulator.Instance;
            if (sim == null)
            {
                GUILayout.Label("MarketSimulator not active.", _labelStyle);
                return;
            }

            GUILayout.Label("Active Market Events", _subHeaderStyle);
            GUILayout.Space(5);

            if (sim.ActiveEvents.Count == 0)
            {
                GUILayout.Label("  No active market events.", _labelStyle);
            }
            else
            {
                foreach (var evt in sim.ActiveEvents)
                {
                    var def = evt.definition;
                    Color evtColor = def.priceMultiplier > 1f ?
                        new Color(1f, 0.7f, 0.3f) : new Color(0.3f, 0.7f, 1f);

                    GUILayout.BeginVertical("box");

                    var oldColor = GUI.contentColor;
                    GUI.contentColor = evtColor;
                    GUILayout.Label($"{def.eventName} ({def.eventType})", _subHeaderStyle);
                    GUI.contentColor = oldColor;

                    GUILayout.Label($"  {def.description}", _labelStyle);
                    GUILayout.Label($"  Remaining: {evt.remainingDays} / {def.durationDays} days " +
                        $"(intensity: {def.GetIntensity(evt.Progress) * 100:F0}%)", _labelStyle);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Price: x{evt.EffectivePriceMultiplier:F2}", _labelStyle);
                    GUILayout.Label($"  Demand: x{evt.EffectiveDemandMultiplier:F2}", _labelStyle);
                    GUILayout.Label($"  Supply: x{evt.EffectiveSupplyMultiplier:F2}", _labelStyle);
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                    GUILayout.Space(3);
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Event Probability (daily)", _subHeaderStyle);
            if (sim.ActiveEvents.Count < 3)
            {
                GUILayout.Label("  Events roll each game day based on probability.", _labelStyle);
                GUILayout.Label("  High-impact events are rare but game-changing.", _labelStyle);
            }
        }

        // ─── Tab: Manipulation ──────────────────────────────────────

        private void DrawManipulation()
        {
            var sim = MarketSimulator.Instance;
            if (sim == null)
            {
                GUILayout.Label("MarketSimulator not active.", _labelStyle);
                return;
            }

            GUILayout.Label("Market Manipulation", _subHeaderStyle);
            GUILayout.Space(5);

            // Tactic selection
            GUILayout.Label("Select Tactic:", _labelStyle);
            GUILayout.BeginHorizontal();
            foreach (ManipulationType tactic in Enum.GetValues(typeof(ManipulationType)))
            {
                GUIStyle style = (tactic == _selectedTactic) ? _tabActiveStyle : _tabStyle;
                if (GUILayout.Button(tactic.ToString(), style, GUILayout.Height(24)))
                    _selectedTactic = tactic;
            }
            GUILayout.EndHorizontal();

            // Profile
            var profile = MarketManipulation.GetProfile(_selectedTactic);
            GUILayout.Space(5);
            GUILayout.Label($"  {profile.description}", _labelStyle);
            GUILayout.Label($"  Duration: {profile.estimatedDuration}", _labelStyle);
            GUILayout.Label($"  Risk: {profile.riskLevel}", _labelStyle);
            GUILayout.Label($"  Heat: {profile.heatGenerated}", _labelStyle);
            if (profile.minCost > 0)
                GUILayout.Label($"  Min Investment: ${profile.minCost:F0}", _labelStyle);

            GUILayout.Space(8);

            // Target selection
            GUILayout.Label("Target Product:", _labelStyle);
            _selectedProduct = GUILayout.TextField(_selectedProduct, GUILayout.Width(200));

            GUILayout.Label("Target District:", _labelStyle);
            _selectedDistrict = GUILayout.TextField(_selectedDistrict, GUILayout.Width(200));

            if (profile.requiresCash || profile.minCost > 0)
            {
                GUILayout.Label("Investment Amount:", _labelStyle);
                string inputStr = GUILayout.TextField(_manipulationInput.ToString("F0"), GUILayout.Width(120));
                float.TryParse(inputStr, out _manipulationInput);
            }

            GUILayout.Space(5);

            // Execute button
            if (GUILayout.Button("EXECUTE TACTIC", _buttonStyle, GUILayout.Height(30), GUILayout.Width(200)))
            {
                if (string.IsNullOrEmpty(_selectedProduct) || string.IsNullOrEmpty(_selectedDistrict))
                {
                    _lastManipulationResult = "Enter both product and district.";
                }
                else
                {
                    var result = MarketManipulation.Execute(
                        _selectedTactic, _selectedProduct, _selectedDistrict, _manipulationInput);
                    _lastManipulationResult = result.message;
                }
            }

            // Result
            if (!string.IsNullOrEmpty(_lastManipulationResult))
            {
                GUILayout.Space(5);
                GUILayout.Label(_lastManipulationResult, _valueStyle);
            }
        }

        // ─── Sparkline Renderer ─────────────────────────────────────

        private void DrawSparkline(IReadOnlyList<float> data, float width, float height)
        {
            if (data == null || data.Count < 2)
            {
                GUILayout.Label("—", _labelStyle, GUILayout.Width(width));
                return;
            }

            Rect rect = GUILayoutUtility.GetRect(width, height);

            // Find min/max
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i] < min) min = data[i];
                if (data[i] > max) max = data[i];
            }
            float range = Mathf.Max(0.01f, max - min);

            // Background
            GUI.DrawTexture(rect, Texture2D.blackTexture);

            // Line
            Color lineColor = data[data.Count - 1] >= data[0] ?
                new Color(0.2f, 0.9f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);

            float stepX = rect.width / (data.Count - 1);
            for (int i = 0; i < data.Count - 1; i++)
            {
                float x1 = rect.x + i * stepX;
                float y1 = rect.y + rect.height - ((data[i] - min) / range) * rect.height;
                float x2 = rect.x + (i + 1) * stepX;
                float y2 = rect.y + rect.height - ((data[i + 1] - min) / range) * rect.height;

                DrawLine(x1, y1, x2, y2, lineColor);
            }
        }

        private static void DrawLine(float x1, float y1, float x2, float y2, Color color)
        {
            // Simple 1px line via GUI.DrawTexture rotation
            float dx = x2 - x1;
            float dy = y2 - y1;
            float length = Mathf.Sqrt(dx * dx + dy * dy);
            if (length < 0.5f) return;

            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

            Texture2D tex = Texture2D.whiteTexture;
            var oldColor = GUI.color;
            GUI.color = color;

            var pivot = new Vector2(x1, y1);
            var matrixBackup = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, pivot);
            GUI.DrawTexture(new Rect(x1, y1 - 0.5f, length, 1f), tex);
            GUI.matrix = matrixBackup;
            GUI.color = oldColor;
        }

        // ─── Styles ─────────────────────────────────────────────────

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _windowStyle = new GUIStyle(GUI.skin.box);
            _windowStyle.normal.background = MakeTex(2, 2, new Color(0.08f, 0.08f, 0.12f, 0.96f));

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 0.9f, 0.4f) },
                alignment = TextAnchor.MiddleCenter
            };

            _subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.8f, 0.8f, 0.2f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.75f, 0.75f, 0.75f) }
            };

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 1f, 0.5f) }
            };

            _tabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                normal = { textColor = Color.gray }
            };

            _tabActiveStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11, fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _tabActiveStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.5f, 0.3f, 0.8f));

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12, fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _buttonStyle.normal.background = MakeTex(2, 2, new Color(0.6f, 0.2f, 0.2f, 0.9f));
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static string Truncate(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= maxLen ? s : s.Substring(0, maxLen - 2) + "..";
        }
    }
}
