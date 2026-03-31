using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;

namespace Clout.Stats
{
    /// <summary>
    /// Per-character runtime stats — health, stamina, and combat values.
    ///
    /// OFFLINE/ONLINE:
    /// - SyncVars replicate when networked
    /// - Offline backing fields used when FishNet isn't running
    /// - All public accessors work in both modes
    /// </summary>
    public class RuntimeStats : NetworkBehaviour
    {
        [Header("Health")]
        public readonly SyncVar<int> health = new SyncVar<int>(100);
        public int maxHealth = 100;

        [Header("Stamina")]
        public readonly SyncVar<float> stamina = new SyncVar<float>(100f);
        public float maxStamina = 100f;
        public float staminaRegenRate = 15f;
        public float staminaRegenDelay = 1.5f;
        private float _staminaRegenTimer;

        [Header("Poise")]
        public float poise = 100f;
        public float maxPoise = 100f;
        public float poiseRegenRate = 20f;
        public float poiseRegenDelay = 2f;
        private float _poiseRegenTimer;
        [HideInInspector] public bool isStaggered;

        [Header("Combat Costs")]
        public float lightAttackCost = 15f;
        public float heavyAttackCost = 25f;
        public float rollCost = 20f;
        public float sprintCostPerSec = 10f;

        [Header("Defense")]
        public float armor = 0f;
        public float bulletResistance = 0f;

        // Offline backing fields
        private int _offlineHealth = 100;
        private float _offlineStamina = 100f;
        private bool _isNetworked;
        private bool _networkChecked;

        public event Action OnHealthChanged;
        public event Action OnStaminaChanged;
        public event Action OnDeath;

        // ─── Network Check ───────────────────────────────────────

        private new bool IsNetworked
        {
            get
            {
                if (!_networkChecked)
                {
                    _networkChecked = true;
                    try { _isNetworked = IsSpawned; }
                    catch { _isNetworked = false; }
                }
                return _isNetworked;
            }
        }

        private void Awake()
        {
            _offlineHealth = maxHealth;
            _offlineStamina = maxStamina;
        }

        // ─── Accessors (work both online and offline) ────────────

        public int Health
        {
            get
            {
                if (IsNetworked) { try { return health.Value; } catch { } }
                return _offlineHealth;
            }
            private set
            {
                if (IsNetworked) { try { health.Value = value; return; } catch { } }
                _offlineHealth = value;
            }
        }

        public float Stamina
        {
            get
            {
                if (IsNetworked) { try { return stamina.Value; } catch { } }
                return _offlineStamina;
            }
            private set
            {
                if (IsNetworked) { try { stamina.Value = value; return; } catch { } }
                _offlineStamina = value;
            }
        }

        // ─── Core Methods ────────────────────────────────────────

        public void TakeDamage(float damage)
        {
            float mitigated = damage * (1f - Mathf.Clamp01(armor / 100f));
            Health = Mathf.Max(0, Health - Mathf.CeilToInt(mitigated));
            OnHealthChanged?.Invoke();

            if (Health <= 0)
                OnDeath?.Invoke();
        }

        public bool ConsumeStamina(float amount)
        {
            if (Stamina < amount) return false;
            Stamina -= amount;
            _staminaRegenTimer = staminaRegenDelay;
            OnStaminaChanged?.Invoke();
            return true;
        }

        public void Heal(int amount)
        {
            Health = Mathf.Min(Health + amount, maxHealth);
            OnHealthChanged?.Invoke();
        }

        public void RestoreStamina(float amount)
        {
            Stamina = Mathf.Min(Stamina + amount, maxStamina);
            OnStaminaChanged?.Invoke();
        }

        /// <summary>
        /// Handle stamina regen/drain. Called by HandleStats action each frame.
        /// </summary>
        public void HandleStamina(float delta, bool isSprinting)
        {
            if (isSprinting)
            {
                Stamina = Mathf.Max(0, Stamina - sprintCostPerSec * delta);
                _staminaRegenTimer = staminaRegenDelay;
                OnStaminaChanged?.Invoke();
                return;
            }

            if (Stamina < maxStamina)
            {
                _staminaRegenTimer -= delta;
                if (_staminaRegenTimer <= 0)
                {
                    Stamina = Mathf.Min(Stamina + staminaRegenRate * delta, maxStamina);
                    OnStaminaChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Handle poise recovery. Called by HandleStats action each frame.
        /// </summary>
        public void HandlePoise(float delta)
        {
            if (poise < maxPoise)
            {
                _poiseRegenTimer -= delta;
                if (_poiseRegenTimer <= 0)
                {
                    poise = Mathf.Min(poise + poiseRegenRate * delta, maxPoise);
                    if (poise >= maxPoise * 0.5f)
                        isStaggered = false;
                }
            }
        }

        /// <summary>
        /// Apply poise damage — stagger when poise breaks.
        /// </summary>
        public void ApplyPoiseDamage(float amount)
        {
            poise -= amount;
            _poiseRegenTimer = poiseRegenDelay;

            if (poise <= 0)
            {
                poise = 0;
                isStaggered = true;
            }
        }

        private void Update()
        {
            // Passive stamina regen (fallback when HandleStats isn't running)
            if (Stamina < maxStamina)
            {
                _staminaRegenTimer -= Time.deltaTime;
                if (_staminaRegenTimer <= 0)
                {
                    Stamina = Mathf.Min(Stamina + staminaRegenRate * Time.deltaTime, maxStamina);
                    OnStaminaChanged?.Invoke();
                }
            }
        }
    }
}
