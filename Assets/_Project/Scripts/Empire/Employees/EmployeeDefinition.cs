using UnityEngine;
using Clout.Core;

namespace Clout.Empire.Employees
{
    /// <summary>
    /// Hireable NPC employee — works in your properties.
    /// Employees have skills, loyalty, and risk of betrayal.
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Empire/Employee Template")]
    public class EmployeeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string employeeName;
        [TextArea(2, 3)]
        public string backstory;
        public Sprite portrait;

        [Header("Role")]
        public EmployeeRole role;

        [Header("Stats")]
        [Range(0f, 1f)] public float skill = 0.5f;
        [Range(0f, 1f)] public float loyalty = 0.5f;
        [Range(0f, 1f)] public float discretion = 0.5f;    // Won't talk to police
        [Range(0f, 1f)] public float ambition = 0.3f;       // May try to steal

        [Header("Economics")]
        public float dailyWage = 100f;
        public float hiringCost = 500f;

        [Header("Risk")]
        [Range(0f, 1f)] public float betrayalChance = 0.01f;   // Daily chance
        [Range(0f, 1f)] public float arrestChance = 0.02f;     // Daily chance
        public bool hasRecord = false;                          // Known to police
    }
}
