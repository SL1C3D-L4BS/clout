using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Economy;
using Clout.Empire.Properties;
using Clout.Empire.Reputation;
using Clout.Utils;

namespace Clout.Empire.Employees
{
    /// <summary>
    /// Singleton workforce orchestrator — manages all hired workers.
    ///
    /// Responsibilities:
    /// - Hire/fire workers (validated against CLOUT rank + property slots)
    /// - Process daily wages via CashManager
    /// - Run daily betrayal + arrest checks per Spec v2.0 Section 13
    /// - Spawn/despawn worker GameObjects in the world
    /// - Track aggregate workforce stats for UI
    ///
    /// Subscribes to TransactionLedger.OnDayEnd for daily processing.
    /// </summary>
    public class WorkerManager : MonoBehaviour
    {
        public static WorkerManager Instance { get; private set; }

        [Header("Workforce Limits (by CLOUT Rank)")]
        [Tooltip("Max workers at each CLOUT rank. Index = rank.")]
        public int[] maxWorkersByRank = { 1, 2, 4, 7, 11, 15 };

        [Header("Shift Config")]
        public float shiftDuration = 300f;    // 5 min real-time per shift
        public float restDuration = 60f;       // 1 min rest between shifts

        // ─── State ───────────────────────────────────────────────────

        private List<WorkerInstance> _workers = new List<WorkerInstance>();

        // ─── Events ─────────────────────────────────────────────────

        public event System.Action<WorkerInstance> OnWorkerHired;
        public event System.Action<WorkerInstance> OnWorkerFired;
        public event System.Action<WorkerInstance> OnWorkerArrested;
        public event System.Action<WorkerInstance> OnWorkerBetrayed;

        // ─── Properties ──────────────────────────────────────────────

        public IReadOnlyList<WorkerInstance> Workers => _workers;
        public int WorkerCount => _workers.Count;

        // ─── Lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Subscribe to day-end for wage processing and daily checks
            var ledger = FindAnyObjectByType<TransactionLedger>();
            if (ledger != null)
                ledger.OnDayEnd += OnDayEnd;
        }

        private void OnDestroy()
        {
            var ledger = FindAnyObjectByType<TransactionLedger>();
            if (ledger != null)
                ledger.OnDayEnd -= OnDayEnd;

            if (Instance == this) Instance = null;
        }

        // ─── Hire / Fire ─────────────────────────────────────────────

        /// <summary>
        /// Hire a worker from a definition. Spawns them in the world at the assigned property.
        /// Returns null if hire fails (no slots, can't afford, no property).
        /// </summary>
        public WorkerInstance HireWorker(EmployeeDefinition definition, Property property, EmployeeRole role)
        {
            // Validate
            if (definition == null || property == null)
            {
                Debug.LogWarning("[WorkerManager] Cannot hire: null definition or property.");
                return null;
            }

            if (!property.IsOwned)
            {
                Debug.LogWarning("[WorkerManager] Cannot hire: property not owned.");
                return null;
            }

            // Check workforce cap
            int maxWorkers = GetMaxWorkers();
            if (_workers.Count >= maxWorkers)
            {
                Debug.LogWarning($"[WorkerManager] At capacity ({maxWorkers}). Increase CLOUT rank for more slots.");
                return null;
            }

            // Check property employee slots
            int workersAtProperty = GetWorkerCountAtProperty(property);
            if (workersAtProperty >= property.GetMaxEmployeeSlots())
            {
                Debug.LogWarning($"[WorkerManager] Property {property.Definition.propertyName} is full ({workersAtProperty}/{property.GetMaxEmployeeSlots()}).");
                return null;
            }

            // Check funds
            CashManager cash = CashManager.Instance;
            if (cash != null && !cash.CanAfford(definition.hiringCost))
            {
                Debug.LogWarning($"[WorkerManager] Cannot afford hiring cost: ${definition.hiringCost}");
                return null;
            }

            // Pay hiring cost
            if (cash != null)
                cash.Spend(definition.hiringCost, $"Hired {definition.employeeName}");

            // Create instance
            WorkerInstance worker = WorkerInstance.Create(definition, property, role);

            // Spawn world object
            SpawnWorkerInWorld(worker);

            _workers.Add(worker);

            // Events
            OnWorkerHired?.Invoke(worker);
            EventBus.Publish(new WorkerHiredEvent
            {
                workerId = worker.workerId,
                workerType = role.ToString(),
                assignedPropertyId = worker.assignedPropertyId
            });

            // Reputation boost
            var rep = FindAnyObjectByType<ReputationManager>();
            if (rep != null)
            {
                rep.AddClout(ReputationManager.CloutValues.HireEmployee, $"hired {definition.employeeName}");
                rep.ModifyReputationBulk(ReputationManager.ReputationModifiers.HireWorker, "hired worker");
            }

            Debug.Log($"[WorkerManager] Hired {definition.employeeName} as {role} at {property.Definition.propertyName}. Total workers: {_workers.Count}");
            return worker;
        }

        /// <summary>
        /// Fire a worker. Removes from roster, despawns from world.
        /// </summary>
        public void FireWorker(WorkerInstance worker)
        {
            if (worker == null || !_workers.Contains(worker)) return;

            _workers.Remove(worker);
            DespawnWorkerFromWorld(worker);

            OnWorkerFired?.Invoke(worker);
            EventBus.Publish(new WorkerFiredEvent
            {
                workerId = worker.workerId,
                workerName = worker.workerName,
                reason = "fired",
                assignedPropertyId = worker.assignedPropertyId
            });

            Debug.Log($"[WorkerManager] Fired {worker.workerName}. Workers remaining: {_workers.Count}");
        }

        /// <summary>
        /// Reassign a worker to a different property.
        /// </summary>
        public bool ReassignWorker(WorkerInstance worker, Property newProperty)
        {
            if (worker == null || newProperty == null || !newProperty.IsOwned) return false;

            int workersAtNew = GetWorkerCountAtProperty(newProperty);
            if (workersAtNew >= newProperty.GetMaxEmployeeSlots()) return false;

            worker.assignedProperty = newProperty;
            worker.assignedPropertyId = newProperty.Definition.propertyName;
            worker.state = WorkerState.Idle;

            // Teleport worker to new property
            if (worker.worldObject != null)
                worker.worldObject.transform.position = newProperty.transform.position + Vector3.right * 2f;

            return true;
        }

        // ─── Daily Processing ────────────────────────────────────────

        private void OnDayEnd(int dayNumber)
        {
            ProcessDailyWages();
            ProcessDailyChecks();
        }

        /// <summary>
        /// Pay all workers their daily wage. If can't afford, loyalty drops.
        /// </summary>
        private void ProcessDailyWages()
        {
            CashManager cash = CashManager.Instance;
            if (cash == null) return;

            float totalWages = 0f;
            foreach (var worker in _workers)
            {
                float wage = worker.definition.dailyWage * worker.definition.WageDemandMultiplier;
                totalWages += wage;

                if (cash.CanAfford(wage))
                {
                    cash.SpendDirty(wage, $"Wage: {worker.workerName}");
                    worker.ModifyLoyalty(0.01f);  // Paid on time = slight loyalty boost
                }
                else
                {
                    // Can't pay — loyalty tanks
                    worker.ModifyLoyalty(-0.05f);
                    Debug.LogWarning($"[WorkerManager] Cannot pay {worker.workerName}! Loyalty dropping.");
                }
            }
        }

        /// <summary>
        /// Daily betrayal and arrest checks per Spec v2.0.
        /// </summary>
        private void ProcessDailyChecks()
        {
            var rep = FindAnyObjectByType<ReputationManager>();
            float betrayalSuppression = rep != null ? rep.GetBetrayalSuppression() : 0f;

            for (int i = _workers.Count - 1; i >= 0; i--)
            {
                var worker = _workers[i];
                if (worker.state == WorkerState.Arrested || worker.state == WorkerState.Dead) continue;

                // Betrayal check
                if (worker.CheckBetrayal(betrayalSuppression))
                {
                    ProcessBetrayal(worker);
                    continue;
                }

                // Arrest check
                float propertyHeat = 0f; // TODO: get from WantedSystem per-property
                if (worker.CheckArrest(propertyHeat))
                {
                    ProcessArrest(worker);
                }
            }
        }

        private void ProcessBetrayal(WorkerInstance worker)
        {
            // Determine betrayal type based on stats
            string betrayalType;
            float damage;

            if (worker.corruptibility > 0.6f)
            {
                betrayalType = "informant";
                damage = worker.knowledgeLevel * 10f;  // Info leaked scales with knowledge
            }
            else if (worker.greed > 0.6f)
            {
                betrayalType = "theft";
                damage = worker.cashOnHand + worker.totalCashEarned * 0.1f;
            }
            else
            {
                betrayalType = "rival_defection";
                damage = 0f;
            }

            worker.state = WorkerState.Dead; // Effectively removed
            _workers.Remove(worker);
            DespawnWorkerFromWorld(worker);

            OnWorkerBetrayed?.Invoke(worker);
            EventBus.Publish(new WorkerBetrayedEvent
            {
                workerId = worker.workerId,
                workerName = worker.workerName,
                betrayalType = betrayalType,
                damageAmount = damage,
                assignedPropertyId = worker.assignedPropertyId
            });

            var rep = FindAnyObjectByType<ReputationManager>();
            if (rep != null)
            {
                rep.RemoveClout(Mathf.Abs(ReputationManager.CloutValues.EmployeeBetray), "worker betrayal");
                rep.ModifyReputationBulk(ReputationManager.ReputationModifiers.WorkerBetrayal, "worker betrayed");
            }

            Debug.LogWarning($"[WorkerManager] {worker.workerName} BETRAYED! Type: {betrayalType}, Damage: ${damage:F0}");
        }

        private void ProcessArrest(WorkerInstance worker)
        {
            worker.state = WorkerState.Arrested;
            _workers.Remove(worker);
            DespawnWorkerFromWorld(worker);

            OnWorkerArrested?.Invoke(worker);
            EventBus.Publish(new WorkerArrestedEvent
            {
                workerId = worker.workerId,
                workerName = worker.workerName,
                heatGenerated = 15f,
                knewCriticalInfo = worker.knowledgeLevel >= 3,
                assignedPropertyId = worker.assignedPropertyId
            });

            Debug.LogWarning($"[WorkerManager] {worker.workerName} ARRESTED! Knowledge level: {worker.knowledgeLevel}");
        }

        // ─── Queries ─────────────────────────────────────────────────

        public int GetMaxWorkers()
        {
            var rep = FindAnyObjectByType<ReputationManager>();
            int rank = rep != null ? rep.CurrentRank : 0;
            rank = Mathf.Clamp(rank, 0, maxWorkersByRank.Length - 1);
            return maxWorkersByRank[rank];
        }

        public int GetWorkerCountAtProperty(Property property)
        {
            int count = 0;
            foreach (var w in _workers)
                if (w.assignedProperty == property) count++;
            return count;
        }

        public List<WorkerInstance> GetWorkersByRole(EmployeeRole role)
        {
            var result = new List<WorkerInstance>();
            foreach (var w in _workers)
                if (w.role == role) result.Add(w);
            return result;
        }

        public List<WorkerInstance> GetWorkersByProperty(Property property)
        {
            var result = new List<WorkerInstance>();
            foreach (var w in _workers)
                if (w.assignedProperty == property) result.Add(w);
            return result;
        }

        public float GetTotalDailyWages()
        {
            float total = 0f;
            foreach (var w in _workers)
                total += w.definition.dailyWage * w.definition.WageDemandMultiplier;
            return total;
        }

        public float GetTotalDailyEarnings()
        {
            float total = 0f;
            foreach (var w in _workers)
                total += w.totalCashEarned / Mathf.Max(1, w.shiftsCompleted);
            return total;
        }

        // ─── World Spawning ──────────────────────────────────────────

        private void SpawnWorkerInWorld(WorkerInstance worker)
        {
            Vector3 spawnPos = worker.assignedProperty != null
                ? worker.assignedProperty.transform.position + Vector3.right * 2f + Vector3.up * 0.5f
                : transform.position;

            GameObject workerObj = new GameObject($"Worker_{worker.workerName}_{worker.workerId}");
            workerObj.transform.position = spawnPos;

            // Add NavMeshAgent for movement
            NavMeshAgent agent = workerObj.AddComponent<NavMeshAgent>();
            agent.speed = 3f;
            agent.stoppingDistance = 1.5f;
            agent.radius = 0.3f;
            agent.height = 1.8f;

            // Add placeholder visual (capsule)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(workerObj.transform);
            visual.transform.localPosition = Vector3.up * 1f;
            visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Color by role
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color roleColor = worker.role switch
                {
                    EmployeeRole.Dealer => new Color(0.2f, 0.8f, 0.2f),    // Green
                    EmployeeRole.Cook => new Color(0.8f, 0.6f, 0.2f),       // Orange
                    EmployeeRole.Guard => new Color(0.8f, 0.2f, 0.2f),      // Red
                    EmployeeRole.Grower => new Color(0.3f, 0.9f, 0.3f),     // Bright green
                    EmployeeRole.Driver => new Color(0.2f, 0.2f, 0.8f),     // Blue
                    _ => Color.gray
                };
                renderer.material.color = roleColor;
            }

            // Remove default collider from visual (NavMeshAgent has its own)
            var capsuleCollider = visual.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null) Object.Destroy(capsuleCollider);

            // Add role-specific AI component
            switch (worker.role)
            {
                case EmployeeRole.Dealer:
                    var dealer = workerObj.AddComponent<DealerAI>();
                    dealer.Initialize(worker);
                    break;
                case EmployeeRole.Cook:
                    var cook = workerObj.AddComponent<CookAI>();
                    cook.Initialize(worker);
                    break;
                case EmployeeRole.Guard:
                    var guard = workerObj.AddComponent<GuardAI>();
                    guard.Initialize(worker);
                    break;
                default:
                    // Generic idle worker for unimplemented roles
                    break;
            }

            worker.worldObject = workerObj;
        }

        private void DespawnWorkerFromWorld(WorkerInstance worker)
        {
            if (worker.worldObject != null)
            {
                Destroy(worker.worldObject);
                worker.worldObject = null;
            }
        }
    }
}
