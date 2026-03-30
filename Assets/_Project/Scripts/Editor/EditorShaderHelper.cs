#if UNITY_EDITOR
using UnityEngine;

namespace Clout.Editor
{
    /// <summary>
    /// Shared helper for editor scripts that need to create materials.
    /// Handles URP shader availability gracefully — no more pink materials.
    /// </summary>
    public static class EditorShaderHelper
    {
        private static Shader _cachedShader;

        /// <summary>
        /// Get a lit shader that works. Tries URP Lit, then Standard, then Unlit.
        /// </summary>
        public static Shader GetLitShader()
        {
            if (_cachedShader != null) return _cachedShader;

            // Try URP Lit (exact name varies by URP version)
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
                    return s;
                }
            }

            // Absolute fallback — should never happen
            _cachedShader = Shader.Find("Hidden/InternalErrorShader");
            return _cachedShader;
        }

        /// <summary>
        /// Create a material with the best available lit shader and a color.
        /// </summary>
        public static Material CreateMaterial(Color color)
        {
            Shader shader = GetLitShader();
            Material mat = new Material(shader);

            // URP Lit uses _BaseColor, Standard uses _Color
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;

            return mat;
        }
    }
}
#endif
