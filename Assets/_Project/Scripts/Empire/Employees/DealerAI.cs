using UnityEngine;
using UnityEngine.AI;
using Clout.Core;
using Clout.Empire.Economy;
using Clout.Empire.Properties;
using Clout.Utils;
using Clout.World.NPCs;

namespace Clout.Empire.Employees
{
    /// <summary>
    /// Autonomous dealer worker AI — loads product from property stash, walks a dealing route,
    /// detects nearby customers via OverlapSphere, negotiates and closes deals, then returns
    /// cash to the property.
    ///
    /// Behavior loop (Spec v2.0 Section 13):
    ///   Idle → LoadProduct → TravelToRoute → SeekCustomers → NegotiateDeal → ReturnToBase → DepositCash → Rest
    ///
    /// Price calculation uses EconomyManager.CalculatePrice() × worker skill modifier.
    /// Customer acceptance uses CustomerAI.EvaluateDeal() with quality and price inputs.
    /// All earnings flow through CashManager.EarnDirty().
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class DealerAI : MonoBehaviour
    {
        // ─── Configuration ──────────────────────────────────────────

        [Header("Dealing")]
        [Tooltip("Radius to detect nearby customers in Seeking state.")]
        public float customerDetectionRadius = 15f;

        [Tooltip("Maximum units to carry per trip.")]
        public int maxCarryCapacity = 10;

        [Tooltip("Time spent per deal negotiation (seconds).")]
        public float dealDuration = 3f;

        [Tooltip("How far from property the dealer roams.")]
        public float roamRadius = 30f;

        [Tooltip("Time between customer scans while roaming.")]
        public float scanInterval = 2f;

        // ─── State ──────────────────────────────────────────────────

        private enum DealerState
        {
            Idle,
            LoadingProduct,
            TravelingToRoute,
            SeekingCustomers,
            ApproachingCustomer,
            Dealing,
            ReturningToBase,
            DepositingCash,
            Resting
        }

        private DealerState _state = DealerState.Idle;
        private WorkerInstance _worker;
        private NavMeshAgent _agent;

        // Inventory carried
        private string _carriedProductId;
        private int _carriedQuantity;
        private float _carriedQuality;
        private float _cashCollected;

        // Targets
        private CustomerAI _targetCustomer;
        private Vector3 _routeTarget;
        private Vector3 _homePosition;

        // Timers
        private float _dealTimer;
        private float _scanTimer;
        private float _restTimer;
        private float _stateTimer; // General timeout for stuck detection

        // ─── Initialization ─────────────────────────────────────────

        public void Initialize(WorkerInstance worker)
        {
            _worker = worker;
            _agent = GetComponent<NavMeshAgent>();
            _homePosition = worker.assignedProperty != null
                ? worker.assignedProperty.transform.position
                : transform.position;

            _state = DealerState.Idle;
        }

        // ─── Update Loop ────────────────────────────────────────────

        private void Update()
        {
            if (_worker == null || _worker.state == WorkerState.Arrested || _worker.state == WorkerState.Dead)
                return;

            _stateTimer += Time.deltaTime;

            switch (_state)
            {
                case DealerState.Idle:
                    HandleIdle();
                    break;
                case DealerState.LoadingProduct:
                    HandleLoadProduct();
                    break;
                case DealerState.TravelingToRoute:
                    HandleTravelToRoute();
                    break;
                case DealerState.SeekingCustomers:
                    HandleSeekCustomers();
                    break;
                case DealerState.ApproachingCustomer:
                    HandleApproachCustomer();
                    break;
                case DealerState.Dealing:
                    HandleDealing();
                    break;
                case DealerState.ReturningToBase:
                    HandleReturnToBase();
                    break;
                case DealerState.DepositingCash:
                    HandleDepositCash();
                    break;
                case DealerState.Resting:
                    HandleResting();
                    break;
            }
        }

        // ─── State Handlers ─────────────────────────────────────────

        private void HandleIdle()
        {
            // Start a new shift if we have a property with product
            if (_worker.assignedProperty == null) return;

            _worker.state = WorkerState.Working;
            TransitionTo(DealerState.LoadingProduct);
        }

        private void HandleLoadProduct()
        {
            Property prop = _worker.assignedProperty;
            if (prop == null || prop.StashCount == 0)
            {
                // No product available — wait
                TransitionTo(DealerState.Resting);
                return;
            }

            // Find the first product in stash and load up
            foreach (var slot in prop.Stash)
            {
                if (slot.quantity <= 0) continue;

                int toTake = Mathf.Min(maxCarryCapacity, slot.quantity);
                int retrieved = prop.RetrieveProduct(slot.productId, toTake);

                if (retrieved > 0)
                {
                    _carriedProductId = slot.productId;
                    _carriedQuantity = retrieved;
                    _carriedQuality = slot.quality;
                    break;
                }
            }

            if (_carriedQuantity > 0)
            {
                TransitionTo(DealerState.TravelingToRoute);
            }
            else
            {
                TransitionTo(DealerState.Resting);
            }
        }

        private void HandleTravelToRoute()
        {
            if (!_agent.hasPath || _agent.remainingDistance < 1.5f)
            {
                // Pick a random point within roam radius of home
                _routeTarget = GetRandomNavMeshPoint(_homePosition, roamRadius);
                _agent.SetDestination(_routeTarget);
            }

            // Arrived at route point — start scanning
            if (_agent.remainingDistance < 2f && !_agent.pathPending)
            {
                _worker.state = WorkerState.Dealing;
                TransitionTo(DealerState.SeekingCustomers);
            }

            // Stuck timeout — pick new point
            if (_stateTimer > 20f)
            {
                _routeTarget = GetRandomNavMeshPoint(_homePosition, roamRadius);
                _agent.SetDestination(_routeTarget);
                _stateTimer = 0f;
            }
        }

        private void HandleSeekCustomers()
        {
            _scanTimer += Time.deltaTime;

            if (_scanTimer >= scanInterval)
            {
                _scanTimer = 0f;
                _targetCustomer = FindNearbyCustomer();

                if (_targetCustomer != null)
                {
                    _agent.SetDestination(_targetCustomer.transform.position);
                    TransitionTo(DealerState.ApproachingCustomer);
                    return;
                }
            }

            // Roam while scanning — pick new points periodically
            if (!_agent.hasPath || _agent.remainingDistance < 2f)
            {
                _routeTarget = GetRandomNavMeshPoint(_homePosition, roamRadius);
                _agent.SetDestination(_routeTarget);
            }

            // After extended seeking with no luck, head home
            if (_stateTimer > 60f)
            {
                TransitionTo(DealerState.ReturningToBase);
            }
        }

        private void HandleApproachCustomer()
        {
            if (_targetCustomer == null || _targetCustomer.currentState == CustomerState.Fleeing)
            {
                _targetCustomer = null;
                TransitionTo(DealerState.SeekingCustomers);
                return;
            }

            // Update destination as customer moves
            float distToCustomer = Vector3.Distance(transform.position, _targetCustomer.transform.position);

            if (distToCustomer < 3f)
            {
                // Close enough — start deal
                _dealTimer = 0f;
                TransitionTo(DealerState.Dealing);
                return;
            }

            // Re-target periodically
            if (_stateTimer > 1f)
            {
                _agent.SetDestination(_targetCustomer.transform.position);
                _stateTimer = 0f;
            }

            // Customer moved too far or timeout
            if (distToCustomer > customerDetectionRadius * 1.5f || _stateTimer > 15f)
            {
                _targetCustomer = null;
                TransitionTo(DealerState.SeekingCustomers);
            }
        }

        private void HandleDealing()
        {
            _dealTimer += Time.deltaTime;

            if (_dealTimer >= dealDuration)
            {
                ExecuteDeal();
                TransitionTo(_carriedQuantity > 0
                    ? DealerState.SeekingCustomers
                    : DealerState.ReturningToBase);
            }
        }

        private void HandleReturnToBase()
        {
            if (!_agent.hasPath || _agent.remainingDistance < 1f)
            {
                _agent.SetDestination(_homePosition);
            }

            if (Vector3.Distance(transform.position, _homePosition) < 3f)
            {
                TransitionTo(DealerState.DepositingCash);
            }

            // Stuck timeout
            if (_stateTimer > 30f)
            {
                transform.position = _homePosition;
                TransitionTo(DealerState.DepositingCash);
            }
        }

        private void HandleDepositCash()
        {
            // Deposit all collected cash
            if (_cashCollected > 0f)
            {
                CashManager cash = CashManager.Instance;
                if (cash != null)
                {
                    cash.EarnDirty(_cashCollected, $"Dealer:{_worker.workerName}");
                }

                _worker.totalCashEarned += _cashCollected;
                _worker.cashOnHand = 0f;
                _cashCollected = 0f;
            }

            // Return any unsold product
            if (_carriedQuantity > 0 && _worker.assignedProperty != null)
            {
                _worker.assignedProperty.StoreProduct(_carriedProductId, _carriedQuantity, _carriedQuality);
                _carriedQuantity = 0;
            }

            // Complete shift
            _worker.shiftsCompleted++;
            _worker.ImproveSkill();
            _worker.state = WorkerState.Resting;

            EventBus.Publish(new WorkerShiftEndEvent
            {
                workerId = _worker.workerId,
                workerName = _worker.workerName,
                role = "Dealer",
                cashEarned = _worker.totalCashEarned,
                dealsMade = _worker.totalDeals,
                assignedPropertyId = _worker.assignedPropertyId
            });

            _restTimer = 0f;
            TransitionTo(DealerState.Resting);
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
                TransitionTo(DealerState.Idle);
            }
        }

        // ─── Deal Execution ─────────────────────────────────────────

        private void ExecuteDeal()
        {
            if (_targetCustomer == null || _carriedQuantity <= 0) return;

            // Calculate price using economy system
            float price = CalculateDealPrice();
            int dealQuantity = Mathf.Min(_carriedQuantity, 3); // Max 3 units per deal

            // Customer evaluates the deal
            bool accepted = _targetCustomer.EvaluateDeal(price * dealQuantity, _carriedQuality, 0);

            if (accepted)
            {
                // Deal success
                float totalCash = price * dealQuantity;
                _cashCollected += totalCash;
                _carriedQuantity -= dealQuantity;
                _worker.cashOnHand += totalCash;
                _worker.totalDeals++;

                _targetCustomer.CompletePurchase(0, _carriedQuality);

                // Publish events
                EventBus.Publish(new WorkerDealCompleteEvent
                {
                    workerId = _worker.workerId,
                    customerId = _targetCustomer.customerName,
                    productId = _carriedProductId,
                    quantity = dealQuantity,
                    cashEarned = totalCash,
                    quality = _carriedQuality
                });

                EventBus.Publish(new DealCompletedEvent
                {
                    productId = _carriedProductId,
                    quantity = dealQuantity,
                    cashEarned = totalCash,
                    customerId = _targetCustomer.customerName
                });

                // Record sale in economy for market dynamics
                var economy = FindAnyObjectByType<EconomyManager>();
                if (economy != null)
                {
                    economy.RecordSale(_carriedProductId, "", dealQuantity, _carriedQuality);
                }

                // Knowledge grows with deals — more deals = knows more about operations
                if (_worker.totalDeals % 10 == 0 && _worker.knowledgeLevel < 5)
                {
                    _worker.knowledgeLevel++;
                }
            }

            _targetCustomer = null;
        }

        private float CalculateDealPrice()
        {
            var economy = FindAnyObjectByType<EconomyManager>();
            float basePrice = 50f;

            if (economy != null)
            {
                basePrice = economy.CalculatePrice(_carriedProductId, "", _carriedQuality);
            }

            // Worker skill modifier — skilled dealers negotiate better prices
            float skillMod = 0.8f + (_worker.skill * 0.4f); // 0.8x to 1.2x

            return basePrice * skillMod;
        }

        // ─── Customer Detection ─────────────────────────────────────

        private CustomerAI FindNearbyCustomer()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, customerDetectionRadius);
            CustomerAI best = null;
            float bestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var customer = hit.GetComponent<CustomerAI>();
                if (customer == null) continue;

                // Only target customers who are seeking or wandering
                if (customer.currentState != CustomerState.Seeking &&
                    customer.currentState != CustomerState.Wandering)
                    continue;

                float dist = Vector3.Distance(transform.position, customer.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = customer;
                }
            }

            return best;
        }

        // ─── Navigation Helpers ─────────────────────────────────────

        private Vector3 GetRandomNavMeshPoint(Vector3 center, float radius)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 randomDir = Random.insideUnitSphere * radius;
                randomDir.y = 0f;
                Vector3 candidate = center + randomDir;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius * 0.5f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            return center;
        }

        private void TransitionTo(DealerState newState)
        {
            _state = newState;
            _stateTimer = 0f;
        }
    }
}
