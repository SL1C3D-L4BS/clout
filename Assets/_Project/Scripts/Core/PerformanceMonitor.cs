using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Clout.Empire.Employees;
using Clout.Empire.Properties;
using Clout.World.Police;
using Clout.World.Districts;

namespace Clout.Core
{
    /// <summary>
    /// Step 10 — Runtime performance monitor.
    /// Tracks FPS, memory, NavMesh agent count, active GameObjects, and system budgets.
    /// Toggle with F3.
    ///
    /// Provides:
    /// - Real-time FPS counter with 1% low tracking
    /// - Memory allocation monitoring (managed heap)
    /// - NavMeshAgent count and active AI budget
    /// - Per-system object counts (properties, workers, police, NPCs, buildings)
    /// - Frame time budget breakdown
    /// - Performance warnings when budgets exceeded
    ///
    /// This data drives optimization decisions for Phase 3+.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        // ─── Singleton ──────────────────────────────────────────────

        public static PerformanceMonitor Instance { get; private set; }

        // ─── Config ─────────────────────────────────────────────────

        [Header("Budget Limits")]
        [Tooltip("Maximum NavMeshAgents before warning")]
        public int maxNavMeshAgentBudget = 50;

        [Tooltip("Maximum active GameObjects before warning")]
        public int maxGameObjectBudget = 2000;

        [Tooltip("Target FPS")]
        public int targetFPS = 60;

        [Tooltip("Minimum acceptable FPS before warning")]
        public int minAcceptableFPS = 30;

        // ─── State ──────────────────────────────────────────────────

        private bool _visible;
        private float _fpsUpdateInterval = 0.5f;
        private float _fpsTimer;
        private int _frameCount;

        // FPS tracking
        private float _currentFPS;
        private float _avgFPS;
        private float _onePercentLow = 999f;
        private Queue<float> _fpsHistory = new Queue<float>();
        private const int FPS_HISTORY_SIZE = 120; // 60 seconds at 0.5s intervals

        // Memory
        private long _managedMemoryMB;
        private long _totalAllocatedMB;

        // Object counts
        private int _navMeshAgentCount;
        private int _totalGameObjects;
        private int _activeMonoBehaviours;

        // System-specific counts
        private int _workerCount;
        private int _policeCount;
        private int _customerCount;
        // _civilianCount removed — DistrictManager doesn't expose civilian count separately
        private int _propertyCount;
        private int _buildingCount;

        // Frame time
        private float _frameTimeMs;
        private float _worstFrameTimeMs;
        private float _worstFrameResetTimer;

        // Warnings
        private List<string> _warnings = new List<string>();
        private float _warningUpdateTimer;

        // ─── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Toggle visibility with F3
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
                _visible = !_visible;

            // Always track FPS even when not visible
            _frameCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            _frameTimeMs = Time.unscaledDeltaTime * 1000f;

            if (_frameTimeMs > _worstFrameTimeMs)
                _worstFrameTimeMs = _frameTimeMs;

            _worstFrameResetTimer += Time.unscaledDeltaTime;
            if (_worstFrameResetTimer > 5f)
            {
                _worstFrameTimeMs = _frameTimeMs;
                _worstFrameResetTimer = 0f;
            }

            if (_fpsTimer >= _fpsUpdateInterval)
            {
                _currentFPS = _frameCount / _fpsTimer;
                _frameCount = 0;
                _fpsTimer = 0f;

                // Track history
                _fpsHistory.Enqueue(_currentFPS);
                while (_fpsHistory.Count > FPS_HISTORY_SIZE)
                    _fpsHistory.Dequeue();

                // Calculate avg and 1% low
                CalculateFPSStats();
            }

            // Update system counts periodically (every 2 seconds to avoid perf overhead)
            _warningUpdateTimer += Time.unscaledDeltaTime;
            if (_visible && _warningUpdateTimer > 2f)
            {
                UpdateSystemCounts();
                UpdateWarnings();
                _warningUpdateTimer = 0f;
            }
        }

        // ─── Calculations ───────────────────────────────────────────

        private void CalculateFPSStats()
        {
            if (_fpsHistory.Count == 0) return;

            float sum = 0f;

            // Sort for 1% low
            List<float> sorted = new List<float>(_fpsHistory);
            sorted.Sort();

            sum = 0f;
            foreach (float f in sorted) sum += f;
            _avgFPS = sum / sorted.Count;

            // 1% low = average of bottom 1% of samples
            int onePercentCount = Mathf.Max(1, sorted.Count / 100);
            float lowSum = 0f;
            for (int i = 0; i < onePercentCount; i++)
                lowSum += sorted[i];
            _onePercentLow = lowSum / onePercentCount;
        }

        private void UpdateSystemCounts()
        {
            // NavMeshAgents
            _navMeshAgentCount = FindObjectsByType<NavMeshAgent>().Length;

            // Total objects (approximation — count root objects * avg hierarchy depth)
            _totalGameObjects = FindObjectsByType<Transform>().Length;

            // MonoBehaviours
            _activeMonoBehaviours = FindObjectsByType<MonoBehaviour>().Length;

            // System-specific: use cached managers when available
            var workerMgr = WorkerManager.Instance;
            _workerCount = workerMgr != null ? workerMgr.WorkerCount : 0;

            var heatResp = HeatResponseManager.Instance;
            _policeCount = heatResp != null ? heatResp.OfficerCount : 0;

            var distMgr = DistrictManager.Instance;
            _customerCount = distMgr != null ? distMgr.ActiveCustomers.Count : 0;

            var propMgr = PropertyManager.Instance;
            _propertyCount = propMgr != null ? propMgr.PropertyCount : 0;

            // Memory
            _managedMemoryMB = System.GC.GetTotalMemory(false) / (1024 * 1024);
            _totalAllocatedMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        }

        private void UpdateWarnings()
        {
            _warnings.Clear();

            if (_currentFPS < minAcceptableFPS)
                _warnings.Add($"LOW FPS: {_currentFPS:F0} (target: {targetFPS})");

            if (_navMeshAgentCount > maxNavMeshAgentBudget)
                _warnings.Add($"NAVMESH AGENTS: {_navMeshAgentCount}/{maxNavMeshAgentBudget}");

            if (_totalGameObjects > maxGameObjectBudget)
                _warnings.Add($"GAMEOBJECTS: {_totalGameObjects}/{maxGameObjectBudget}");

            if (_managedMemoryMB > 512)
                _warnings.Add($"HIGH MEMORY: {_managedMemoryMB}MB managed heap");

            if (_worstFrameTimeMs > 50f)
                _warnings.Add($"FRAME SPIKE: {_worstFrameTimeMs:F1}ms (>50ms)");
        }

        // ─── Public API ─────────────────────────────────────────────

        /// <summary>Get current FPS.</summary>
        public float GetCurrentFPS() => _currentFPS;

        /// <summary>Get average FPS over history window.</summary>
        public float GetAverageFPS() => _avgFPS;

        /// <summary>Get 1% low FPS.</summary>
        public float GetOnePercentLow() => _onePercentLow;

        /// <summary>Check if any performance budgets are exceeded.</summary>
        public bool HasWarnings() => _warnings.Count > 0;

        /// <summary>Get all active performance warnings.</summary>
        public IReadOnlyList<string> GetWarnings() => _warnings;

        // ─── OnGUI Overlay ──────────────────────────────────────────

        private void OnGUI()
        {
            if (!_visible) return;

            float x = 10f;
            float y = Screen.height - 340f;
            float w = 280f;
            float lineH = 18f;

            // Background panel
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.Box(new Rect(x - 5, y - 5, w + 10, 335), "");

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            GUIStyle valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
            GUIStyle warnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            // ── FPS ─────────────────────────────────────────────
            GUI.color = GetFPSColor(_currentFPS);
            GUI.Label(new Rect(x, y, w, lineH), $"FPS: {_currentFPS:F0}", headerStyle);
            y += lineH;

            GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            GUI.Label(new Rect(x, y, w, lineH), $"  Avg: {_avgFPS:F0}  |  1% Low: {_onePercentLow:F0}", valueStyle);
            y += lineH;

            GUI.Label(new Rect(x, y, w, lineH), $"  Frame: {_frameTimeMs:F1}ms  |  Worst: {_worstFrameTimeMs:F1}ms", valueStyle);
            y += lineH + 4;

            // ── Memory ──────────────────────────────────────────
            GUI.color = new Color(0.6f, 0.8f, 1f, 1f);
            GUI.Label(new Rect(x, y, w, lineH), "MEMORY", headerStyle);
            y += lineH;

            GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            GUI.Label(new Rect(x, y, w, lineH), $"  Managed: {_managedMemoryMB}MB  |  Total: {_totalAllocatedMB}MB", valueStyle);
            y += lineH + 4;

            // ── Objects ─────────────────────────────────────────
            GUI.color = new Color(0.6f, 1f, 0.6f, 1f);
            GUI.Label(new Rect(x, y, w, lineH), "SCENE OBJECTS", headerStyle);
            y += lineH;

            GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            GUI.Label(new Rect(x, y, w, lineH), $"  GameObjects: {_totalGameObjects}", valueStyle);
            y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), $"  MonoBehaviours: {_activeMonoBehaviours}", valueStyle);
            y += lineH;

            Color agentColor = _navMeshAgentCount > maxNavMeshAgentBudget
                ? new Color(1f, 0.3f, 0.2f) : new Color(0.8f, 0.8f, 0.8f);
            GUI.color = agentColor;
            GUI.Label(new Rect(x, y, w, lineH), $"  NavMeshAgents: {_navMeshAgentCount}/{maxNavMeshAgentBudget}", valueStyle);
            y += lineH + 4;

            // ── Game Systems ────────────────────────────────────
            GUI.color = new Color(1f, 0.85f, 0.4f, 1f);
            GUI.Label(new Rect(x, y, w, lineH), "GAME SYSTEMS", headerStyle);
            y += lineH;

            GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            GUI.Label(new Rect(x, y, w, lineH), $"  Workers: {_workerCount}  |  Police: {_policeCount}", valueStyle);
            y += lineH;
            GUI.Label(new Rect(x, y, w, lineH), $"  Customers: {_customerCount}  |  Properties: {_propertyCount}", valueStyle);
            y += lineH + 4;

            // ── Warnings ────────────────────────────────────────
            if (_warnings.Count > 0)
            {
                GUI.color = new Color(1f, 0.2f, 0.1f, 1f);
                GUI.Label(new Rect(x, y, w, lineH), "WARNINGS", headerStyle);
                y += lineH;

                foreach (string warn in _warnings)
                {
                    GUI.color = new Color(1f, 0.4f, 0.3f, 1f);
                    GUI.Label(new Rect(x + 4, y, w - 4, lineH), $"  ! {warn}", warnStyle);
                    y += lineH;
                }
            }
            else
            {
                GUI.color = new Color(0.3f, 0.9f, 0.3f, 1f);
                GUI.Label(new Rect(x, y, w, lineH), "ALL SYSTEMS NOMINAL", headerStyle);
            }

            GUI.color = Color.white;

            // Toggle hint at bottom
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft
            };
            GUI.Label(new Rect(x, Screen.height - 15, w, 15), "[F3] Toggle Performance Monitor", hintStyle);
            GUI.color = Color.white;
        }

        private Color GetFPSColor(float fps)
        {
            if (fps >= targetFPS) return new Color(0.3f, 1f, 0.3f);
            if (fps >= minAcceptableFPS) return new Color(1f, 0.9f, 0.3f);
            return new Color(1f, 0.2f, 0.1f);
        }
    }
}
