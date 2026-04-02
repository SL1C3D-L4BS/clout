#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using Unity.Cinemachine;
using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet.Managing.Client;
using FishNet.Managing.Timing;
using FishNet.Managing.Transporting;
using FishNet.Managing.Scened;
using FishNet.Managing.Observing;
using FishNet.Transporting.Tugboat;
using Clout.Core;
using Clout.Network;
using System.Collections.Generic;

namespace Clout.Editor
{
    /// <summary>
    /// Builds the full Clout scene hierarchy:
    ///
    ///   Bootstrap.unity  — persistent scene, loads first, never unloaded.
    ///     [Clout] NetworkManager   — FishNet NetworkManager + Tugboat
    ///     [Clout] NetworkBootstrapper
    ///
    ///   Main.unity       — base game scene, loaded additively after Bootstrap.
    ///     Main Camera + CinemachineBrain
    ///     Directional Light
    ///     EventSystem (New Input System)
    ///     SpawnPoints parent
    ///
    /// Menu: Clout > Build Bootstrap Scenes
    /// </summary>
    public static class SceneBootstrapBuilder
    {
        private const string BOOTSTRAP_PATH = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        private const string MAIN_PATH       = "Assets/_Project/Scenes/Main/Main.unity";

        [MenuItem("Clout/Build Bootstrap Scenes", false, 50)]
        public static void BuildBootstrapScenes()
        {
            if (!EditorUtility.DisplayDialog(
                "Clout — Build Bootstrap Scenes",
                "This will create Bootstrap.unity and Main.unity with all core systems wired.\n\n" +
                "• Bootstrap: FishNet NetworkManager, NetworkBootstrapper\n" +
                "• Main: Camera, Lighting, EventSystem, SpawnPoints\n\n" +
                "Both scenes are added to Build Settings.\n\nProceed?",
                "Build Scenes", "Cancel"))
                return;

            BuildBootstrapScenesHeadless();

            EditorUtility.DisplayDialog("Clout",
                "Bootstrap scenes created!\n\n" +
                "Bootstrap.unity → persistent network layer\n" +
                "Main.unity      → base game scene\n\n" +
                "Build Settings updated. Open Bootstrap.unity and hit Play.",
                "Let's Go");
        }

        /// <summary>No-dialog entry point — safe to call from auto-triggers.</summary>
        public static void BuildBootstrapScenesHeadless()
        {
            BuildBootstrapScene();
            BuildMainScene();
            AddScenesToBuildSettings();
        }

        // ──────────────────────────────────────────────────────────
        //  BOOTSTRAP SCENE
        // ──────────────────────────────────────────────────────────

        private static void BuildBootstrapScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── FishNet NetworkManager ──
            GameObject nmGO = new GameObject("[FishNet] NetworkManager");
            NetworkManager nm = nmGO.AddComponent<NetworkManager>();

            // FishNet auto-creates managers in Awake via GetOrCreateComponent.
            // We add them now so they're visible and configurable in Inspector.
            nmGO.AddComponent<ServerManager>();
            nmGO.AddComponent<ClientManager>();
            nmGO.AddComponent<TimeManager>();
            TransportManager tm = nmGO.AddComponent<TransportManager>();
            nmGO.AddComponent<FishNet.Managing.Scened.SceneManager>();
            nmGO.AddComponent<ObserverManager>();

            // Tugboat transport (UDP, LAN/WAN)
            Tugboat tug = nmGO.AddComponent<Tugboat>();
            tug.SetPort(7770);
            tug.SetClientAddress("localhost");

            // ── Clout NetworkBootstrapper ──
            GameObject nbGO = new GameObject("[Clout] NetworkBootstrapper");
            NetworkBootstrapper nb = nbGO.AddComponent<NetworkBootstrapper>();
            nb.autoStartAsHost = true;
            nb.serverPort = 7770;
            nb.serverAddress = "localhost";

            // ── Persistent scene loader ──
            GameObject loaderGO = new GameObject("[Clout] SceneLoader");
            loaderGO.AddComponent<BootstrapSceneLoader>();

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(BOOTSTRAP_PATH));
            EditorSceneManager.SaveScene(scene, BOOTSTRAP_PATH);
            Debug.Log("[Clout] Bootstrap scene saved: " + BOOTSTRAP_PATH);
        }

        // ──────────────────────────────────────────────────────────
        //  MAIN SCENE
        // ──────────────────────────────────────────────────────────

        private static void BuildMainScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Lighting ──
            GameObject light = new GameObject("Directional Light");
            Light dirLight = light.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1f, 0.96f, 0.84f);
            dirLight.intensity = 1.2f;
            dirLight.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // ── Main Camera ──
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0f, 5f, -8f);
            camGO.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            UnityEngine.Camera cam = camGO.AddComponent<UnityEngine.Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<CinemachineBrain>();

            // ── EventSystem (New Input System) ──
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();

            // ── Spawn Points ──
            GameObject spawnRoot = new GameObject("SpawnPoints");
            var spawnMarkers = new List<Transform>();
            spawnMarkers.Add(CreateSpawnPoint(spawnRoot.transform, "PlayerSpawn_01", new Vector3(0f, 0f, 0f)));
            spawnMarkers.Add(CreateSpawnPoint(spawnRoot.transform, "PlayerSpawn_02", new Vector3(5f, 0f, 0f)));
            spawnMarkers.Add(CreateSpawnPoint(spawnRoot.transform, "PlayerSpawn_03", new Vector3(-5f, 0f, 0f)));
            spawnMarkers.Add(CreateSpawnPoint(spawnRoot.transform, "PlayerSpawn_04", new Vector3(0f, 0f, 5f)));

            // ── Player Spawn Manager ──
            GameObject psmGO = new GameObject("[Clout] PlayerSpawnManager");
            PlayerSpawnManager psm = psmGO.AddComponent<PlayerSpawnManager>();
            psm.spawnPoints = spawnMarkers.ToArray();

            // ── Environment placeholder ──
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            ground.isStatic = true;

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(MAIN_PATH));
            EditorSceneManager.SaveScene(scene, MAIN_PATH);
            Debug.Log("[Clout] Main scene saved: " + MAIN_PATH);
        }

        private static Transform CreateSpawnPoint(Transform parent, string spawnName, Vector3 pos)
        {
            GameObject sp = new GameObject(spawnName);
            sp.transform.SetParent(parent);
            sp.transform.position = pos;
            sp.AddComponent<SpawnPointMarker>();
            return sp.transform;
        }

        // ──────────────────────────────────────────────────────────
        //  BUILD SETTINGS
        // ──────────────────────────────────────────────────────────

        private static void AddScenesToBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(BOOTSTRAP_PATH, true),
                new EditorBuildSettingsScene(MAIN_PATH, true),
            };

            // Preserve existing scenes (TestArena etc.) after the two core scenes
            var existing = EditorBuildSettings.scenes;
            var all = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
            foreach (var s in existing)
            {
                if (s.path != BOOTSTRAP_PATH && s.path != MAIN_PATH)
                    all.Add(s);
            }

            EditorBuildSettings.scenes = all.ToArray();
            Debug.Log("[Clout] Build Settings updated: Bootstrap(0), Main(1).");
        }
    }
}
#endif
