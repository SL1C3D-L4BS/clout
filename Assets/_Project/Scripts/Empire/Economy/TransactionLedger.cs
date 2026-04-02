using UnityEngine;
using System;
using System.Collections.Generic;

namespace Clout.Empire.Economy
{
    /// <summary>
    /// Financial record-keeping — tracks every transaction for profit/loss analysis,
    /// phone UI finance tab, and save/load persistence.
    ///
    /// Maintains rolling windows for daily/weekly/all-time metrics.
    /// Used by Phone UI (Step 9) for financial reports.
    /// </summary>
    public class TransactionLedger : MonoBehaviour
    {
        public static TransactionLedger Instance { get; private set; }

        [Header("Config")]
        public int maxLedgerEntries = 500;
        public float gameDayDuration = 600f; // 10 real minutes = 1 game day

        // Full transaction history (capped)
        private readonly List<Transaction> _ledger = new List<Transaction>();

        // Rolling metrics
        private float _dayStartTime;
        private float _dayIncome;
        private float _dayExpenses;
        private float _weekIncome;
        private float _weekExpenses;
        private int _currentDay;

        // Category tracking
        private readonly Dictionary<string, float> _incomeBySource = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _expenseBySource = new Dictionary<string, float>();

        // ─── Properties ──────────────────────────────────────
        public float DailyIncome => _dayIncome;
        public float DailyExpenses => _dayExpenses;
        public float DailyProfit => _dayIncome - _dayExpenses;
        public float WeeklyIncome => _weekIncome;
        public float WeeklyExpenses => _weekExpenses;
        public float WeeklyProfit => _weekIncome - _weekExpenses;
        public int CurrentDay => _currentDay;
        public IReadOnlyList<Transaction> RecentTransactions => _ledger;

        // ─── Events ──────────────────────────────────────────
        public event Action OnDayEnd;
        public event Action<Transaction> OnTransactionRecorded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            _dayStartTime = Time.time;
        }

        private void Start()
        {
            // Subscribe to CashManager transactions
            if (CashManager.Instance != null)
                CashManager.Instance.OnTransaction += RecordTransaction;
        }

        private void OnDestroy()
        {
            if (CashManager.Instance != null)
                CashManager.Instance.OnTransaction -= RecordTransaction;
        }

        private void Update()
        {
            // Day cycle
            if (Time.time - _dayStartTime >= gameDayDuration)
            {
                EndDay();
            }
        }

        // ─── Recording ───────────────────────────────────────

        public void RecordTransaction(Transaction tx)
        {
            _ledger.Add(tx);

            // Cap ledger size
            if (_ledger.Count > maxLedgerEntries)
                _ledger.RemoveAt(0);

            // Update daily metrics
            switch (tx.type)
            {
                case TransactionType.Income:
                    _dayIncome += tx.amount;
                    _weekIncome += tx.amount;
                    AddToDict(_incomeBySource, tx.source, tx.amount);
                    break;

                case TransactionType.Expense:
                    _dayExpenses += tx.amount;
                    _weekExpenses += tx.amount;
                    AddToDict(_expenseBySource, tx.source, tx.amount);
                    break;

                case TransactionType.Laundering:
                    // Laundering doesn't count as income/expense — it's a conversion
                    break;

                case TransactionType.Confiscation:
                    _dayExpenses += tx.amount;
                    _weekExpenses += tx.amount;
                    AddToDict(_expenseBySource, "confiscation", tx.amount);
                    break;
            }

            OnTransactionRecorded?.Invoke(tx);
        }

        // ─── Day Cycle ───────────────────────────────────────

        private void EndDay()
        {
            _currentDay++;
            Debug.Log($"[Ledger] Day {_currentDay} ended — Income: ${_dayIncome:F0}, " +
                      $"Expenses: ${_dayExpenses:F0}, Profit: ${DailyProfit:F0}");

            OnDayEnd?.Invoke();

            // Reset daily
            _dayIncome = 0;
            _dayExpenses = 0;
            _dayStartTime = Time.time;

            // Reset weekly every 7 days
            if (_currentDay % 7 == 0)
            {
                _weekIncome = 0;
                _weekExpenses = 0;
            }
        }

        // ─── Queries ─────────────────────────────────────────

        /// <summary>
        /// Get the top income sources for the current period.
        /// </summary>
        public List<KeyValuePair<string, float>> GetTopIncomeSources(int count = 5)
        {
            var sorted = new List<KeyValuePair<string, float>>(_incomeBySource);
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
            return sorted.GetRange(0, Mathf.Min(count, sorted.Count));
        }

        /// <summary>
        /// Get the top expense categories for the current period.
        /// </summary>
        public List<KeyValuePair<string, float>> GetTopExpenseSources(int count = 5)
        {
            var sorted = new List<KeyValuePair<string, float>>(_expenseBySource);
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
            return sorted.GetRange(0, Mathf.Min(count, sorted.Count));
        }

        /// <summary>
        /// Get recent transactions of a specific type.
        /// </summary>
        public List<Transaction> GetRecentByType(TransactionType type, int count = 10)
        {
            var result = new List<Transaction>();
            for (int i = _ledger.Count - 1; i >= 0 && result.Count < count; i--)
            {
                if (_ledger[i].type == type)
                    result.Add(_ledger[i]);
            }
            return result;
        }

        private static void AddToDict(Dictionary<string, float> dict, string key, float amount)
        {
            // Normalize source to category
            string category = NormalizeCategory(key);
            if (dict.ContainsKey(category))
                dict[category] += amount;
            else
                dict[category] = amount;
        }

        private static string NormalizeCategory(string source)
        {
            if (string.IsNullOrEmpty(source)) return "other";
            string lower = source.ToLower();
            if (lower.Contains("deal")) return "dealing";
            if (lower.Contains("ingredient") || lower.Contains("supply")) return "supplies";
            if (lower.Contains("launder")) return "laundering";
            if (lower.Contains("property") || lower.Contains("rent")) return "property";
            if (lower.Contains("wage") || lower.Contains("employee")) return "wages";
            if (lower.Contains("confiscat") || lower.Contains("arrest") || lower.Contains("fine")) return "confiscation";
            if (lower.Contains("shop")) return "shop";
            return source.Length > 20 ? source.Substring(0, 20) : source;
        }
    }
}
