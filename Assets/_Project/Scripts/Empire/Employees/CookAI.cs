using UnityEngine;
using UnityEngine.AI;
using Clout.Core;
using Clout.Empire.Crafting;
using Clout.Empire.Properties;
using Clout.Utils;

namespace Clout.Empire.Employees
{
    /// <summary>
    /// Autonomous cook worker AI — operates CraftingStations to produce product.
    ///
    /// Behavior loop (Spec v2.0 Section 13):
    ///   Idle → TravelToStation → StartBatch → WaitForBatch → StoreOutput → Rest
    ///
    /// Quality output = station base + worker QualityModifier bonus.
    /// Cooks improve skill with each completed batch (logarithmic growth).
    /// Knowledge level increases with batches — cooks who know recipes are more dangerous if flipped.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CookAI : MonoBehaviour
    {
        // ─── Configuration ──────────────────────────────────────────

        [Header("Cooking")]
        [Tooltip("How close the cook needs to be to the station to operate it.")]
        public float stationInteractionRange = 2.5f;

        [Tooltip("Time between checking for available stations.")]
        public float stationScanInterval = 5f;

        // ─── State ──────────────────────────────────────────────────

        private enum CookState
        {
            Idle,
            TravelingToStation,
            StartingBatch,
            WaitingForBatch,
            StoringOutput,
            Resting
        }

        private CookState _state = CookState.Idle;
        private WorkerInstance _worker;
        private NavMeshAgent _agent;

        // Targets
        private CraftingStation _targetStation;
        private CraftingBatch _activeBatch;
        private Vector3 _homePosition;

        // Timers
        private float _scanTimer;
        private float _restTimer;
        private float _stateTimer;

        // ─── Initialization ─────────────────────────────────────────

        public void Initialize(WorkerInstance worker)
        {
            _worker = worker;
            _agent = GetComponent<NavMeshAgent>();
            _homePosition = worker.assignedProperty != null
                ? worker.assignedProperty.transform.position
                : transform.position;

            _state = CookState.Idle;
        }

        // ─── Update Loop ────────────────────────────────────────────

        private void Update()
        {
            if (_worker == null || _worker.state == WorkerState.Arrested || _worker.state == WorkerState.Dead)
                return;

            _stateTimer += Time.deltaTime;

            switch (_state)
            {
                case CookState.Idle:
                    HandleIdle();
                    break;
                case CookState.TravelingToStation:
                    HandleTravelToStation();
                    break;
                case CookState.StartingBatch:
                    HandleStartBatch();
                    break;
                case CookState.WaitingForBatch:
                    HandleWaitForBatch();
                    break;
                case CookState.StoringOutput:
                    HandleStoreOutput();
                    break;
                case CookState.Resting:
                    HandleResting();
                    break;
            }
        }

        // ─── State Handlers ─────────────────────────────────────────

        private void HandleIdle()
        {
            _scanTimer += Time.deltaTime;

            if (_scanTimer >= stationScanInterval)
            {
                _scanTimer = 0f;
                _targetStation = FindAvailableStation();

                if (_targetStation != null)
                {
                    _worker.state = WorkerState.Traveling;
                    _agent.SetDestination(_targetStation.transform.position);
                    TransitionTo(CookState.TravelingToStation);
                }
            }
        }

        private void HandleTravelToStation()
        {
            if (_targetStation == null)
            {
                TransitionTo(CookState.Idle);
                return;
            }

            float dist = Vector3.Distance(transform.position, _targetStation.transform.position);

            if (dist <= stationInteractionRange)
            {
                _worker.state = WorkerState.Working;
                TransitionTo(CookState.StartingBatch);
                return;
            }

            // Stuck timeout
            if (_stateTimer > 20f)
            {
                _targetStation = null;
                TransitionTo(CookState.Idle);
            }
        }

        private void HandleStartBatch()
        {
            if (_targetStation == null || _targetStation.IsFull)
            {
                TransitionTo(CookState.Idle);
                return;
            }

            // Find a recipe this station can craft
            // Use the first available recipe — cooks aren't picky
            if (_targetStation.availableRecipes != null && _targetStation.availableRecipes.Length > 0)
            {
                var recipe = _targetStation.availableRecipes[0];

                // Start batch — pass null for player since this is NPC-operated
                _activeBatch = _targetStation.StartBatch(recipe, null, false);

                if (_activeBatch != null)
                {
                    // Subscribe to completion
                    _targetStation.OnBatchComplete += OnBatchComplete;
                    TransitionTo(CookState.WaitingForBatch);
                    return;
                }
            }

            // Can't start a batch — rest and try again
            TransitionTo(CookState.Resting);
        }

        private void HandleWaitForBatch()
        {
            // Just wait — OnBatchComplete callback will transition us
            // Safety timeout in case callback never fires
            if (_stateTimer > 600f) // 10 minute max
            {
                CleanupStation();
                TransitionTo(CookState.Resting);
            }
        }

        private void HandleStoreOutput()
        {
            // Output is already handled by CraftingStation's batch completion.
            // Cook just needs to ensure product gets to the property stash.
            // CraftingStation stores output directly; cook tracks stats.

            _worker.shiftsCompleted++;
            _worker.ImproveSkill();

            // Knowledge grows with production — cooks learn recipe details
            if (_worker.shiftsCompleted % 5 == 0 && _worker.knowledgeLevel < 5)
            {
                _worker.knowledgeLevel++;
            }

            EventBus.Publish(new WorkerShiftEndEvent
            {
                workerId = _worker.workerId,
                workerName = _worker.workerName,
                role = "Cook",
                unitsProduced = _worker.totalUnitsProduced,
                assignedPropertyId = _worker.assignedPropertyId
            });

            _restTimer = 0f;
            _worker.state = WorkerState.Resting;
            TransitionTo(CookState.Resting);
        }

        private void HandleResting()
        {
            _restTimer += Time.deltaTime;
            float restDuration = WorkerManager.Instance != null
                ? WorkerManager.Instance.restDuration
                : 60f;

            if (_restTimer >= restDuration)
            {
                _worker.state = WorkerState.Idle;
                TransitionTo(CookState.Idle);
            }
        }

        // ─── Callbacks ──────────────────────────────────────────────

        private void OnBatchComplete(CraftingResult result)
        {
            if (_targetStation != null)
                _targetStation.OnBatchComplete -= OnBatchComplete;

            _worker.totalUnitsProduced += result.quantity;

            // Store product in assigned property if available
            if (_worker.assignedProperty != null)
            {
                _worker.assignedProperty.StoreProduct(
                    result.product != null ? result.product.name : result.recipe.name,
                    result.quantity,
                    result.quality
                );
            }

            _activeBatch = null;
            TransitionTo(CookState.StoringOutput);
        }

        // ─── Station Discovery ──────────────────────────────────────

        private CraftingStation FindAvailableStation()
        {
            // Search for stations at the assigned property first
            if (_worker.assignedProperty != null)
            {
                var stations = _worker.assignedProperty.GetComponentsInChildren<CraftingStation>();
                foreach (var station in stations)
                {
                    if (!station.IsFull && station.isAutomated &&
                        station.requiredEmployee == EmployeeRole.Cook)
                    {
                        return station;
                    }
                }

                // Fallback — any non-full station at the property
                foreach (var station in stations)
                {
                    if (!station.IsFull) return station;
                }
            }

            // Global fallback — find any available station in the scene
            var allStations = FindObjectsByType<CraftingStation>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var station in allStations)
            {
                if (!station.IsFull) return station;
            }

            return null;
        }

        // ─── Cleanup ────────────────────────────────────────────────

        private void CleanupStation()
        {
            if (_targetStation != null)
                _targetStation.OnBatchComplete -= OnBatchComplete;

            _targetStation = null;
            _activeBatch = null;
        }

        private void TransitionTo(CookState newState)
        {
            _state = newState;
            _stateTimer = 0f;
        }

        private void OnDisable()
        {
            CleanupStation();
        }
    }
}
