using UnityEngine;
using System.Collections.Generic;
using Clout.Core;
using Clout.AI.Factions;
using Clout.Utils;

namespace Clout.UI.Factions
{
    /// <summary>
    /// Step 14 — OnGUI diplomacy panel showing all rival factions,
    /// their disposition, relationship status, and available actions.
    ///
    /// Toggle: F key (consistent with existing OnGUI keybind patterns)
    /// Phase 5 migration target: UI Toolkit
    /// </summary>
    public class DiplomacyUI : MonoBehaviour
    {
        [Header("Toggle")]
        [Tooltip("Key to toggle diplomacy panel")]
        public KeyCode toggleKey = KeyCode.F;

        [Header("Layout")]
        public float panelWidth = 520f;
        public float panelHeight = 600f;

        private bool _isOpen = false;
        private Vector2 _scrollPos;
        private FactionRuntimeState _selectedFaction;
        private string _statusMessage = "";
        private float _statusTimer = 0f;

        // ─── Notification Queue ────────────────────────────────────

        private struct Notification
        {
            public string message;
            public float timer;
            public Color color;
        }

        private List<Notification> _notifications = new List<Notification>();

        // ─── Lifecycle ─────────────────────────────────────────────

        private void Start()
        {
            EventBus.Subscribe<FactionWarDeclaredEvent>(OnWarDeclared);
            EventBus.Subscribe<FactionAllianceFormedEvent>(OnAllianceFormed);
            EventBus.Subscribe<FactionBetrayalEvent>(OnBetrayal);
            EventBus.Subscribe<FactionAttackEvent>(OnAttack);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<FactionWarDeclaredEvent>(OnWarDeclared);
            EventBus.Unsubscribe<FactionAllianceFormedEvent>(OnAllianceFormed);
            EventBus.Unsubscribe<FactionBetrayalEvent>(OnBetrayal);
            EventBus.Unsubscribe<FactionAttackEvent>(OnAttack);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                _isOpen = !_isOpen;

            // Decay status message
            if (_statusTimer > 0f)
            {
                _statusTimer -= Time.deltaTime;
                if (_statusTimer <= 0f) _statusMessage = "";
            }

            // Decay notifications
            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                var n = _notifications[i];
                n.timer -= Time.deltaTime;
                _notifications[i] = n;
                if (n.timer <= 0f) _notifications.RemoveAt(i);
            }
        }

        // ─── Event Handlers (Notifications) ────────────────────────

        private void OnWarDeclared(FactionWarDeclaredEvent evt)
        {
            if (evt.targetsPlayer)
            {
                var state = FactionManager.Instance?.GetFaction(evt.aggressor);
                string name = state?.Profile.factionName ?? evt.aggressor.ToString();
                AddNotification($"⚔ {name} has declared WAR!", Color.red);
            }
        }

        private void OnAllianceFormed(FactionAllianceFormedEvent evt)
        {
            if (evt.withPlayer)
            {
                var state = FactionManager.Instance?.GetFaction(evt.factionId);
                string name = state?.Profile.factionName ?? evt.factionId.ToString();
                AddNotification($"🤝 Alliance formed with {name}!", Color.green);
            }
        }

        private void OnBetrayal(FactionBetrayalEvent evt)
        {
            if (evt.victimIsPlayer)
            {
                var state = FactionManager.Instance?.GetFaction(evt.betrayer);
                string name = state?.Profile.factionName ?? evt.betrayer.ToString();
                AddNotification($"🗡 {name} BETRAYED you!", new Color(1f, 0.3f, 0f));
            }
        }

        private void OnAttack(FactionAttackEvent evt)
        {
            if (evt.targetsPlayer)
            {
                var state = FactionManager.Instance?.GetFaction(evt.attacker);
                string name = state?.Profile.factionName ?? evt.attacker.ToString();
                AddNotification($"💥 {name} is attacking!", Color.yellow);
            }
        }

        private void AddNotification(string message, Color color)
        {
            _notifications.Add(new Notification { message = message, timer = 5f, color = color });
        }

        // ═══════════════════════════════════════════════════════════
        //  OnGUI RENDERING
        // ═══════════════════════════════════════════════════════════

        private void OnGUI()
        {
            // Always draw notifications (even when panel is closed)
            DrawNotifications();

            // Draw pending action alerts
            DrawPendingActions();

            if (!_isOpen) return;
            if (FactionManager.Instance == null) return;

            float x = (Screen.width - panelWidth) / 2f;
            float y = (Screen.height - panelHeight) / 2f;
            Rect panelRect = new Rect(x, y, panelWidth, panelHeight);

            GUI.Box(panelRect, "");

            GUILayout.BeginArea(panelRect);

            // ─── Header ───────────────────────────────────────────

            GUILayout.BeginHorizontal("box");
            GUILayout.Label("FACTION DIPLOMACY", new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            });
            if (GUILayout.Button("X", GUILayout.Width(30)))
                _isOpen = false;
            GUILayout.EndHorizontal();

            // ─── Status Message ───────────────────────────────────

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                GUILayout.Label(_statusMessage, new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.yellow },
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Italic
                });
            }

            // ─── Faction List ─────────────────────────────────────

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            var factions = FactionManager.Instance.GetAllFactions();
            foreach (var state in factions)
            {
                DrawFactionCard(state);
                GUILayout.Space(4);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ─── Faction Card ──────────────────────────────────────────

        private void DrawFactionCard(FactionRuntimeState state)
        {
            var profile = state.Profile;
            var relationship = FactionDiplomacy.GetRelationship(state.PlayerDisposition);

            GUILayout.BeginVertical("box");

            // ─── Name + Relationship ──────────────────────────────

            GUILayout.BeginHorizontal();

            // Faction color indicator
            var colorStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };
            Color prevColor = GUI.contentColor;
            GUI.contentColor = profile.factionColor;
            GUILayout.Label(profile.factionName.ToUpper(), colorStyle, GUILayout.Width(200));
            GUI.contentColor = prevColor;

            // Disposition bar
            DrawDispositionBar(state.PlayerDisposition);

            // Relationship tag
            GUILayout.Label(relationship.ToString(), GetRelationshipStyle(relationship), GUILayout.Width(70));

            GUILayout.EndHorizontal();

            // ─── Stats Row ────────────────────────────────────────

            GUILayout.BeginHorizontal();
            GUILayout.Label($"  Leader: {profile.leaderName}", GUILayout.Width(160));
            GUILayout.Label($"Zones: {state.ControlledZones.Count}", GUILayout.Width(70));
            GUILayout.Label($"Strength: {DrawStrengthPips(state.CombatStrength)}", GUILayout.Width(130));
            GUILayout.Label($"Mood: {state.CurrentMood}", GUILayout.Width(110));
            GUILayout.EndHorizontal();

            // ─── Archetype + Cash ─────────────────────────────────

            GUILayout.BeginHorizontal();
            GUILayout.Label($"  Type: {profile.GetArchetype()}", GUILayout.Width(160));
            GUILayout.Label($"Cash: ${state.Cash:N0}", GUILayout.Width(140));

            if (state.IsAtWarWithPlayer)
            {
                var warStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red }, fontStyle = FontStyle.Bold };
                GUILayout.Label("AT WAR", warStyle);
            }
            else if (state.IsAlliedWithPlayer)
            {
                var allyStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.green }, fontStyle = FontStyle.Bold };
                GUILayout.Label("ALLIED", allyStyle);
            }

            GUILayout.EndHorizontal();

            // ─── Action Buttons ───────────────────────────────────

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);

            if (FactionDiplomacy.Instance != null)
            {
                if (!state.IsAtWarWithPlayer)
                {
                    if (!state.IsAlliedWithPlayer)
                    {
                        if (GUILayout.Button("Alliance", GUILayout.Width(70)))
                        {
                            bool success = FactionDiplomacy.Instance.PlayerProposeAlliance(profile.factionId);
                            SetStatus(success ? $"Alliance formed with {profile.factionName}!"
                                              : $"{profile.factionName} rejected alliance.");
                        }
                    }

                    if (GUILayout.Button("Tribute $5K", GUILayout.Width(85)))
                    {
                        FactionDiplomacy.Instance.PlayerOfferTribute(profile.factionId, 5000f);
                        SetStatus($"Paid $5,000 tribute to {profile.factionName}");
                    }

                    if (GUILayout.Button("Trade", GUILayout.Width(55)))
                    {
                        ProductType product = profile.preferredProducts != null && profile.preferredProducts.Length > 0
                            ? profile.preferredProducts[0] : ProductType.Cannabis;
                        bool success = FactionDiplomacy.Instance.PlayerProposeTrade(profile.factionId, product, 10f);
                        SetStatus(success ? $"Trade deal with {profile.factionName}!"
                                          : $"{profile.factionName} declined trade.");
                    }

                    if (!state.IsAlliedWithPlayer)
                    {
                        if (GUILayout.Button("Declare War", GUILayout.Width(85)))
                        {
                            FactionDiplomacy.Instance.PlayerDeclareWar(profile.factionId);
                            SetStatus($"War declared on {profile.factionName}!");
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Betray", GUILayout.Width(55)))
                        {
                            FactionDiplomacy.Instance.PlayerBetrayAlliance(profile.factionId);
                            SetStatus($"You betrayed {profile.factionName}!");
                        }
                    }
                }
                else
                {
                    // At war — show ceasefire
                    if (GUILayout.Button("Ceasefire", GUILayout.Width(80)))
                    {
                        bool success = FactionDiplomacy.Instance.PlayerRequestCeasefire(profile.factionId, out float cost);
                        SetStatus(success ? $"Ceasefire with {profile.factionName} — cost: ${cost:N0}"
                                          : $"{profile.factionName} rejected ceasefire!");
                    }

                    if (GUILayout.Button("Tribute $5K", GUILayout.Width(85)))
                    {
                        FactionDiplomacy.Instance.PlayerOfferTribute(profile.factionId, 5000f);
                        SetStatus($"Paid $5,000 tribute to {profile.factionName}");
                    }
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        // ─── UI Helpers ────────────────────────────────────────────

        private void DrawDispositionBar(float disposition)
        {
            float normalizedDisposition = (disposition + 1f) / 2f; // Map -1..1 to 0..1
            Rect barRect = GUILayoutUtility.GetRect(120, 16);

            // Background
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
            GUI.DrawTexture(barRect, Texture2D.whiteTexture);

            // Fill
            Color barColor;
            if (disposition < -0.3f) barColor = Color.red;
            else if (disposition < 0.3f) barColor = Color.yellow;
            else barColor = Color.green;

            GUI.color = barColor;
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * normalizedDisposition, barRect.height);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

            // Center marker
            GUI.color = Color.white;
            Rect centerMark = new Rect(barRect.x + barRect.width * 0.5f - 1, barRect.y, 2, barRect.height);
            GUI.DrawTexture(centerMark, Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        private string DrawStrengthPips(float strength)
        {
            int filled = Mathf.RoundToInt(strength * 10);
            int empty = 10 - filled;
            return new string('#', filled) + new string('-', empty);
        }

        private GUIStyle GetRelationshipStyle(FactionRelationship rel)
        {
            Color c = rel switch
            {
                FactionRelationship.War => Color.red,
                FactionRelationship.Hostile => new Color(1f, 0.5f, 0f),
                FactionRelationship.Neutral => Color.white,
                FactionRelationship.Friendly => Color.cyan,
                FactionRelationship.Allied => Color.green,
                _ => Color.white
            };

            return new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = c },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void SetStatus(string message)
        {
            _statusMessage = message;
            _statusTimer = 3f;
        }

        // ─── Notifications (top-right corner) ─────────────────────

        private void DrawNotifications()
        {
            if (_notifications.Count == 0) return;

            float yOffset = 10f;
            float notifWidth = 350f;
            float notifHeight = 28f;

            for (int i = 0; i < _notifications.Count && i < 5; i++)
            {
                var n = _notifications[i];
                float alpha = Mathf.Clamp01(n.timer / 1.5f); // Fade out in last 1.5s
                Rect rect = new Rect(Screen.width - notifWidth - 10, yOffset, notifWidth, notifHeight);

                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.8f * alpha);
                GUI.Box(rect, "");
                GUI.backgroundColor = prevBg;

                var style = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = new Color(n.color.r, n.color.g, n.color.b, alpha) },
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13
                };
                GUI.Label(rect, n.message, style);

                yOffset += notifHeight + 4f;
            }
        }

        // ─── Pending Action Alerts ─────────────────────────────────

        private void DrawPendingActions()
        {
            if (FactionManager.Instance == null) return;

            var factions = FactionManager.Instance.GetAllFactions();
            float yOffset = Screen.height - 80f;

            foreach (var state in factions)
            {
                if (!state.HasPendingAction) continue;

                float alertWidth = 400f;
                float alertHeight = 50f;
                Rect alertRect = new Rect((Screen.width - alertWidth) / 2f, yOffset, alertWidth, alertHeight);

                GUI.Box(alertRect, "");

                GUILayout.BeginArea(alertRect);
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);

                string actionText = state.PendingActionType switch
                {
                    DiplomacyAction.DemandTribute =>
                        $"{state.Profile.factionName} demands ${state.PendingDemand:N0} tribute!",
                    DiplomacyAction.ProposeAlliance =>
                        $"{state.Profile.factionName} proposes an alliance!",
                    _ => $"{state.Profile.factionName} wants to negotiate."
                };

                GUILayout.Label(actionText, new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.yellow },
                    fontStyle = FontStyle.Bold
                });

                if (GUILayout.Button("Accept", GUILayout.Width(60)))
                {
                    HandlePendingAccept(state);
                    state.HasPendingAction = false;
                }

                if (GUILayout.Button("Reject", GUILayout.Width(60)))
                {
                    HandlePendingReject(state);
                    state.HasPendingAction = false;
                }

                GUILayout.EndHorizontal();
                GUILayout.EndArea();

                yOffset -= alertHeight + 5f;
            }
        }

        private void HandlePendingAccept(FactionRuntimeState state)
        {
            switch (state.PendingActionType)
            {
                case DiplomacyAction.DemandTribute:
                    if (FactionDiplomacy.Instance != null)
                        FactionDiplomacy.Instance.PlayerOfferTribute(state.Profile.factionId, state.PendingDemand);
                    SetStatus($"Paid ${state.PendingDemand:N0} tribute to {state.Profile.factionName}");
                    break;

                case DiplomacyAction.ProposeAlliance:
                    if (FactionDiplomacy.Instance != null)
                        FactionDiplomacy.Instance.PlayerProposeAlliance(state.Profile.factionId);
                    SetStatus($"Allied with {state.Profile.factionName}!");
                    break;
            }
        }

        private void HandlePendingReject(FactionRuntimeState state)
        {
            switch (state.PendingActionType)
            {
                case DiplomacyAction.DemandTribute:
                    FactionManager.Instance.ModifyDisposition(state.Profile.factionId, -0.15f, "Tribute demand rejected");
                    SetStatus($"Rejected {state.Profile.factionName}'s demand — they're not happy.");
                    break;

                case DiplomacyAction.ProposeAlliance:
                    FactionManager.Instance.ModifyDisposition(state.Profile.factionId, -0.05f, "Alliance proposal rejected");
                    SetStatus($"Rejected {state.Profile.factionName}'s alliance offer.");
                    break;
            }
        }
    }
}
