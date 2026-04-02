using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Economy;
using Clout.Empire.Economy.Laundering;

namespace Clout.UI.Laundering
{
    /// <summary>
    /// Step 11E — Money laundering dashboard UI.
    ///
    /// Displays all laundering operations in a unified dashboard:
    ///   - Dirty/clean cash balances with daily capacity gauge
    ///   - IRS attention meter with stage indicator
    ///   - Front business list with per-business stats and suspicion
    ///   - Active pipeline batches with progress bars
    ///   - Method selection and amount input for new operations
    ///   - Audit status panel when under investigation
    ///
    /// OnGUI implementation — migrates to UI Toolkit in Phase 5.
    /// Toggle with F6 key (same pattern as other debug UIs).
    /// </summary>
    public class LaunderingUI : MonoBehaviour
    {
        [Header("Toggle")]
        public Key toggleKey = Key.L;
        private bool _isVisible;

        [Header("Layout")]
        [SerializeField] private float _panelWidth = 520f;
        [SerializeField] private float _panelX = 20f;
        [SerializeField] private float _panelY = 20f;

        // ─── State ────────────────────────────────────────────
        private LaunderingManager _manager;
        private CashManager _cash;
        private float _launderAmount = 1000f;
        private int _selectedMethodIndex;
        private int _selectedFrontIndex;
        private Vector2 _scrollPosition;
        private bool _showMethodSelect;
        private bool _showAuditPanel;

        // ─── Cached Styles ────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _greenText;
        private GUIStyle _redText;
        private GUIStyle _yellowText;
        private GUIStyle _whiteText;
        private bool _stylesInitialized;

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current[toggleKey].wasPressedThisFrame)
                _isVisible = !_isVisible;
            if (_isVisible && Keyboard.current.escapeKey.wasPressedThisFrame)
                _isVisible = false;
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            // Lazy-find singletons
            if (_manager == null) _manager = LaunderingManager.Instance;
            if (_cash == null) _cash = CashManager.Instance;
            if (_manager == null) return;

            InitStyles();

            float y = _panelY;

            // ── Main Panel ────────────────────────────────────
            GUI.Box(new Rect(_panelX - 5, y - 5, _panelWidth + 10, 850), "", _boxStyle);

            // Header
            GUI.Label(new Rect(_panelX, y, _panelWidth, 28), "MONEY LAUNDERING DASHBOARD", _headerStyle);
            y += 32;

            // ── Cash Overview ─────────────────────────────────
            y = DrawCashOverview(y);

            // ── IRS Attention ─────────────────────────────────
            y = DrawIRSSection(y);

            // ── Front Businesses ──────────────────────────────
            y = DrawFrontBusinesses(y);

            // ── Active Pipelines ──────────────────────────────
            y = DrawActivePipelines(y);

            // ── New Operation ─────────────────────────────────
            y = DrawNewOperation(y);

            // ── Audit Panel ───────────────────────────────────
            if (_manager.IRS.IsUnderInvestigation)
                y = DrawAuditPanel(y);
        }

        // ─── Cash Overview ────────────────────────────────────

        private float DrawCashOverview(float y)
        {
            GUI.Label(new Rect(_panelX, y, _panelWidth, 20),
                $"Dirty Cash:  ${_cash?.DirtyCash ?? 0:N0}", _redText);
            y += 18;

            GUI.Label(new Rect(_panelX, y, _panelWidth, 20),
                $"Clean Cash:  ${_cash?.CleanCash ?? 0:N0}", _greenText);
            y += 18;

            GUI.Label(new Rect(_panelX, y, _panelWidth, 20),
                $"In Pipeline: ${_manager.CashInPipeline:N0}", _yellowText);
            y += 22;

            // Daily capacity bar
            float capMax = _manager.DailyCapacityMax;
            float capUsed = _manager.DailyCapacityUsed;
            float capRatio = capMax > 0 ? capUsed / capMax : 0f;

            GUI.Label(new Rect(_panelX, y, 200, 18),
                $"Daily Capacity: ${capUsed:N0} / ${capMax:N0}", _whiteText);

            // Progress bar
            Rect barRect = new Rect(_panelX + 280, y + 2, 200, 14);
            GUI.Box(barRect, "");
            Color barColor = capRatio > 0.8f ? Color.red : capRatio > 0.5f ? Color.yellow : Color.green;
            Color prev = GUI.color;
            GUI.color = barColor;
            GUI.Box(new Rect(barRect.x + 1, barRect.y + 1, (barRect.width - 2) * capRatio, barRect.height - 2), "");
            GUI.color = prev;
            y += 24;

            DrawSeparator(y);
            return y + 6;
        }

        // ─── IRS Section ──────────────────────────────────────

        private float DrawIRSSection(float y)
        {
            IRSInvestigation irs = _manager.IRS;

            GUI.Label(new Rect(_panelX, y, _panelWidth, 20), "IRS ATTENTION", _subHeaderStyle);
            y += 22;

            // Attention meter
            float attention = irs.Attention;
            string stageLabel = irs.Stage.ToString().ToUpper();
            Color attColor = attention > 0.8f ? Color.red :
                             attention > 0.6f ? new Color(1f, 0.5f, 0f) :
                             attention > 0.4f ? Color.yellow : Color.green;

            GUI.Label(new Rect(_panelX, y, 150, 18),
                $"Attention: {attention:P0}", attention > 0.6f ? _redText : _whiteText);

            // Attention bar
            Rect attBar = new Rect(_panelX + 160, y + 2, 220, 14);
            GUI.Box(attBar, "");
            Color prevC = GUI.color;
            GUI.color = attColor;
            GUI.Box(new Rect(attBar.x + 1, attBar.y + 1, (attBar.width - 2) * attention, attBar.height - 2), "");
            GUI.color = prevC;

            // Stage badge
            if (irs.Stage != IRSInvestigationStage.None)
            {
                GUI.Label(new Rect(_panelX + 395, y, 120, 18),
                    $"[{stageLabel}]", _redText);
            }
            y += 18;

            // Threshold markers
            GUI.Label(new Rect(_panelX + 20, y, _panelWidth, 14),
                "FLAG: 40%    INVESTIGATION: 60%    AUDIT: 80%",
                _whiteText);
            y += 20;

            // Lifetime laundered
            GUI.Label(new Rect(_panelX, y, _panelWidth, 18),
                $"Lifetime Laundered: ${irs.TotalLaunderedLifetime:N0}", _whiteText);
            y += 22;

            DrawSeparator(y);
            return y + 6;
        }

        // ─── Front Businesses ─────────────────────────────────

        private float DrawFrontBusinesses(float y)
        {
            var fronts = _manager.FrontBusinesses;
            GUI.Label(new Rect(_panelX, y, _panelWidth, 20),
                $"FRONT BUSINESSES ({fronts.Count})", _subHeaderStyle);
            y += 22;

            if (fronts.Count == 0)
            {
                GUI.Label(new Rect(_panelX + 10, y, _panelWidth, 18),
                    "No front businesses registered. Designate a property as a front.", _yellowText);
                y += 22;
            }
            else
            {
                for (int i = 0; i < fronts.Count; i++)
                {
                    FrontBusiness front = fronts[i];
                    bool isSelected = _selectedFrontIndex == i;

                    // Selection highlight
                    if (isSelected)
                    {
                        GUI.Box(new Rect(_panelX, y - 1, _panelWidth, 42), "");
                    }

                    // Business name and type
                    string typeIcon = front.BusinessType switch
                    {
                        FrontBusinessType.Restaurant => "[R]",
                        FrontBusinessType.AutoShop => "[A]",
                        FrontBusinessType.Nightclub => "[N]",
                        FrontBusinessType.Laundromat => "[L]",
                        FrontBusinessType.CarWash => "[C]",
                        _ => "[?]"
                    };

                    GUI.Label(new Rect(_panelX + 5, y, 200, 18),
                        $"{typeIcon} {front.BusinessName}", _whiteText);

                    // Revenue
                    GUI.Label(new Rect(_panelX + 210, y, 100, 18),
                        $"Rev: ${front.LegitimateRevenue:N0}", _greenText);

                    // Capacity used
                    GUI.Label(new Rect(_panelX + 320, y, 120, 18),
                        $"Cap: ${front.CurrentDayVolume:N0}/${front.LaunderingCapacity:N0}",
                        front.CapacityUtilization > 0.8f ? _redText : _whiteText);
                    y += 18;

                    // Suspicion bar
                    GUI.Label(new Rect(_panelX + 15, y, 80, 16), "Suspicion:", _whiteText);
                    Rect suspBar = new Rect(_panelX + 100, y + 2, 120, 12);
                    GUI.Box(suspBar, "");
                    Color suspColor = front.SuspicionLevel > 0.6f ? Color.red :
                                      front.SuspicionLevel > 0.3f ? Color.yellow : Color.green;
                    Color pc = GUI.color;
                    GUI.color = suspColor;
                    GUI.Box(new Rect(suspBar.x + 1, suspBar.y + 1,
                        (suspBar.width - 2) * front.SuspicionLevel, suspBar.height - 2), "");
                    GUI.color = pc;

                    GUI.Label(new Rect(_panelX + 230, y, 60, 16),
                        $"{front.SuspicionLevel:P0}", front.SuspicionLevel > 0.5f ? _redText : _whiteText);

                    // Renovation tier
                    string reno = front.RenovationTier > 0 ? $"Reno: T{front.RenovationTier}" : "";
                    string bookkeeper = front.HasBookkeeper ? " [Acct]" : "";
                    GUI.Label(new Rect(_panelX + 310, y, 150, 16),
                        $"{reno}{bookkeeper}", _yellowText);

                    // Select button
                    if (GUI.Button(new Rect(_panelX + 460, y - 8, 50, 28),
                        isSelected ? "SEL" : "Use"))
                    {
                        _selectedFrontIndex = i;
                    }

                    y += 22;
                }
            }

            DrawSeparator(y);
            return y + 6;
        }

        // ─── Active Pipelines ─────────────────────────────────

        private float DrawActivePipelines(float y)
        {
            var batches = _manager.ActivePipelines;
            GUI.Label(new Rect(_panelX, y, _panelWidth, 20),
                $"ACTIVE OPERATIONS ({batches.Count})", _subHeaderStyle);
            y += 22;

            if (batches.Count == 0)
            {
                GUI.Label(new Rect(_panelX + 10, y, _panelWidth, 18),
                    "No active laundering operations.", _whiteText);
                y += 22;
            }
            else
            {
                int maxShow = Mathf.Min(batches.Count, 6);
                for (int i = 0; i < maxShow; i++)
                {
                    LaunderingBatch batch = batches[i];

                    // Method and amount
                    GUI.Label(new Rect(_panelX + 5, y, 250, 18),
                        $"{batch.method} via {batch.frontBusinessId}: ${batch.dirtyAmount:N0}",
                        _whiteText);

                    // Stage label
                    GUI.Label(new Rect(_panelX + 270, y, 80, 18),
                        $"[{batch.stage}]", _yellowText);

                    // Progress bar
                    Rect progBar = new Rect(_panelX + 360, y + 2, 140, 14);
                    GUI.Box(progBar, "");
                    Color pbc = GUI.color;
                    GUI.color = Color.cyan;
                    GUI.Box(new Rect(progBar.x + 1, progBar.y + 1,
                        (progBar.width - 2) * batch.TotalProgress, progBar.height - 2), "");
                    GUI.color = pbc;

                    y += 20;
                }

                if (batches.Count > maxShow)
                {
                    GUI.Label(new Rect(_panelX + 10, y, _panelWidth, 18),
                        $"... and {batches.Count - maxShow} more", _whiteText);
                    y += 20;
                }
            }

            DrawSeparator(y);
            return y + 6;
        }

        // ─── New Operation ────────────────────────────────────

        private float DrawNewOperation(float y)
        {
            GUI.Label(new Rect(_panelX, y, _panelWidth, 20),
                "START NEW OPERATION", _subHeaderStyle);
            y += 24;

            var fronts = _manager.FrontBusinesses;
            var methods = _manager.availableMethods;

            if (fronts.Count == 0 || methods == null || methods.Length == 0)
            {
                GUI.Label(new Rect(_panelX + 10, y, _panelWidth, 18),
                    "Need at least one front business and one laundering method.", _yellowText);
                return y + 24;
            }

            // Amount slider
            float maxAmount = _cash != null ? _cash.DirtyCash : 0;
            GUI.Label(new Rect(_panelX, y, 80, 20), "Amount:", _whiteText);
            _launderAmount = GUI.HorizontalSlider(
                new Rect(_panelX + 80, y + 4, 200, 16),
                _launderAmount, 100f, Mathf.Max(100f, maxAmount));
            _launderAmount = Mathf.Round(_launderAmount / 100f) * 100f; // Snap to $100
            GUI.Label(new Rect(_panelX + 290, y, 80, 20),
                $"${_launderAmount:N0}", _greenText);
            y += 24;

            // Method selection
            GUI.Label(new Rect(_panelX, y, 80, 20), "Method:", _whiteText);
            _selectedMethodIndex = Mathf.Clamp(_selectedMethodIndex, 0, methods.Length - 1);
            LaunderingMethod selectedMethod = methods[_selectedMethodIndex];

            if (GUI.Button(new Rect(_panelX + 80, y, 180, 22),
                selectedMethod != null ? selectedMethod.methodName : "Select Method"))
            {
                _selectedMethodIndex = (_selectedMethodIndex + 1) % methods.Length;
            }

            if (selectedMethod != null)
            {
                GUI.Label(new Rect(_panelX + 270, y, 240, 20),
                    $"Risk:{selectedMethod.riskProfile:P0}  Fee:{selectedMethod.feePercentage:P0}  " +
                    $"Days:{selectedMethod.TotalDays:F1}",
                    selectedMethod.riskProfile > 0.2f ? _yellowText : _whiteText);
            }
            y += 26;

            // Preview
            if (selectedMethod != null)
            {
                float fee = selectedMethod.CalculateFee(_launderAmount);
                float clean = selectedMethod.CalculateCleanAmount(_launderAmount);

                GUI.Label(new Rect(_panelX + 10, y, _panelWidth, 18),
                    $"Preview: ${_launderAmount:N0} dirty → ${clean:N0} clean (fee: ${fee:N0}) " +
                    $"in {selectedMethod.TotalDays:F1} days",
                    _greenText);
                y += 22;
            }

            // Execute button
            bool canLaunder = _cash != null && _cash.DirtyCash >= _launderAmount &&
                              _selectedFrontIndex < fronts.Count &&
                              selectedMethod != null;

            GUI.enabled = canLaunder;
            if (GUI.Button(new Rect(_panelX + 100, y, 200, 30), "START LAUNDERING"))
            {
                FrontBusiness front = fronts[_selectedFrontIndex];
                _manager.StartLaundering(_launderAmount, front, selectedMethod);
            }
            GUI.enabled = true;
            y += 36;

            return y;
        }

        // ─── Audit Panel ──────────────────────────────────────

        private float DrawAuditPanel(float y)
        {
            IRSInvestigation irs = _manager.IRS;

            DrawSeparator(y);
            y += 6;

            Color warnColor = irs.Stage == IRSInvestigationStage.Seizure ? Color.red : Color.yellow;
            Color pc = GUI.color;
            GUI.color = warnColor;
            GUI.Label(new Rect(_panelX, y, _panelWidth, 20),
                $"IRS {irs.Stage.ToString().ToUpper()} — DAY {irs.DaysInStage}", _headerStyle);
            GUI.color = pc;
            y += 24;

            switch (irs.Stage)
            {
                case IRSInvestigationStage.Flag:
                    GUI.Label(new Rect(_panelX + 10, y, _panelWidth, 36),
                        "WARNING: Suspicious activity flagged. Reduce laundering volume\n" +
                        "within 7 days or face investigation.", _yellowText);
                    y += 40;
                    break;

                case IRSInvestigationStage.Investigation:
                    GUI.Label(new Rect(_panelX + 10, y, _panelWidth, 36),
                        "IRS agent assigned. All transactions are being monitored.\n" +
                        "Reduce activity or hire an accountant to lower risk.", _yellowText);
                    y += 40;
                    break;

                case IRSInvestigationStage.Audit:
                    GUI.Label(new Rect(_panelX + 10, y, _panelWidth, 18),
                        $"Auditing: {irs.TargetBusinessId}", _redText);
                    y += 20;

                    // Audit progress bar
                    Rect auditBar = new Rect(_panelX + 10, y, 300, 16);
                    GUI.Box(auditBar, "");
                    Color abc = GUI.color;
                    GUI.color = Color.red;
                    GUI.Box(new Rect(auditBar.x + 1, auditBar.y + 1,
                        (auditBar.width - 2) * irs.AuditProgress, auditBar.height - 2), "");
                    GUI.color = abc;

                    GUI.Label(new Rect(_panelX + 320, y, 100, 16),
                        $"{irs.AuditProgress:P0}", _redText);
                    y += 24;

                    // Contest button
                    if (GUI.Button(new Rect(_panelX + 10, y, 160, 24), "Contest Audit (Lawyer)"))
                    {
                        irs.ContestAudit();
                    }
                    y += 28;
                    break;

                case IRSInvestigationStage.Seizure:
                    float fine = irs.CalculateFineCost();
                    GUI.Label(new Rect(_panelX + 10, y, _panelWidth, 36),
                        $"SEIZURE PENDING — Fine: ${fine:N0}\n" +
                        "Pay the fine or face business seizure and criminal charges.", _redText);
                    y += 40;

                    bool canPay = _cash != null && _cash.CanAfford(fine);
                    GUI.enabled = canPay;
                    if (GUI.Button(new Rect(_panelX + 10, y, 160, 24), $"Pay Fine (${fine:N0})"))
                    {
                        if (_cash.Spend(fine, "IRS fine payment"))
                        {
                            // Reset IRS state after paying
                            // The seizure handler in LaunderingManager will process this
                        }
                    }
                    GUI.enabled = true;
                    y += 30;
                    break;
            }

            return y;
        }

        // ─── Utilities ────────────────────────────────────────

        private void DrawSeparator(float y)
        {
            GUI.Box(new Rect(_panelX, y, _panelWidth, 2), "");
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft
            };

            _subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.8f, 0.8f, 1f) }
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.15f, 0.92f)) }
            };

            _greenText = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color(0.3f, 1f, 0.3f) },
                fontSize = 12
            };

            _redText = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color(1f, 0.3f, 0.3f) },
                fontSize = 12
            };

            _yellowText = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color(1f, 0.9f, 0.3f) },
                fontSize = 12
            };

            _whiteText = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
                fontSize = 12
            };
        }

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
