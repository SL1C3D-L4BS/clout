using UnityEngine;
using Clout.Core;
using Clout.Empire.Properties;

namespace Clout.Empire.Employees
{
    /// <summary>
    /// Runtime mutable state for a single hired worker. Created when WorkerManager.HireWorker()
    /// is called, destroyed when worker is fired/killed/arrested.
    ///
    /// The EmployeeDefinition SO provides base stats; this tracks the evolving state:
    /// skill growth, loyalty drift, accumulated earnings, shift history, etc.
    ///
    /// Spec v2.0 Section 13: Workers are nodes in a directed weighted graph.
    /// WorkerInstance holds the per-node mutable data.
    /// </summary>
    [System.Serializable]
    public class WorkerInstance
    {
        // ─── Identity ────────────────────────────────────────────────

        public string workerId;
        public string workerName;
        public EmployeeDefinition definition;
        public EmployeeRole role;

        // ─── Assignment ──────────────────────────────────────────────

        public Property assignedProperty;
        public string assignedPropertyId;

        // ─── Mutable Stats (evolve from base definition) ─────────────

        public float skill;
        public float loyalty;
        public float discretion;
        public float greed;
        public float courage;
        public float intelligence;
        public float corruptibility;
        public float ambition;

        // ─── Work State ──────────────────────────────────────────────

        public WorkerState state;
        public float shiftTimer;
        public float restTimer;
        public int shiftsCompleted;
        public int totalDeals;
        public int totalUnitsProduced;
        public float totalCashEarned;
        public float cashOnHand;           // Dealers carry cash until shift end

        // ─── World Reference ─────────────────────────────────────────

        public GameObject worldObject;     // The spawned MonoBehaviour in the scene

        // ─── Compartmentalization ────────────────────────────────────

        public int knowledgeLevel;         // 1 = knows handler only, 5 = knows everything
        public float compartmentalizationFactor => Mathf.Max(1f, knowledgeLevel);

        // ─── Factory ─────────────────────────────────────────────────

        public static WorkerInstance Create(EmployeeDefinition def, Property property, EmployeeRole assignedRole)
        {
            return new WorkerInstance
            {
                workerId = System.Guid.NewGuid().ToString().Substring(0, 8),
                workerName = def.employeeName,
                definition = def,
                role = assignedRole,
                assignedProperty = property,
                assignedPropertyId = property != null && property.Definition != null
                    ? property.Definition.propertyName : "unassigned",

                // Copy base stats (these will evolve at runtime)
                skill = def.skill,
                loyalty = def.loyalty,
                discretion = def.discretion,
                greed = def.greed,
                courage = def.courage,
                intelligence = def.intelligence,
                corruptibility = def.corruptibility,
                ambition = def.ambition,

                state = WorkerState.Idle,
                knowledgeLevel = 1  // New hires know minimal info
            };
        }

        // ─── Stat Evolution ──────────────────────────────────────────

        /// <summary>
        /// Improve skill through work. Logarithmic growth — diminishing returns near 1.0.
        /// Called after each completed shift.
        /// </summary>
        public void ImproveSkill()
        {
            float learningRate = definition.LearningRate;
            skill = Mathf.Clamp01(skill + learningRate * (1f - skill));
        }

        /// <summary>
        /// Loyalty drifts based on treatment. Positive = fair wages + no danger.
        /// Negative = unpaid, exposed to raids, seeing peers fired.
        /// </summary>
        public void ModifyLoyalty(float amount)
        {
            loyalty = Mathf.Clamp01(loyalty + amount);
        }

        /// <summary>
        /// Daily betrayal check per Spec v2.0 Section 13:
        ///   P(betray) = (Greed + ExternalOffer - Loyalty) × (1 - PlayerBetrayalSuppression) / Compartmentalization
        /// Returns true if the worker betrays this day.
        /// </summary>
        public bool CheckBetrayal(float playerBetrayalSuppression, float externalOfferValue = 0f)
        {
            float numerator = greed + externalOfferValue - loyalty;
            float suppression = 1f - Mathf.Clamp01(playerBetrayalSuppression);
            float probability = Mathf.Clamp01((numerator * suppression) / compartmentalizationFactor);

            // Scale by base betrayal chance so it's not too frequent
            probability *= definition.betrayalChance * 10f;

            return Random.value < probability;
        }

        /// <summary>
        /// Daily arrest check. Discretion and heat affect probability.
        /// </summary>
        public bool CheckArrest(float propertyHeat)
        {
            float probability = definition.EffectiveArrestChance * (1f + propertyHeat * 0.01f);
            return Random.value < probability;
        }
    }

    public enum WorkerState
    {
        Idle,
        Traveling,
        Working,         // Cook: cooking, Guard: patrolling
        Dealing,         // Dealer: actively selling
        Returning,       // Heading back to property
        Resting,         // Between shifts
        Fleeing,         // Running from danger
        Arrested,
        Dead
    }
}
