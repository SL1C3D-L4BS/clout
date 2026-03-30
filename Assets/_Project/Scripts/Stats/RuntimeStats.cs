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
        [SyncVar] public int health = 100;
        public int maxHealth = 100;

        [Header("Stamina")]
        [SyncVar] public float stamina = 100f;
        public float maxStamina = 100f;
        public float staminaRegenRate = 15f;
        public float staminaRegenDelay = 1.5f;
        private float _staminaRegenTimer;

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
            health -= Mathf.CeilToInt(mitigated);
            health = Mathf.Max(0, health);
            OnHealthChanged?.Invoke();

            if (health <= 0)
                OnDeath?.Invoke();
        }

        public bool ConsumeStamina(float amount)
        {
            if (stamina < amount) return false;
            stamina -= amount;
            _staminaRegenTimer = staminaRegenDelay;
            OnStaminaChanged?.Invoke();
            return true;
        }

        public void Heal(int amount)
        {
            health = Mathf.Min(health + amount, maxHealth);
            OnHealthChanged?.Invoke();
        }

        private void Update()
        {
            // Stamina regen
            if (stamina < maxStamina)
            {
                _staminaRegenTimer -= Time.deltaTime;
                if (_staminaRegenTimer <= 0)
                {
                    stamina = Mathf.Min(stamina + staminaRegenRate * Time.deltaTime, maxStamina);
                    OnStaminaChanged?.Invoke();
                }
            }
        }
    }
}
