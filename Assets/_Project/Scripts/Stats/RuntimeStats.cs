using UnityEngine;
using System;

namespace Clout.Stats
{
    /// <summary>
    /// Per-character runtime stats — health, stamina, and combat values.
    /// Phase 2 singleplayer — FishNet SyncVars will be restored in Phase 4.
    /// </summary>
    public class RuntimeStats : MonoBehaviour
    {
        [Header("Health")]
        // SyncVar<int> for Phase 4 multiplayer
        public int maxHealth = 100;

        [Header("Stamina")]
        // SyncVar<float> for Phase 4 multiplayer
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

        // Backing fields
        private int _offlineHealth = 100;
        private float _offlineStamina = 100f;

        public event Action OnHealthChanged;
        public event Action OnStaminaChanged;
        public event Action OnDeath;

        private void Awake()
        {
            _offlineHealth = maxHealth;
            _offlineStamina = maxStamina;
        }

        // ─── Accessors ──────────────────────────────────────────

        public int Health
        {
            get => _offlineHealth;
            private set => _offlineHealth = value;
        }

        public float Stamina
        {
            get => _offlineStamina;
            private set => _offlineStamina = value;
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
