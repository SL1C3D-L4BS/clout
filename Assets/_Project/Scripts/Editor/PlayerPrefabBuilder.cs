#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using FishNet.Object;
using FishNet.Component.Transforming;
using Clout.Core;
using Clout.Stats;
using Clout.Combat;
using Clout.Camera;
using Clout.Player;
using Clout.Network;
using Clout.Inventory;
using Clout.Empire.Reputation;
using Clout.World.Police;

namespace Clout.Editor
{
    /// <summary>
    /// Builds the Player prefab with all components wired and saves it to:
    ///   Assets/_Project/Prefabs/Player/Player.prefab
    ///
    /// Prerequisites: Bootstrap scene must exist (for NetworkManager reference).
    ///
    /// Menu: Clout > Build Player Prefab
    /// </summary>
    public static class PlayerPrefabBuilder
    {
        private const string PREFAB_DIR  = "Assets/_Project/Prefabs/Player";
        private const string PREFAB_PATH = "Assets/_Project/Prefabs/Player/Player.prefab";

        [MenuItem("Clout/Build Player Prefab", false, 60)]
        public static void BuildPlayerPrefab()
        {
            if (!EditorUtility.DisplayDialog(
                "Clout — Build Player Prefab",
                "Creates a fully wired Player.prefab with:\n" +
                "• FishNet NetworkObject + NetworkTransform\n" +
                "• PlayerStateManager + all combat components\n" +
                "• CameraManager (4-mode Cinemachine)\n" +
                "• ReputationManager, WantedSystem, InventoryManager\n" +
                "• RuntimeStats, AmmoCacheManager\n\n" +
                "Saved to Assets/_Project/Prefabs/Player/Player.prefab\n\nProceed?",
                "Build Prefab", "Cancel"))
                return;

            BuildPlayerPrefabHeadless();
        }

        /// <summary>No-dialog entry point — safe to call from auto-triggers.</summary>
        public static void BuildPlayerPrefabHeadless()
        {
            System.IO.Directory.CreateDirectory(PREFAB_DIR);

            GameObject root = BuildPlayerHierarchy();

            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out success);
            Object.DestroyImmediate(root);

            if (success)
            {
                AssetDatabase.Refresh();
                Debug.Log("[Clout] Player prefab saved: " + PREFAB_PATH);
            }
            else
            {
                Debug.LogError("[Clout] Failed to save Player prefab!");
            }
        }

        // ──────────────────────────────────────────────────────────
        //  HIERARCHY
        // ──────────────────────────────────────────────────────────

        public static GameObject BuildPlayerHierarchy()
        {
            // ── Root ──
            GameObject root = new GameObject("Player");
            root.tag = "Player";
            root.layer = LayerMask.NameToLayer("Default");

            // FishNet: NetworkObject must be on root
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            root.AddComponent<NetworkAnimatorSync>();

            // Physics
            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.radius = 0.35f;
            col.height = 2f;

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 70f;
            rb.linearDamping = 4f;
            rb.angularDamping = 999f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // NavMeshAgent — disabled; player uses Rigidbody
            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.enabled = false;

            // Animator
            Animator anim = root.AddComponent<Animator>();

            // ── Visual Model ──
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "Model";
            model.transform.SetParent(root.transform);
            model.transform.localPosition = new Vector3(0f, 1f, 0f);
            Object.DestroyImmediate(model.GetComponent<Collider>());
            Renderer r = model.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.6f, 1f);
            r.sharedMaterial = mat;

            // ── Camera Follow Target ──
            GameObject camTarget = new GameObject("CameraFollowTarget");
            camTarget.transform.SetParent(root.transform);
            camTarget.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            // ── Lock-on Reference ──
            GameObject lockOnRef = new GameObject("LockOnRef");
            lockOnRef.transform.SetParent(root.transform);
            lockOnRef.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            // ── Weapon Hooks ──
            WeaponHolderManager weaponHolder = root.AddComponent<WeaponHolderManager>();

            GameObject rightHookGO = new GameObject("RightWeaponHook");
            rightHookGO.transform.SetParent(root.transform);
            rightHookGO.transform.localPosition = new Vector3(0.3f, 0.9f, 0.2f);
            WeaponHolderHook rightHook = rightHookGO.AddComponent<WeaponHolderHook>();
            rightHook.isLeftHook = false;

            GameObject leftHookGO = new GameObject("LeftWeaponHook");
            leftHookGO.transform.SetParent(root.transform);
            leftHookGO.transform.localPosition = new Vector3(-0.3f, 0.9f, 0.2f);
            WeaponHolderHook leftHook = leftHookGO.AddComponent<WeaponHolderHook>();
            leftHook.isLeftHook = true;

            // ── Empire Components ──
            RuntimeStats stats = root.AddComponent<RuntimeStats>();
            stats.maxHealth = 100;
            stats.maxStamina = 100;
            stats.maxPoise = 100;
            stats.poise = 100;

            AmmoCacheManager ammoCache = root.AddComponent<AmmoCacheManager>();
            ReputationManager repManager = root.AddComponent<ReputationManager>();
            WantedSystem wantedSystem = root.AddComponent<WantedSystem>();
            InventoryManager inventory = root.AddComponent<InventoryManager>();

            // ── Input ──
            root.AddComponent<PlayerInput>();
            PlayerInputHandler inputHandler = root.AddComponent<PlayerInputHandler>();

            // ── Camera Manager ──
            GameObject camManagerGO = new GameObject("CameraManager");
            camManagerGO.transform.SetParent(root.transform);
            CameraManager cameraManager = camManagerGO.AddComponent<CameraManager>();

            // ── Network Damage Handler ──
            root.AddComponent<NetworkDamageHandler>();

            // ── Player State Manager ──
            PlayerStateManager psm = root.AddComponent<PlayerStateManager>();
            psm.model          = model.transform;
            psm.lockOnTarget   = lockOnRef.transform;
            psm.cameraManager  = cameraManager;
            psm.inputHandler   = inputHandler;
            psm.inventoryManager  = inventory;
            psm.reputationManager = repManager;
            psm.wantedSystem   = wantedSystem;
            psm.rigid          = rb;
            psm.agent          = agent;
            psm.anim           = anim;
            psm.runtimeStats   = stats;
            psm.weaponHolderManager = weaponHolder;
            psm.ammoCacheManager    = ammoCache;

            return root;
        }
    }
}
#endif
