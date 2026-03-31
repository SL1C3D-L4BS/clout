using UnityEngine;
using Clout.Empire.Crafting;
using Clout.Empire.Dealing;
using Clout.Player;
using System.Collections.Generic;

namespace Clout.UI.Production
{
    /// <summary>
    /// Full production UI — recipe browser, ingredient status, batch progress, quality preview.
    ///
    /// OnGUI-based for rapid iteration (Phase 2). Will be replaced with
    /// proper uGUI/UIToolkit in Phase 5.
    ///
    /// Shows:
    /// - Recipe list with craftability indicators
    /// - Ingredient requirements with have/need counts
    /// - Quality preview with additive breakdown
    /// - Active batch progress bars
    /// - Profit margin estimates
    /// - Risk warnings
    ///
    /// Subscribes to CraftingStation.OnStationOpened/OnStationClosed events.
    /// </summary>
    public class CraftingUI : MonoBehaviour
    {
        [Header("References")]
        public ProductionManager productionManager;

        // ─── State ───────────────────────────────────────────────

        private bool _isOpen;
        private CraftingStation _currentStation;
        private PlayerStateManager _currentPlayer;
        private IngredientInventory _ingredientInv;
        private ProductInventory _productInv;

        // Selection
        private int _selectedRecipeIndex = -1;
        private RecipeDefinition _selectedRecipe;
        private bool _useAdditives = true;

        // Scroll
        private Vector2 _recipeScroll;
        private Vector2 _ingredientScroll;
        private Vector2 _batchScroll;

        // Layout
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _boxStyle;
        private bool _stylesInit;

        // ─── Font Safety ─────────────────────────────────────────

        private static Font _safeFont;

        private static Font GetSafeFont()
        {
            if (_safeFont != null) return _safeFont;
            _safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_safeFont == null)
            {
                try { _safeFont = Font.CreateDynamicFontFromOSFont("Arial", 14); }
                catch { }
            }
            if (_safeFont == null)
            {
                string[] fonts = Font.GetOSInstalledFontNames();
                if (fonts.Length > 0)
                    _safeFont = Font.CreateDynamicFontFromOSFont(fonts[0], 14);
            }
            return _safeFont;
        }

        // ─── Setup ───────────────────────────────────────────────

        private void Start()
        {
            // Auto-find production manager
            if (productionManager == null)
                productionManager = ProductionManager.Instance;

            // Subscribe to all station events in scene
            var stations = FindObjectsByType<CraftingStation>(FindObjectsInactive.Exclude);
            foreach (var station in stations)
            {
                station.OnStationOpened += Open;
                station.OnStationClosed += Close;
            }
        }

        public void Open(CraftingStation station, PlayerStateManager player)
        {
            _currentStation = station;
            _currentPlayer = player;
            _ingredientInv = player.GetComponent<IngredientInventory>();
            _productInv = player.GetComponent<ProductInventory>();
            _selectedRecipeIndex = -1;
            _selectedRecipe = null;
            _isOpen = true;
        }

        public void Close()
        {
            _isOpen = false;
            _currentStation = null;
            _currentPlayer = null;
        }

        // ─── Init Styles ─────────────────────────────────────────

        private void InitStyles()
        {
            if (_stylesInit) return;
            _stylesInit = true;

            Font font = GetSafeFont();

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                font = font
            };
            _headerStyle.normal.textColor = Color.white;

            _subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                font = font
            };
            _subHeaderStyle.normal.textColor = new Color(0.9f, 0.8f, 0.3f);

            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, font = font };
            _labelStyle.normal.textColor = Color.white;

            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, font = font };

            _boxStyle = new GUIStyle(GUI.skin.box) { font = font };
        }

        // ─── OnGUI ───────────────────────────────────────────────

        private void OnGUI()
        {
            if (!_isOpen || _currentStation == null) return;

            InitStyles();

            float panelW = 700f;
            float panelH = 520f;
            float x = (Screen.width - panelW) / 2f;
            float y = (Screen.height - panelH) / 2f;
            Rect panelRect = new Rect(x, y, panelW, panelH);

            // Background
            GUI.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            GUI.Box(panelRect, "");
            GUI.color = Color.white;

            GUILayout.BeginArea(panelRect);
            GUILayout.Space(8);

            // ── Header ──
            GUILayout.Label($"\u2697 {_currentStation.stationName}", _headerStyle);
            GUILayout.Space(4);

            // ── Main layout: recipes left, details right ──
            GUILayout.BeginHorizontal();

            // LEFT: Recipe list
            DrawRecipeList(panelW * 0.38f, panelH - 100f);

            GUILayout.Space(8);

            // RIGHT: Details + actions
            DrawDetailsPanel(panelW * 0.58f, panelH - 100f);

            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // ── Active Batches ──
            DrawActiveBatches();

            GUILayout.Space(4);

            // ── Close button ──
            if (GUILayout.Button("Close [ESC]", _buttonStyle, GUILayout.Height(28)))
            {
                _currentStation.CloseStation();
            }

            GUILayout.EndArea();
        }

        // ─── Recipe List ─────────────────────────────────────────

        private void DrawRecipeList(float width, float height)
        {
            GUILayout.BeginVertical(GUILayout.Width(width));

            GUILayout.Label("RECIPES", _subHeaderStyle);

            _recipeScroll = GUILayout.BeginScrollView(_recipeScroll,
                GUILayout.Width(width), GUILayout.Height(height - 40f));

            if (_currentStation.availableRecipes != null)
            {
                for (int i = 0; i < _currentStation.availableRecipes.Length; i++)
                {
                    RecipeDefinition recipe = _currentStation.availableRecipes[i];
                    if (recipe == null) continue;

                    bool canCraft = _ingredientInv != null &&
                                    _ingredientInv.HasIngredientsForRecipe(recipe);
                    bool isSelected = (i == _selectedRecipeIndex);

                    // Recipe button
                    GUI.color = isSelected ? new Color(0.3f, 0.6f, 1f) :
                                canCraft ? new Color(0.2f, 0.8f, 0.3f, 0.8f) :
                                new Color(0.5f, 0.3f, 0.3f, 0.8f);

                    string status = canCraft ? "\u2713" : "\u2717";
                    string label = $"{status} {recipe.recipeName}";

                    if (recipe.outputProduct != null)
                        label += $"\n   \u2192 {recipe.outputQuantity}x {recipe.outputProduct.productName}";

                    if (GUILayout.Button(label, _buttonStyle, GUILayout.Height(45)))
                    {
                        _selectedRecipeIndex = i;
                        _selectedRecipe = recipe;
                    }
                    GUI.color = Color.white;
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        // ─── Details Panel ───────────────────────────────────────

        private void DrawDetailsPanel(float width, float height)
        {
            GUILayout.BeginVertical(GUILayout.Width(width));

            if (_selectedRecipe == null)
            {
                GUILayout.Label("Select a recipe to view details.", _labelStyle);
                GUILayout.EndVertical();
                return;
            }

            RecipeDefinition recipe = _selectedRecipe;

            // Recipe name + description
            GUILayout.Label(recipe.recipeName, _subHeaderStyle);
            if (!string.IsNullOrEmpty(recipe.description))
            {
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                GUILayout.Label(recipe.description, _labelStyle);
                GUI.color = Color.white;
            }

            GUILayout.Space(6);

            // ── Ingredients Required ──
            GUILayout.Label("INGREDIENTS REQUIRED:", _subHeaderStyle);

            _ingredientScroll = GUILayout.BeginScrollView(_ingredientScroll,
                GUILayout.Height(100f));

            if (recipe.ingredients != null)
            {
                foreach (var slot in recipe.ingredients)
                {
                    if (slot.ingredient == null) continue;

                    int has = _ingredientInv != null ?
                        _ingredientInv.GetCount(slot.ingredient.ingredientName) : 0;
                    bool enough = has >= slot.quantity;

                    GUI.color = enough ? Color.green : Color.red;
                    GUILayout.Label(
                        $"  {slot.ingredient.ingredientName}: {has}/{slot.quantity} " +
                        $"(${slot.ingredient.basePurchasePrice:F0}/ea)",
                        _labelStyle);
                    GUI.color = Color.white;
                }
            }

            // Optional additives
            if (recipe.optionalAdditives != null && recipe.optionalAdditives.Length > 0)
            {
                GUILayout.Space(4);
                GUI.color = new Color(0.6f, 0.6f, 0.9f);
                GUILayout.Label("  OPTIONAL ADDITIVES:", _labelStyle);
                GUI.color = Color.white;

                foreach (var slot in recipe.optionalAdditives)
                {
                    if (slot.ingredient == null) continue;

                    int has = _ingredientInv != null ?
                        _ingredientInv.GetCount(slot.ingredient.ingredientName) : 0;
                    bool enough = has >= slot.quantity;

                    GUI.color = enough ? new Color(0.5f, 0.8f, 1f) : new Color(0.5f, 0.5f, 0.5f);
                    string effectSummary = GetAdditiveEffectSummary(slot.ingredient);
                    GUILayout.Label(
                        $"  {slot.ingredient.ingredientName}: {has}/{slot.quantity} {effectSummary}",
                        _labelStyle);
                    GUI.color = Color.white;
                }
            }

            GUILayout.EndScrollView();
            GUILayout.Space(6);

            // ── Quality Preview ──
            float previewQuality = _currentStation.PreviewQuality(recipe, null);
            string qualityTier = "Standard";
            Color qualityColor = Color.white;

            if (recipe.outputProduct != null && recipe.outputProduct.qualityTiers != null)
            {
                foreach (var tier in recipe.outputProduct.qualityTiers)
                {
                    if (previewQuality >= tier.minQuality)
                    {
                        qualityTier = tier.tierName;
                        qualityColor = tier.tierColor;
                    }
                }
            }

            GUI.color = qualityColor;
            GUILayout.Label($"Expected Quality: {previewQuality:P0} ({qualityTier})", _labelStyle);
            GUI.color = Color.white;

            // ── Economics ──
            if (recipe.outputProduct != null && productionManager != null)
            {
                float cost = productionManager.EstimateIngredientCost(recipe);
                float profit = productionManager.EstimateProfitMargin(recipe, previewQuality);

                GUI.color = profit > 0 ? Color.green : Color.red;
                GUILayout.Label(
                    $"Cost: ${cost:F0}  |  Est. Revenue: ${cost + profit:F0}  |  Profit: ${profit:F0}",
                    _labelStyle);
                GUI.color = Color.white;
            }

            // ── Time + Risk ──
            float duration = recipe.craftingTime / _currentStation.speedMultiplier;
            GUILayout.Label($"Cook Time: {duration:F0}s  |  Output: {recipe.outputQuantity}x", _labelStyle);

            if (recipe.explosionRisk > 0 || recipe.fumeDetectionRisk > 0)
            {
                GUI.color = new Color(1f, 0.4f, 0.3f);
                string risk = "";
                if (recipe.explosionRisk > 0)
                    risk += $"Explosion: {recipe.explosionRisk:P0}  ";
                if (recipe.fumeDetectionRisk > 0)
                    risk += $"Fume Leak: {recipe.fumeDetectionRisk:P0}";
                GUILayout.Label($"\u26A0 Risk: {risk}", _labelStyle);
                GUI.color = Color.white;
            }

            GUILayout.Space(8);

            // ── Additives toggle ──
            _useAdditives = GUILayout.Toggle(_useAdditives, " Use available additives", _labelStyle);

            GUILayout.Space(4);

            // ── Cook Button ──
            bool canCraft = _ingredientInv != null &&
                            _ingredientInv.HasIngredientsForRecipe(recipe) &&
                            !_currentStation.IsFull;

            GUI.enabled = canCraft;
            GUI.color = canCraft ? new Color(0.2f, 1f, 0.3f) : new Color(0.4f, 0.4f, 0.4f);

            string cookLabel = _currentStation.IsFull ? "STATION FULL" :
                               !canCraft ? "MISSING INGREDIENTS" :
                               $"\u2697 COOK {recipe.recipeName.ToUpper()}";

            if (GUILayout.Button(cookLabel, _buttonStyle, GUILayout.Height(36)))
            {
                var batch = _currentStation.StartBatch(recipe, _currentPlayer, _useAdditives);
                if (batch != null)
                {
                    Debug.Log($"[CraftingUI] Batch started: {recipe.recipeName}");
                }
            }

            GUI.enabled = true;
            GUI.color = Color.white;

            GUILayout.EndVertical();
        }

        // ─── Active Batches ──────────────────────────────────────

        private void DrawActiveBatches()
        {
            if (_currentStation == null || _currentStation.ActiveBatches.Count == 0) return;

            GUILayout.Label("ACTIVE BATCHES:", _subHeaderStyle);

            foreach (var batch in _currentStation.ActiveBatches)
            {
                float progress = batch.Progress;
                float remaining = batch.TimeRemaining;

                GUILayout.BeginHorizontal();

                GUILayout.Label(
                    $"  {batch.recipe.recipeName} — {progress:P0} ({remaining:F0}s left)",
                    _labelStyle, GUILayout.Width(350f));

                // Progress bar
                Rect barRect = GUILayoutUtility.GetRect(200f, 16f);
                GUI.color = new Color(0.2f, 0.2f, 0.2f);
                GUI.Box(barRect, "");
                GUI.color = Color.Lerp(new Color(1f, 0.6f, 0.1f), new Color(0.2f, 1f, 0.3f), progress);
                GUI.Box(new Rect(barRect.x, barRect.y, barRect.width * progress, barRect.height), "");
                GUI.color = Color.white;

                GUILayout.EndHorizontal();
            }
        }

        // ─── Helpers ─────────────────────────────────────────────

        private string GetAdditiveEffectSummary(IngredientDefinition ingredient)
        {
            if (ingredient.effects == null || ingredient.effects.Length == 0)
                return "";

            string summary = "(";
            foreach (var effect in ingredient.effects)
            {
                if (effect.qualityModifier != 0)
                    summary += effect.qualityModifier > 0 ? "+Q " : "-Q ";
                if (effect.valueModifier != 0)
                    summary += effect.valueModifier > 0 ? "+$ " : "-$ ";
                if (effect.addictionModifier != 0)
                    summary += "+Addict ";
            }
            return summary.TrimEnd() + ")";
        }

        private void Update()
        {
            // ESC to close
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                _currentStation?.CloseStation();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from stations
            var stations = FindObjectsByType<CraftingStation>(FindObjectsInactive.Exclude);
            foreach (var station in stations)
            {
                station.OnStationOpened -= Open;
                station.OnStationClosed -= Close;
            }
        }
    }
}
