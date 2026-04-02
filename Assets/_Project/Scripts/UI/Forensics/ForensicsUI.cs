using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Clout.Core;
using Clout.Forensics;

namespace Clout.UI.Forensics
{
    /// <summary>
    /// Step 12 UI — Forensic Intelligence Dashboard (Player-facing).
    ///
    /// Shows the player's forensic exposure: how many signatures are in the
    /// database, active forensic investigations, scrubber status per station,
    /// and facility link warnings. Toggled with the F key.
    ///
    /// Design: OnGUI (Phase 2-3 pattern). Migration to UI Toolkit in Phase 5.
    /// </summary>
    public class ForensicsUI : MonoBehaviour
    {
        [Header("Toggle")]
        public Key toggleKey = Key.F;

        [Header("Layout")]
        public float panelWidth = 520f;
        public float panelHeight = 600f;

        private bool _isOpen;
        private Vector2 _scrollPosition;

        // Cached references
        private SignatureDatabase _sigDb;
        private ForensicLabAI _lab;

        // ─── Styles ──────────────────────────────────────────────
        private GUIStyle _panelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInit;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                _isOpen = !_isOpen;
                if (_isOpen) CacheReferences();
            }
        }

        private void CacheReferences()
        {
            _sigDb = SignatureDatabase.Instance;
            _lab = ForensicLabAI.Instance;
        }

        private void InitStyles()
        {
            if (_stylesInit) return;

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12)
            };
            _panelStyle.normal.background = MakeTex(2, 2, new Color(0.08f, 0.08f, 0.12f, 0.95f));

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _headerStyle.normal.textColor = new Color(0.3f, 0.85f, 1f);

            _subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            _subHeaderStyle.normal.textColor = new Color(0.9f, 0.75f, 0.3f);

            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            _labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleRight
            };
            _valueStyle.normal.textColor = Color.white;

            _warningStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            _warningStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);

            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12 };

            _stylesInit = true;
        }

        private void OnGUI()
        {
            if (!_isOpen) return;
            InitStyles();

            float x = (Screen.width - panelWidth) / 2f;
            float y = (Screen.height - panelHeight) / 2f;
            Rect panelRect = new Rect(x, y, panelWidth, panelHeight);

            GUI.Box(panelRect, "", _panelStyle);
            GUILayout.BeginArea(new Rect(x + 12, y + 12, panelWidth - 24, panelHeight - 24));

            // ── Header ─────────────────────────────────────
            GUILayout.Label("FORENSIC INTELLIGENCE", _headerStyle);
            GUILayout.Space(4);
            DrawSeparator();
            GUILayout.Space(8);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            DrawDatabaseOverview();
            GUILayout.Space(12);
            DrawLabStatus();
            GUILayout.Space(12);
            DrawClusterIntel();
            GUILayout.Space(12);
            DrawRecentResults();
            GUILayout.Space(12);
            DrawScrubberStatus();

            GUILayout.EndScrollView();

            GUILayout.Space(8);
            if (GUILayout.Button("Close [F]", _buttonStyle, GUILayout.Height(28)))
                _isOpen = false;

            GUILayout.EndArea();
        }

        // ─── Sections ────────────────────────────────────────────

        private void DrawDatabaseOverview()
        {
            GUILayout.Label("Signature Database", _subHeaderStyle);

            if (_sigDb == null)
            {
                GUILayout.Label("  No forensic database active.", _labelStyle);
                return;
            }

            DrawRow("Signatures on File", $"{_sigDb.EntryCount}");
            DrawRow("Active Clusters", $"{_sigDb.Clusters.Count}");
            DrawRow("Cluster Threshold", $"{_sigDb.clusterThreshold:F2}");
            DrawRow("Degradation Rate", $"{_sigDb.degradationRate:F3}/day");
            DrawRow("Max Reliable Age", $"{_sigDb.maxReliableDays:F0} days");

            // Threat level based on cluster count
            int clusters = _sigDb.Clusters.Count;
            string threat = clusters switch
            {
                0 => "CLEAR - No evidence on file",
                1 => "LOW - Single cluster identified",
                <= 3 => "MODERATE - Multiple clusters",
                <= 6 => "HIGH - Significant evidence trail",
                _ => "CRITICAL - Extensive forensic profile"
            };

            Color threatColor = clusters switch
            {
                0 => Color.green,
                1 => new Color(0.6f, 1f, 0.6f),
                <= 3 => new Color(1f, 1f, 0.4f),
                <= 6 => new Color(1f, 0.6f, 0.2f),
                _ => new Color(1f, 0.3f, 0.3f)
            };

            GUILayout.Space(4);
            var threatStyle = new GUIStyle(_warningStyle);
            threatStyle.normal.textColor = threatColor;
            GUILayout.Label($"  Threat Level: {threat}", threatStyle);
        }

        private void DrawLabStatus()
        {
            GUILayout.Label("Forensic Lab", _subHeaderStyle);

            if (_lab == null)
            {
                GUILayout.Label("  No forensic lab active.", _labelStyle);
                return;
            }

            DrawRow("Evidence Queue", $"{_lab.QueuedCount} items");
            DrawRow("Total Processed", $"{_lab.TotalProcessed}");
            DrawRow("Facility Links Found", $"{_lab.TotalLinks}");
            DrawRow("Processing", _lab.IsProcessing ? "ACTIVE" : "Idle");

            if (_lab.IsProcessing && _lab.ActiveAnalysis.signature != null)
            {
                DrawRow("  Analyzing", _lab.ActiveAnalysis.signature.BatchId);
                DrawRow("  Progress", $"{_lab.AnalysisProgress:P0}");
                DrawRow("  Source", _lab.ActiveAnalysis.source.ToString());
            }

            if (_lab.TotalLinks > 0)
            {
                GUILayout.Space(2);
                GUILayout.Label($"  WARNING: {_lab.TotalLinks} facility link(s) confirmed!", _warningStyle);
            }
        }

        private void DrawClusterIntel()
        {
            if (_sigDb == null) return;

            var clusters = _sigDb.Clusters;
            if (clusters.Count == 0) return;

            GUILayout.Label($"Identified Clusters ({clusters.Count})", _subHeaderStyle);

            int shown = 0;
            foreach (var cluster in clusters)
            {
                if (shown >= 8) { GUILayout.Label("  ... more clusters", _labelStyle); break; }

                string products = string.Join(", ", cluster.productIds);
                DrawRow($"  Cluster #{cluster.facilitySeed:X4}",
                    $"{cluster.MemberCount} samples, {products}");
                shown++;
            }
        }

        private void DrawRecentResults()
        {
            if (_lab == null) return;

            var results = _lab.CompletedResults;
            if (results.Count == 0) return;

            GUILayout.Label("Recent Analysis Results", _subHeaderStyle);

            int start = Mathf.Max(0, results.Count - 5);
            for (int i = results.Count - 1; i >= start; i--)
            {
                var r = results[i];
                string status = r.facilityIdentified
                    ? $"LINKED to facility {r.facilitySeed:X4} ({r.facilityConfidence:P0})"
                    : "No facility match";

                Color statusColor = r.facilityIdentified ? new Color(1f, 0.3f, 0.3f) : new Color(0.6f, 1f, 0.6f);
                var statusStyle = new GUIStyle(_labelStyle);
                statusStyle.normal.textColor = statusColor;

                GUILayout.Label($"  {r.batchId}: {status}", statusStyle);
            }
        }

        private void DrawScrubberStatus()
        {
            GUILayout.Label("Scrubber Equipment", _subHeaderStyle);

            var scrubbers = FindObjectsByType<SignatureScrubber>(FindObjectsInactive.Exclude);
            if (scrubbers == null || scrubbers.Length == 0)
            {
                GUILayout.Label("  No scrubbers installed.", _labelStyle);
                GUILayout.Label("  Install at a CraftingStation to reduce forensic exposure.", _labelStyle);
                return;
            }

            foreach (var scrubber in scrubbers)
            {
                string stationName = scrubber.LinkedStation != null
                    ? scrubber.LinkedStation.stationName : "Unknown";
                string status = scrubber.IsEnabled ? "ACTIVE" : "Disabled";
                Color statusColor = scrubber.IsEnabled ? Color.green : Color.gray;

                var statusStyle = new GUIStyle(_labelStyle);
                statusStyle.normal.textColor = statusColor;

                GUILayout.Label($"  {stationName}: {scrubber.LevelName} [{status}]", statusStyle);
                DrawRow($"    Yield Penalty", $"-{scrubber.YieldPenalty:P0}");

                if (scrubber.ScrubLevel < 3)
                {
                    float nextCost = scrubber.GetNextUpgradeCost();
                    DrawRow($"    Next Upgrade", $"${nextCost:F0}");
                }
            }
        }

        // ─── Helpers ─────────────────────────────────────────────

        private void DrawRow(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"  {label}", _labelStyle, GUILayout.Width(panelWidth * 0.55f));
            GUILayout.Label(value, _valueStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawSeparator()
        {
            var rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorDrawLine(rect, new Color(0.3f, 0.5f, 0.7f, 0.6f));
        }

        private static void EditorDrawLine(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}
