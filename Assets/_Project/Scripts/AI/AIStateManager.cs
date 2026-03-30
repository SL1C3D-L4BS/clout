using UnityEngine;
using UnityEngine.AI;
using Clout.Core;
using Clout.Combat;
using Clout.Actions;

namespace Clout.AI
{
    /// <summary>
    /// Enemy AI controller — utility theory for action selection.
    /// Same CharacterStateManager base as the player — one architecture, two controllers.
    ///
    /// Clout AI types (derived classes in future):
    /// - RivalGangAI: aggressive, territorial, fight for turf
    /// - PoliceAI: respond to wanted level, escalate force
    /// - CivilianAI: flee from combat, call police
    ///
    /// Aggression level replaces NullReach's corruption — higher aggression means
    /// faster attacks, wider detection, more lethal decisions.
    /// </summary>
    public class AIStateManager : CharacterStateManager
    {
        [Header("AI Detection")]
        public float detectionRadius = 20f;
        public float attackDistance = 2.5f;
        public float chaseSpeed = 3.5f;
        public float patrolSpeed = 1.5f;
        public LayerMask detectionLayer;
        [Tooltip("Layers that block line-of-sight (walls, terrain)")]
        public LayerMask environmentLayer;

        [Header("AI State IDs")]
        public string patrolStateId = "patrol";
        public string chaseStateId = "chase";
        public string rangedAttackStateId = "rangedAttack";

        [Header("Target")]
        public CharacterStateManager currentTarget;

        [Header("AI Combat")]
        public float attackCooldown = 2f;
        [HideInInspector] public float attackCooldownTimer;
        public float aiRotationSpeed = 3f;

        [Header("AI Ranged Combat")]
        public float rangedAttackDistance = 20f;
        public float preferredRangedDistance = 12f;

        [Header("Aggression — Clout")]
        [Range(0f, 1f)]
        [Tooltip("0 = calm patrol, 1 = berserker. Affects detection, speed, cooldowns.")]
        public float aggressionLevel = 0f;

        // States
        public State patrolState;
        public State chaseState;
        public State aiAttackState;
        public State rangedAttackState;
        public State aiStaggerState;
        public State aiDeathState;

        protected override void InitStates()
        {
            locomotionStateId = patrolStateId;

            if (agent != null)
            {
                agent.updatePosition = true;
                agent.updateRotation = false;
                agent.speed = patrolSpeed;
                agent.stoppingDistance = attackDistance * 0.8f;
            }

            if (lockOnTarget == null)
            {
                Transform lockRef = transform.Find("LockOnRef");
                lockOnTarget = lockRef != null ? lockRef : transform;
            }

            // === PATROL STATE ===
            patrolState = new State { id = patrolStateId };
            patrolState.updateActions.Add(new Actions.AIDetection());
            patrolState.updateActions.Add(new Actions.AIPatrol());
            patrolState.onEnter = () =>
            {
                if (agent != null) agent.speed = patrolSpeed;
            };
            RegisterState(patrolState);

            // === CHASE STATE ===
            chaseState = new State { id = chaseStateId };
            chaseState.updateActions.Add(new Actions.AIDetection());

            WeaponItem startWeapon = weaponHolderManager?.rightItem;
            if (startWeapon != null && startWeapon.HasRangedCapability)
                chaseState.updateActions.Add(new Actions.AICombatSelector());
            else
                chaseState.updateActions.Add(new Actions.AIChaseTarget());

            chaseState.onEnter = () =>
            {
                if (agent != null) agent.speed = chaseSpeed;
            };
            RegisterState(chaseState);

            // === RANGED ATTACK STATE ===
            rangedAttackState = new State { id = rangedAttackStateId };
            rangedAttackState.updateActions.Add(new Actions.AIDetection());
            rangedAttackState.updateActions.Add(new Actions.AIRangedAttack());
            rangedAttackState.onEnter = () =>
            {
                isAiming = true;
                if (agent != null) agent.speed = chaseSpeed * 0.6f;
                anim?.SetBool("isAiming", true);
                anim?.SetBool("lockOn", true);
            };
            rangedAttackState.onExit = () =>
            {
                isAiming = false;
                anim?.SetBool("isAiming", false);
                anim?.SetBool("lockOn", false);
            };
            RegisterState(rangedAttackState);

            // === ATTACK STATE ===
            aiAttackState = new State { id = attackStateId };
            aiAttackState.updateActions.Add(new MonitorInteraction());
            aiAttackState.updateActions.Add(new HandleRotation());
            aiAttackState.onEnter = () =>
            {
                useRootMotion = true;
                canRotate = false;
                if (agent != null) agent.velocity = Vector3.zero;
            };
            aiAttackState.onExit = () =>
            {
                useRootMotion = false;
                attackCooldownTimer = attackCooldown;
                attackCooldownTimer *= (1f - aggressionLevel * 0.5f);
            };
            RegisterState(aiAttackState);

            // === STAGGER STATE ===
            aiStaggerState = new State { id = staggerStateId };
            aiStaggerState.updateActions.Add(new MonitorInteraction());
            aiStaggerState.onEnter = () =>
            {
                useRootMotion = true;
                isInteracting = true;
                if (agent != null) agent.velocity = Vector3.zero;
            };
            RegisterState(aiStaggerState);

            // === DEATH STATE ===
            aiDeathState = new State { id = deathStateId };
            aiDeathState.onEnter = () =>
            {
                isDead = true;
                isInteracting = true;
                if (agent != null) agent.enabled = false;

                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            };
            RegisterState(aiDeathState);

            ChangeState(patrolStateId);
        }

        protected override void Update()
        {
            if (isDead) return;
            base.Update();

            if (attackCooldownTimer > 0)
                attackCooldownTimer -= Time.deltaTime;
        }

        public override void OnDamage(DamageEvent damageEvent)
        {
            base.OnDamage(damageEvent);

            if (isDead) return;

            if (damageEvent.attacker != null && currentTarget == null)
            {
                currentTarget = damageEvent.attacker;
                ChangeState(chaseStateId);
            }
        }

        public override void PlayTargetItemAction(AttackInputs attackInput)
        {
            WeaponItem weapon = weaponHolderManager?.rightItem;
            if (weapon == null || weapon.itemActions == null)
            {
                PlayTargetAnimation("oh_attack_1", true);
                ChangeState(attackStateId);
                return;
            }

            // Specific input requested (from AIRangedAttack)
            if (attackInput != AttackInputs.none && attackInput != AttackInputs.rb)
            {
                for (int i = 0; i < weapon.itemActions.Length; i++)
                {
                    if (weapon.itemActions[i] == null || weapon.itemActions[i].itemAction == null)
                        continue;
                    if (weapon.itemActions[i].attackInput == attackInput)
                    {
                        weapon.itemActions[i].itemActual = weapon;
                        weapon.itemActions[i].ExecuteItemAction(this);
                        return;
                    }
                }
            }

            // Utility-theory scoring
            Actions.ScoredAction bestAction = Actions.AIActionScoring.ScoreBestAction(this);
            if (bestAction.IsValid)
            {
                for (int i = 0; i < weapon.itemActions.Length; i++)
                {
                    if (weapon.itemActions[i] == null || weapon.itemActions[i].itemAction == null)
                        continue;
                    if (weapon.itemActions[i].attackInput == bestAction.input)
                    {
                        weapon.itemActions[i].itemActual = weapon;
                        weapon.itemActions[i].ExecuteItemAction(this);
                        return;
                    }
                }
            }

            // Fallback — random valid action
            int validCount = 0;
            for (int i = 0; i < weapon.itemActions.Length; i++)
            {
                if (weapon.itemActions[i] != null && weapon.itemActions[i].itemAction != null)
                    validCount++;
            }

            if (validCount == 0)
            {
                PlayTargetAnimation("oh_attack_1", true);
                ChangeState(attackStateId);
                return;
            }

            int pick = Random.Range(0, weapon.itemActions.Length);
            int attempts = 0;
            while ((weapon.itemActions[pick] == null || weapon.itemActions[pick].itemAction == null) && attempts < 10)
            {
                pick = Random.Range(0, weapon.itemActions.Length);
                attempts++;
            }

            if (weapon.itemActions[pick] != null)
            {
                weapon.itemActions[pick].itemActual = weapon;
                weapon.itemActions[pick].ExecuteItemAction(this);
            }
        }
    }
}
