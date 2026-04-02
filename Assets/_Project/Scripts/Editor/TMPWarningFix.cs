using UnityEngine;
using UnityEditor;
using TMPro;

namespace Clout.Editor
{
    /// <summary>
    /// Suppresses the Unity 6 TMP "Can't Generate Mesh, No Font Asset has been assigned"
    /// editor warning caused by HandleUtility:BeginHandles firing before TMP's editor
    /// module fully initializes.
    ///
    /// Three-layer fix:
    ///   1. Static constructor — fires at editor domain reload, before first Scene view draw
    ///   2. SceneView.beforeSceneGui — intercepts before handles render each frame
    ///   3. delayCall fallback — catches late-init edge cases
    ///
    /// Unity 6 (6000.x) specific: HandleUtility.BeginHandles calls TMP mesh generation
    /// during gizmo rendering. If no default font is loaded, it logs the warning.
    /// </summary>
    [InitializeOnLoad]
    public static class TMPWarningFix
    {
        private static bool _fontReady;

        static TMPWarningFix()
        {
            // Layer 1: Immediate — runs at domain reload before any Scene view draw
            ForceLoadDefaultFont();

            // Layer 2: Guard every Scene view draw until font is confirmed
            SceneView.beforeSceneGui += OnBeforeSceneGui;

            // Layer 3: Deferred fallback for edge cases
            EditorApplication.delayCall += () =>
            {
                ForceLoadDefaultFont();
                // Unhook scene guard once confirmed
                if (_fontReady)
                    SceneView.beforeSceneGui -= OnBeforeSceneGui;
            };
        }

        private static void OnBeforeSceneGui(SceneView sv)
        {
            if (_fontReady) return;
            ForceLoadDefaultFont();
            if (_fontReady)
                SceneView.beforeSceneGui -= OnBeforeSceneGui;
        }

        private static void ForceLoadDefaultFont()
        {
            if (_fontReady) return;

            // Force TMP Settings singleton to load
            TMP_Settings settings = TMP_Settings.instance;
            if (settings == null) return;

            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
            {
                // Find any SDF font in the project and assign it
                string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                    if (font != null)
                    {
                        SerializedObject so = new SerializedObject(settings);
                        SerializedProperty fontProp = so.FindProperty("m_defaultFontAsset");
                        if (fontProp != null)
                        {
                            fontProp.objectReferenceValue = font;
                            so.ApplyModifiedPropertiesWithoutUndo();
                        }
                        defaultFont = font;
                        Debug.Log($"[TMP Fix] Assigned default font: {font.name}");
                    }
                }
            }

            if (defaultFont == null) return;

            // Pre-warm the font atlas — forces mesh generation data to cache
            // so HandleUtility:BeginHandles never hits the null font path
            defaultFont.HasCharacter('A');
            defaultFont.HasCharacter(' ');

            // Force the material to load (prevents secondary mesh gen failure)
            if (defaultFont.material != null)
            {
                _ = defaultFont.material.shader;
            }

            _fontReady = true;
        }
    }
}
