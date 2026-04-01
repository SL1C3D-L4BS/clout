#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Clout.Editor
{
    /// <summary>
    /// Self-deleting trigger — imports TextMeshPro essential resources
    /// to fix "Can't Generate Mesh, No Font Asset has been assigned" warnings.
    /// </summary>
    [InitializeOnLoad]
    public static class _ImportTMPEssentials
    {
        static _ImportTMPEssentials()
        {
            EditorApplication.delayCall += RunOnce;
        }

        private static void RunOnce()
        {
            EditorApplication.delayCall -= RunOnce;

            // Check if TMP resources already exist
            string tmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            if (File.Exists(tmpSettingsPath))
            {
                Debug.Log("[Clout] TMP essential resources already imported.");
                SelfDelete();
                return;
            }

            // Find the TMP essentials package
            string[] guids = AssetDatabase.FindAssets("TMP_Essential_Resources");
            string packagePath = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".unitypackage"))
                {
                    packagePath = path;
                    break;
                }
            }

            if (packagePath != null)
            {
                Debug.Log($"[Clout] Importing TMP essential resources from: {packagePath}");
                AssetDatabase.ImportPackage(packagePath, false);
                Debug.Log("[Clout] TMP essential resources imported successfully.");
            }
            else
            {
                // Fallback: try the known Unity package cache path
                string[] searchPaths = new[]
                {
                    "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage",
                };

                foreach (string sp in searchPaths)
                {
                    if (File.Exists(sp))
                    {
                        Debug.Log($"[Clout] Importing TMP essential resources from: {sp}");
                        AssetDatabase.ImportPackage(sp, false);
                        Debug.Log("[Clout] TMP essential resources imported.");
                        SelfDelete();
                        return;
                    }
                }

                Debug.LogWarning("[Clout] Could not find TMP Essential Resources package. " +
                    "Please import manually: Window > TextMeshPro > Import TMP Essential Resources.");
            }

            SelfDelete();
        }

        private static void SelfDelete()
        {
            string path = "Assets/_Project/Scripts/Editor/_ImportTMPEssentials.cs";
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
