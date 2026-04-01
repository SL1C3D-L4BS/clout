using UnityEngine;
using UnityEngine.InputSystem;
using Clout.Core;
using Clout.Empire.Economy;
using Clout.Empire.Properties;

namespace Clout.Empire.Employees
{
    /// <summary>
    /// OnGUI worker management panel — view, fire, and reassign active workers.
    ///
    /// Toggle with Y key. Displays all hired workers with:
    ///   - Name, role, assigned property
    ///   - State (working, resting, dealing, etc.)
    ///   - Loyalty bar (color-coded: green > 0.6, yellow > 0.3, red)
    ///   - Performance stats (deals, units produced, cash earned)
    ///   - Fire / Reassign actions
    ///
    /// Phase 2 placeholder UI — replaced with full UGUI in Phase 3.
    /// </summary>
    public class WorkerManagementUI : MonoBehaviour
    {
        [Header("Layout")]
        public float panelWidth = 550f;
        public float panelHeight = 500f;

        private bool _isOpen;
        private Vector2 _scrollPosition;
        private int _selectedWorkerIndex = -1;
        private bool _reassignMode;
        private Property _reassignTarget;

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _workerStyle;
        private GUIStyle _selectedStyle;
        private GUIStyle _loyaltyHighStyle;
        private GUIStyle _loyaltyMidStyle;
        private GUIStyle _loyaltyLowStyle;
        private bool _stylesInitialized;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame)
            {
                _isOpen = !_isOpen;
                _reassignMode = false;
            }
        }

        private void OnGUI()
        {
            if (!_isOpen) return;

            InitStyles();

            WorkerManager wm = WorkerManager.Instance;
            if (wm == null) return;

            float x = (Screen.width - panelWidth) / 2f;
            float y = (Screen.height - panelHeight) / 2f;
            Rect panelRect = new Rect(x, y, panelWidth, panelHeight);

            GUI.Box(panelRect, "");

            GUILayout.BeginArea(panelRect);
            GUILayout.Space(10);

            // Header
            GUILayout.Label("WORKFORCE MANAGEMENT", _headerStyle);
            GUILayout.Space(5);

            // Summary
            GUILayout.Label($"Workers: {wm.WorkerCount}/{wm.GetMaxWorkers()}  |  Daily Wages: ${wm.GetTotalDailyWages():F0}  |  Daily Revenue: ${wm.GetTotalDailyEarnings():F0}");
            GUILayout.Space(5);

            // Worker list
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(panelHeight - 170f));

            for (int i = 0; i < wm.Workers.Count; i++)
            {
                var worker = wm.Workers[i];
                bool isSelected = i == _selectedWorkerIndex;

                GUIStyle style = isSelected ? _selectedStyle : _workerStyle;

                GUILayout.BeginVertical(style);

                // Name + Role + State
                string stateStr = worker.state.ToString().ToUpper();
                GUILayout.Label($"{worker.workerName}  [{worker.role}]  — {stateStr}");

                // Loyalty bar
                GUIStyle loyaltyStyle = worker.loyalty > 0.6f ? _loyaltyHighStyle
                    : worker.loyalty > 0.3f ? _loyaltyMidStyle
                    : _loyaltyLowStyle;
                GUILayout.Label($"  Loyalty: {LoyaltyBar(worker.loyalty)}  ({worker.loyalty:P0})", loyaltyStyle);

                // Assignment
                GUILayout.Label($"  Property: {worker.assignedPropertyId}  |  Knowledge: Lv{worker.knowledgeLevel}");

                // Performance stats
                if (isSelected)
                {
                    GUILayout.Label($"  Skill: {worker.skill:F2}  |  Shifts: {worker.shiftsCompleted}");

                    if (worker.role == EmployeeRole.Dealer)
                        GUILayout.Label($"  Deals: {worker.totalDeals}  |  Cash Earned: ${worker.totalCashEarned:F0}");
                    else if (worker.role == EmployeeRole.Cook)
                        GUILayout.Label($"  Units Produced: {worker.totalUnitsProduced}");

                    GUILayout.Label($"  Greed: {worker.greed:F2}  |  Discretion: {worker.discretion:F2}  |  Courage: {worker.courage:F2}");
                    GUILayout.Label($"  Wage: ${worker.definition.dailyWage * worker.definition.WageDemandMultiplier:F0}/day");
                }

                GUILayout.EndVertical();

                // Select on click
                if (Event.current.type == EventType.MouseDown)
                {
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    if (lastRect.Contains(Event.current.mousePosition))
                    {
                        _selectedWorkerIndex = isSelected ? -1 : i;
                        _reassignMode = false;
                        Event.current.Use();
                    }
                }
            }

            if (wm.Workers.Count == 0)
                GUILayout.Label("No workers hired. Open Recruitment (Tab) to hire.");

            GUILayout.EndScrollView();

            // Action buttons
            GUILayout.Space(5);

            if (_reassignMode && _selectedWorkerIndex >= 0)
            {
                DrawReassignPanel(wm);
            }
            else
            {
                GUILayout.BeginHorizontal();

                GUI.enabled = _selectedWorkerIndex >= 0;

                if (GUILayout.Button("FIRE", GUILayout.Height(30)))
                {
                    if (_selectedWorkerIndex >= 0 && _selectedWorkerIndex < wm.Workers.Count)
                    {
                        var worker = wm.Workers[_selectedWorkerIndex];
                        wm.FireWorker(worker);
                        _selectedWorkerIndex = -1;
                    }
                }

                if (GUILayout.Button("REASSIGN", GUILayout.Height(30)))
                {
                    _reassignMode = true;
                    _reassignTarget = null;
                }

                GUI.enabled = true;

                if (GUILayout.Button("CLOSE", GUILayout.Height(30)))
                {
                    _isOpen = false;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        // ─── Reassign Panel ─────────────────────────────────────────

        private void DrawReassignPanel(WorkerManager wm)
        {
            GUILayout.Label("Select target property:");

            GUILayout.BeginHorizontal();

            var properties = FindObjectsByType<Property>();

            foreach (var prop in properties)
            {
                if (!prop.IsOwned) continue;

                string label = prop.Definition != null ? prop.Definition.propertyName : "Property";
                int count = wm.GetWorkerCountAtProperty(prop);
                int slots = prop.GetMaxEmployeeSlots();

                if (GUILayout.Button($"{label} ({count}/{slots})", GUILayout.Height(25)))
                {
                    _reassignTarget = prop;
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUI.enabled = _reassignTarget != null;
            if (GUILayout.Button("CONFIRM REASSIGN", GUILayout.Height(28)))
            {
                if (_selectedWorkerIndex >= 0 && _selectedWorkerIndex < wm.Workers.Count)
                {
                    var worker = wm.Workers[_selectedWorkerIndex];
                    bool success = wm.ReassignWorker(worker, _reassignTarget);
                    if (success)
                        Debug.Log($"[WorkerUI] Reassigned {worker.workerName} to {_reassignTarget.Definition.propertyName}");
                    _reassignMode = false;
                }
            }
            GUI.enabled = true;

            if (GUILayout.Button("CANCEL", GUILayout.Height(28)))
            {
                _reassignMode = false;
            }

            GUILayout.EndHorizontal();
        }

        // ─── Helpers ────────────────────────────────────────────────

        private string LoyaltyBar(float value)
        {
            int filled = Mathf.RoundToInt(value * 10f);
            return "[" + new string('\u2588', filled) + new string('\u2591', 10 - filled) + "]";
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _workerStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(2, 2, 2, 2)
            };

            _selectedStyle = new GUIStyle(_workerStyle);
            _selectedStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.3f, 0.5f, 0.8f));

            _loyaltyHighStyle = new GUIStyle(GUI.skin.label);
            _loyaltyHighStyle.normal.textColor = new Color(0.2f, 0.9f, 0.2f);

            _loyaltyMidStyle = new GUIStyle(GUI.skin.label);
            _loyaltyMidStyle.normal.textColor = new Color(0.9f, 0.9f, 0.2f);

            _loyaltyLowStyle = new GUIStyle(GUI.skin.label);
            _loyaltyLowStyle.normal.textColor = new Color(0.9f, 0.2f, 0.2f);
        }

        private Texture2D MakeTex(int w, int h, Color col)
        {
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}
