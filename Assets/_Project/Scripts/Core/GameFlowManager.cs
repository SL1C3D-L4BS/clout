using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Clout.Utils;
using Clout.Save;
using Clout.Empire.Economy;
using Clout.Empire.Properties;
using Clout.Empire.Employees;
using Clout.Empire.Reputation;
using Clout.World.Police;
using Clout.World.Districts;

namespace Clout.Core
{
    /// <summary>
    /// Step 10 — Game Flow Manager: the master orchestrator.
    ///
    /// Responsibilities:
    /// 1. Session lifecycle: NewGame → Playing → Paused → Saving → Loading
    /// 2. Milestone tracking: first deal, first property, first worker, phone unlock, first raid
    /// 3. Tutorial prompt system: contextual OnGUI hints triggered by milestones
    /// 4. Auto-save on day-end events
    /// 5. Pause/resume with time scale control
    /// 6. Game-over conditions (arrested, bankrupt)
    /// 7. Wires TransactionLedger.OnDayEnd to all daily-tick consumers
    ///
    /// This is the glue that makes all 9 prior systems function as one cohesive game loop.
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        // ─── Singleton ──────────────────────────────────────────────

        public static GameFlowManager Instance { get; private set; }

        // ─── State ──────────────────────────────────────────────────

        public GameState CurrentState { get; private set; } = GameState.Initializing;
        public float SessionTime { get; private set; }
        public int DaysSurvived { get; private set; }

        // ─── Milestones ─────────────────────────────────────────────

        [NonSerialized] public MilestoneTracker Milestones = new MilestoneTracker();

        // ─── Tutorial ───────────────────────────────────────────────

        private string _activeTutorialText;
        private float _tutorialTimer;
        private float _tutorialFadeStart;
        private const float TUTORIAL_DISPLAY_TIME = 8f;
        private const float TUTORIAL_FADE_TIME = 2f;
        private Queue<TutorialPrompt> _tutorialQueue = new Queue<TutorialPrompt>();
        private bool _tutorialActive;

        // ─── Auto-Save ──────────────────────────────────────────────

        [Tooltip("Auto-save slot (0-2)")]
        public int autoSaveSlot = 0;

        private bool _autoSaveEnabled = true;
        private int _lastAutoSaveDay = -1;

        // ─── Pause ──────────────────────────────────────────────────

        public bool IsPaused { get; private set; }

        // ─── Daily Tick Wiring ──────────────────────────────────────

        private TransactionLedger _ledger;
        private PropertyManager _propertyMgr;
        private WorkerManager _workerMgr;
        private ReputationManager _repMgr;
        private CashManager _cashMgr;

        // ─── Game Over ──────────────────────────────────────────────

        private bool _gameOver;
        private string _gameOverReason;
        private float _gameOverTimer;

        // ─── Events ─────────────────────────────────────────────────

        public event Action<GameState> OnStateChanged;
        public event Action<Milestone> OnMilestoneReached;
        public event Action<int> OnDayPassed;

        // =============================================================
        //  LIFECYCLE
        // =============================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Cache manager references
            _ledger = TransactionLedger.Instance;
            _propertyMgr = PropertyManager.Instance;
            _workerMgr = WorkerManager.Instance;
            _repMgr = FindAnyObjectByType<ReputationManager>();
            _cashMgr = CashManager.Instance;

            // Wire daily tick chain
            if (_ledger != null)
                _ledger.OnDayEnd += OnDayEnd;

            // Subscribe to milestone events
            SubscribeToMilestoneEvents();

            // Transition to playing
            SetState(GameState.Playing);

            // Show opening tutorial
            EnqueueTutorial("Welcome to The Fillmore",
                "You're a nobody on the corner. Buy ingredients, cook product, deal to customers.\n" +
                "Build your empire from nothing. Press [TAB] to hire workers. [M] for your phone.",
                10f);

            Debug.Log("[GameFlowManager] Initialized. Game loop active.");
        }

        private void OnDestroy()
        {
            if (_ledger != null)
                _ledger.OnDayEnd -= OnDayEnd;

            UnsubscribeFromMilestoneEvents();

            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_gameOver) return;

            // Pause toggle: Escape (only when phone is closed)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // Don't pause if phone is open — phone handles its own Escape
                var phone = Clout.UI.Phone.PhoneController.Instance;
                if (phone == null || !phone.IsOpen)
                    TogglePause();
            }

            if (IsPaused) return;

            SessionTime += Time.deltaTime;

            // Tutorial timer
            UpdateTutorial();

            // Check game-over conditions
            CheckGameOverConditions();
        }

        // =============================================================
        //  STATE MANAGEMENT
        // =============================================================

        private void SetState(GameState newState)
        {
            if (CurrentState == newState) return;
            GameState prev = CurrentState;
            CurrentState = newState;
            Debug.Log($"[GameFlowManager] State: {prev} → {newState}");
            OnStateChanged?.Invoke(newState);
        }

        public void TogglePause()
        {
            if (_gameOver) return;

            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0f : 1f;
            SetState(IsPaused ? GameState.Paused : GameState.Playing);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        // =============================================================
        //  DAILY TICK — THE HEARTBEAT
        // =============================================================

        /// <summary>
        /// Called by TransactionLedger.OnDayEnd every game day.
        /// Propagates the daily tick to all consumers in correct order.
        /// </summary>
        private void OnDayEnd()
        {
            DaysSurvived++;
            Debug.Log($"[GameFlowManager] Day {DaysSurvived} ended.");

            // 1. Properties: collect revenue, pay upkeep, check raids
            // (PropertyManager listens directly to OnDayEnd)

            // 2. Workers: pay wages, check betrayal/arrest
            // (WorkerManager listens directly to OnDayEnd)

            // 3. Reputation: natural decay toward equilibrium
            if (_repMgr != null)
                _repMgr.ProcessDailyDecay();

            // 4. Auto-save
            if (_autoSaveEnabled)
            {
                int saveInterval = GameBalanceConfig.Active != null
                    ? GameBalanceConfig.Active.autoSaveIntervalDays : 1;

                if (DaysSurvived - _lastAutoSaveDay >= saveInterval)
                {
                    AutoSave();
                    _lastAutoSaveDay = DaysSurvived;
                }
            }

            // 5. Notify listeners
            OnDayPassed?.Invoke(DaysSurvived);

            // 6. Day survival milestone
            if (DaysSurvived == 1) TryCompleteMilestone(Milestone.SurvivedDay1);
            if (DaysSurvived == 7) TryCompleteMilestone(Milestone.SurvivedWeek1);
            if (DaysSurvived == 30) TryCompleteMilestone(Milestone.SurvivedMonth1);
        }

        // =============================================================
        //  MILESTONE SYSTEM
        // =============================================================

        private void SubscribeToMilestoneEvents()
        {
            EventBus.Subscribe<DealCompletedEvent>(OnDealCompleted);
            EventBus.Subscribe<PropertyPurchasedEvent>(OnPropertyPurchased);
            EventBus.Subscribe<WorkerHiredEvent>(OnWorkerHired);
            EventBus.Subscribe<PropertyRaidedEvent>(OnPropertyRaided);
            EventBus.Subscribe<WantedLevelChangedEvent>(OnWantedChanged);
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<ProductCookedEvent>(OnProductCooked);
        }

        private void UnsubscribeFromMilestoneEvents()
        {
            EventBus.Unsubscribe<DealCompletedEvent>(OnDealCompleted);
            EventBus.Unsubscribe<PropertyPurchasedEvent>(OnPropertyPurchased);
            EventBus.Unsubscribe<WorkerHiredEvent>(OnWorkerHired);
            EventBus.Unsubscribe<PropertyRaidedEvent>(OnPropertyRaided);
            EventBus.Unsubscribe<WantedLevelChangedEvent>(OnWantedChanged);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<ProductCookedEvent>(OnProductCooked);
        }

        private void OnDealCompleted(DealCompletedEvent evt)
        {
            Milestones.totalDeals++;
            Milestones.totalRevenue += evt.cashEarned;

            if (Milestones.totalDeals == 1)
            {
                TryCompleteMilestone(Milestone.FirstDeal);
                EnqueueTutorial("First Deal Complete!",
                    "You made your first sale. Keep dealing to build your rep.\n" +
                    "Buy a property to set up a base of operations.");
            }
            else if (Milestones.totalDeals == 10)
                TryCompleteMilestone(Milestone.TenDeals);
            else if (Milestones.totalDeals == 100)
                TryCompleteMilestone(Milestone.HundredDeals);

            if (Milestones.totalRevenue >= 10000f && !Milestones.IsComplete(Milestone.Earned10K))
                TryCompleteMilestone(Milestone.Earned10K);
            if (Milestones.totalRevenue >= 100000f && !Milestones.IsComplete(Milestone.Earned100K))
                TryCompleteMilestone(Milestone.Earned100K);
        }

        private void OnProductCooked(ProductCookedEvent evt)
        {
            Milestones.totalProductsCooked++;
            if (Milestones.totalProductsCooked == 1)
            {
                TryCompleteMilestone(Milestone.FirstCook);
                EnqueueTutorial("Product Cooked!",
                    "Quality matters — higher skill workers produce better product.\n" +
                    "Better product sells for more on the street.");
            }
        }

        private void OnPropertyPurchased(PropertyPurchasedEvent evt)
        {
            Milestones.totalProperties++;
            if (Milestones.totalProperties == 1)
            {
                TryCompleteMilestone(Milestone.FirstProperty);
                EnqueueTutorial("Property Acquired!",
                    "You own real estate now. Press [TAB] to hire workers and assign them here.\n" +
                    "Workers automate dealing and production. Build your crew.");
            }
            if (Milestones.totalProperties == 3)
                TryCompleteMilestone(Milestone.ThreeProperties);
        }

        private void OnWorkerHired(WorkerHiredEvent evt)
        {
            Milestones.totalWorkersHired++;
            if (Milestones.totalWorkersHired == 1)
            {
                TryCompleteMilestone(Milestone.FirstWorker);
                TryCompleteMilestone(Milestone.PhoneUnlocked);
                EnqueueTutorial("First Worker Hired!",
                    "Your crew is growing. Press [M] to open your phone.\n" +
                    "Track your empire: map, contacts, products, finances, messages.\n" +
                    "Keep workers loyal — pay wages, don't overwork them.");
            }
            if (Milestones.totalWorkersHired == 5)
                TryCompleteMilestone(Milestone.FiveWorkers);
        }

        private void OnPropertyRaided(PropertyRaidedEvent evt)
        {
            Milestones.totalRaids++;
            if (Milestones.totalRaids == 1)
            {
                TryCompleteMilestone(Milestone.FirstRaid);
                EnqueueTutorial("Property Raided!",
                    "The cops hit your spot. Lower your heat to avoid more raids.\n" +
                    "Lay low, use safe zones, or invest in security upgrades.");
            }
        }

        private void OnWantedChanged(WantedLevelChangedEvent evt)
        {
            if (evt.newLevel >= 3 && !Milestones.IsComplete(Milestone.Hunted))
            {
                TryCompleteMilestone(Milestone.Hunted);
                EnqueueTutorial("HUNTED",
                    "You're hot! Police are actively pursuing you.\n" +
                    "Find a safe zone, lay low, or fight your way out.");
            }
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            Milestones.totalKills++;
            if (Milestones.totalKills == 1)
                TryCompleteMilestone(Milestone.FirstKill);
        }

        private void TryCompleteMilestone(Milestone milestone)
        {
            if (Milestones.IsComplete(milestone)) return;

            Milestones.Complete(milestone);
            Debug.Log($"[GameFlowManager] Milestone reached: {milestone}");
            OnMilestoneReached?.Invoke(milestone);
        }

        // =============================================================
        //  TUTORIAL SYSTEM
        // =============================================================

        public void EnqueueTutorial(string title, string body, float duration = TUTORIAL_DISPLAY_TIME)
        {
            _tutorialQueue.Enqueue(new TutorialPrompt
            {
                title = title,
                body = body,
                duration = duration
            });
        }

        private void UpdateTutorial()
        {
            if (_tutorialActive)
            {
                _tutorialTimer -= Time.deltaTime;
                if (_tutorialTimer <= 0f)
                {
                    _tutorialActive = false;
                    _activeTutorialText = null;
                }
                return;
            }

            if (_tutorialQueue.Count > 0)
            {
                TutorialPrompt next = _tutorialQueue.Dequeue();
                _activeTutorialText = $"<b>{next.title}</b>\n{next.body}";
                _tutorialTimer = next.duration;
                _tutorialFadeStart = next.duration - TUTORIAL_FADE_TIME;
                _tutorialActive = true;
            }
        }

        // =============================================================
        //  SAVE / LOAD
        // =============================================================

        /// <summary>Capture full game state into CloutSaveData.</summary>
        public CloutSaveData CaptureGameState()
        {
            CloutSaveData data = new CloutSaveData();

            // Meta
            data.playTime = SessionTime;
            data.currentDistrict = DistrictManager.Instance?.CurrentDistrictId ?? "fillmore";

            // Player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                data.playerPosition = player.transform.position;
                data.playerRotation = player.transform.eulerAngles;

                var stats = player.GetComponent<Stats.RuntimeStats>();
                if (stats != null)
                {
                    data.playerHealth = stats.Health;
                    data.playerStamina = stats.Stamina;
                }
            }

            // Cash
            if (_cashMgr != null)
            {
                data.dirtyMoney = _cashMgr.DirtyCash;
                data.cleanMoney = _cashMgr.CleanCash;
            }

            // Reputation
            if (_repMgr != null)
            {
                data.cloutScore = _repMgr.CurrentClout;
                data.cloutRank = _repMgr.CurrentRankName;
                data.streetRep = _repMgr.streetRep;
                data.civilianRep = _repMgr.civilianRep;

                // 4D reputation vector (new in V2)
                var repVec = _repMgr.GetReputationVector();
                data.reputationFear = repVec.fear;
                data.reputationRespect = repVec.respect;
                data.reputationReliability = repVec.reliability;
                data.reputationRuthlessness = repVec.ruthlessness;
            }

            // Wanted
            var wanted = FindAnyObjectByType<WantedSystem>();
            if (wanted != null)
            {
                data.currentHeat = wanted.CurrentHeat;
                data.wantedLevel = (int)wanted.CurrentLevel;
            }

            // Properties
            if (_propertyMgr != null)
            {
                foreach (var prop in _propertyMgr.OwnedProperties)
                {
                    SavedProperty sp = new SavedProperty
                    {
                        propertyId = prop.Definition != null ? prop.Definition.propertyName : "unknown",
                        districtId = prop.Definition != null ? prop.Definition.districtName : "",
                        upgradeLevel = 0,
                        stash = new List<SavedProductStack>()
                    };

                    foreach (var slot in prop.Stash)
                    {
                        sp.stash.Add(new SavedProductStack
                        {
                            productId = slot.productId,
                            quantity = slot.quantity,
                            qualityTier = 0,
                            potency = slot.quality
                        });
                    }

                    // Count applied upgrades
                    if (prop.Definition != null && prop.Definition.availableUpgrades != null)
                    {
                        for (int i = 0; i < prop.Definition.availableUpgrades.Length; i++)
                        {
                            if (prop.HasUpgrade(i)) sp.upgradeLevel++;
                        }
                    }

                    data.ownedProperties.Add(sp);
                }
            }

            // Workers
            if (_workerMgr != null)
            {
                foreach (var worker in _workerMgr.Workers)
                {
                    data.hiredWorkers.Add(new SavedWorker
                    {
                        workerId = worker.workerId,
                        workerName = worker.workerName,
                        assignedPropertyId = worker.assignedPropertyId,
                        workerType = worker.role.ToString(),
                        loyalty = worker.loyalty,
                        skill = worker.skill,
                        discretion = worker.discretion,
                        greed = worker.greed,
                        courage = worker.courage,
                        intelligence = worker.intelligence,
                        totalDeals = worker.totalDeals,
                        totalUnitsProduced = worker.totalUnitsProduced,
                        totalCashEarned = worker.totalCashEarned,
                        shiftsCompleted = worker.shiftsCompleted
                    });
                }
            }

            // Milestones
            data.completedMilestones = new List<string>();
            foreach (Milestone m in Enum.GetValues(typeof(Milestone)))
            {
                if (Milestones.IsComplete(m))
                    data.completedMilestones.Add(m.ToString());
            }

            // Session stats
            data.daysSurvived = DaysSurvived;
            data.totalDeals = Milestones.totalDeals;
            data.totalRevenue = Milestones.totalRevenue;
            data.totalKills = Milestones.totalKills;

            // District runtime state
            var distMgr = DistrictManager.Instance;
            if (distMgr != null)
            {
                var rtState = distMgr.GetRuntimeState("fillmore");
                data.districtControlLevel = rtState.controlLevel;
                data.districtTotalDeals = rtState.totalDealsCompleted;
                data.districtTotalRevenue = rtState.totalRevenue;
            }

            return data;
        }

        /// <summary>Save current game state to a slot.</summary>
        public bool SaveGame(int slot = 0)
        {
            SetState(GameState.Saving);
            CloutSaveData data = CaptureGameState();
            bool success = SaveManager.Save(data, slot);
            SetState(GameState.Playing);
            return success;
        }

        /// <summary>Load game state from a slot and restore all managers.</summary>
        public bool LoadGame(int slot = 0)
        {
            CloutSaveData data = SaveManager.Load(slot);
            if (data == null) return false;

            SetState(GameState.Loading);
            RestoreGameState(data);
            SetState(GameState.Playing);
            return true;
        }

        private void AutoSave()
        {
            if (SaveGame(autoSaveSlot))
                Debug.Log($"[GameFlowManager] Auto-saved to slot {autoSaveSlot} on day {DaysSurvived}.");
        }

        /// <summary>Restore all manager states from save data.</summary>
        public void RestoreGameState(CloutSaveData data)
        {
            // Session
            SessionTime = data.playTime;
            DaysSurvived = data.daysSurvived;

            // Player position
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = data.playerPosition;
                player.transform.eulerAngles = data.playerRotation;

                var stats = player.GetComponent<Stats.RuntimeStats>();
                if (stats != null)
                {
                    // Health and Stamina have private setters — use Heal/RestoreStamina
                    int healthDiff = data.playerHealth - stats.Health;
                    if (healthDiff > 0) stats.Heal(healthDiff);
                    stats.RestoreStamina(data.playerStamina - stats.Stamina);
                }
            }

            // Cash
            if (_cashMgr != null)
                _cashMgr.DebugSetCash(data.dirtyMoney, data.cleanMoney);

            // Reputation
            if (_repMgr != null)
            {
                // Restore 4D vector
                _repMgr.fear = data.reputationFear;
                _repMgr.respect = data.reputationRespect;
                _repMgr.reliability = data.reputationReliability;
                _repMgr.ruthlessness = data.reputationRuthlessness;
                _repMgr.streetRep = data.streetRep;
                _repMgr.civilianRep = data.civilianRep;
            }

            // Milestones
            if (data.completedMilestones != null)
            {
                foreach (string ms in data.completedMilestones)
                {
                    if (Enum.TryParse<Milestone>(ms, out Milestone m))
                        Milestones.Complete(m);
                }
            }

            // Stats
            Milestones.totalDeals = data.totalDeals;
            Milestones.totalRevenue = data.totalRevenue;
            Milestones.totalKills = data.totalKills;

            Debug.Log($"[GameFlowManager] Game state restored from save. Day {DaysSurvived}, ${data.dirtyMoney + data.cleanMoney:N0} cash.");
        }

        // =============================================================
        //  GAME OVER
        // =============================================================

        private void CheckGameOverConditions()
        {
            // Bankrupt: no cash, no properties, no workers, no inventory
            if (_cashMgr != null && _cashMgr.TotalCash <= 0f &&
                _propertyMgr != null && _propertyMgr.PropertyCount == 0 &&
                _workerMgr != null && _workerMgr.WorkerCount == 0)
            {
                // Grace period — don't trigger on first frame
                if (SessionTime > 30f && DaysSurvived > 0)
                {
                    _gameOverTimer += Time.deltaTime;
                    if (_gameOverTimer > 10f) // 10 seconds of being bankrupt
                        TriggerGameOver("BANKRUPT — You lost everything. The streets don't forgive.");
                }
            }
            else
            {
                _gameOverTimer = 0f;
            }
        }

        public void TriggerGameOver(string reason)
        {
            if (_gameOver) return;
            _gameOver = true;
            _gameOverReason = reason;
            SetState(GameState.GameOver);
            Debug.Log($"[GameFlowManager] GAME OVER: {reason}");
        }

        // =============================================================
        //  QUICK SAVE / QUICK LOAD (F5 / F9)
        // =============================================================

        private void LateUpdate()
        {
            if (Keyboard.current == null) return;

            // F5 = Quick Save
            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SaveGame(0);
                EnqueueTutorial("Game Saved", "Progress saved to slot 1.", 3f);
            }

            // F9 = Quick Load
            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                if (SaveManager.HasSave(0))
                {
                    LoadGame(0);
                    EnqueueTutorial("Game Loaded", "Restored from last save.", 3f);
                }
            }
        }

        // =============================================================
        //  OnGUI — TUTORIAL PROMPTS + PAUSE + GAME OVER
        // =============================================================

        private void OnGUI()
        {
            // ─── Pause Screen ───────────────────────────────────
            if (IsPaused)
            {
                DrawPauseScreen();
                return;
            }

            // ─── Game Over Screen ───────────────────────────────
            if (_gameOver)
            {
                DrawGameOverScreen();
                return;
            }

            // ─── Tutorial Prompt ────────────────────────────────
            if (_tutorialActive && !string.IsNullOrEmpty(_activeTutorialText))
            {
                DrawTutorialPrompt();
            }

            // ─── Save/Load Indicator ────────────────────────────
            if (CurrentState == GameState.Saving)
            {
                GUI.color = new Color(1f, 0.85f, 0.2f, 0.9f);
                GUI.Label(new Rect(Screen.width - 180, 10, 170, 30),
                    "Saving...", GetIndicatorStyle());
                GUI.color = Color.white;
            }

            // ─── Day Counter ────────────────────────────────────
            DrawDayCounter();

            // ─── Milestone Toast ────────────────────────────────
            // (handled by tutorial queue)
        }

        private void DrawTutorialPrompt()
        {
            float alpha = 1f;
            if (_tutorialTimer < TUTORIAL_FADE_TIME)
                alpha = _tutorialTimer / TUTORIAL_FADE_TIME;

            float boxW = 500f;
            float boxH = 100f;
            float boxX = (Screen.width - boxW) / 2f;
            float boxY = Screen.height * 0.15f;

            // Background
            GUI.color = new Color(0.05f, 0.05f, 0.1f, 0.85f * alpha);
            GUI.Box(new Rect(boxX - 5, boxY - 5, boxW + 10, boxH + 10), "");

            // Accent bar
            GUI.color = new Color(0.2f, 0.8f, 0.4f, alpha);
            GUI.DrawTexture(new Rect(boxX, boxY, 4f, boxH), Texture2D.whiteTexture);

            // Text
            GUI.color = new Color(0.9f, 0.95f, 0.9f, alpha);
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                richText = true,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(14, 10, 8, 8)
            };
            GUI.Label(new Rect(boxX, boxY, boxW, boxH), _activeTutorialText, style);

            // Skip hint
            GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.5f * alpha);
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.LowerRight };
            GUI.Label(new Rect(boxX, boxY, boxW - 10, boxH - 5), "[any key to dismiss]", hintStyle);

            GUI.color = Color.white;

            // Dismiss on any key
            if (Event.current.type == EventType.KeyDown || Event.current.type == EventType.MouseDown)
            {
                _tutorialTimer = Mathf.Min(_tutorialTimer, TUTORIAL_FADE_TIME * 0.5f);
            }
        }

        private void DrawPauseScreen()
        {
            // Dim overlay
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;

            // Title
            GUI.color = new Color(0.9f, 0.95f, 1f, 1f);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(centerX - 200, centerY - 120, 400, 60), "PAUSED", titleStyle);

            // Stats summary
            GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);

            string statsText = $"Day {DaysSurvived}  |  " +
                               $"${(_cashMgr != null ? _cashMgr.TotalCash : 0f):N0}  |  " +
                               $"{Milestones.totalDeals} deals  |  " +
                               $"{(_workerMgr != null ? _workerMgr.WorkerCount : 0)} workers";
            GUI.Label(new Rect(centerX - 250, centerY - 50, 500, 30), statsText, infoStyle);

            // Buttons
            GUI.color = Color.white;
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fixedHeight = 45 };

            float btnW = 220f;
            float btnX = centerX - btnW / 2f;

            if (GUI.Button(new Rect(btnX, centerY + 10, btnW, 45), "Resume [ESC]", btnStyle))
                Resume();

            if (GUI.Button(new Rect(btnX, centerY + 65, btnW, 45), "Quick Save [F5]", btnStyle))
            {
                SaveGame(0);
                Resume();
            }

            if (GUI.Button(new Rect(btnX, centerY + 120, btnW, 45), "Quick Load [F9]", btnStyle))
            {
                if (SaveManager.HasSave(0))
                {
                    LoadGame(0);
                    Resume();
                }
            }

            GUI.color = Color.white;
        }

        private void DrawGameOverScreen()
        {
            GUI.color = new Color(0.1f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;

            // Title
            GUI.color = new Color(1f, 0.15f, 0.1f, 1f);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 48,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(centerX - 300, centerY - 140, 600, 70), "GAME OVER", titleStyle);

            // Reason
            GUI.color = new Color(0.8f, 0.6f, 0.5f, 1f);
            GUIStyle reasonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUI.Label(new Rect(centerX - 250, centerY - 50, 500, 40), _gameOverReason, reasonStyle);

            // Final stats
            GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            GUIStyle statStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            string finalStats = $"Survived {DaysSurvived} days  |  " +
                                $"{Milestones.totalDeals} deals  |  " +
                                $"${Milestones.totalRevenue:N0} earned  |  " +
                                $"{Milestones.totalKills} kills";
            GUI.Label(new Rect(centerX - 250, centerY + 10, 500, 30), finalStats, statStyle);

            // Load button
            GUI.color = Color.white;
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fixedHeight = 45 };
            if (SaveManager.HasSave(0))
            {
                if (GUI.Button(new Rect(centerX - 100, centerY + 70, 200, 45), "Load Last Save", btnStyle))
                {
                    _gameOver = false;
                    _gameOverReason = null;
                    Time.timeScale = 1f;
                    LoadGame(0);
                }
            }
        }

        private void DrawDayCounter()
        {
            // Small day counter in top-right corner
            GUIStyle dayStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            GUI.color = new Color(0.9f, 0.9f, 0.8f, 0.7f);
            GUI.Label(new Rect(Screen.width - 160, 40, 150, 25), $"DAY {DaysSurvived + 1}", dayStyle);
            GUI.color = Color.white;
        }

        private GUIStyle GetIndicatorStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
        }
    }

    // =================================================================
    //  ENUMS & DATA STRUCTURES
    // =================================================================

    public enum GameState
    {
        Initializing,
        Playing,
        Paused,
        Saving,
        Loading,
        GameOver
    }

    public enum Milestone
    {
        FirstDeal,
        FirstCook,
        FirstProperty,
        FirstWorker,
        FirstKill,
        FirstRaid,
        PhoneUnlocked,
        Hunted,
        TenDeals,
        HundredDeals,
        ThreeProperties,
        FiveWorkers,
        Earned10K,
        Earned100K,
        SurvivedDay1,
        SurvivedWeek1,
        SurvivedMonth1
    }

    [Serializable]
    public class MilestoneTracker
    {
        private HashSet<Milestone> _completed = new HashSet<Milestone>();

        // Running counters
        public int totalDeals;
        public float totalRevenue;
        public int totalKills;
        public int totalProperties;
        public int totalWorkersHired;
        public int totalProductsCooked;
        public int totalRaids;

        public bool IsComplete(Milestone m) => _completed.Contains(m);
        public void Complete(Milestone m) => _completed.Add(m);
        public int CompletedCount => _completed.Count;
        public IEnumerable<Milestone> Completed => _completed;
    }

    public struct TutorialPrompt
    {
        public string title;
        public string body;
        public float duration;
    }
}
