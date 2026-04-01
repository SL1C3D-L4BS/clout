using UnityEngine;
using System.Collections.Generic;
using Clout.Empire.Economy;
using Clout.Utils;

namespace Clout.UI.Phone
{
    /// <summary>
    /// Phone Finance Tab — empire financial dashboard.
    ///
    /// Shows:
    ///   - Total cash (dirty vs clean split)
    ///   - Income/expense summary
    ///   - Transaction history (scrollable log)
    ///   - Revenue breakdown by source
    ///   - Laundering efficiency
    ///   - Daily burn rate (wages + upkeep)
    ///
    /// Real-time data from CashManager singleton + EventBus subscriptions.
    /// Transaction history maintained locally with ring buffer.
    /// </summary>
    public class PhoneFinanceTab : MonoBehaviour
    {
        private CashManager _cashManager;
        private Vector2 _scrollPos;

        // Transaction history (ring buffer)
        private readonly List<TransactionRecord> _recentTransactions = new List<TransactionRecord>();
        private const int MAX_HISTORY = 50;

        // Aggregated stats
        private float _todayIncome;
        private float _todayExpenses;
        private float _lastDayReset;

        private void Start()
        {
            _cashManager = CashManager.Instance;

            // Subscribe to money events
            EventBus.Subscribe<MoneyChangedEvent>(OnMoneyChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MoneyChangedEvent>(OnMoneyChanged);
        }

        private void OnMoneyChanged(MoneyChangedEvent e)
        {
            // Track daily income/expenses
            if (e.changeAmount > 0)
                _todayIncome += e.changeAmount;
            else
                _todayExpenses += Mathf.Abs(e.changeAmount);

            // Add to transaction log
            _recentTransactions.Insert(0, new TransactionRecord
            {
                amount = e.changeAmount,
                source = e.source,
                timestamp = Time.time,
                balanceAfter = e.totalCash,
                isDirty = e.changeAmount > 0 && e.dirtyCash > e.cleanCash
            });

            // Trim history
            while (_recentTransactions.Count > MAX_HISTORY)
                _recentTransactions.RemoveAt(_recentTransactions.Count - 1);
        }

        public void DrawTab(Rect rect)
        {
            if (_cashManager == null) _cashManager = CashManager.Instance;

            GUILayout.BeginArea(rect);

            // ─── Cash Summary ────────────────────────────────────
            DrawCashSummary(rect.width);

            GUILayout.Space(8);

            // ─── Income / Expense Today ──────────────────────────
            DrawDailySummary(rect.width);

            GUILayout.Space(8);

            // ─── Laundering Status ───────────────────────────────
            DrawLaunderingStatus(rect.width);

            GUILayout.Space(8);

            // ─── Transaction Log ─────────────────────────────────
            GUI.color = PhoneController.TextCol;
            GUILayout.Label("RECENT TRANSACTIONS", PhoneController.Instance.HeaderStyle);
            GUI.color = Color.white;

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(rect.height - 220));
            DrawTransactionLog(rect.width);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawCashSummary(float width)
        {
            // Total cash header
            float total = _cashManager != null ? _cashManager.TotalCash : 0;
            float dirty = _cashManager != null ? _cashManager.DirtyCash : 0;
            float clean = _cashManager != null ? _cashManager.CleanCash : 0;

            // Big total number
            GUIStyle totalStyle = new GUIStyle(GUI.skin.label);
            totalStyle.fontSize = 24;
            totalStyle.fontStyle = FontStyle.Bold;
            totalStyle.normal.textColor = PhoneController.AccentGreen;
            totalStyle.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label($"${total:N0}", totalStyle);

            // Dirty / Clean split bar
            GUILayout.Space(4);
            Rect barRect = GUILayoutUtility.GetRect(width - 20, 14);
            float barX = barRect.x + 10;
            float barW = barRect.width - 20;

            float totalCash = Mathf.Max(1f, dirty + clean);
            float dirtyRatio = dirty / totalCash;
            float cleanRatio = clean / totalCash;

            // Bar background
            GUI.color = new Color(0.15f, 0.15f, 0.2f);
            GUI.DrawTexture(new Rect(barX, barRect.y, barW, barRect.height), Texture2D.whiteTexture);

            // Dirty portion (red/amber)
            GUI.color = new Color(0.85f, 0.4f, 0.15f);
            GUI.DrawTexture(new Rect(barX, barRect.y, barW * dirtyRatio, barRect.height), Texture2D.whiteTexture);

            // Clean portion (green)
            GUI.color = PhoneController.AccentGreen;
            GUI.DrawTexture(new Rect(barX + barW * dirtyRatio, barRect.y,
                barW * cleanRatio, barRect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Labels below bar
            GUIStyle splitStyle = new GUIStyle(GUI.skin.label);
            splitStyle.fontSize = 10;
            splitStyle.alignment = TextAnchor.MiddleCenter;

            Rect labelRect = GUILayoutUtility.GetRect(width - 20, 14);
            splitStyle.normal.textColor = new Color(0.85f, 0.4f, 0.15f);
            GUI.Label(new Rect(labelRect.x, labelRect.y, labelRect.width / 2, 14),
                $"Dirty: ${dirty:N0}", splitStyle);

            splitStyle.normal.textColor = PhoneController.AccentGreen;
            GUI.Label(new Rect(labelRect.x + labelRect.width / 2, labelRect.y, labelRect.width / 2, 14),
                $"Clean: ${clean:N0}", splitStyle);
        }

        private void DrawDailySummary(float width)
        {
            // Reset daily tracking every 600s (1 game day)
            if (Time.time - _lastDayReset > 600f)
            {
                _todayIncome = 0;
                _todayExpenses = 0;
                _lastDayReset = Time.time;
            }

            Rect summaryRect = GUILayoutUtility.GetRect(width - 20, 40);
            GUI.color = new Color(0.12f, 0.12f, 0.17f);
            GUI.DrawTexture(summaryRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            float halfW = summaryRect.width / 2;

            // Income
            GUIStyle headerS = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            headerS.normal.textColor = PhoneController.DimText;
            GUI.Label(new Rect(summaryRect.x + 8, summaryRect.y + 2, halfW, 14), "TODAY'S INCOME", headerS);

            GUIStyle valueS = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            valueS.normal.textColor = PhoneController.AccentGreen;
            GUI.Label(new Rect(summaryRect.x + 8, summaryRect.y + 18, halfW, 18), $"+${_todayIncome:N0}", valueS);

            // Expenses
            GUI.Label(new Rect(summaryRect.x + halfW, summaryRect.y + 2, halfW, 14), "TODAY'S EXPENSES", headerS);
            valueS.normal.textColor = PhoneController.AccentRed;
            GUI.Label(new Rect(summaryRect.x + halfW, summaryRect.y + 18, halfW, 18),
                $"-${_todayExpenses:N0}", valueS);
        }

        private void DrawLaunderingStatus(float width)
        {
            float totalLaundered = _cashManager != null ? _cashManager.TotalLaundered : 0;
            float totalEarned = _cashManager != null ? _cashManager.TotalEarned : 0;
            float efficiency = totalEarned > 0 ? totalLaundered / totalEarned : 0;

            Rect launderRect = GUILayoutUtility.GetRect(width - 20, 30);
            GUI.color = new Color(0.12f, 0.12f, 0.17f);
            GUI.DrawTexture(launderRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle lStyle = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            lStyle.normal.textColor = PhoneController.DimText;
            GUI.Label(new Rect(launderRect.x + 8, launderRect.y + 2, launderRect.width * 0.5f, 14),
                "LAUNDERING", lStyle);

            lStyle.normal.textColor = PhoneController.TextCol;
            GUI.Label(new Rect(launderRect.x + 8, launderRect.y + 14, launderRect.width * 0.5f, 14),
                $"Total: ${totalLaundered:N0}", lStyle);

            lStyle.alignment = TextAnchor.MiddleRight;
            lStyle.normal.textColor = efficiency > 0.5f ? PhoneController.AccentGreen : PhoneController.AccentGold;
            GUI.Label(new Rect(launderRect.x + launderRect.width * 0.5f, launderRect.y + 4,
                launderRect.width * 0.45f, 20),
                $"Efficiency: {efficiency:P0}", lStyle);
        }

        private void DrawTransactionLog(float width)
        {
            if (_recentTransactions.Count == 0)
            {
                GUI.color = PhoneController.DimText;
                GUILayout.Space(20);
                GUILayout.Label("No transactions yet.\nStart dealing to see financial activity.",
                    PhoneController.Instance.BodyStyle);
                GUI.color = Color.white;
                return;
            }

            foreach (var tx in _recentTransactions)
            {
                DrawTransactionEntry(tx, width);
            }
        }

        private void DrawTransactionEntry(TransactionRecord tx, float width)
        {
            Rect entryRect = GUILayoutUtility.GetRect(width - 30, 24);

            bool isIncome = tx.amount > 0;
            Color amountColor = isIncome ? PhoneController.AccentGreen : PhoneController.AccentRed;

            // Time ago
            float secondsAgo = Time.time - tx.timestamp;
            string timeStr;
            if (secondsAgo < 60) timeStr = $"{secondsAgo:F0}s ago";
            else if (secondsAgo < 3600) timeStr = $"{secondsAgo / 60:F0}m ago";
            else timeStr = $"{secondsAgo / 3600:F1}h ago";

            GUIStyle timeStyle = new GUIStyle(GUI.skin.label) { fontSize = 9 };
            timeStyle.normal.textColor = PhoneController.DimText;
            GUI.Label(new Rect(entryRect.x, entryRect.y + 2, 50, 18), timeStr, timeStyle);

            // Source
            GUIStyle srcStyle = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            srcStyle.normal.textColor = PhoneController.TextCol;
            string source = tx.source ?? "Unknown";
            if (source.Length > 30) source = source.Substring(0, 27) + "...";
            GUI.Label(new Rect(entryRect.x + 52, entryRect.y + 2, entryRect.width * 0.5f, 18), source, srcStyle);

            // Amount
            GUIStyle amtStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight };
            amtStyle.normal.textColor = amountColor;
            string sign = isIncome ? "+" : "";
            GUI.Label(new Rect(entryRect.x + entryRect.width * 0.6f, entryRect.y + 1,
                entryRect.width * 0.38f, 20),
                $"{sign}${tx.amount:N0}", amtStyle);

            // Separator
            GUI.color = new Color(0.2f, 0.2f, 0.25f);
            GUI.DrawTexture(new Rect(entryRect.x, entryRect.yMax - 1, entryRect.width, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // ─── Data Structures ─────────────────────────────────────

        private struct TransactionRecord
        {
            public float amount;
            public string source;
            public float timestamp;
            public float balanceAfter;
            public bool isDirty;
        }
    }
}
