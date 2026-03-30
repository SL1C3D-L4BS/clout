using UnityEngine;
using Clout.Core;
using Clout.Stats;

namespace Clout.Inventory
{
    /// <summary>
    /// Consumable item system — ported from Sharp Accent's Consumable + ConsumableHolder + AddStatOverTime.
    /// Upgraded for CLOUT: healing items, food from fronts, drug sampling (risk/reward),
    /// armor vests (temp damage reduction), energy drinks (temp stamina boost).
    ///
    /// Supports: instant effects, effects over time (Sharp Accent's tick-based pattern),
    /// limited uses, animation playback, VFX on consume.
    /// </summary>
    [CreateAssetMenu(menuName = "CLOUT/Items/Consumable")]
    public class ConsumableItem : ScriptableObject
    {
        [Header("Identity")]
        public string itemName;
        public Sprite icon;
        public GameObject worldPrefab;

        [Header("Animations")]
        public string consumeAnimation = "consume_item";
        public string emptyAnimation = "consume_empty";

        [Header("Effects")]
        public ConsumableEffect[] effects;

        [Header("Usage")]
        public int maxUses = 3;
        public float cooldown = 1f;

        /// <summary>Apply all effects to the target.</summary>
        public void OnConsume(CharacterStateManager target)
        {
            if (target == null) return;
            RuntimeStats stats = target.runtimeStats;
            if (stats == null) return;

            foreach (var effect in effects)
            {
                switch (effect.type)
                {
                    case ConsumableEffectType.HealInstant:
                        stats.Heal((int)effect.value);
                        break;
                    case ConsumableEffectType.HealOverTime:
                        // Apply via tick component
                        var hot = target.gameObject.AddComponent<StatOverTimeEffect>();
                        hot.Init(StatOverTimeEffect.StatType.Health, effect.value, effect.duration, effect.tickInterval);
                        break;
                    case ConsumableEffectType.StaminaRestore:
                        stats.RestoreStamina(effect.value);
                        break;
                    case ConsumableEffectType.StaminaBoost:
                        var stam = target.gameObject.AddComponent<StatOverTimeEffect>();
                        stam.Init(StatOverTimeEffect.StatType.Stamina, effect.value, effect.duration, effect.tickInterval);
                        break;
                    case ConsumableEffectType.DamageReduction:
                        // TODO: Wire to damage reduction modifier on RuntimeStats
                        Debug.Log($"[Consumable] {effect.value}% damage reduction for {effect.duration}s");
                        break;
                    case ConsumableEffectType.SpeedBoost:
                        // TODO: Wire to movement speed modifier
                        Debug.Log($"[Consumable] {effect.value}x speed for {effect.duration}s");
                        break;
                }
            }
        }
    }

    [System.Serializable]
    public class ConsumableEffect
    {
        public ConsumableEffectType type;
        public float value;
        [Tooltip("Duration in seconds (0 = instant)")]
        public float duration;
        [Tooltip("Seconds between ticks (for over-time effects)")]
        public float tickInterval = 1f;
    }

    public enum ConsumableEffectType
    {
        HealInstant,
        HealOverTime,
        StaminaRestore,
        StaminaBoost,
        DamageReduction,
        SpeedBoost
    }

    /// <summary>Runtime holder for consumable instances (tracks remaining uses).</summary>
    [System.Serializable]
    public class ConsumableHolder
    {
        public ConsumableItem consumableBase;
        public int remainingUses;
        public bool isUnlimited;

        public ConsumableHolder(ConsumableItem item)
        {
            consumableBase = item;
            remainingUses = item.maxUses;
        }

        public bool CanUse => isUnlimited || remainingUses > 0;

        public void Use(CharacterStateManager target)
        {
            if (!CanUse) return;
            consumableBase.OnConsume(target);
            if (!isUnlimited) remainingUses--;
        }
    }

    /// <summary>
    /// Stat modification over time — ported from Sharp Accent's AddStatOverTime.
    /// Tick-based healing/regen that auto-destroys when complete.
    /// </summary>
    public class StatOverTimeEffect : MonoBehaviour
    {
        public enum StatType { Health, Stamina }

        private StatType _statType;
        private float _valuePerTick;
        private float _duration;
        private float _tickInterval;
        private float _timer;
        private float _elapsed;
        private RuntimeStats _stats;

        public void Init(StatType type, float totalValue, float duration, float tickInterval)
        {
            _statType = type;
            _duration = duration;
            _tickInterval = Mathf.Max(0.1f, tickInterval);
            int tickCount = Mathf.CeilToInt(duration / _tickInterval);
            _valuePerTick = totalValue / tickCount;
            _stats = GetComponent<RuntimeStats>();
        }

        private void Update()
        {
            if (_stats == null) { Destroy(this); return; }

            _elapsed += Time.deltaTime;
            _timer += Time.deltaTime;

            if (_timer >= _tickInterval)
            {
                _timer -= _tickInterval;
                ApplyTick();
            }

            if (_elapsed >= _duration)
                Destroy(this);
        }

        private void ApplyTick()
        {
            switch (_statType)
            {
                case StatType.Health:
                    _stats.Heal((int)_valuePerTick);
                    break;
                case StatType.Stamina:
                    _stats.RestoreStamina(_valuePerTick);
                    break;
            }
        }
    }
}
