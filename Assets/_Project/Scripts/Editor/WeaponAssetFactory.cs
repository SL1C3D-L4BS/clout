#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using Clout.Combat;
using Clout.Core;

namespace Clout.Editor
{
    /// <summary>
    /// Creates starter weapon ScriptableObject assets and model prefabs
    /// using Sharp Accent placeholder meshes.
    ///
    /// Menu: Clout > Setup > Create Starter Weapons
    ///
    /// Creates:
    /// - Fists (Unarmed) — no model, punch/kick combo
    /// - Bat (one-hand melee) — uses BaseballBat_Low mesh
    /// - Knife (one-hand melee) — uses bottle mesh as placeholder
    /// - AttackAction SO (shared by all melee weapons)
    /// - Weapon model prefabs with DamageColliders
    /// </summary>
    public static class WeaponAssetFactory
    {
        private const string WEAPON_SO_PATH = "Assets/_Project/ScriptableObjects/Weapons";
        private const string ACTION_SO_PATH = "Assets/_Project/ScriptableObjects/Actions";
        private const string PREFAB_PATH = "Assets/_Project/Prefabs/Weapons";
        private const string PLACEHOLDER_MODELS = "Assets/_Placeholder/Models";
        private const string PROJECT_MODELS = "Assets/_Project/Models/Weapons";

        [MenuItem("Clout/Setup/Create Starter Weapons", false, 201)]
        public static void CreateStarterWeapons()
        {
            CreateStarterWeaponsHeadless();
            EditorUtility.DisplayDialog("Clout — Starter Weapons",
                "Created 4 weapon SOs + 3 weapon prefabs:\n\n" +
                "• Fists (unarmed — punch/kick)\n" +
                "• Bat (BaseballBat mesh — 3-hit combo)\n" +
                "• Knife (bottle mesh — slash/stab)\n" +
                "• Lead Pipe (stretched bat — 2-hit heavy)\n\n" +
                "Weapon prefabs have DamageColliders ready.\n" +
                "Assign to player via PlayerStateManager.startingWeapons",
                "Done");
        }

        /// <summary>Headless variant — no dialog. Safe to call from trigger scripts.</summary>
        public static void CreateStarterWeaponsHeadless()
        {
            EnsureDirectories();

            // Shared attack action SO
            AttackAction meleeAttack = CreateOrLoadAsset<AttackAction>(
                $"{ACTION_SO_PATH}/MeleeAttackAction.asset");
            meleeAttack.name = "MeleeAttackAction";
            EditorUtility.SetDirty(meleeAttack);

            // ═══════════════════════════════════════════════════════
            //  1. FISTS (Unarmed)
            // ═══════════════════════════════════════════════════════
            WeaponItem fists = CreateOrLoadAsset<WeaponItem>(
                $"{WEAPON_SO_PATH}/WPN_Fists.asset");
            fists.itemName = "Fists";
            fists.weaponType = WeaponType.Unarmed;
            fists.primaryDamageType = DamageType.Blunt;
            fists.baseDamage = 8f;
            fists.motionValueLight = 1.0f;
            fists.motionValueHeavy = 1.4f;
            fists.weight = 0f;
            fists.poiseDamage = 5f;
            fists.oneHanded_anim = "Empty";
            fists.modelPrefab = null; // No model for fists

            // Fist combos: punch chain (light), kick (heavy)
            fists.itemActions = new ItemActionContainer[4];
            fists.itemActions[0] = new ItemActionContainer
            {
                attackInput = AttackInputs.rb,
                animNames = new[] { "punch_1", "oh_attack_1", "oh_attack_2" },
                itemAction = meleeAttack,
                isMirrored = false,
                isTwoHanded = false
            };
            fists.itemActions[1] = new ItemActionContainer
            {
                attackInput = AttackInputs.rt,
                animNames = new[] { "kick_1" },
                itemAction = meleeAttack,
                isMirrored = false,
                isTwoHanded = false
            };
            fists.itemActions[2] = new ItemActionContainer();
            fists.itemActions[3] = new ItemActionContainer();
            EditorUtility.SetDirty(fists);

            // ═══════════════════════════════════════════════════════
            //  2. BAT (one-hand melee — BaseballBat_Low mesh)
            // ═══════════════════════════════════════════════════════
            GameObject batPrefab = CreateWeaponPrefab("BaseballBat_Low", "WPN_Bat",
                new Vector3(0, 0, 0), new Vector3(90, 0, 0), Vector3.one,
                PROJECT_MODELS);

            WeaponItem bat = CreateOrLoadAsset<WeaponItem>(
                $"{WEAPON_SO_PATH}/WPN_Bat.asset");
            bat.itemName = "Bat";
            bat.weaponType = WeaponType.MeleeOneHand;
            bat.primaryDamageType = DamageType.Blunt;
            bat.baseDamage = 25f;
            bat.motionValueLight = 1.0f;
            bat.motionValueHeavy = 1.8f;
            bat.weight = 3f;
            bat.poiseDamage = 20f;
            bat.oneHanded_anim = "oh_idle";
            bat.modelPrefab = batPrefab;

            bat.itemActions = new ItemActionContainer[4];
            bat.itemActions[0] = new ItemActionContainer
            {
                attackInput = AttackInputs.rb,
                animNames = new[] { "oh_attack_1", "oh_attack_2", "oh_attack_3" },
                itemAction = meleeAttack,
                isMirrored = false,
                isTwoHanded = false
            };
            bat.itemActions[1] = new ItemActionContainer
            {
                attackInput = AttackInputs.rt,
                animNames = new[] { "th_attack_1", "th_attack_2" },
                itemAction = meleeAttack,
                isMirrored = false,
                isTwoHanded = false
            };
            bat.itemActions[2] = new ItemActionContainer();
            bat.itemActions[3] = new ItemActionContainer();
            EditorUtility.SetDirty(bat);

            // ═══════════════════════════════════════════════════════
            //  3. KNIFE (one-hand melee — bottle mesh as placeholder)
            // ═══════════════════════════════════════════════════════
            GameObject knifePrefab = CreateWeaponPrefab("bottle", "WPN_Knife",
                new Vector3(0, 0, 0), new Vector3(0, 0, 0), Vector3.one * 0.8f);

            WeaponItem knife = CreateOrLoadAsset<WeaponItem>(
                $"{WEAPON_SO_PATH}/WPN_Knife.asset");
            knife.itemName = "Knife";
            knife.weaponType = WeaponType.MeleeOneHand;
            knife.primaryDamageType = DamageType.Slash;
            knife.baseDamage = 18f;
            knife.motionValueLight = 1.2f;
            knife.motionValueHeavy = 2.0f;
            knife.weight = 1f;
            knife.poiseDamage = 10f;
            knife.oneHanded_anim = "oh_idle";
            knife.modelPrefab = knifePrefab;

            knife.itemActions = new ItemActionContainer[4];
            knife.itemActions[0] = new ItemActionContainer
            {
                attackInput = AttackInputs.rb,
                animNames = new[] { "oh_attack_1", "oh_attack_2", "oh_attack_3" },
                itemAction = meleeAttack,
                isMirrored = false,
                isTwoHanded = false
            };
            knife.itemActions[1] = new ItemActionContainer
            {
                attackInput = AttackInputs.rt,
                animNames = new[] { "parry_attack" },
                itemAction = meleeAttack,
                isMirrored = false,
                isTwoHanded = false
            };
            knife.itemActions[2] = new ItemActionContainer();
            knife.itemActions[3] = new ItemActionContainer();
            EditorUtility.SetDirty(knife);

            // ═══════════════════════════════════════════════════════
            //  4. PIPE (two-hand melee — stretched bat mesh)
            // ═══════════════════════════════════════════════════════
            GameObject pipePrefab = CreateWeaponPrefab("BaseballBat_Low", "WPN_Pipe",
                new Vector3(0, 0, 0), new Vector3(90, 0, 0), new Vector3(0.7f, 0.7f, 1.5f),
                PROJECT_MODELS);

            WeaponItem pipe = CreateOrLoadAsset<WeaponItem>(
                $"{WEAPON_SO_PATH}/WPN_Pipe.asset");
            pipe.itemName = "Lead Pipe";
            pipe.weaponType = WeaponType.MeleeTwoHand;
            pipe.primaryDamageType = DamageType.Blunt;
            pipe.baseDamage = 35f;
            pipe.motionValueLight = 1.0f;
            pipe.motionValueHeavy = 2.2f;
            pipe.weight = 6f;
            pipe.poiseDamage = 30f;
            pipe.twoHanded_anim = "th_idle";
            pipe.modelPrefab = pipePrefab;

            pipe.itemActions = new ItemActionContainer[4];
            pipe.itemActions[0] = new ItemActionContainer
            {
                attackInput = AttackInputs.rb,
                animNames = new[] { "th_attack_1", "th_attack_2" },
                itemAction = meleeAttack,
                isMirrored = false,
                isTwoHanded = true
            };
            pipe.itemActions[1] = new ItemActionContainer
            {
                attackInput = AttackInputs.rt,
                animNames = new[] { "oh_attack_3" },
                itemAction = meleeAttack,
                isMirrored = false,
                isTwoHanded = true
            };
            pipe.itemActions[2] = new ItemActionContainer();
            pipe.itemActions[3] = new ItemActionContainer();
            EditorUtility.SetDirty(pipe);

            // Save everything
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Clout] Starter weapons created:");
            Debug.Log("  • WPN_Fists — Unarmed (punch/kick combo)");
            Debug.Log("  • WPN_Bat — One-hand melee (BaseballBat mesh, 3-hit light, 2-hit heavy)");
            Debug.Log("  • WPN_Knife — One-hand melee (3-hit slash, parry stab)");
            Debug.Log("  • WPN_Pipe — Two-hand melee (2-hit heavy, overhead smash)");
            Debug.Log("  • MeleeAttackAction — Shared attack logic SO");
        }

        // ─────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────

        private static void EnsureDirectories()
        {
            EnsureFolder("Assets/_Project/ScriptableObjects");
            EnsureFolder(WEAPON_SO_PATH);
            EnsureFolder(ACTION_SO_PATH);
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder(PREFAB_PATH);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent); // Recursive
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>
        /// Creates a weapon prefab from a placeholder model with a BoxCollider trigger
        /// for the DamageCollider system.
        /// </summary>
        private static GameObject CreateWeaponPrefab(string modelName, string prefabName,
            Vector3 posOffset, Vector3 rotOffset, Vector3 scale,
            string searchPath = null)
        {
            string prefabPath = $"{PREFAB_PATH}/{prefabName}.prefab";

            // Check if prefab already exists
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null) return existingPrefab;

            // Find the source model — search project path first, then placeholder
            string[] searchPaths = searchPath != null
                ? new[] { searchPath, PLACEHOLDER_MODELS }
                : new[] { PLACEHOLDER_MODELS };
            string[] modelGuids = AssetDatabase.FindAssets(modelName, searchPaths);
            GameObject sourceModel = null;
            foreach (string guid in modelGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.EndsWith(".fbx") || assetPath.EndsWith(".FBX"))
                {
                    sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    break;
                }
            }

            // Create prefab root
            GameObject root = new GameObject(prefabName);

            if (sourceModel != null)
            {
                // Instantiate model as child
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(sourceModel);
                model.transform.SetParent(root.transform);
                model.transform.localPosition = posOffset;
                model.transform.localEulerAngles = rotOffset;
                model.transform.localScale = scale;
                model.name = "Model";
            }
            else
            {
                // Fallback: primitive cube as weapon shape
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(root.transform);
                cube.transform.localPosition = new Vector3(0, 0.3f, 0);
                cube.transform.localScale = new Vector3(0.1f, 0.6f, 0.1f);
                cube.name = "Model";
                // Remove the auto-generated collider from primitive
                Object.DestroyImmediate(cube.GetComponent<Collider>());
            }

            // Add DamageCollider child with trigger
            GameObject damageColliderObj = new GameObject("DamageCollider");
            damageColliderObj.transform.SetParent(root.transform);
            damageColliderObj.transform.localPosition = new Vector3(0, 0.3f, 0);
            damageColliderObj.layer = LayerMask.NameToLayer("Default"); // Will be reassigned in game

            BoxCollider trigger = damageColliderObj.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(0.15f, 0.5f, 0.15f);

            // Add DamageCollider component
            DamageCollider dc = damageColliderObj.AddComponent<DamageCollider>();

            // Add WeaponHook to root and wire the damage collider reference
            WeaponHook hook = root.AddComponent<WeaponHook>();
            hook.damageCollider = dc;

            // Save as prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            return prefab;
        }
    }
}
#endif
