#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Clout.Editor
{
    /// <summary>
    /// Creates URP pipeline assets and assigns them to Graphics + Quality settings.
    /// Without this, ALL materials show as pink/magenta because no render pipeline is active.
    ///
    /// Menu: Clout > Setup > Configure URP Pipeline
    /// </summary>
    public static class URPSetup
    {
        private const string SETTINGS_DIR = "Assets/_Project/Settings/Rendering";
        private const string PIPELINE_PATH = "Assets/_Project/Settings/Rendering/URP_PipelineAsset.asset";
        private const string RENDERER_PATH = "Assets/_Project/Settings/Rendering/URP_RendererData.asset";

        [MenuItem("Clout/Setup/Configure URP Pipeline", false, 10)]
        public static void ConfigureURP()
        {
            ConfigureURPHeadless();
            EditorUtility.DisplayDialog("Clout — URP Setup",
                "URP Pipeline configured!\n\n" +
                "• Created URP Pipeline Asset\n" +
                "• Created URP Renderer Data\n" +
                "• Assigned to Graphics Settings\n" +
                "• Assigned to all Quality Levels\n\n" +
                "Pink materials should now render correctly.\n" +
                "You may need to rebuild the Test Arena.",
                "Done");
        }

        public static void ConfigureURPHeadless()
        {
            System.IO.Directory.CreateDirectory(SETTINGS_DIR);

            // Check if assets already exist
            UniversalRenderPipelineAsset existingPipeline =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PIPELINE_PATH);

            if (existingPipeline != null)
            {
                // Already exists — just make sure it's assigned
                AssignPipelineToSettings(existingPipeline);
                Debug.Log("[Clout] URP Pipeline already exists. Re-assigned to settings.");
                return;
            }

            // Create Renderer Data first
            UniversalRendererData rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, RENDERER_PATH);
            EditorUtility.SetDirty(rendererData);

            // Create Pipeline Asset referencing the renderer
            UniversalRenderPipelineAsset pipelineAsset =
                UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipelineAsset, PIPELINE_PATH);

            // Configure for good defaults
            pipelineAsset.renderScale = 1f;
            pipelineAsset.msaaSampleCount = 4;  // 4x MSAA
            pipelineAsset.supportsHDR = true;
            pipelineAsset.shadowDistance = 80f;

            EditorUtility.SetDirty(pipelineAsset);
            AssetDatabase.SaveAssets();

            // Assign to project settings
            AssignPipelineToSettings(pipelineAsset);

            Debug.Log("[Clout] URP Pipeline created and assigned: " + PIPELINE_PATH);
        }

        private static void AssignPipelineToSettings(UniversalRenderPipelineAsset pipelineAsset)
        {
            // Assign to Graphics Settings (default pipeline)
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            // Assign to ALL quality levels
            string[] qualityNames = QualitySettings.names;
            int currentLevel = QualitySettings.GetQualityLevel();

            for (int i = 0; i < qualityNames.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipelineAsset;
            }

            // Restore original quality level
            QualitySettings.SetQualityLevel(currentLevel, true);

            Debug.Log($"[Clout] URP Pipeline assigned to Graphics Settings + {qualityNames.Length} quality levels.");
        }

        /// <summary>
        /// Quick check: is URP actually configured? Call from other editor tools.
        /// </summary>
        public static bool IsURPConfigured()
        {
            return GraphicsSettings.defaultRenderPipeline != null;
        }

        /// <summary>
        /// Auto-configure URP if not set up. Safe to call from any editor tool.
        /// </summary>
        public static void EnsureURPConfigured()
        {
            if (!IsURPConfigured())
            {
                Debug.LogWarning("[Clout] URP Pipeline not configured! Setting up now...");
                ConfigureURPHeadless();
            }
        }
    }
}
#endif
