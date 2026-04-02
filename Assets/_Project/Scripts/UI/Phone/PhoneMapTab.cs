using UnityEngine;
using Clout.Empire.Properties;
using Clout.Empire.Employees;
using Clout.World.Districts;
using Clout.World.Police;

namespace Clout.UI.Phone
{
    /// <summary>
    /// Phone Map Tab — district overview with territory control, heat radar, and entity blips.
    ///
    /// Renders a top-down 2D map of the current district showing:
    ///   - Street grid (from DistrictDefinition)
    ///   - Territory control overlay (color-coded influence)
    ///   - Heat radar (expanding rings from player)
    ///   - Player blip (blue)
    ///   - Property blips (yellow — owned, grey — available)
    ///   - Police blips (red — active officers)
    ///   - Worker blips (orange)
    ///   - Customer blips (green)
    ///
    /// Minimap mode renders a small version in the HUD corner.
    /// Full map opens inside the phone.
    /// </summary>
    public class PhoneMapTab : MonoBehaviour
    {
        // Cached references
        private Transform _playerTransform;
        private DistrictManager _districtManager;
        private PropertyManager _propertyManager;
        private WorkerManager _workerManager;
        private WantedSystem _wantedSystem;

        private float _mapScale = 1f;
        private Vector2 _mapOffset = Vector2.zero;

        // ─── Blip Colors ────────────────────────────────────────
        private static readonly Color PLAYER_BLIP = new Color(0.2f, 0.5f, 1f);
        private static readonly Color PROPERTY_OWNED = new Color(0.95f, 0.8f, 0.2f);
        private static readonly Color PROPERTY_AVAIL = new Color(0.5f, 0.5f, 0.55f);
        private static readonly Color POLICE_BLIP = new Color(0.9f, 0.15f, 0.15f);
        private static readonly Color WORKER_BLIP = new Color(1f, 0.6f, 0.15f);
        private static readonly Color CUSTOMER_BLIP = new Color(0.2f, 0.85f, 0.35f);
        private static readonly Color ROAD_MAP_COLOR = new Color(0.3f, 0.32f, 0.38f);
        private static readonly Color HEAT_RING = new Color(0.9f, 0.2f, 0.1f, 0.15f);
        private static readonly Color TERRITORY_FILL = new Color(0.2f, 0.4f, 0.9f, 0.1f);

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;

            _districtManager = DistrictManager.Instance;
            _propertyManager = PropertyManager.Instance;
            _workerManager = WorkerManager.Instance;
            _wantedSystem = FindAnyObjectByType<WantedSystem>();
        }

        public void DrawTab(Rect rect)
        {
            // Re-cache if needed
            if (_playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _playerTransform = player.transform;
            }

            // Map background
            GUI.color = new Color(0.1f, 0.12f, 0.15f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var district = _districtManager != null ? _districtManager.CurrentDistrict : null;

            if (district == null)
            {
                GUI.color = PhoneController.DimText;
                GUI.Label(rect, "No district data available", PhoneController.Instance.BodyStyle);
                GUI.color = Color.white;
                return;
            }

            // Compute map transform: world coords → screen coords
            float mapW = district.halfExtents.x * 2f;
            float mapH = district.halfExtents.z * 2f;
            float scaleX = rect.width / mapW * _mapScale;
            float scaleZ = rect.height / mapH * _mapScale;
            float scale = Mathf.Min(scaleX, scaleZ) * 0.9f;
            Vector2 center = new Vector2(rect.x + rect.width / 2f, rect.y + rect.height / 2f);
            Vector2 worldCenter = new Vector2(district.worldCenter.x, district.worldCenter.z);

            // ─── Draw Road Grid ─────────────────────────────────
            DrawRoadGrid(rect, district, center, worldCenter, scale);

            // ─── Draw Territory Overlay ──────────────────────────
            DrawTerritoryOverlay(rect, district, center, worldCenter, scale);

            // ─── Draw Heat Radar ─────────────────────────────────
            if (_playerTransform != null && _wantedSystem != null && _wantedSystem.CurrentHeat > 0)
            {
                DrawHeatRadar(center, worldCenter, scale);
            }

            // ─── Draw Blips ──────────────────────────────────────
            DrawPropertyBlips(center, worldCenter, scale);
            DrawPoliceBlips(center, worldCenter, scale);
            DrawWorkerBlips(center, worldCenter, scale);
            DrawCustomerBlips(center, worldCenter, scale);

            // Player blip (always on top)
            if (_playerTransform != null)
            {
                Vector2 playerScreen = WorldToMap(_playerTransform.position, center, worldCenter, scale);
                DrawBlip(playerScreen, 6f, PLAYER_BLIP, true);
            }

            // ─── District Info Overlay ───────────────────────────
            DrawDistrictInfo(rect, district);

            // ─── Legend ──────────────────────────────────────────
            DrawLegend(rect);
        }

        // ─── Map Rendering ──────────────────────────────────────

        private void DrawRoadGrid(Rect rect, DistrictDefinition district, Vector2 center,
            Vector2 worldCenter, float scale)
        {
            GUI.color = ROAD_MAP_COLOR;
            float halfW = district.halfExtents.x;
            float halfD = district.halfExtents.z;

            // N-S streets
            for (int i = 0; i < district.majorStreetsNS; i++)
            {
                float t = (i + 1f) / (district.majorStreetsNS + 1f);
                float x = Mathf.Lerp(-halfW, halfW, t);

                Vector2 start = WorldToMap(new Vector3(x, 0, -halfD), center, worldCenter, scale);
                Vector2 end = WorldToMap(new Vector3(x, 0, halfD), center, worldCenter, scale);

                float roadW = district.streetWidth * scale * 0.5f;
                roadW = Mathf.Max(2f, roadW);
                GUI.DrawTexture(new Rect(start.x - roadW / 2, Mathf.Min(start.y, end.y),
                    roadW, Mathf.Abs(end.y - start.y)), Texture2D.whiteTexture);
            }

            // E-W streets
            for (int i = 0; i < district.majorStreetsEW; i++)
            {
                float t = (i + 1f) / (district.majorStreetsEW + 1f);
                float z = Mathf.Lerp(-halfD, halfD, t);

                Vector2 start = WorldToMap(new Vector3(-halfW, 0, z), center, worldCenter, scale);
                Vector2 end = WorldToMap(new Vector3(halfW, 0, z), center, worldCenter, scale);

                float roadH = district.streetWidth * scale * 0.5f;
                roadH = Mathf.Max(2f, roadH);
                GUI.DrawTexture(new Rect(Mathf.Min(start.x, end.x), start.y - roadH / 2,
                    Mathf.Abs(end.x - start.x), roadH), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }

        private void DrawTerritoryOverlay(Rect rect, DistrictDefinition district,
            Vector2 center, Vector2 worldCenter, float scale)
        {
            if (_districtManager == null) return;
            var state = _districtManager.GetRuntimeState(district.districtId);
            if (state.controlLevel <= 0) return;

            float alpha = state.controlLevel / 100f * 0.15f;
            GUI.color = new Color(0.2f, 0.4f, 0.9f, alpha);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawHeatRadar(Vector2 center, Vector2 worldCenter, float scale)
        {
            if (_playerTransform == null || _wantedSystem == null) return;

            Vector2 playerScreen = WorldToMap(_playerTransform.position, center, worldCenter, scale);
            float heat = _wantedSystem.CurrentHeat;
            float maxHeat = _wantedSystem.maxHeat;
            float normalizedHeat = heat / maxHeat;

            // Concentric rings expanding with heat level
            int rings = Mathf.CeilToInt(normalizedHeat * 4f);
            for (int r = 1; r <= rings; r++)
            {
                float radius = r * 15f + Mathf.Sin(Time.time * 2f + r) * 3f;
                float alpha = (1f - (float)r / (rings + 1)) * normalizedHeat * 0.3f;
                GUI.color = new Color(HEAT_RING.r, HEAT_RING.g, HEAT_RING.b, alpha);

                // Approximate circle with rotated rectangles
                for (int a = 0; a < 12; a++)
                {
                    float angle = a * 30f * Mathf.Deg2Rad;
                    float x = playerScreen.x + Mathf.Cos(angle) * radius;
                    float y = playerScreen.y + Mathf.Sin(angle) * radius;
                    GUI.DrawTexture(new Rect(x - 1.5f, y - 1.5f, 3f, 3f), Texture2D.whiteTexture);
                }
            }
            GUI.color = Color.white;
        }

        private void DrawPropertyBlips(Vector2 center, Vector2 worldCenter, float scale)
        {
            if (_propertyManager == null) return;

            foreach (var prop in _propertyManager.OwnedProperties)
            {
                if (prop == null) continue;
                Vector2 screenPos = WorldToMap(prop.transform.position, center, worldCenter, scale);
                DrawBlip(screenPos, 5f, PROPERTY_OWNED, false);
            }
        }

        private void DrawPoliceBlips(Vector2 center, Vector2 worldCenter, float scale)
        {
            var officers = FindObjectsByType<PoliceOfficerAI>();
            foreach (var cop in officers)
            {
                if (cop == null) continue;
                Vector2 screenPos = WorldToMap(cop.transform.position, center, worldCenter, scale);
                DrawBlip(screenPos, 4f, POLICE_BLIP, false);
            }
        }

        private void DrawWorkerBlips(Vector2 center, Vector2 worldCenter, float scale)
        {
            if (_workerManager == null || _workerManager.Workers == null) return;

            foreach (var worker in _workerManager.Workers)
            {
                if (worker == null || worker.worldObject == null) continue;
                Vector2 screenPos = WorldToMap(worker.worldObject.transform.position, center, worldCenter, scale);
                DrawBlip(screenPos, 4f, WORKER_BLIP, false);
            }
        }

        private void DrawCustomerBlips(Vector2 center, Vector2 worldCenter, float scale)
        {
            if (_districtManager == null) return;

            foreach (var customer in _districtManager.ActiveCustomers)
            {
                if (customer == null) continue;
                Vector2 screenPos = WorldToMap(customer.transform.position, center, worldCenter, scale);
                DrawBlip(screenPos, 3f, CUSTOMER_BLIP, false);
            }
        }

        private void DrawBlip(Vector2 pos, float size, Color color, bool pulsing)
        {
            if (pulsing)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.3f;
                size *= pulse;
            }

            GUI.color = color;
            GUI.DrawTexture(new Rect(pos.x - size / 2, pos.y - size / 2, size, size), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawDistrictInfo(Rect rect, DistrictDefinition district)
        {
            // Overlay district name and stats in top-left corner of map
            GUI.color = new Color(0, 0, 0, 0.6f);
            Rect infoRect = new Rect(rect.x + 4, rect.y + 4, rect.width * 0.45f, 60);
            GUI.DrawTexture(infoRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
            nameStyle.fontSize = 13;
            nameStyle.fontStyle = FontStyle.Bold;
            nameStyle.normal.textColor = PhoneController.TextCol;

            GUIStyle detailStyle = new GUIStyle(GUI.skin.label);
            detailStyle.fontSize = 10;
            detailStyle.normal.textColor = PhoneController.DimText;

            GUI.Label(new Rect(infoRect.x + 4, infoRect.y + 2, infoRect.width - 8, 18),
                district.districtName, nameStyle);

            string wantedStr = _wantedSystem != null ? _wantedSystem.CurrentLevel.ToString() : "Clean";
            GUI.Label(new Rect(infoRect.x + 4, infoRect.y + 20, infoRect.width - 8, 16),
                $"Wealth: {district.wealthLevel:P0}  Police: {district.policePresenceMultiplier:F1}x", detailStyle);
            GUI.Label(new Rect(infoRect.x + 4, infoRect.y + 36, infoRect.width - 8, 16),
                $"Heat: {wantedStr}  Rival: {district.rivalCrewName}", detailStyle);
        }

        private void DrawLegend(Rect rect)
        {
            float legendY = rect.yMax - 50;
            float legendX = rect.x + 4;

            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(new Rect(legendX, legendY, rect.width * 0.5f, 48), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle legendStyle = new GUIStyle(GUI.skin.label);
            legendStyle.fontSize = 9;
            legendStyle.normal.textColor = PhoneController.DimText;

            float x = legendX + 4;
            float y = legendY + 4;
            float spacing = 55f;

            DrawLegendItem(x, y, PLAYER_BLIP, "You", legendStyle); x += spacing;
            DrawLegendItem(x, y, PROPERTY_OWNED, "Property", legendStyle); x += spacing;
            DrawLegendItem(x, y, POLICE_BLIP, "Police", legendStyle);
            x = legendX + 4; y += 18;
            DrawLegendItem(x, y, WORKER_BLIP, "Worker", legendStyle); x += spacing;
            DrawLegendItem(x, y, CUSTOMER_BLIP, "Customer", legendStyle);
        }

        private void DrawLegendItem(float x, float y, Color color, string label, GUIStyle style)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y + 3, 6, 6), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 10, y, 50, 16), label, style);
        }

        // ─── Coordinate Transform ───────────────────────────────

        private Vector2 WorldToMap(Vector3 worldPos, Vector2 screenCenter, Vector2 worldMapCenter, float scale)
        {
            float sx = screenCenter.x + (worldPos.x - worldMapCenter.x) * scale;
            float sy = screenCenter.y - (worldPos.z - worldMapCenter.y) * scale; // Z→Y, inverted
            return new Vector2(sx, sy);
        }
    }
}
