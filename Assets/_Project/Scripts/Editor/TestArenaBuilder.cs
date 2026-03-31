#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using Clout.Core;
using Clout.Combat;
using Clout.Stats;
using Clout.Player;
using Clout.AI;
using Clout.Camera;
using Clout.Empire.Reputation;
using Clout.World.Police;
using Clout.Inventory;
using Clout.Empire.Economy;
using Clout.Empire.Properties;
using Clout.UI;
using UnityEditor.Animations;

namespace Clout.Editor
{
    /// <summary>
    /// Phase 1 Test Arena Builder — creates a fully wired test scene from the editor menu.
    ///
    /// Menu: Clout > Build Test Arena
    ///
    /// Creates:
    /// - Ground plane with NavMesh bake
    /// - Player with all combat, camera, network, empire components
    /// - 3 enemy types: melee thug, ranged shooter, hybrid enforcer
    /// - Main camera with CinemachineBrain
    /// - Directional light + ambient
    /// - EventSystem for New Input System
    /// - Combat HUD (health, stamina, CLOUT, ammo, wanted level)
    /// - Cover objects and obstacles for ranged AI testing
    /// </summary>
    public static class TestArenaBuilder
    {
        private const string SCENE_PATH = "Assets/_Project/Scenes/TestArena.unity";

        [MenuItem("Clout/Build Test Arena", false, 100)]
        public static void BuildTestArena()
        {
            // Ensure URP pipeline is configured before building anything
            URPSetup.EnsureURPConfigured();

            // Auto-create animator controller if it doesn't exist
            string acPath = "Assets/_Project/Animations/Controllers/AC_Character.controller";
            if (!System.IO.File.Exists(acPath))
            {
                if (EditorUtility.DisplayDialog("Clout — Animator Setup Required",
                    "No Animator Controller found. Create one from Sharp Accent placeholder animations first?",
                    "Create & Continue", "Cancel"))
                {
                    AnimatorSetup.CreateAnimatorController();
                }
                else return;
            }

            // Auto-create starter weapons if they don't exist
            string fistsSO = "Assets/_Project/ScriptableObjects/Weapons/WPN_Fists.asset";
            if (!System.IO.File.Exists(fistsSO))
            {
                WeaponAssetFactory.CreateStarterWeapons();
            }

            if (!EditorUtility.DisplayDialog(
                "Clout — Build Test Arena",
                "This will create a new test scene with player, 3 enemies, NavMesh, camera, and HUD.\n\nProceed?",
                "Build It", "Cancel"))
                return;

            // Create new scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // === ENVIRONMENT ===
            BuildEnvironment();

            // === CAMERA ===
            GameObject mainCam = BuildMainCamera();

            // === INPUT SYSTEM ===
            BuildEventSystem();

            // === PLAYER ===
            GameObject player = BuildPlayer(mainCam);

            // === ENEMIES ===
            BuildMeleeEnemy(new Vector3(10f, 0f, 10f));
            BuildRangedEnemy(new Vector3(-12f, 0f, 15f));
            BuildHybridEnemy(new Vector3(0f, 0f, 20f));

            // === HUD ===
            BuildHUD(player);

            // === ECONOMY ===
            BuildEconomy();

            // === SHOPS ===
            EconomySystemFactory.CreateEconomySystemHeadless();

            // === PROPERTIES ===
            PropertySystemFactory.CreatePropertySystemHeadless();
            PropertySystemFactory.SpawnPropertiesHeadless();

            // === NAVMESH ===
            BakeNavMesh();

            // Save scene
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SCENE_PATH));
            EditorSceneManager.SaveScene(scene, SCENE_PATH);

            Debug.Log("[Clout] Test Arena built successfully! Open: " + SCENE_PATH);
            EditorUtility.DisplayDialog("Clout", "Test Arena built!\n\n" +
                "• 1 Player (all systems wired)\n" +
                "• 3 Enemies (melee, ranged, hybrid)\n" +
                "• NavMesh baked\n" +
                "• Combat HUD active\n" +
                "• Economy system (CashManager, Ledger, Market)\n" +
                "• 3 Shop NPCs (Chemo's Supply, Pawn It, Big Tony's)\n" +
                "• 8 Property buildings (surrounding arena)\n\n" +
                "Hit Play to test.", "Let's Go");
        }

        // ─────────────────────────────────────────────────────────
        //  HEADLESS BUILD — no dialogs, safe for trigger scripts
        // ─────────────────────────────────────────────────────────

        public static void BuildTestArenaHeadless()
        {
            // Ensure URP pipeline is configured before building anything
            URPSetup.EnsureURPConfigured();

            string acPath = "Assets/_Project/Animations/Controllers/AC_Character.controller";
            if (!System.IO.File.Exists(acPath))
                AnimatorSetup.CreateAnimatorControllerHeadless();

            string fistsSO = "Assets/_Project/ScriptableObjects/Weapons/WPN_Fists.asset";
            if (!System.IO.File.Exists(fistsSO))
                WeaponAssetFactory.CreateStarterWeaponsHeadless();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildEnvironment();
            GameObject mainCam = BuildMainCamera();
            BuildEventSystem();
            GameObject player = BuildPlayer(mainCam);
            BuildMeleeEnemy(new Vector3(10f, 0f, 10f));
            BuildRangedEnemy(new Vector3(-12f, 0f, 15f));
            BuildHybridEnemy(new Vector3(0f, 0f, 20f));
            BuildHUD(player);
            BuildEconomy();
            EconomySystemFactory.CreateEconomySystemHeadless();
            PropertySystemFactory.CreatePropertySystemHeadless();
            PropertySystemFactory.SpawnPropertiesHeadless();
            BakeNavMesh();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SCENE_PATH));
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.Refresh();
            Debug.Log("[Clout] Test Arena built (headless): " + SCENE_PATH);
        }

        // ─────────────────────────────────────────────────────────
        //  ENVIRONMENT
        // ─────────────────────────────────────────────────────────

        private static void BuildEnvironment()
        {
            // Directional Light
            GameObject light = new GameObject("Directional Light");
            Light dirLight = light.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1f, 0.96f, 0.84f);
            dirLight.intensity = 1.2f;
            dirLight.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Ground plane — large enough for combat
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(80f, 1f, 80f);
            ground.isStatic = true;

            // Ground material
            Renderer groundRenderer = ground.GetComponent<Renderer>();
            Material groundMat = EditorShaderHelper.CreateMaterial(new Color(0.35f, 0.38f, 0.34f));
            groundRenderer.sharedMaterial = groundMat;

            // NavMeshSurface for baking
            ground.AddComponent<NavMeshSurface>();

            // Cover objects — create urban obstacles
            BuildCoverObject("Cover_Dumpster", new Vector3(5f, 0.5f, 5f), new Vector3(2f, 1f, 1f));
            BuildCoverObject("Cover_CarFrame", new Vector3(-6f, 0.4f, 8f), new Vector3(3f, 0.8f, 1.5f));
            BuildCoverObject("Cover_Crate_A", new Vector3(8f, 0.4f, 15f), new Vector3(1f, 0.8f, 1f));
            BuildCoverObject("Cover_Crate_B", new Vector3(-4f, 0.4f, 18f), new Vector3(1.2f, 0.8f, 1.2f));
            BuildCoverObject("Cover_Wall_A", new Vector3(15f, 1.5f, 0f), new Vector3(0.3f, 3f, 8f));
            BuildCoverObject("Cover_Wall_B", new Vector3(-15f, 1.5f, 5f), new Vector3(0.3f, 3f, 6f));
            BuildCoverObject("Cover_Barrel_A", new Vector3(3f, 0.5f, 12f), new Vector3(0.8f, 1f, 0.8f));
            BuildCoverObject("Cover_Barrel_B", new Vector3(-8f, 0.5f, 22f), new Vector3(0.8f, 1f, 0.8f));

            // Boundary walls — keep AI from wandering off
            BuildBoundaryWall("Wall_North", new Vector3(0f, 2f, 38f), new Vector3(80f, 4f, 1f));
            BuildBoundaryWall("Wall_South", new Vector3(0f, 2f, -38f), new Vector3(80f, 4f, 1f));
            BuildBoundaryWall("Wall_East", new Vector3(38f, 2f, 0f), new Vector3(1f, 4f, 80f));
            BuildBoundaryWall("Wall_West", new Vector3(-38f, 2f, 0f), new Vector3(1f, 4f, 80f));
        }

        private static void BuildCoverObject(string name, Vector3 pos, Vector3 scale)
        {
            GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.name = name;
            cover.transform.position = pos;
            cover.transform.localScale = scale;
            cover.isStatic = true;
            cover.layer = LayerMask.NameToLayer("Default");

            Renderer r = cover.GetComponent<Renderer>();
            Material mat = EditorShaderHelper.CreateMaterial(new Color(0.45f, 0.42f, 0.38f));
            r.sharedMaterial = mat;

            // NavMesh obstacle modifier
            var modifier = cover.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = 1; // Not walkable
        }

        private static void BuildBoundaryWall(string name, Vector3 pos, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.isStatic = true;

            Renderer r = wall.GetComponent<Renderer>();
            Material mat = EditorShaderHelper.CreateMaterial(new Color(0.25f, 0.25f, 0.28f));
            r.sharedMaterial = mat;
        }

        // ─────────────────────────────────────────────────────────
        //  MAIN CAMERA
        // ─────────────────────────────────────────────────────────

        private static GameObject BuildMainCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0f, 5f, -8f);
            camObj.transform.rotation = Quaternion.Euler(20f, 0f, 0f);

            UnityEngine.Camera cam = camObj.AddComponent<UnityEngine.Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;

            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<CinemachineBrain>();

            return camObj;
        }

        // ─────────────────────────────────────────────────────────
        //  EVENT SYSTEM (New Input System)
        // ─────────────────────────────────────────────────────────

        private static void BuildEventSystem()
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        // ─────────────────────────────────────────────────────────
        //  PLAYER
        // ─────────────────────────────────────────────────────────

        private static GameObject BuildPlayer(GameObject mainCam)
        {
            // Root
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 0f, 0f);
            player.layer = LayerMask.NameToLayer("Default");

            // Character model — try boxMan first, fall back to capsule
            GameObject model = null;
            GameObject boxManPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Placeholder/Models/boxMan.fbx");
            if (boxManPrefab != null)
            {
                model = (GameObject)PrefabUtility.InstantiatePrefab(boxManPrefab);
                model.name = "Model";
                model.transform.SetParent(player.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                // Tint blue for player identification
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                Material playerMat = EditorShaderHelper.CreateMaterial(new Color(0.2f, 0.6f, 1f));
                foreach (Renderer r in renderers) r.sharedMaterial = playerMat;
            }
            else
            {
                // Fallback capsule
                model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                model.name = "Model";
                model.transform.SetParent(player.transform);
                model.transform.localPosition = new Vector3(0f, 1f, 0f);
                Object.DestroyImmediate(model.GetComponent<Collider>());

                Renderer modelRenderer = model.GetComponent<Renderer>();
                Material playerMat = EditorShaderHelper.CreateMaterial(new Color(0.2f, 0.6f, 1f));
                modelRenderer.sharedMaterial = playerMat;
            }

            // Physics collider on root
            CapsuleCollider col = player.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.radius = 0.35f;
            col.height = 2f;

            // Rigidbody
            Rigidbody rb = player.AddComponent<Rigidbody>();
            rb.mass = 70f;
            rb.linearDamping = 4f;
            rb.angularDamping = 999f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // NavMeshAgent (passive mode — only for AI path queries on player if needed)
            NavMeshAgent agent = player.AddComponent<NavMeshAgent>();
            agent.enabled = false; // Player uses Rigidbody, not NavMesh

            // Animator — use boxMan's Animator if present, else add one
            Animator anim = player.GetComponentInChildren<Animator>();
            if (anim == null) anim = player.AddComponent<Animator>();

            // Wire animator controller
            AnimatorController ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/_Project/Animations/Controllers/AC_Character.controller");
            if (ac != null)
            {
                anim.runtimeAnimatorController = ac;
                Debug.Log("[Clout] Animator Controller assigned to player.");
            }
            else
            {
                Debug.LogWarning("[Clout] No AC_Character.controller found! Run Clout > Setup > Create Animator Controller first.");
            }

            // Configure animator for humanoid
            anim.applyRootMotion = false; // We use Rigidbody for movement
            anim.updateMode = AnimatorUpdateMode.Normal;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // AnimatorHook — receives animation events (damage colliders, combos, etc.)
            AnimatorHook playerAnimHook = anim.gameObject.AddComponent<AnimatorHook>();

            // Camera follow target
            GameObject camTarget = new GameObject("CameraFollowTarget");
            camTarget.transform.SetParent(player.transform);
            camTarget.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            // Lock-on reference point
            GameObject lockOnRef = new GameObject("LockOnRef");
            lockOnRef.transform.SetParent(player.transform);
            lockOnRef.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            // Weapon holder structure
            WeaponHolderManager weaponHolder = player.AddComponent<WeaponHolderManager>();

            GameObject rightHookObj = new GameObject("RightWeaponHook");
            rightHookObj.transform.SetParent(player.transform);
            rightHookObj.transform.localPosition = new Vector3(0.3f, 0.9f, 0.2f);
            WeaponHolderHook rightHook = rightHookObj.AddComponent<WeaponHolderHook>();
            rightHook.isLeftHook = false;

            GameObject leftHookObj = new GameObject("LeftWeaponHook");
            leftHookObj.transform.SetParent(player.transform);
            leftHookObj.transform.localPosition = new Vector3(-0.3f, 0.9f, 0.2f);
            WeaponHolderHook leftHook = leftHookObj.AddComponent<WeaponHolderHook>();
            leftHook.isLeftHook = true;

            // Core components
            RuntimeStats stats = player.AddComponent<RuntimeStats>();
            stats.maxHealth = 100;
            stats.maxStamina = 100;
            stats.maxPoise = 100;
            stats.poise = 100;

            AmmoCacheManager ammoCache = player.AddComponent<AmmoCacheManager>();
            ReputationManager repManager = player.AddComponent<ReputationManager>();
            WantedSystem wantedSystem = player.AddComponent<WantedSystem>();
            InventoryManager inventory = player.AddComponent<InventoryManager>();

            // Input
            PlayerInput playerInput = player.AddComponent<PlayerInput>();
            PlayerInputHandler inputHandler = player.AddComponent<PlayerInputHandler>();

            // Camera Manager
            GameObject camManagerObj = new GameObject("CameraManager");
            camManagerObj.transform.SetParent(player.transform);
            CameraManager cameraManager = camManagerObj.AddComponent<CameraManager>();

            // Player State Manager — the god class
            PlayerStateManager psm = player.AddComponent<PlayerStateManager>();
            psm.model = model.transform;
            psm.lockOnTarget = lockOnRef.transform;
            psm.cameraTransform = mainCam.transform;
            psm.cameraManager = cameraManager;
            psm.inputHandler = inputHandler;
            psm.inventoryManager = inventory;
            psm.reputationManager = repManager;
            psm.wantedSystem = wantedSystem;

            // Wire components that Init() auto-discovers
            psm.rigid = rb;
            psm.agent = agent;
            psm.anim = anim;
            psm.runtimeStats = stats;
            psm.weaponHolderManager = weaponHolder;
            psm.ammoCacheManager = ammoCache;
            psm.animHook = playerAnimHook;

            // Init camera (builds virtual cams)
            cameraManager.Init(player.transform, camTarget.transform);

            // Starting weapons — unarmed for Phase 2 (no sword/bat clutter)
            // Weapons will be acquirable through gameplay in later phases
            psm.startingRightWeapon = null;
            psm.startingLeftWeapon = null;

            Debug.Log("[Clout] Player built: PlayerStateManager + all combat/empire systems wired.");
            if (psm.startingRightWeapon != null)
                Debug.Log($"[Clout] Starting weapon: {psm.startingRightWeapon.itemName}");
            return player;
        }

        // ─────────────────────────────────────────────────────────
        //  ENEMIES
        // ─────────────────────────────────────────────────────────

        private static GameObject BuildEnemyBase(string name, Vector3 position, Color color, float aggression)
        {
            GameObject enemy = new GameObject(name);
            enemy.transform.position = position;
            enemy.layer = LayerMask.NameToLayer("Default");

            // Character model — try boxMan first, fall back to capsule
            GameObject model = null;
            GameObject boxManPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Placeholder/Models/boxMan.fbx");
            if (boxManPrefab != null)
            {
                model = (GameObject)PrefabUtility.InstantiatePrefab(boxManPrefab);
                model.name = "Model";
                model.transform.SetParent(enemy.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                Material mat = EditorShaderHelper.CreateMaterial(color);
                foreach (Renderer rend in renderers) rend.sharedMaterial = mat;
            }
            else
            {
                model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                model.name = "Model";
                model.transform.SetParent(enemy.transform);
                model.transform.localPosition = new Vector3(0f, 1f, 0f);
                Object.DestroyImmediate(model.GetComponent<Collider>());

                Renderer r = model.GetComponent<Renderer>();
                Material mat = EditorShaderHelper.CreateMaterial(color);
                r.sharedMaterial = mat;
            }

            // Physics
            CapsuleCollider col = enemy.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.radius = 0.35f;
            col.height = 2f;

            Rigidbody rb = enemy.AddComponent<Rigidbody>();
            rb.isKinematic = true; // AI uses NavMesh, not physics

            // NavMeshAgent
            NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
            agent.speed = 3.5f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 2f;
            agent.radius = 0.4f;
            agent.height = 2f;

            // Animator — use model's if present, else add
            Animator anim = enemy.GetComponentInChildren<Animator>();
            if (anim == null) anim = enemy.AddComponent<Animator>();

            AnimatorController enemyAC = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/_Project/Animations/Controllers/AC_Character.controller");
            if (enemyAC != null)
                anim.runtimeAnimatorController = enemyAC;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // AnimatorHook — receives animation events (damage colliders, combos, etc.)
            AnimatorHook enemyAnimHook = anim.gameObject.AddComponent<AnimatorHook>();

            // Lock-on reference
            GameObject lockOnRef = new GameObject("LockOnRef");
            lockOnRef.transform.SetParent(enemy.transform);
            lockOnRef.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            // Weapon holders
            WeaponHolderManager weaponHolder = enemy.AddComponent<WeaponHolderManager>();

            GameObject rightHookObj = new GameObject("RightWeaponHook");
            rightHookObj.transform.SetParent(enemy.transform);
            rightHookObj.transform.localPosition = new Vector3(0.3f, 0.9f, 0.2f);
            WeaponHolderHook rightHook = rightHookObj.AddComponent<WeaponHolderHook>();
            rightHook.isLeftHook = false;

            // Stats
            RuntimeStats stats = enemy.AddComponent<RuntimeStats>();
            stats.maxHealth = 80;
            stats.maxPoise = 60;
            stats.poise = 60;

            // AI State Manager
            AIStateManager aiSM = enemy.AddComponent<AIStateManager>();
            aiSM.model = model.transform;
            aiSM.lockOnTarget = lockOnRef.transform;
            aiSM.rigid = rb;
            aiSM.agent = agent;
            aiSM.anim = anim;
            aiSM.runtimeStats = stats;
            aiSM.weaponHolderManager = weaponHolder;
            aiSM.animHook = enemyAnimHook;
            aiSM.aggressionLevel = aggression;
            aiSM.detectionRadius = 20f;
            aiSM.attackDistance = 2.5f;
            aiSM.chaseSpeed = 3.5f;
            aiSM.patrolSpeed = 1.5f;
            aiSM.detectionLayer = ~0; // All layers

            return enemy;
        }

        private static void BuildMeleeEnemy(Vector3 position)
        {
            GameObject enemy = BuildEnemyBase(
                "Enemy_Melee_Thug",
                position,
                new Color(1f, 0.3f, 0.2f), // Red — melee thug
                0.4f // Moderate aggression
            );

            AIStateManager ai = enemy.GetComponent<AIStateManager>();
            ai.attackDistance = 2.5f;
            ai.attackCooldown = 1.8f;

            // Indicator: tall narrow top
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "TypeIndicator_Melee";
            indicator.transform.SetParent(enemy.transform);
            indicator.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            indicator.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            Object.DestroyImmediate(indicator.GetComponent<Collider>());
            Renderer ir = indicator.GetComponent<Renderer>();
            Material im = EditorShaderHelper.CreateMaterial(Color.red);
            ir.sharedMaterial = im;

            // Equip melee weapon
            WeaponItem batWeapon = AssetDatabase.LoadAssetAtPath<WeaponItem>(
                "Assets/_Project/ScriptableObjects/Weapons/WPN_Bat.asset");
            if (batWeapon != null && ai != null)
                ai.startingWeapon = batWeapon;

            Debug.Log("[Clout] Melee Thug spawned at " + position);
        }

        private static void BuildRangedEnemy(Vector3 position)
        {
            GameObject enemy = BuildEnemyBase(
                "Enemy_Ranged_Shooter",
                position,
                new Color(1f, 0.8f, 0.2f), // Yellow — ranged shooter
                0.3f // Lower aggression — keeps distance
            );

            AIStateManager ai = enemy.GetComponent<AIStateManager>();
            ai.attackDistance = 3f;
            ai.attackCooldown = 1.5f;
            ai.rangedAttackDistance = 25f;
            ai.preferredRangedDistance = 14f;

            RuntimeStats stats = enemy.GetComponent<RuntimeStats>();
            stats.maxHealth = 60; // Squishier

            // Indicator: diamond shape (two pyramids)
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "TypeIndicator_Ranged";
            indicator.transform.SetParent(enemy.transform);
            indicator.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            indicator.transform.localScale = new Vector3(0.25f, 0.4f, 0.25f);
            Object.DestroyImmediate(indicator.GetComponent<Collider>());
            Renderer ir = indicator.GetComponent<Renderer>();
            Material im = EditorShaderHelper.CreateMaterial(Color.yellow);
            ir.sharedMaterial = im;

            // Equip fists for now (until guns are added)
            WeaponItem fistWeapon = AssetDatabase.LoadAssetAtPath<WeaponItem>(
                "Assets/_Project/ScriptableObjects/Weapons/WPN_Fists.asset");
            if (fistWeapon != null && ai != null)
                ai.startingWeapon = fistWeapon;

            Debug.Log("[Clout] Ranged Shooter spawned at " + position);
        }

        private static void BuildHybridEnemy(Vector3 position)
        {
            GameObject enemy = BuildEnemyBase(
                "Enemy_Hybrid_Enforcer",
                position,
                new Color(0.8f, 0.2f, 0.8f), // Purple — hybrid enforcer
                0.7f // High aggression — boss-like
            );

            AIStateManager ai = enemy.GetComponent<AIStateManager>();
            ai.attackDistance = 2.8f;
            ai.attackCooldown = 1.2f;
            ai.rangedAttackDistance = 20f;
            ai.preferredRangedDistance = 10f;

            RuntimeStats stats = enemy.GetComponent<RuntimeStats>();
            stats.maxHealth = 120; // Tankier
            stats.maxPoise = 100;
            stats.poise = 100;
            stats.armor = 15f;

            // Indicator: large sphere
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "TypeIndicator_Hybrid";
            indicator.transform.SetParent(enemy.transform);
            indicator.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            indicator.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            Object.DestroyImmediate(indicator.GetComponent<Collider>());
            Renderer ir = indicator.GetComponent<Renderer>();
            Material im = EditorShaderHelper.CreateMaterial(new Color(0.8f, 0.2f, 0.8f));
            ir.sharedMaterial = im;

            // Equip heavy weapon
            WeaponItem pipeWeapon = AssetDatabase.LoadAssetAtPath<WeaponItem>(
                "Assets/_Project/ScriptableObjects/Weapons/WPN_Pipe.asset");
            if (pipeWeapon != null && ai != null)
                ai.startingWeapon = pipeWeapon;

            Debug.Log("[Clout] Hybrid Enforcer spawned at " + position);
        }

        // ─────────────────────────────────────────────────────────
        //  HUD
        // ─────────────────────────────────────────────────────────

        private static void BuildHUD(GameObject player)
        {
            GameObject hudObj = new GameObject("CombatHUD");
            // ScreenSpaceOverlay ignores world position at runtime, but placing at y=100
            // keeps the Canvas rect gizmo out of the arena floor in scene view.
            hudObj.transform.position = new Vector3(0f, 100f, 0f);
            CombatHUD hud = hudObj.AddComponent<CombatHUD>();

            // Wire references — HUD will find canvas and create UI in Awake
            PlayerStateManager psm = player.GetComponent<PlayerStateManager>();
            hud.playerStateManager = psm;
            hud.runtimeStats = player.GetComponent<RuntimeStats>();
            hud.reputationManager = player.GetComponent<ReputationManager>();
            hud.wantedSystem = player.GetComponent<WantedSystem>();
            hud.ammoCacheManager = player.GetComponent<AmmoCacheManager>();

            Debug.Log("[Clout] Combat HUD created and wired to player.");
        }

        // ─────────────────────────────────────────────────────────
        //  ECONOMY
        // ─────────────────────────────────────────────────────────

        private static void BuildEconomy()
        {
            GameObject econObj = new GameObject("EconomySystem");

            // CashManager — player wallet (dirty/clean cash tracking)
            CashManager cashMgr = econObj.AddComponent<CashManager>();
            cashMgr.startingDirtyCash = 500f;
            cashMgr.startingCleanCash = 0f;

            // TransactionLedger — financial record keeping
            econObj.AddComponent<TransactionLedger>();

            // EconomyManager — market price simulation
            econObj.AddComponent<EconomyManager>();

            // PropertyManager — property ownership and daily tick
            econObj.AddComponent<PropertyManager>();

            Debug.Log("[Clout] Economy system created: CashManager + TransactionLedger + EconomyManager + PropertyManager.");
        }

        // ─────────────────────────────────────────────────────────
        //  NAVMESH
        // ─────────────────────────────────────────────────────────

        private static void BakeNavMesh()
        {
            NavMeshSurface surface = Object.FindAnyObjectByType<NavMeshSurface>();
            if (surface != null)
            {
                surface.BuildNavMesh();
                Debug.Log("[Clout] NavMesh baked successfully.");
            }
            else
            {
                Debug.LogWarning("[Clout] NavMeshSurface not found — NavMesh not baked.");
            }
        }
    }
}
#endif
