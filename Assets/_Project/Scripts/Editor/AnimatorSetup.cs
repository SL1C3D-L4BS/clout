#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace Clout.Editor
{
    /// <summary>
    /// Creates the master Animator Controller for CLOUT characters using
    /// Sharp Accent placeholder animations.
    ///
    /// Menu: Clout > Setup > Create Animator Controller
    ///
    /// Builds:
    /// - Base Layer: Locomotion blend tree (idle, walk, run, strafe)
    /// - Override Layer: Combat attacks, rolls, interactions, damage
    /// - Parameters: vertical, horizontal, isInteracting, lockOn, stance,
    ///   isAiming, isShooting, weaponType, canDoCombo, mirror, isOnAir, isDead
    /// </summary>
    public static class AnimatorSetup
    {
        private const string ANIM_ROOT = "Assets/_Placeholder/Animations";
        private const string OUTPUT_PATH = "Assets/_Project/Animations/Controllers";
        private const string CONTROLLER_NAME = "AC_Character.controller";

        [MenuItem("Clout/Setup/Create Animator Controller", false, 200)]
        public static void CreateAnimatorController()
        {
            CreateAnimatorControllerHeadless();
            EditorUtility.DisplayDialog("Clout — Animator Controller",
                $"Created: {OUTPUT_PATH}/{CONTROLLER_NAME}\n\n" +
                "• Locomotion blend tree (idle, walk, run, strafe)\n" +
                "• 8 attack states\n" +
                "• 5 roll states\n" +
                "• 3 interaction states\n" +
                "• 5 damage states\n\n" +
                "Assign to characters via Clout > Build Test Arena",
                "Done");
        }

        /// <summary>Headless variant — no dialog. Safe to call from trigger scripts.</summary>
        public static void CreateAnimatorControllerHeadless()
        {
            // Ensure output directory
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Animations"))
                AssetDatabase.CreateFolder("Assets/_Project", "Animations");
            if (!AssetDatabase.IsValidFolder(OUTPUT_PATH))
                AssetDatabase.CreateFolder("Assets/_Project/Animations", "Controllers");

            string fullPath = $"{OUTPUT_PATH}/{CONTROLLER_NAME}";

            // Create controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(fullPath);

            // ═══════════════════════════════════════════════════════
            //  PARAMETERS
            // ═══════════════════════════════════════════════════════
            controller.AddParameter("vertical", AnimatorControllerParameterType.Float);
            controller.AddParameter("horizontal", AnimatorControllerParameterType.Float);
            controller.AddParameter("isInteracting", AnimatorControllerParameterType.Bool);
            controller.AddParameter("lockOn", AnimatorControllerParameterType.Bool);
            controller.AddParameter("stance", AnimatorControllerParameterType.Int);
            controller.AddParameter("isAiming", AnimatorControllerParameterType.Bool);
            controller.AddParameter("isShooting", AnimatorControllerParameterType.Bool);
            controller.AddParameter("weaponType", AnimatorControllerParameterType.Int);
            controller.AddParameter("canDoCombo", AnimatorControllerParameterType.Bool);
            controller.AddParameter("mirror", AnimatorControllerParameterType.Bool);
            controller.AddParameter("isOnAir", AnimatorControllerParameterType.Bool);
            controller.AddParameter("isDead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("isGrounded", AnimatorControllerParameterType.Bool);

            // ═══════════════════════════════════════════════════════
            //  LAYER 0: BASE (Locomotion)
            // ═══════════════════════════════════════════════════════
            AnimatorControllerLayer baseLayer = controller.layers[0];
            AnimatorStateMachine baseSM = baseLayer.stateMachine;

            // Load locomotion clips
            AnimationClip idle = LoadClip("Locomotion/oh_idle");
            AnimationClip walk = LoadClip("Locomotion/walking");
            AnimationClip run = LoadClip("Locomotion/unarmed_run");
            AnimationClip jogF = LoadClip("Locomotion/jog_f");
            AnimationClip jogBack = LoadClip("Locomotion/jog_backwards");
            AnimationClip strafeL = LoadClip("Locomotion/strafe_l");
            AnimationClip strafeR = LoadClip("Locomotion/strafe_r");
            AnimationClip strafeWalkL = LoadClip("Locomotion/strafe_left_walk");

            // --- LOCOMOTION BLEND TREE ---
            BlendTree locomotionTree;
            AnimatorState locoState = controller.CreateBlendTreeInController(
                "Locomotion", out locomotionTree, 0);
            locomotionTree.blendType = BlendTreeType.FreeformCartesian2D;
            locomotionTree.blendParameter = "horizontal";
            locomotionTree.blendParameterY = "vertical";

            // Center = idle
            if (idle != null) locomotionTree.AddChild(idle, new Vector2(0, 0));
            // Forward movement
            if (walk != null) locomotionTree.AddChild(walk, new Vector2(0, 0.5f));
            if (jogF != null) locomotionTree.AddChild(jogF, new Vector2(0, 1f));
            // Backward
            if (jogBack != null) locomotionTree.AddChild(jogBack, new Vector2(0, -0.5f));
            // Strafe
            if (strafeL != null) locomotionTree.AddChild(strafeL, new Vector2(-1f, 0));
            if (strafeR != null) locomotionTree.AddChild(strafeR, new Vector2(1f, 0));
            // Diagonal walk
            if (strafeWalkL != null) locomotionTree.AddChild(strafeWalkL, new Vector2(-0.5f, 0.5f));

            baseSM.defaultState = locoState;

            // --- EMPTY / INTERACTION STATE ---
            AnimatorState emptyState = baseSM.AddState("Empty", new Vector3(500, 0, 0));
            emptyState.motion = idle; // fallback

            // Locomotion → Empty (when isInteracting)
            var toEmpty = locoState.AddTransition(emptyState);
            toEmpty.AddCondition(AnimatorConditionMode.If, 0, "isInteracting");
            toEmpty.hasExitTime = false;
            toEmpty.duration = 0.15f;

            // Empty → Locomotion (when !isInteracting)
            var toLocomotion = emptyState.AddTransition(locoState);
            toLocomotion.AddCondition(AnimatorConditionMode.IfNot, 0, "isInteracting");
            toLocomotion.hasExitTime = false;
            toLocomotion.duration = 0.15f;

            // --- DEATH STATE ---
            AnimationClip deathClip = LoadClip("Damage/damage_3"); // Use heavy damage as death
            if (deathClip != null)
            {
                AnimatorState deathState = baseSM.AddState("Death", new Vector3(500, 200, 0));
                deathState.motion = deathClip;

                var toDeath = baseSM.AddAnyStateTransition(deathState);
                toDeath.AddCondition(AnimatorConditionMode.If, 0, "isDead");
                toDeath.hasExitTime = false;
                toDeath.duration = 0.1f;
                toDeath.canTransitionToSelf = false;
            }

            // ═══════════════════════════════════════════════════════
            //  LAYER 1: OVERRIDE (Combat, Rolls, Interactions)
            // ═══════════════════════════════════════════════════════

            // Use AddLayer(string) — Unity creates and registers the AnimatorStateMachine
            // as a proper sub-asset internally. Manually creating one and calling
            // AddObjectToAsset does NOT work; Unity still reports it as missing at assign time.
            controller.AddLayer("Override");

            // controller.layers returns a copy — modify the copy then write it back.
            AvatarMask upperMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(
                "Assets/_Placeholder/AnimatorControllers/Upper Body.mask");
            AnimatorControllerLayer[] allLayers = controller.layers;
            allLayers[1].defaultWeight = 1f;
            allLayers[1].blendingMode = AnimatorLayerBlendingMode.Override;
            allLayers[1].avatarMask = upperMask; // May be null if mask didn't import
            controller.layers = allLayers; // write back

            // Now fetch the live state machine reference (registered inside controller asset)
            AnimatorStateMachine overrideSM = controller.layers[1].stateMachine;

            // Empty default state (no override when not attacking)
            AnimatorState overrideEmpty = overrideSM.AddState("Empty", new Vector3(250, 0, 0));
            overrideSM.defaultState = overrideEmpty;

            // --- ATTACKS ---
            AddOverrideClip(controller, overrideSM, overrideEmpty, "oh_attack_1", "Attacks/oh_attack_1", 0);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "oh_attack_2", "Attacks/oh_attack_2", 1);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "oh_attack_3", "Attacks/oh_attack_3", 2);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "th_attack_1", "Attacks/th_attack_1", 3);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "th_attack_2", "Attacks/th_attack_2", 4);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "punch_1", "Attacks/punch 1", 5);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "kick_1", "Attacks/kick 1", 6);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "parry_attack", "Attacks/parry_attack", 7);

            // --- ROLLS ---
            AddOverrideClip(controller, overrideSM, overrideEmpty, "roll_forward", "Rolls/roll_forward", 8);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "roll_backward", "Rolls/roll_backwards", 9);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "roll_left", "Rolls/roll_left", 10);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "roll_right", "Rolls/roll_right", 11);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "step_back", "Rolls/step_back", 12);

            // --- INTERACTIONS ---
            AddOverrideClip(controller, overrideSM, overrideEmpty, "pick_up", "Interactions/pick_up", 13);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "use_item", "Interactions/use_item", 14);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "equip_weapon", "Interactions/equip_weapon", 15);

            // --- DAMAGE ---
            AddOverrideClip(controller, overrideSM, overrideEmpty, "damage_1", "Damage/damage 1", 16);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "damage_2", "Damage/damage 2", 17);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "damage_3", "Damage/damage_3", 18);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "getting_parried", "Damage/getting_parried", 19);
            AddOverrideClip(controller, overrideSM, overrideEmpty, "getting_backstabbed", "Damage/getting_backstabbed", 20);

            // Save
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Clout] Animator Controller created at {fullPath}");
            Debug.Log("[Clout] Parameters: vertical, horizontal, isInteracting, lockOn, stance, " +
                      "isAiming, isShooting, weaponType, canDoCombo, mirror, isOnAir, isDead, isGrounded");
            Debug.Log("[Clout] Locomotion blend tree + 21 override states (attacks, rolls, interactions, damage)");
        }

        // ─────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────

        private static AnimationClip LoadClip(string relativePath)
        {
            string path = $"{ANIM_ROOT}/{relativePath}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
                Debug.LogWarning($"[AnimatorSetup] Clip not found: {path}");
            return clip;
        }

        /// <summary>
        /// Add an animation clip as a state in the override layer.
        /// Each state is reachable via PlayTargetAnimation() crossfade by name.
        /// </summary>
        private static void AddOverrideClip(AnimatorController controller,
            AnimatorStateMachine sm, AnimatorState emptyState,
            string stateName, string clipPath, int index)
        {
            AnimationClip clip = LoadClip(clipPath);
            if (clip == null) return;

            float x = (index % 5) * 200f;
            float y = (index / 5 + 1) * 80f;

            AnimatorState state = sm.AddState(stateName, new Vector3(x, y, 0));
            state.motion = clip;

            // Return to empty when done (exit time)
            var toEmpty = state.AddTransition(emptyState);
            toEmpty.hasExitTime = true;
            toEmpty.exitTime = 0.9f;
            toEmpty.duration = 0.15f;
        }
    }
}
#endif
