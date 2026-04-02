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
    /// Forces TMP to load its default font asset on editor startup, ensuring it's
    /// available before Scene view handles attempt to render any TMP gizmos.
    /// </summary>
    [InitializeOnLoad]
    public static class TMPWarningFix
    {
        static TMPWarningFix()
        {
            EditorApplication.delayCall += EnsureDefaultFont;
        }

        private static void EnsureDefaultFont()
        {
            // Force TMP Settings to load and cache the default font
            TMP_Settings settings = TMP_Settings.instance;
            if (settings == null) return;

            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
            {
                // Try to find any SDF font in the project as fallback
                string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                    if (font != null)
                    {
                        // Force-assign via serialized property
                        SerializedObject so = new SerializedObject(settings);
                        SerializedProperty fontProp = so.FindProperty("m_defaultFontAsset");
                        if (fontProp != null)
                        {
                            fontProp.objectReferenceValue = font;
                            so.ApplyModifiedPropertiesWithoutUndo();
                        }
                        Debug.Log($"[TMP Fix] Assigned default font: {font.name}");
                    }
                }
            }

            // Pre-warm the font atlas to prevent HandleUtility mesh gen failures
            if (defaultFont != null)
            {
                defaultFont.HasCharacter('A');
            }
        }
    }
}
