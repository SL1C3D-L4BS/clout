using UnityEngine;
using Clout.Empire.Economy;
using Clout.Empire.Properties;

namespace Clout.Empire.Employees
{
    /// <summary>
    /// OnGUI recruitment interface — browse available recruits, compare stats, hire workers.
    ///
    /// Toggle with Tab key. Displays the RecruitmentManager hire pool with:
    ///   - Candidate name, role, backstory
    ///   - Star rating (quality tier)
    ///   - Key stats (skill, loyalty, discretion, greed)
    ///   - Hiring cost and daily wage
    ///   - Hire button (requires owned property + funds + workforce capacity)
    ///
    /// Phase 2 placeholder UI — will be replaced with full UGUI in Phase 3.
    /// </summary>
    public class HireUI : MonoBehaviour
    {
        [Header("Toggle")]
        public KeyCode toggleKey = KeyCode.Tab;

        [Header("Layout")]
        public float panelWidth = 500f;
        public float panelHeight = 600f;

        private bool _isOpen;
        private Vector2 _scrollPosition;
        private int _selectedCandidateIndex = -1;
        private Property _selectedProperty;

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _candidateStyle;
        private GUIStyle _selectedStyle;
        private GUIStyle _statLabelStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                _isOpen = !_isOpen;
        }

        private void OnGUI()
        {
            if (!_isOpen) return;

            InitStyles();

            RecruitmentManager rm = RecruitmentManager.Instance;
            if (rm == null) return;

            float x = (Screen.width - panelWidth) / 2f;
            float y = (Screen.height - panelHeight) / 2f;
            Rect panelRect = new Rect(x, y, panelWidth, panelHeight);

            GUI.Box(panelRect, "");

            GUILayout.BeginArea(panelRect);
            GUILayout.Space(10);

            // Header
            GUILayout.Label("RECRUITMENT", _headerStyle);
            GUILayout.Space(5);

            // Property selector
            DrawPropertySelector();
            GUILayout.Space(5);

            // Workforce status
            WorkerManager wm = WorkerManager.Instance;
            if (wm != null)
            {
                int current = wm.WorkerCount;
                int max = wm.GetMaxWorkers();
                GUILayout.Label($"Workforce: {current}/{max}  |  Daily Wages: ${wm.GetTotalDailyWages():F0}");
            }

            // Cash
            CashManager cash = CashManager.Instance;
            if (cash != null)
                GUILayout.Label($"Available Cash: ${cash.TotalCash:F0}");

            GUILayout.Space(5);

            // Candidate list
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(panelHeight - 180f));

            for (int i = 0; i < rm.HirePool.Count; i++)
            {
                var candidate = rm.HirePool[i];
                bool isSelected = i == _selectedCandidateIndex;

                GUIStyle style = isSelected ? _selectedStyle : _candidateStyle;

                GUILayout.BeginVertical(style);

                // Name + Role + Stars
                string stars = new string('\u2605', candidate.qualityTier + 1);
                GUILayout.Label($"{candidate.definition.employeeName}  [{candidate.role}]  {stars}", _statLabelStyle);

                // Key stats bar
                GUILayout.Label($"  SKL:{Bar(candidate.definition.skill)}  LOY:{Bar(candidate.definition.loyalty)}  DSC:{Bar(candidate.definition.discretion)}  GRD:{Bar(candidate.definition.greed)}");

                // Cost
                GUILayout.Label($"  Hire: ${candidate.definition.hiringCost:F0}  |  Wage: ${candidate.definition.dailyWage:F0}/day  |  Record: {(candidate.definition.hasRecord ? "YES" : "No")}");

                // Backstory
                if (isSelected && !string.IsNullOrEmpty(candidate.definition.backstory))
                    GUILayout.Label($"  \"{candidate.definition.backstory}\"");

                GUILayout.EndVertical();

                // Select on click
                if (Event.current.type == EventType.MouseDown)
                {
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    if (lastRect.Contains(Event.current.mousePosition))
                    {
                        _selectedCandidateIndex = isSelected ? -1 : i;
                        Event.current.Use();
                    }
                }
            }

            if (rm.HirePool.Count == 0)
                GUILayout.Label("No candidates available. Check back tomorrow.");

            GUILayout.EndScrollView();

            // Action buttons
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();

            GUI.enabled = _selectedCandidateIndex >= 0 && _selectedProperty != null;
            if (GUILayout.Button("HIRE", _buttonStyle, GUILayout.Height(35)))
            {
                WorkerInstance hired = rm.HireCandidate(_selectedCandidateIndex, _selectedProperty);
                if (hired != null)
                {
                    _selectedCandidateIndex = -1;
                    Debug.Log($"[HireUI] Hired {hired.workerName}!");
                }
            }
            GUI.enabled = true;

            if (GUILayout.Button("REFRESH POOL", GUILayout.Height(35)))
            {
                rm.RefreshPool();
                _selectedCandidateIndex = -1;
            }

            if (GUILayout.Button("CLOSE", GUILayout.Height(35)))
            {
                _isOpen = false;
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        // ─── Property Selector ──────────────────────────────────────

        private void DrawPropertySelector()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Assign to: ", GUILayout.Width(70));

            // Find all owned properties
            var properties = FindObjectsByType<Property>(FindObjectsInactive.Exclude);
            bool found = false;

            foreach (var prop in properties)
            {
                if (!prop.IsOwned) continue;
                found = true;

                bool isSelected = _selectedProperty == prop;
                string label = prop.Definition != null ? prop.Definition.propertyName : "Property";

                if (isSelected)
                    label = $"[{label}]";

                if (GUILayout.Button(label, GUILayout.Height(22)))
                {
                    _selectedProperty = prop;
                }
            }

            if (!found)
                GUILayout.Label("(No owned properties)");

            GUILayout.EndHorizontal();
        }

        // ─── Helpers ────────────────────────────────────────────────

        private string Bar(float value)
        {
            int filled = Mathf.RoundToInt(value * 5f);
            return new string('\u2588', filled) + new string('\u2591', 5 - filled);
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

            _candidateStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(2, 2, 2, 2)
            };

            _selectedStyle = new GUIStyle(_candidateStyle);
            _selectedStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.2f, 0.8f));

            _statLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
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
