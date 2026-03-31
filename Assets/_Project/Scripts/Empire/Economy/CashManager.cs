using UnityEngine;
using System;
using Clout.Utils;

namespace Clout.Empire.Economy
{
    /// <summary>
    /// Per-player wallet — tracks dirty cash (from dealing/crime) and clean cash
    /// (from laundering through businesses). All money flows through this singleton.
    ///
    /// Phase 2 singleplayer — FishNet SyncVars will be restored in Phase 4.
    /// </summary>
    public class CashManager : MonoBehaviour
    {
        public static CashManager Instance { get; private set; }

        [Header("Starting Cash")]
        public float startingDirtyCash = 500f;
        public float startingCleanCash = 0f;

        [Header("Config")]
        public float maxCash = 10_000_000f;

        // Backing fields (Phase 4: SyncVar<float>)
        private float _dirtyCash;
        private float _cleanCash;

        // Lifetime tracking
        private float _totalEarned;
        private float _totalSpent;
        private float _totalLaundered;

        // ─── Events ──────────────────────────────────────────
        public event Action OnCashChanged;
        public event Action<Transaction> OnTransaction;

        // ─── Properties ──────────────────────────────────────
        public float DirtyCash => _dirtyCash;
        public float CleanCash => _cleanCash;
        public float TotalCash => _dirtyCash + _cleanCash;
        public float TotalEarned => _totalEarned;
        public float TotalSpent => _totalSpent;
        public float TotalLaundered => _totalLaundered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            _dirtyCash = startingDirtyCash;
            _cleanCash = startingCleanCash;
        }

        // ─── Earning ─────────────────────────────────────────

        /// <summary>
        /// Add dirty cash from deals, crime, etc.
        /// </summary>
        public void EarnDirty(float amount, string source)
        {
            if (amount <= 0) return;
            _dirtyCash = Mathf.Min(_dirtyCash + amount, maxCash);
            _totalEarned += amount;

            var tx = new Transaction
            {
                type = TransactionType.Income,
                amount = amount,
                isDirty = true,
                source = source,
                timestamp = Time.time,
                balanceAfter = TotalCash
            };

            OnTransaction?.Invoke(tx);
            OnCashChanged?.Invoke();
            EventBus.Publish(new MoneyChangedEvent
            {
                totalCash = TotalCash,
                dirtyCash = _dirtyCash,
                cleanCash = _cleanCash,
                changeAmount = amount,
                source = source
            });
        }

        /// <summary>
        /// Add clean cash from legitimate sources (laundered, business income).
        /// </summary>
        public void EarnClean(float amount, string source)
        {
            if (amount <= 0) return;
            _cleanCash = Mathf.Min(_cleanCash + amount, maxCash);
            _totalEarned += amount;

            var tx = new Transaction
            {
                type = TransactionType.Income,
                amount = amount,
                isDirty = false,
                source = source,
                timestamp = Time.time,
                balanceAfter = TotalCash
            };

            OnTransaction?.Invoke(tx);
            OnCashChanged?.Invoke();
            EventBus.Publish(new MoneyChangedEvent
            {
                totalCash = TotalCash,
                dirtyCash = _dirtyCash,
                cleanCash = _cleanCash,
                changeAmount = amount,
                source = source
            });
        }

        // ─── Spending ────────────────────────────────────────

        /// <summary>
        /// Check if the player can afford a purchase.
        /// </summary>
        public bool CanAfford(float amount)
        {
            return TotalCash >= amount;
        }

        /// <summary>
        /// Check if player has enough clean cash specifically.
        /// Some purchases (property, legal) require clean money.
        /// </summary>
        public bool CanAffordClean(float amount)
        {
            return _cleanCash >= amount;
        }

        /// <summary>
        /// Spend cash. Draws from clean first, then dirty.
        /// Returns true if successful.
        /// </summary>
        public bool Spend(float amount, string reason)
        {
            if (!CanAfford(amount)) return false;

            float remaining = amount;

            // Draw from clean first (preferred for legit purchases)
            float fromClean = Mathf.Min(_cleanCash, remaining);
            _cleanCash -= fromClean;
            remaining -= fromClean;

            // Then dirty
            if (remaining > 0)
            {
                _dirtyCash -= remaining;
            }

            _totalSpent += amount;

            var tx = new Transaction
            {
                type = TransactionType.Expense,
                amount = amount,
                isDirty = remaining > 0, // Was any dirty money used?
                source = reason,
                timestamp = Time.time,
                balanceAfter = TotalCash
            };

            OnTransaction?.Invoke(tx);
            OnCashChanged?.Invoke();
            EventBus.Publish(new MoneyChangedEvent
            {
                totalCash = TotalCash,
                dirtyCash = _dirtyCash,
                cleanCash = _cleanCash,
                changeAmount = -amount,
                source = reason
            });

            return true;
        }

        /// <summary>
        /// Spend only dirty cash (for illegal purchases like ingredients from shady suppliers).
        /// </summary>
        public bool SpendDirty(float amount, string reason)
        {
            if (_dirtyCash < amount) return false;
            _dirtyCash -= amount;
            _totalSpent += amount;

            var tx = new Transaction
            {
                type = TransactionType.Expense,
                amount = amount,
                isDirty = true,
                source = reason,
                timestamp = Time.time,
                balanceAfter = TotalCash
            };

            OnTransaction?.Invoke(tx);
            OnCashChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Spend only clean cash (for legal purchases like property, upgrades).
        /// </summary>
        public bool SpendClean(float amount, string reason)
        {
            if (_cleanCash < amount) return false;
            _cleanCash -= amount;
            _totalSpent += amount;

            var tx = new Transaction
            {
                type = TransactionType.Expense,
                amount = amount,
                isDirty = false,
                source = reason,
                timestamp = Time.time,
                balanceAfter = TotalCash
            };

            OnTransaction?.Invoke(tx);
            OnCashChanged?.Invoke();
            return true;
        }

        // ─── Laundering ──────────────────────────────────────

        /// <summary>
        /// Convert dirty cash to clean through a business front.
        /// Returns the clean amount received (after laundering fee).
        /// </summary>
        public float Launder(float dirtyAmount, float feeRate, string businessName)
        {
            if (dirtyAmount <= 0 || _dirtyCash < dirtyAmount) return 0;

            _dirtyCash -= dirtyAmount;
            float fee = dirtyAmount * Mathf.Clamp01(feeRate);
            float cleanAmount = dirtyAmount - fee;
            _cleanCash += cleanAmount;
            _totalLaundered += dirtyAmount;

            var tx = new Transaction
            {
                type = TransactionType.Laundering,
                amount = dirtyAmount,
                isDirty = true,
                source = $"Laundered through {businessName} (fee: ${fee:F0})",
                timestamp = Time.time,
                balanceAfter = TotalCash
            };

            OnTransaction?.Invoke(tx);
            OnCashChanged?.Invoke();

            Debug.Log($"[Cash] Laundered ${dirtyAmount:F0} → ${cleanAmount:F0} clean via {businessName}");
            return cleanAmount;
        }

        // ─── Penalties ───────────────────────────────────────

        /// <summary>
        /// Lose cash from being arrested, robbed, or fined.
        /// Takes from dirty first (cops seize illegal money).
        /// </summary>
        public float Confiscate(float amount, string reason)
        {
            float taken = 0;

            float fromDirty = Mathf.Min(_dirtyCash, amount);
            _dirtyCash -= fromDirty;
            taken += fromDirty;

            float remaining = amount - fromDirty;
            if (remaining > 0)
            {
                float fromClean = Mathf.Min(_cleanCash, remaining);
                _cleanCash -= fromClean;
                taken += fromClean;
            }

            if (taken > 0)
            {
                var tx = new Transaction
                {
                    type = TransactionType.Confiscation,
                    amount = taken,
                    isDirty = true,
                    source = reason,
                    timestamp = Time.time,
                    balanceAfter = TotalCash
                };

                OnTransaction?.Invoke(tx);
                OnCashChanged?.Invoke();
                Debug.Log($"[Cash] Confiscated ${taken:F0} — {reason}");
            }

            return taken;
        }

        // ─── Debug ───────────────────────────────────────────

        /// <summary>
        /// Force-set cash values (editor/debug only).
        /// </summary>
        public void DebugSetCash(float dirty, float clean)
        {
            _dirtyCash = dirty;
            _cleanCash = clean;
            OnCashChanged?.Invoke();
        }
    }

    // ─── Transaction Types ───────────────────────────────────

    public enum TransactionType
    {
        Income,
        Expense,
        Laundering,
        Confiscation,
        Transfer
    }

    [System.Serializable]
    public struct Transaction
    {
        public TransactionType type;
        public float amount;
        public bool isDirty;
        public string source;
        public float timestamp;
        public float balanceAfter;
    }
}
