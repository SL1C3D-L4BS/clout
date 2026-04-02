using UnityEngine;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Economy;
using Clout.Empire.Properties;
using Clout.World.Districts;

namespace Clout.UI.Phone
{
    /// <summary>
    /// Phone Products Tab — inventory dashboard and market pricing.
    ///
    /// Shows:
    ///   - Total product value across all stashes
    ///   - Per-product inventory count + quality distribution
    ///   - Current market prices per district
    ///   - Supply/demand indicators
    ///   - Production rates (if any cook workers active)
    /// </summary>
    public class PhoneProductsTab : MonoBehaviour
    {
        private PropertyManager _propertyManager;
        private EconomyManager _economyManager;
        private DistrictManager _districtManager;
        private Vector2 _scrollPos;

        private void Start()
        {
            _propertyManager = PropertyManager.Instance;
            _economyManager = FindAnyObjectByType<EconomyManager>();
            _districtManager = DistrictManager.Instance;
        }

        public void DrawTab(Rect rect)
        {
            if (_propertyManager == null) _propertyManager = PropertyManager.Instance;
            if (_economyManager == null) _economyManager = FindAnyObjectByType<EconomyManager>();
            if (_districtManager == null) _districtManager = DistrictManager.Instance;

            GUILayout.BeginArea(rect);

            // ─── Inventory Summary ───────────────────────────────
            DrawInventorySummary(rect.width);

            GUILayout.Space(8);

            // ─── Market Prices ───────────────────────────────────
            GUI.color = PhoneController.TextCol;
            GUILayout.Label("MARKET PRICES", PhoneController.Instance.HeaderStyle);
            GUI.color = Color.white;

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(rect.height - 150));

            DrawMarketPrices(rect.width);

            GUILayout.Space(12);

            // ─── Stash Breakdown ─────────────────────────────────
            DrawStashBreakdown(rect.width);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawInventorySummary(float width)
        {
            // Aggregate all stashes
            int totalUnits = 0;
            float totalValue = 0;
            var productCounts = new Dictionary<string, int>();

            if (_propertyManager != null)
            {
                foreach (var prop in _propertyManager.OwnedProperties)
                {
                    if (prop == null || prop.Stash == null) continue;
                    foreach (var slot in prop.Stash)
                    {
                        totalUnits += slot.quantity;
                        if (!productCounts.ContainsKey(slot.productId))
                            productCounts[slot.productId] = 0;
                        productCounts[slot.productId] += slot.quantity;

                        // Estimate value from economy manager
                        string districtId = _districtManager != null
                            ? _districtManager.CurrentDistrictId : "default";
                        float price = _economyManager != null
                            ? _economyManager.GetStreetPrice(slot.productId, districtId)
                            : 50f;
                        totalValue += slot.quantity * price;
                    }
                }
            }

            // Summary card
            Rect summaryRect = GUILayoutUtility.GetRect(width - 20, 50);
            GUI.color = new Color(0.12f, 0.12f, 0.17f);
            GUI.DrawTexture(summaryRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle headerS = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            headerS.normal.textColor = PhoneController.DimText;
            GUI.Label(new Rect(summaryRect.x + 8, summaryRect.y + 2, 100, 14), "TOTAL INVENTORY", headerS);

            GUIStyle bigVal = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            bigVal.normal.textColor = PhoneController.AccentGold;
            GUI.Label(new Rect(summaryRect.x + 8, summaryRect.y + 18, summaryRect.width * 0.45f, 24),
                $"{totalUnits} units", bigVal);

            bigVal.alignment = TextAnchor.MiddleRight;
            bigVal.normal.textColor = PhoneController.AccentGreen;
            GUI.Label(new Rect(summaryRect.x + summaryRect.width * 0.45f, summaryRect.y + 18,
                summaryRect.width * 0.5f, 24),
                $"~${totalValue:N0}", bigVal);

            // Product type count
            GUIStyle countStyle = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            countStyle.normal.textColor = PhoneController.DimText;
            GUI.Label(new Rect(summaryRect.x + 8, summaryRect.y + 38, summaryRect.width, 14),
                $"{productCounts.Count} product types across {(_propertyManager?.OwnedProperties?.Count ?? 0)} properties",
                countStyle);
        }

        private void DrawMarketPrices(float width)
        {
            string districtId = _districtManager != null ? _districtManager.CurrentDistrictId : "default";
            string districtName = _districtManager?.CurrentDistrict?.districtName ?? "Unknown";

            GUIStyle locStyle = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            locStyle.normal.textColor = PhoneController.DimText;
            GUILayout.Label($"Prices in: {districtName}", locStyle);

            GUILayout.Space(4);

            // Header row
            Rect hdr = GUILayoutUtility.GetRect(width - 30, 16);
            GUIStyle colStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, fontStyle = FontStyle.Bold };
            colStyle.normal.textColor = PhoneController.DimText;

            float col1 = hdr.x;
            float col2 = hdr.x + hdr.width * 0.4f;
            float col3 = hdr.x + hdr.width * 0.65f;
            float colW = hdr.width * 0.35f;

            GUI.Label(new Rect(col1, hdr.y, colW, 16), "PRODUCT", colStyle);
            colStyle.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(col2, hdr.y, hdr.width * 0.25f, 16), "PRICE", colStyle);
            GUI.Label(new Rect(col3, hdr.y, colW, 16), "DEMAND", colStyle);

            // Product rows
            var products = new[] {
                ProductType.Cannabis, ProductType.Methamphetamine, ProductType.Cocaine,
                ProductType.MDMA, ProductType.Prescription
            };

            foreach (var product in products)
            {
                string productId = product.ToString();
                float price = _economyManager != null
                    ? _economyManager.GetStreetPrice(productId, districtId)
                    : 50f;
                float dsRatio = _economyManager != null
                    ? _economyManager.GetDemandSupplyRatio(productId, districtId)
                    : 1f;

                DrawPriceRow(width - 30, productId, price, dsRatio);
            }
        }

        private void DrawPriceRow(float width, string product, float price, float dsRatio)
        {
            Rect row = GUILayoutUtility.GetRect(width, 22);

            float col1 = row.x;
            float col2 = row.x + row.width * 0.4f;
            float col3 = row.x + row.width * 0.65f;

            GUIStyle nameS = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            nameS.normal.textColor = PhoneController.TextCol;
            GUI.Label(new Rect(col1, row.y, row.width * 0.4f, 20), product, nameS);

            GUIStyle priceS = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight };
            priceS.normal.textColor = PhoneController.AccentGreen;
            GUI.Label(new Rect(col2, row.y, row.width * 0.22f, 20), $"${price:F0}", priceS);

            // D/S indicator
            string demandStr;
            Color demandColor;
            if (dsRatio > 1.5f) { demandStr = "HIGH"; demandColor = PhoneController.AccentGreen; }
            else if (dsRatio > 0.8f) { demandStr = "NORMAL"; demandColor = PhoneController.AccentGold; }
            else { demandStr = "LOW"; demandColor = PhoneController.AccentRed; }

            GUIStyle demandS = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight };
            demandS.normal.textColor = demandColor;
            GUI.Label(new Rect(col3, row.y, row.width * 0.33f, 20), demandStr, demandS);

            // Separator
            GUI.color = new Color(0.18f, 0.18f, 0.22f);
            GUI.DrawTexture(new Rect(row.x, row.yMax - 1, row.width, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawStashBreakdown(float width)
        {
            if (_propertyManager == null || _propertyManager.OwnedProperties.Count == 0)
            {
                GUI.color = PhoneController.DimText;
                GUILayout.Label("No properties owned.\nBuy properties to store product.",
                    PhoneController.Instance.BodyStyle);
                GUI.color = Color.white;
                return;
            }

            GUI.color = PhoneController.TextCol;
            GUILayout.Label("STASH BY PROPERTY", PhoneController.Instance.HeaderStyle);
            GUI.color = Color.white;

            foreach (var prop in _propertyManager.OwnedProperties)
            {
                if (prop == null) continue;

                string propName = prop.Definition != null ? prop.Definition.propertyName : "Unknown";
                int stashCount = prop.StashCount;

                Rect propRect = GUILayoutUtility.GetRect(width - 30, 24);
                GUI.color = new Color(0.13f, 0.13f, 0.18f);
                GUI.DrawTexture(propRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                GUIStyle pStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
                pStyle.normal.textColor = PhoneController.TextCol;
                GUI.Label(new Rect(propRect.x + 6, propRect.y + 2, propRect.width * 0.6f, 18), propName, pStyle);

                pStyle.alignment = TextAnchor.MiddleRight;
                pStyle.normal.textColor = stashCount > 0 ? PhoneController.AccentGold : PhoneController.DimText;
                GUI.Label(new Rect(propRect.x + propRect.width * 0.5f, propRect.y + 2,
                    propRect.width * 0.45f, 18),
                    stashCount > 0 ? $"{stashCount} stacks" : "Empty", pStyle);

                GUILayout.Space(2);
            }
        }
    }
}
