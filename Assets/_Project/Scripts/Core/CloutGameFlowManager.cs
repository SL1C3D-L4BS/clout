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
    /// Step 10: Game Flow Manager - the master orchestrator.
    /// Session lifecycle, milestones, tutorials, auto-save, pause, game-over.
    /// Wires all 9 prior systems into one cohesive game loop.
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        public GameFlowState CurrentState { get; private set; } = GameFlowState.Initializing;
        public float SessionTime { get; private set; }
        public int DaysSurvived { get; private set; }

        [NonSerialized] public MilestoneTracker Milestones = new MilestoneTracker();

        private string _activeTutorialText;
        private float _tutorialTimer;
        private float _tutorialFadeStart;
        private const float TUTORIAL_DISPLAY_TIME = 8f;
        private const float TUTORIAL_FADE_TIME = 2f;
        private Queue<TutorialPrompt> _tutorialQueue = new Queue<TutorialPrompt>();
        private bool _tutorialActive;

        [Tooltip("Auto-save slot (0-2)")]
        public int autoSaveSlot = 0;
        private bool _autoSaveEnabled = true;
        private int _lastAutoSaveDay = -1;

        public bool IsPaused { get; private set; }

        private TransactionLedger _ledger;
        private PropertyManager _propertyMgr;
        private WorkerManager _workerMgr;
        private ReputationManager _repMgr;
        private CashManager _cashMgr;

        private bool _gameOver;
        private string _gameOverReason;
        private float _gameOverTimer;

        public event Action<GameFlowState> OnStateChanged;
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
            _ledger = TransactionLedger.Instance;
            _propertyMgr = PropertyManager.Instance;
            _workerMgr = WorkerManager.Instance;
            _repMgr = FindAnyObjectByType<ReputationManager>();
            _cashMgr = CashManager.Instance;

            if (_ledger != null)
                _ledger.OnDayEnd += OnDayEnd;

            SubscribeToMilestoneEvents();
            SetState(GameFlowState.Playing);

            EnqueueTutorial("Welcome to The Fillmore",
                "You are a nobody on the corner. Buy ingredients, cook product, deal to customers.\n" +
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

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                var phone = Clout.UI.Phone.PhoneController.Instance;
                if (phone == null || !phone.IsOpen)
                    TogglePause();
            }

            if (IsPaused) return;

            SessionTime += Time.deltaTime;
            UpdateTutorial();
            CheckGameOverConditions();
        }

        // =============================================================
        //  STATE MANAGEMENT
        // =============================================================

        private void SetState(GameFlowState newState)
        {
            if (CurrentState == newState) return;
            GameFlowState prev = CurrentState;
            CurrentState = newState;
            Debug.Log("[GameFlowManager] State: " + prev + " -> " + newState);
            OnStateChanged?.Invoke(newState);
        }

        public void TogglePause()
        {
            if (_gameOver) return;
            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0f : 1f;
            SetState(IsPaused ? GameFlowState.Paused : GameFlowState.Playing);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            SetState(GameFlowState.Playing);
        }

        // =============================================================
        //  DAILY TICK - THE HEARTBEAT
        // =============================================================

        private void OnDayEnd()
        {
            DaysSurvived++;
            Debug.Log("[GameFlowManager] Day " + DaysSurvived + " ended.");

            // Reputation decay
            if (_repMgr != null)
                _repMgr.ProcessDailyDecay();

            // Auto-save
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

            OnDayPassed?.Invoke(DaysSurvived);

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
                    "Quality matters. Higher skill workers produce better product.\n" +
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
                    "Keep workers loyal. Pay wages, do not overwork them.");
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
                    "You are hot! Police are actively pursuing you.\n" +
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
            Debug.Log("[GameFlowManager] Milestone reached: " + milestone);
            OnMilestoneReached?.Invoke(milestone);
        }

        // =============================================================
        //  TUTORIAL SYSTEM
        // =============================================================

        public void EnqueueTutorial(string title, string body, float duration = TUTORIAL_DISPLAY_TIME)
        {
            TutorialPrompt prompt;
            prompt.title = title;
            prompt.body = body;
            prompt.duration = duration;
            _tutorialQueue.Enqueue(prompt);
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
                _activeTutorialText = "<b>" + next.title + "</b>\n" + next.body;
                _tutorialTimer = next.duration;
                _tutorialFadeStart = next.duration - TUTORIAL_FADE_TIME;
                _tutorialActive = true;
            }
        }

        // =============================================================
        //  SAVE / LOAD
        // =============================================================

        public CloutSaveData CaptureGameState()
        {
            CloutSaveData data = new CloutSaveData();
            data.playTime = SessionTime;
            data.currentDistrict = DistrictManager.Instance != null
                ? DistrictManager.Instance.CurrentDistrictId : "fillmore";

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                data.playerPosition = player.transform.position;
                data.playerRotation = player.transform.eulerAngles;
            }

            if (_cashMgr != null)
            {
                data.dirtyMoney = _cashMgr.DirtyCash;
                data.cleanMoney = _cashMgr.CleanCash;
            }

            if (_repMgr != null)
            {
                data.cloutScore = _repMgr.CurrentClout;
                data.cloutRank = _repMgr.CurrentRankName;
                data.streetRep = _repMgr.streetRep;
                data.civilianRep = _repMgr.civilianRep;
                ReputationVector repVec = _repMgr.GetReputationVector();
                data.reputationFear = repVec.fear;
                data.reputationRespect = repVec.respect;
                data.reputationReliability = repVec.reliability;
                data.reputationRuthlessness = repVec.ruthlessness;
            }

            WantedSystem wanted = FindAnyObjectByType<WantedSystem>();
            if (wanted != null)
            {
                data.currentHeat = wanted.CurrentHeat;
                data.wantedLevel = (int)wanted.CurrentLevel;
            }

            if (_propertyMgr != null)
            {
                foreach (Property prop in _propertyMgr.OwnedProperties)
                {
                    SavedProperty sp = new SavedProperty();
                    sp.propertyId = prop.Definition != null ? prop.Definition.propertyName : "unknown";
                    sp.districtId = prop.Definition != null ? prop.Definition.districtName : "";
                    sp.upgradeLevel = 0;
                    sp.stash = new List<SavedProductStack>();

                    foreach (StashSlot slot in prop.Stash)
                    {
                        SavedProductStack sps = new SavedProductStack();
                        sps.productId = slot.productId;
                        sps.quantity = slot.quantity;
                        sps.qualityTier = 0;
                        sps.potency = slot.quality;
                        sp.stash.Add(sps);
                    }

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

            if (_workerMgr != null)
            {
                foreach (WorkerInstance worker in _workerMgr.Workers)
                {
                    SavedWorker sw = new SavedWorker();
                    sw.workerId = worker.workerId;
                    sw.workerName = worker.workerName;
                    sw.assignedPropertyId = worker.assignedPropertyId;
                    sw.workerType = worker.role.ToString();
                    sw.loyalty = worker.loyalty;
                    sw.skill = worker.skill;
                    sw.discretion = worker.discretion;
                    sw.greed = worker.greed;
                    sw.courage = worker.courage;
                    sw.intelligence = worker.intelligence;
                    sw.totalDeals = worker.totalDeals;
                    sw.totalUnitsProduced = worker.totalUnitsProduced;
                    sw.totalCashEarned = worker.totalCashEarned;
                    sw.shiftsCompleted = worker.shiftsCompleted;
                    data.hiredWorkers.Add(sw);
                }
            }

            data.completedMilestones = new List<string>();
            foreach (Milestone m in Enum.GetValues(typeof(Milestone)))
            {
                if (Milestones.IsComplete(m))
                    data.completedMilestones.Add(m.ToString());
            }

            data.daysSurvived = DaysSurvived;
            data.totalDeals = Milestones.totalDeals;
            data.totalRevenue = Milestones.totalRevenue;
            data.totalKills = Milestones.totalKills;

            DistrictManager distMgr = DistrictManager.Instance;
            if (distMgr != null)
            {
                DistrictRuntimeState rtState = distMgr.GetRuntimeState("fillmore");
                data.districtControlLevel = rtState.controlLevel;
                data.districtTotalDeals = rtState.totalDealsCompleted;
                data.districtTotalRevenue = rtState.totalRevenue;
            }

            return data;
        }

        public bool SaveGame(int slot = 0)
        {
            SetState(GameFlowState.Saving);
            CloutSaveData data = CaptureGameState();
            bool success = SaveManager.Save(data, slot);
            SetState(GameFlowState.Playing);
            return success;
        }

        public bool LoadGame(int slot = 0)
        {
            CloutSaveData data = SaveManager.Load(slot);
            if (data == null) return false;
            SetState(GameFlowState.Loading);
            RestoreGameState(data);
            SetState(GameFlowState.Playing);
            return true;
        }

        private void AutoSave()
        {
            if (SaveGame(autoSaveSlot))
                Debug.Log("[GameFlowManager] Auto-saved to slot " + autoSaveSlot + " on day " + DaysSurvived);
        }

        public void RestoreGameState(CloutSaveData data)
        {
            SessionTime = data.playTime;
            DaysSurvived = data.daysSurvived;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = data.playerPosition;
                player.transform.eulerAngles = data.playerRotation;
            }

            if (_cashMgr != null)
                _cashMgr.DebugSetCash(data.dirtyMoney, data.cleanMoney);

            if (_repMgr != null)
            {
                _repMgr.fear = data.reputationFear;
                _repMgr.respect = data.reputationRespect;
                _repMgr.reliability = data.reputationReliability;
                _repMgr.ruthlessness = data.reputationRuthlessness;
                _repMgr.streetRep = data.streetRep;
                _repMgr.civilianRep = data.civilianRep;
            }

            if (data.completedMilestones != null)
            {
                foreach (string ms in data.completedMilestones)
                {
                    if (Enum.TryParse(ms, out Milestone m))
                        Milestones.Complete(m);
                }
            }

            Milestones.totalDeals = data.totalDeals;
            Milestones.totalRevenue = data.totalRevenue;
            Milestones.totalKills = data.totalKills;

            Debug.Log("[GameFlowManager] Game state restored. Day " + DaysSurvived);
        }

        // =============================================================
        //  GAME OVER
        // =============================================================

        private void CheckGameOverConditions()
        {
            if (_cashMgr != null && _cashMgr.TotalCash <= 0f &&
                _propertyMgr != null && _propertyMgr.PropertyCount == 0 &&
                _workerMgr != null && _workerMgr.WorkerCount == 0)
            {
                if (SessionTime > 30f && DaysSurvived > 0)
                {
                    _gameOverTimer += Time.deltaTime;
                    if (_gameOverTimer > 10f)
                        TriggerGameOver("BANKRUPT - You lost everything. The streets do not forgive.");
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
            SetState(GameFlowState.GameOver);
            Debug.Log("[GameFlowManager] GAME OVER: " + reason);
        }

        // =============================================================
        //  QUICK SAVE / QUICK LOAD (F5 / F9)
        // =============================================================

        private void LateUpdate()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SaveGame(0);
                EnqueueTutorial("Game Saved", "Progress saved to slot 1.", 3f);
            }
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
        //  OnGUI - TUTORIAL + PAUSE + GAME OVER + DAY COUNTER
        // =============================================================

        private void OnGUI()
        {
            if (IsPaused) { DrawPauseScreen(); return; }
            if (_gameOver) { DrawGameOverScreen(); return; }

            if (_tutorialActive && !string.IsNullOrEmpty(_activeTutorialText))
                DrawTutorialPrompt();

            if (CurrentState == GameFlowState.Saving)
            {
                GUI.color = new Color(1f, 0.85f, 0.2f, 0.9f);
                GUIStyle saveStyle = new GUIStyle(GUI.skin.label);
                saveStyle.fontSize = 14;
                saveStyle.fontStyle = FontStyle.Bold;
                saveStyle.alignment = TextAnchor.MiddleRight;
                GUI.Label(new Rect(Screen.width - 180, 10, 170, 30), "Saving...", saveStyle);
                GUI.color = Color.white;
            }

            DrawDayCounter();
        }

        private void DrawTutorialPrompt()
        {
            float alpha = 1f;
            if (_tutorialTimer < TUTORIAL_FADE_TIME)
                alpha = _tutorialTimer / TUTORIAL_FADE_TIME;

            float boxW = 500f;
            float boxH = 100f;
            float boxX = (Screen.width - boxW) * 0.5f;
            float boxY = Screen.height * 0.15f;

            GUI.color = new Color(0.05f, 0.05f, 0.1f, 0.85f * alpha);
            GUI.Box(new Rect(boxX - 5, boxY - 5, boxW + 10, boxH + 10), "");

            GUI.color = new Color(0.2f, 0.8f, 0.4f, alpha);
            GUI.DrawTexture(new Rect(boxX, boxY, 4f, boxH), Texture2D.whiteTexture);

            GUI.color = new Color(0.9f, 0.95f, 0.9f, alpha);
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.wordWrap = true;
            style.richText = true;
            style.alignment = TextAnchor.UpperLeft;
            style.padding = new RectOffset(14, 10, 8, 8);
            GUI.Label(new Rect(boxX, boxY, boxW, boxH), _activeTutorialText, style);

            GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.5f * alpha);
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = 10;
            hintStyle.alignment = TextAnchor.LowerRight;
            GUI.Label(new Rect(boxX, boxY, boxW - 10, boxH - 5), "[any key to dismiss]", hintStyle);

            GUI.color = Color.white;

            if (Event.current.type == EventType.KeyDown || Event.current.type == EventType.MouseDown)
                _tutorialTimer = Mathf.Min(_tutorialTimer, TUTORIAL_FADE_TIME * 0.5f);
        }

        private void DrawPauseScreen()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            GUI.color = new Color(0.9f, 0.95f, 1f, 1f);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 36;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(cx - 200, cy - 120, 400, 60), "PAUSED", titleStyle);

            GUIStyle infoStyle = new GUIStyle(GUI.skin.label);
            infoStyle.fontSize = 16;
            infoStyle.alignment = TextAnchor.MiddleCenter;
            GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);

            float totalCash = _cashMgr != null ? _cashMgr.TotalCash : 0f;
            int workers = _workerMgr != null ? _workerMgr.WorkerCount : 0;
            string statsText = "Day " + DaysSurvived + "  |  $" + totalCash.ToString("N0") +
                "  |  " + Milestones.totalDeals + " deals  |  " + workers + " workers";
            GUI.Label(new Rect(cx - 250, cy - 50, 500, 30), statsText, infoStyle);

            GUI.color = Color.white;
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 18;
            btnStyle.fixedHeight = 45;
            float btnW = 220f;
            float btnX = cx - btnW * 0.5f;

            if (GUI.Button(new Rect(btnX, cy + 10, btnW, 45), "Resume [ESC]", btnStyle))
                Resume();
            if (GUI.Button(new Rect(btnX, cy + 65, btnW, 45), "Quick Save [F5]", btnStyle))
            {
                SaveGame(0);
                Resume();
            }
            if (GUI.Button(new Rect(btnX, cy + 120, btnW, 45), "Quick Load [F9]", btnStyle))
            {
                if (SaveManager.HasSave(0)) { LoadGame(0); Resume(); }
            }
            GUI.color = Color.white;
        }

        private void DrawGameOverScreen()
        {
            GUI.color = new Color(0.1f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            GUI.color = new Color(1f, 0.15f, 0.1f, 1f);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 48;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(cx - 300, cy - 140, 600, 70), "GAME OVER", titleStyle);

            GUI.color = new Color(0.8f, 0.6f, 0.5f, 1f);
            GUIStyle reasonStyle = new GUIStyle(GUI.skin.label);
            reasonStyle.fontSize = 18;
            reasonStyle.alignment = TextAnchor.MiddleCenter;
            reasonStyle.wordWrap = true;
            GUI.Label(new Rect(cx - 250, cy - 50, 500, 40), _gameOverReason, reasonStyle);

            GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            GUIStyle statStyle = new GUIStyle(GUI.skin.label);
            statStyle.fontSize = 14;
            statStyle.alignment = TextAnchor.MiddleCenter;
            string finalStats = "Survived " + DaysSurvived + " days  |  " +
                Milestones.totalDeals + " deals  |  $" + Milestones.totalRevenue.ToString("N0") +
                " earned  |  " + Milestones.totalKills + " kills";
            GUI.Label(new Rect(cx - 250, cy + 10, 500, 30), finalStats, statStyle);

            GUI.color = Color.white;
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 18;
            btnStyle.fixedHeight = 45;
            if (SaveManager.HasSave(0))
            {
                if (GUI.Button(new Rect(cx - 100, cy + 70, 200, 45), "Load Last Save", btnStyle))
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
            GUIStyle dayStyle = new GUIStyle(GUI.skin.label);
            dayStyle.fontSize = 13;
            dayStyle.fontStyle = FontStyle.Bold;
            dayStyle.alignment = TextAnchor.MiddleRight;
            GUI.color = new Color(0.9f, 0.9f, 0.8f, 0.7f);
            GUI.Label(new Rect(Screen.width - 160, 40, 150, 25), "DAY " + (DaysSurvived + 1), dayStyle);
            GUI.color = Color.white;
        }
    }

    // =================================================================
    //  ENUMS & DATA STRUCTURES
    // =================================================================

    public enum GameFlowState
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

        public int totalDeals;
        public float totalRevenue;
        public int totalKills;
        public int totalProperties;
        public int totalWorkersHired;
        public int totalProductsCooked;
        public int totalRaids;

        public bool IsComplete(Milestone m) { return _completed.Contains(m); }
        public void Complete(Milestone m) { _completed.Add(m); }
        public int CompletedCount { get { return _completed.Count; } }
        public IEnumerable<Milestone> Completed { get { return _completed; } }
    }

    public struct TutorialPrompt
    {
        public string title;
        public string body;
        public float duration;
    }
}
