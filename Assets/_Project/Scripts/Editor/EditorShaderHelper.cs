#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace Clout.Editor
{
    /// <summary>
    /// Shared helper for editor scripts that need to create materials.
    /// Auto-ensures URP pipeline is configured, then uses the pipeline's default shader.
    /// No more pink materials.
    /// </summary>
    public static class EditorShaderHelper
    {
        private static Shader _cachedShader;

        /// <summary>
        /// Get a lit shader that works. Uses the render pipeline's default shader first,
        /// then falls back through a cascade of known shader names.
        /// </summary>
        public static Shader GetLitShader()
        {
            // Invalidate cache if the cached shader was destroyed or became null
            if (_cachedShader != null) return _cachedShader;

            // Step 1: Ensure URP pipeline is actually assigned
            URPSetup.EnsureURPConfigured();

            // Step 2: Try getting the shader from the active render pipeline
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                Shader pipelineShader = GraphicsSettings.defaultRenderPipeline.defaultShader;
                if (pipelineShader != null)
                {
                    _cachedShader = pipelineShader;
                    Debug.Log($"[Clout] Using render pipeline default shader: {pipelineShader.name}");
                    return pipelineShader;
                }
            }

            // Step 3: Manual fallback chain (shouldn't be needed after Step 1-2)
            string[] candidates =
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Shader Graphs/Lit",
                "Standard",
                "Unlit/Color"
            };

            foreach (string name in candidates)
            {
                Shader s = Shader.Find(name);
                if (s != null)
                {
                    _cachedShader = s;
                    Debug.Log($"[Clout] Using fallback shader: {name}");
                    return s;
                }
            }

            // Absolute last resort
            Debug.LogError("[Clout] No valid shader found! Materials will be pink.");
            _cachedShader = Shader.Find("Hidden/InternalErrorShader");
            return _cachedShader;
        }

        /// <summary>
        /// Create a material with the best available lit shader and a color.
        /// Material is saved as an asset if called from editor context with a scene open.
        /// </summary>
        public static Material CreateMaterial(Color color)
        {
            Shader shader = GetLitShader();
            Material mat = new Material(shader);
            mat.name = $"Mat_{ColorToHex(color)}";

            // URP Lit uses _BaseColor, Standard uses _Color
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;

            return mat;
        }

        /// <summary>
        /// Create a material AND save it as a persistent .mat asset.
        /// Use this when building scenes/prefabs to avoid broken shader references on reload.
        /// </summary>
        public static Material CreateAndSaveMaterial(Color color, string assetPath)
        {
            // Check if material already exists at path
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null) return existing;

            Material mat = CreateMaterial(color);

            string dir = System.IO.Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir))
                System.IO.Directory.CreateDirectory(dir);

            AssetDatabase.CreateAsset(mat, assetPath);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static string ColorToHex(Color c)
        {
            return ColorUtility.ToHtmlStringRGB(c);
        }
    }
}
#endif
