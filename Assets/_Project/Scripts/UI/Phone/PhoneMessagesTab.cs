using UnityEngine;
using System.Collections.Generic;
using Clout.Utils;

namespace Clout.UI.Phone
{
    /// <summary>
    /// Phone Messages Tab — notification center and message log.
    ///
    /// Captures events from EventBus and presents them as timestamped messages:
    ///   - Worker reports (arrests, betrayals, shift completions)
    ///   - Raid alerts + warnings
    ///   - Wanted level changes
    ///   - Deal milestones
    ///   - Property events (purchase, upgrade, raid damage)
    ///   - District transitions
    ///
    /// Messages are categorized by severity:
    ///   ALERT  — immediate danger (raids, arrests)
    ///   WARNING — attention needed (heat spikes, betrayals)
    ///   INFO   — routine updates (deals, shifts, district changes)
    ///
    /// Ring buffer maintains last 100 messages.
    /// </summary>
    public class PhoneMessagesTab : MonoBehaviour
    {
        private readonly List<PhoneMessage> _messages = new List<PhoneMessage>();
        private const int MAX_MESSAGES = 100;
        private Vector2 _scrollPos;

        private enum MessageFilter { All, Alerts, Workers, Business }
        private MessageFilter _filter = MessageFilter.All;

        // ─── Lifecycle ──────────────────────────────────────────

        private void Start()
        {
            // Subscribe to all message-worthy events
            EventBus.Subscribe<WorkerArrestedEvent>(OnWorkerArrested);
            EventBus.Subscribe<WorkerBetrayedEvent>(OnWorkerBetrayed);
            EventBus.Subscribe<WorkerFiredEvent>(OnWorkerFired);
            EventBus.Subscribe<WorkerShiftEndEvent>(OnShiftEnd);
            EventBus.Subscribe<WorkerDealCompleteEvent>(OnWorkerDeal);
            EventBus.Subscribe<PropertyRaidAlertEvent>(OnRaidAlert);
            EventBus.Subscribe<PropertyRaidedEvent>(OnRaided);
            EventBus.Subscribe<PropertyPurchasedEvent>(OnPropertyBought);
            EventBus.Subscribe<PropertyUpgradedEvent>(OnPropertyUpgraded);
            EventBus.Subscribe<WantedLevelChangedEvent>(OnWantedChanged);
            EventBus.Subscribe<HeatChangedEvent>(OnHeatChanged);
            EventBus.Subscribe<DistrictEnteredEvent>(OnDistrictEntered);
            EventBus.Subscribe<CrimeWitnessedEvent>(OnCrimeWitnessed);
            EventBus.Subscribe<PoliceBackupRequestEvent>(OnBackupCalled);
            EventBus.Subscribe<DealCompletedEvent>(OnDealCompleted);

            // Welcome message
            AddMessage(MessageSeverity.Info, "SYSTEM",
                "Phone activated. Welcome to CLOUT.", "system");
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<WorkerArrestedEvent>(OnWorkerArrested);
            EventBus.Unsubscribe<WorkerBetrayedEvent>(OnWorkerBetrayed);
            EventBus.Unsubscribe<WorkerFiredEvent>(OnWorkerFired);
            EventBus.Unsubscribe<WorkerShiftEndEvent>(OnShiftEnd);
            EventBus.Unsubscribe<WorkerDealCompleteEvent>(OnWorkerDeal);
            EventBus.Unsubscribe<PropertyRaidAlertEvent>(OnRaidAlert);
            EventBus.Unsubscribe<PropertyRaidedEvent>(OnRaided);
            EventBus.Unsubscribe<PropertyPurchasedEvent>(OnPropertyBought);
            EventBus.Unsubscribe<PropertyUpgradedEvent>(OnPropertyUpgraded);
            EventBus.Unsubscribe<WantedLevelChangedEvent>(OnWantedChanged);
            EventBus.Unsubscribe<HeatChangedEvent>(OnHeatChanged);
            EventBus.Unsubscribe<DistrictEnteredEvent>(OnDistrictEntered);
            EventBus.Unsubscribe<CrimeWitnessedEvent>(OnCrimeWitnessed);
            EventBus.Unsubscribe<PoliceBackupRequestEvent>(OnBackupCalled);
            EventBus.Unsubscribe<DealCompletedEvent>(OnDealCompleted);
        }

        // ─── Event Handlers ─────────────────────────────────────

        private void OnWorkerArrested(WorkerArrestedEvent e)
        {
            string info = e.knewCriticalInfo ? " KNOWS CRITICAL INFO!" : "";
            AddMessage(MessageSeverity.Alert, "ARREST",
                $"{e.workerName} was arrested! Heat +{e.heatGenerated:F0}.{info}", "worker");
        }

        private void OnWorkerBetrayed(WorkerBetrayedEvent e)
        {
            AddMessage(MessageSeverity.Alert, "BETRAYAL",
                $"{e.workerName} betrayed you ({e.betrayalType})! Damage: ${e.damageAmount:N0}", "worker");
        }

        private void OnWorkerFired(WorkerFiredEvent e)
        {
            AddMessage(MessageSeverity.Info, "WORKER",
                $"{e.workerName} fired. Reason: {e.reason}", "worker");
        }

        private void OnShiftEnd(WorkerShiftEndEvent e)
        {
            if (e.cashEarned > 0)
                AddMessage(MessageSeverity.Info, "SHIFT",
                    $"{e.workerName} ({e.role}) earned ${e.cashEarned:N0} " +
                    $"({e.dealsMade} deals)", "worker");
            else if (e.unitsProduced > 0)
                AddMessage(MessageSeverity.Info, "SHIFT",
                    $"{e.workerName} ({e.role}) produced {e.unitsProduced} units", "worker");
        }

        private void OnWorkerDeal(WorkerDealCompleteEvent e)
        {
            // Don't log every deal — too spammy. Only notable ones.
            if (e.cashEarned > 200)
                AddMessage(MessageSeverity.Info, "BIG DEAL",
                    $"Worker sold {e.quantity}x {e.productId} for ${e.cashEarned:N0}", "business");
        }

        private void OnRaidAlert(PropertyRaidAlertEvent e)
        {
            AddMessage(MessageSeverity.Alert, "RAID INCOMING",
                $"Property {e.propertyId} will be raided in {e.warningTime:F0}s! " +
                $"Strength: {e.raidStrength} officers", "alert");
        }

        private void OnRaided(PropertyRaidedEvent e)
        {
            AddMessage(MessageSeverity.Alert, "RAIDED",
                $"Property {e.propertyId} was raided. Lost {e.productConfiscated} units, " +
                $"${e.cashConfiscated:N0} cash.", "alert");
        }

        private void OnPropertyBought(PropertyPurchasedEvent e)
        {
            AddMessage(MessageSeverity.Info, "PROPERTY",
                $"Purchased {e.propertyId} for ${e.price:N0}", "business");
        }

        private void OnPropertyUpgraded(PropertyUpgradedEvent e)
        {
            AddMessage(MessageSeverity.Info, "UPGRADE",
                $"Upgraded {e.propertyId}: {e.upgradeName}", "business");
        }

        private void OnWantedChanged(WantedLevelChangedEvent e)
        {
            if (e.newLevel > e.previousLevel)
            {
                string[] levelNames = { "Clean", "Suspicious", "Wanted", "Hunted", "Most Wanted" };
                string name = e.newLevel < levelNames.Length ? levelNames[e.newLevel] : "Kingpin";
                MessageSeverity sev = e.newLevel >= 3 ? MessageSeverity.Alert : MessageSeverity.Warning;
                AddMessage(sev, "HEAT",
                    $"Wanted level increased to: {name}", "alert");
            }
            else
            {
                AddMessage(MessageSeverity.Info, "HEAT",
                    "Heat reduced. Wanted level decreased.", "alert");
            }
        }

        private void OnHeatChanged(HeatChangedEvent e)
        {
            // Only log significant heat changes
            if (Mathf.Abs(e.changeAmount) > 30)
            {
                AddMessage(MessageSeverity.Warning, "HEAT",
                    $"Heat {(e.changeAmount > 0 ? "+" : "")}{e.changeAmount:F0}: {e.reason}", "alert");
            }
        }

        private void OnDistrictEntered(DistrictEnteredEvent e)
        {
            AddMessage(MessageSeverity.Info, "LOCATION",
                $"Entered district: {e.districtId}", "system");
        }

        private void OnCrimeWitnessed(CrimeWitnessedEvent e)
        {
            if (e.witnessCount > 2)
                AddMessage(MessageSeverity.Warning, "WITNESSES",
                    $"{e.crimeType} witnessed by {e.witnessCount} people. Heat +{e.heatGenerated:F0}", "alert");
        }

        private void OnBackupCalled(PoliceBackupRequestEvent e)
        {
            AddMessage(MessageSeverity.Alert, "BACKUP",
                $"Police called for backup at your location!", "alert");
        }

        private void OnDealCompleted(DealCompletedEvent e)
        {
            // Only log big deals
            if (e.cashEarned > 500)
                AddMessage(MessageSeverity.Info, "DEAL",
                    $"Sold {e.quantity}x {e.productId} for ${e.cashEarned:N0}", "business");
        }

        // ─── Message Management ─────────────────────────────────

        public void AddMessage(MessageSeverity severity, string title, string body, string category)
        {
            _messages.Insert(0, new PhoneMessage
            {
                severity = severity,
                title = title,
                body = body,
                category = category,
                timestamp = Time.time,
                isRead = false
            });

            while (_messages.Count > MAX_MESSAGES)
                _messages.RemoveAt(_messages.Count - 1);

            // Notify phone controller
            if (PhoneController.Instance != null && severity >= MessageSeverity.Warning)
                PhoneController.Instance.AddMessage();
        }

        // ─── Drawing ────────────────────────────────────────────

        public void DrawTab(Rect rect)
        {
            GUILayout.BeginArea(rect);

            // Filter bar
            GUILayout.BeginHorizontal();
            if (DrawFilterButton("All", _filter == MessageFilter.All)) _filter = MessageFilter.All;
            if (DrawFilterButton("Alerts", _filter == MessageFilter.Alerts)) _filter = MessageFilter.Alerts;
            if (DrawFilterButton("Workers", _filter == MessageFilter.Workers)) _filter = MessageFilter.Workers;
            if (DrawFilterButton("Business", _filter == MessageFilter.Business)) _filter = MessageFilter.Business;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Message count
            int filteredCount = GetFilteredCount();
            GUI.color = PhoneController.DimText;
            GUILayout.Label($"{filteredCount} messages", PhoneController.Instance.BodyStyle);
            GUI.color = Color.white;

            GUILayout.Space(4);

            // Scrollable message list
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(rect.height - 60));

            int displayed = 0;
            foreach (var msg in _messages)
            {
                if (!PassesFilter(msg)) continue;
                DrawMessage(msg, rect.width - 30);
                displayed++;
                if (displayed >= 50) break;
            }

            if (displayed == 0)
            {
                GUI.color = PhoneController.DimText;
                GUILayout.Space(20);
                GUILayout.Label("No messages in this category.", PhoneController.Instance.BodyStyle);
                GUI.color = Color.white;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawMessage(PhoneMessage msg, float width)
        {
            Color severityColor = msg.severity switch
            {
                MessageSeverity.Alert => PhoneController.AccentRed,
                MessageSeverity.Warning => PhoneController.AccentGold,
                _ => PhoneController.DimText
            };

            Rect msgRect = GUILayoutUtility.GetRect(width, 42);

            // Background
            GUI.color = msg.isRead ? new Color(0.1f, 0.1f, 0.14f) : new Color(0.13f, 0.13f, 0.19f);
            GUI.DrawTexture(msgRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Severity bar
            GUI.color = severityColor;
            GUI.DrawTexture(new Rect(msgRect.x, msgRect.y, 3, msgRect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float x = msgRect.x + 8;
            float y = msgRect.y + 3;

            // Title + timestamp
            GUIStyle titleS = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold };
            titleS.normal.textColor = severityColor;
            GUI.Label(new Rect(x, y, width * 0.6f, 15), msg.title, titleS);

            // Time
            float seconds = Time.time - msg.timestamp;
            string timeStr;
            if (seconds < 60) timeStr = "Just now";
            else if (seconds < 3600) timeStr = $"{seconds / 60:F0}m ago";
            else timeStr = $"{seconds / 3600:F1}h ago";

            GUIStyle timeS = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.MiddleRight };
            timeS.normal.textColor = PhoneController.DimText;
            GUI.Label(new Rect(x + width * 0.5f, y, width * 0.45f, 14), timeStr, timeS);

            // Body
            GUIStyle bodyS = new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = true };
            bodyS.normal.textColor = PhoneController.TextCol;
            GUI.Label(new Rect(x, y + 16, width - 16, 22), msg.body, bodyS);

            // Mark as read when rendered
            msg.isRead = true;

            GUILayout.Space(2);
        }

        private bool PassesFilter(PhoneMessage msg)
        {
            return _filter switch
            {
                MessageFilter.Alerts => msg.severity >= MessageSeverity.Warning,
                MessageFilter.Workers => msg.category == "worker",
                MessageFilter.Business => msg.category == "business",
                _ => true
            };
        }

        private int GetFilteredCount()
        {
            int count = 0;
            foreach (var msg in _messages)
                if (PassesFilter(msg)) count++;
            return count;
        }

        private bool DrawFilterButton(string label, bool active)
        {
            GUIStyle s = new GUIStyle(GUI.skin.button) { fontSize = 10 };
            s.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
            GUI.color = active ? PhoneController.AccentGold : PhoneController.DimText;
            bool clicked = GUILayout.Button(label, s, GUILayout.Height(22));
            GUI.color = Color.white;
            return clicked;
        }
    }

    // ─── Data Structures ─────────────────────────────────────────

    public enum MessageSeverity
    {
        Info = 0,
        Warning = 1,
        Alert = 2
    }

    public class PhoneMessage
    {
        public MessageSeverity severity;
        public string title;
        public string body;
        public string category;
        public float timestamp;
        public bool isRead;
    }
}
