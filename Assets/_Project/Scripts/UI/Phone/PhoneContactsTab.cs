using UnityEngine;
using Clout.Core;
using Clout.Empire.Employees;
using Clout.Empire.Properties;
using Clout.World.Districts;

namespace Clout.UI.Phone
{
    /// <summary>
    /// Phone Contacts Tab — workforce roster, supplier relationships, customer loyalty.
    ///
    /// Shows:
    ///   - Active workers: name, role, assigned property, loyalty bar, current state
    ///   - Available hire slots
    ///   - Suppliers (from economy system)
    ///   - Key customers (top loyalty)
    /// </summary>
    public class PhoneContactsTab : MonoBehaviour
    {
        private WorkerManager _workerManager;
        private PropertyManager _propertyManager;
        private Vector2 _scrollPos;

        private enum ContactSection { Workers, Suppliers, Customers }
        private ContactSection _section = ContactSection.Workers;

        private void Start()
        {
            _workerManager = WorkerManager.Instance;
            _propertyManager = PropertyManager.Instance;
        }

        public void DrawTab(Rect rect)
        {
            if (_workerManager == null) _workerManager = WorkerManager.Instance;
            if (_propertyManager == null) _propertyManager = PropertyManager.Instance;

            GUILayout.BeginArea(rect);

            // Section switcher
            GUILayout.BeginHorizontal();
            if (DrawSectionButton("Workers", _section == ContactSection.Workers))
                _section = ContactSection.Workers;
            if (DrawSectionButton("Suppliers", _section == ContactSection.Suppliers))
                _section = ContactSection.Suppliers;
            if (DrawSectionButton("Customers", _section == ContactSection.Customers))
                _section = ContactSection.Customers;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(rect.height - 40));

            switch (_section)
            {
                case ContactSection.Workers: DrawWorkers(); break;
                case ContactSection.Suppliers: DrawSuppliers(); break;
                case ContactSection.Customers: DrawCustomers(); break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawWorkers()
        {
            if (_workerManager == null || _workerManager.Workers == null)
            {
                DrawEmptyState("No workers hired yet.\nOpen recruitment with [Tab].");
                return;
            }

            var workers = _workerManager.Workers;
            int maxSlots = _workerManager.GetMaxWorkers();

            // Header
            GUI.color = PhoneController.TextCol;
            GUILayout.Label($"WORKFORCE ({workers.Count}/{maxSlots} slots)", PhoneController.Instance.HeaderStyle);
            GUI.color = Color.white;

            GUILayout.Space(4);

            if (workers.Count == 0)
            {
                DrawEmptyState("No workers hired.\nPress [Tab] to recruit.");
                return;
            }

            foreach (var worker in workers)
            {
                if (worker == null) continue;
                DrawWorkerCard(worker);
                GUILayout.Space(4);
            }
        }

        private void DrawWorkerCard(WorkerInstance worker)
        {
            // Card background
            GUI.color = new Color(0.15f, 0.15f, 0.2f);
            Rect cardRect = GUILayoutUtility.GetRect(0, 65, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(cardRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            float x = cardRect.x + 6;
            float y = cardRect.y + 4;
            float w = cardRect.width - 12;

            // Role icon + Name
            Color roleColor = GetRoleColor(worker.role);
            GUI.color = roleColor;

            GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
            nameStyle.fontSize = 12;
            nameStyle.fontStyle = FontStyle.Bold;
            nameStyle.normal.textColor = roleColor;

            GUI.Label(new Rect(x, y, w * 0.6f, 16), $"{worker.role}: {worker.workerName}", nameStyle);

            // State badge
            GUIStyle stateStyle = new GUIStyle(GUI.skin.label);
            stateStyle.fontSize = 10;
            stateStyle.alignment = TextAnchor.MiddleRight;
            string stateStr = worker.state.ToString();
            stateStyle.normal.textColor = GetStateColor(stateStr);
            GUI.Label(new Rect(x + w * 0.5f, y, w * 0.5f, 16), stateStr, stateStyle);

            GUI.color = Color.white;

            // Property assignment
            GUIStyle detailStyle = new GUIStyle(GUI.skin.label);
            detailStyle.fontSize = 10;
            detailStyle.normal.textColor = PhoneController.DimText;

            string propName = worker.assignedProperty != null && worker.assignedProperty.Definition != null
                ? worker.assignedProperty.Definition.propertyName
                : "Unassigned";
            GUI.Label(new Rect(x, y + 18, w, 14), $"@ {propName}", detailStyle);

            // Loyalty bar
            float barY = y + 34;
            float barW = w * 0.6f;
            float barH = 6f;
            GUI.Label(new Rect(x, barY - 2, 50, 14), "Loyalty:", detailStyle);

            // Background
            GUI.color = new Color(0.2f, 0.2f, 0.25f);
            GUI.DrawTexture(new Rect(x + 50, barY, barW, barH), Texture2D.whiteTexture);
            // Fill
            float loyaltyVal = worker.loyalty;
            Color loyaltyColor = Color.Lerp(PhoneController.AccentRed, PhoneController.AccentGreen, loyaltyVal);
            GUI.color = loyaltyColor;
            GUI.DrawTexture(new Rect(x + 50, barY, barW * loyaltyVal, barH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Stats (skill + daily wage)
            GUIStyle statStyle = new GUIStyle(GUI.skin.label);
            statStyle.fontSize = 10;
            statStyle.normal.textColor = PhoneController.TextCol;
            statStyle.alignment = TextAnchor.MiddleRight;

            float dailyWage = worker.definition != null
                ? worker.definition.dailyWage * worker.definition.WageDemandMultiplier : 0;
            GUI.Label(new Rect(x + w * 0.5f, barY - 2, w * 0.5f, 14),
                $"Skill: {worker.skill * 100f:F0}%  Wage: ${dailyWage:F0}/day", statStyle);
        }

        private void DrawSuppliers()
        {
            GUI.color = PhoneController.TextCol;
            GUILayout.Label("SUPPLIERS", PhoneController.Instance.HeaderStyle);
            GUI.color = Color.white;

            GUILayout.Space(4);

            // Find supplier NPCs in scene
            var suppliers = FindObjectsByType<Clout.World.NPCs.CustomerAI>();

            if (suppliers == null || suppliers.Length == 0)
            {
                DrawEmptyState("No supplier contacts yet.\nVisit shops to establish connections.");
                return;
            }

            // Show first few as supplier contacts (simplified)
            int count = 0;
            foreach (var npc in suppliers)
            {
                if (npc == null || count >= 5) break;
                DrawContactEntry(npc.customerName ?? $"Contact_{count}", "Supplier",
                    $"Product: {npc.preferredProduct}", PhoneController.AccentGold);
                count++;
            }
        }

        private void DrawCustomers()
        {
            GUI.color = PhoneController.TextCol;
            GUILayout.Label("KEY CUSTOMERS", PhoneController.Instance.HeaderStyle);
            GUI.color = Color.white;

            GUILayout.Space(4);

            var dm = DistrictManager.Instance;
            if (dm == null || dm.ActiveCustomers == null || dm.ActiveCustomers.Count == 0)
            {
                DrawEmptyState("No regular customers yet.\nStart dealing to build a client base.");
                return;
            }

            foreach (var customer in dm.ActiveCustomers)
            {
                if (customer == null) continue;
                string loyaltyStr = customer.loyalty > 0.7f ? "Loyal" :
                    customer.loyalty > 0.3f ? "Regular" : "New";
                string addictionStr = customer.addictionLevel > 0.5f ? " [Addicted]" : "";

                DrawContactEntry(
                    customer.customerName ?? "Unknown",
                    loyaltyStr + addictionStr,
                    $"Wants: {customer.preferredProduct}  Max: ${customer.maxWillingToPay:F0}",
                    PhoneController.AccentGreen);
            }
        }

        // ─── UI Helpers ─────────────────────────────────────────

        private void DrawContactEntry(string name, string subtitle, string detail, Color accentColor)
        {
            GUI.color = new Color(0.13f, 0.13f, 0.18f);
            Rect r = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Accent bar
            GUI.color = accentColor;
            GUI.DrawTexture(new Rect(r.x, r.y, 3, r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle ns = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            ns.normal.textColor = PhoneController.TextCol;
            GUI.Label(new Rect(r.x + 8, r.y + 2, r.width - 16, 16), name, ns);

            GUIStyle ss = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            ss.normal.textColor = accentColor;
            GUI.Label(new Rect(r.x + 8, r.y + 16, r.width * 0.4f, 14), subtitle, ss);

            GUIStyle ds = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleRight };
            ds.normal.textColor = PhoneController.DimText;
            GUI.Label(new Rect(r.x + r.width * 0.4f, r.y + 16, r.width * 0.55f, 14), detail, ds);

            GUILayout.Space(2);
        }

        private bool DrawSectionButton(string label, bool isActive)
        {
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 11;
            btnStyle.fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal;

            GUI.color = isActive ? PhoneController.AccentGold : PhoneController.DimText;
            bool clicked = GUILayout.Button(label, btnStyle, GUILayout.Height(24));
            GUI.color = Color.white;
            return clicked;
        }

        private void DrawEmptyState(string message)
        {
            GUI.color = PhoneController.DimText;
            GUILayout.Space(30);
            GUILayout.Label(message, PhoneController.Instance.BodyStyle);
            GUI.color = Color.white;
        }

        private Color GetRoleColor(EmployeeRole role)
        {
            return role switch
            {
                EmployeeRole.Dealer => PhoneController.AccentGreen,
                EmployeeRole.Cook => new Color(0.6f, 0.4f, 0.9f),
                EmployeeRole.Guard => new Color(0.9f, 0.5f, 0.2f),
                EmployeeRole.Grower => new Color(0.3f, 0.8f, 0.3f),
                _ => PhoneController.TextCol
            };
        }

        private Color GetStateColor(string state)
        {
            if (string.IsNullOrEmpty(state)) return PhoneController.DimText;
            string lower = state.ToLower();
            if (lower.Contains("dealing") || lower.Contains("cooking") || lower.Contains("patrol"))
                return PhoneController.AccentGreen;
            if (lower.Contains("rest") || lower.Contains("idle"))
                return PhoneController.DimText;
            if (lower.Contains("flee") || lower.Contains("arrested"))
                return PhoneController.AccentRed;
            return PhoneController.TextCol;
        }
    }
}
