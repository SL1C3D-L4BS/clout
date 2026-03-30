using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;

namespace Clout.Stats
{
    /// <summary>
    /// Per-character runtime stats — health, stamina, and combat values.
    /// Server-authoritative via FishNet SyncVars.
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

        public event Action OnHealthChanged;
        public event Action OnStaminaChanged;
        public event Action OnDeath;

        public void TakeDamage(float damage)
        {
            float mitigated = damage * (1f - Mathf.Clamp01(armor / 100f));
            health.Value -= Mathf.CeilToInt(mitigated);
            health.Value = Mathf.Max(0, health.Value);
            OnHealthChanged?.Invoke();

            if (health.Value <= 0)
                OnDeath?.Invoke();
        }

        public bool ConsumeStamina(float amount)
        {
            if (stamina.Value < amount) return false;
            stamina.Value -= amount;
            _staminaRegenTimer = staminaRegenDelay;
            OnStaminaChanged?.Invoke();
            return true;
        }

        public void Heal(int amount)
        {
            health.Value = Mathf.Min(health.Value + amount, maxHealth);
            OnHealthChanged?.Invoke();
        }

        public void RestoreStamina(float amount)
        {
            stamina.Value = Mathf.Min(stamina.Value + amount, maxStamina);
            OnStaminaChanged?.Invoke();
        }

        /// <summary>
        /// Handle stamina regen/drain. Called by HandleStats action each frame.
        /// </summary>
        public void HandleStamina(float delta, bool isSprinting)
        {
            if (isSprinting)
            {
                stamina.Value -= sprintCostPerSec * delta;
                stamina.Value = Mathf.Max(0, stamina.Value);
                _staminaRegenTimer = staminaRegenDelay;
                OnStaminaChanged?.Invoke();
                return;
            }

            if (stamina.Value < maxStamina)
            {
                _staminaRegenTimer -= delta;
                if (_staminaRegenTimer <= 0)
                {
                    stamina.Value = Mathf.Min(stamina.Value + staminaRegenRate * delta, maxStamina);
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
            if (stamina.Value < maxStamina)
            {
                _staminaRegenTimer -= Time.deltaTime;
                if (_staminaRegenTimer <= 0)
                {
                    stamina.Value = Mathf.Min(stamina.Value + staminaRegenRate * Time.deltaTime, maxStamina);
                    OnStaminaChanged?.Invoke();
                }
            }
        }
    }
}
