using UnityEngine;

namespace Clout.Utils
{
    /// <summary>
    /// ScriptableObject config for the object pooler.
    /// Place in Resources/ folder as "ObjectPoolConfig".
    ///
    /// Ported from Sharp Accent's ObjectPoolAsset — upgraded with auto-expand
    /// and delayed return for CLOUT's combat VFX, bullet casings, blood splats, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "CLOUT/Utils/Object Pool Config")]
    public class ObjectPoolConfig : ScriptableObject
    {
        public PoolDefinition[] pools;
    }

    [System.Serializable]
    public class PoolDefinition
    {
        public string poolId;
        public GameObject prefab;
        [Tooltip("How many to pre-create. Pool auto-expands if needed.")]
        public int initialSize = 10;
    }
}
