using System.Collections.Generic;
using UnityEngine;

namespace Clout.Utils
{
    /// <summary>
    /// Object pooling system — ported from Sharp Accent's ObjectPooler.
    /// Prevents GC spikes from frequent Instantiate/Destroy (bullets, VFX, pickups).
    ///
    /// Usage:
    ///   ObjectPooler.Get("muzzle_flash").SetActive(true);
    ///   ObjectPooler.Return(obj);
    ///
    /// Configuration via ObjectPoolConfig ScriptableObject in Resources/.
    /// </summary>
    public static class ObjectPooler
    {
        private static ObjectPoolConfig _config;
        private static Dictionary<string, PoolBucket> _pools = new Dictionary<string, PoolBucket>();
        private static Transform _poolRoot;
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;

            _poolRoot = new GameObject("[Clout] Object Pool").transform;
            Object.DontDestroyOnLoad(_poolRoot.gameObject);

            _config = Resources.Load<ObjectPoolConfig>("ObjectPoolConfig");
            if (_config != null)
            {
                foreach (var def in _config.pools)
                {
                    RegisterPool(def.poolId, def.prefab, def.initialSize);
                }
            }

            _initialized = true;
        }

        /// <summary>Register a new pool at runtime.</summary>
        public static void RegisterPool(string id, GameObject prefab, int initialSize = 10)
        {
            EnsureInit();
            if (_pools.ContainsKey(id)) return;

            var bucket = new PoolBucket(id, prefab, initialSize, _poolRoot);
            _pools[id] = bucket;
        }

        /// <summary>Get a pooled object by ID. Returns inactive — caller must SetActive(true).</summary>
        public static GameObject Get(string id)
        {
            EnsureInit();
            if (_pools.TryGetValue(id, out PoolBucket bucket))
                return bucket.Get();

            Debug.LogWarning($"[ObjectPooler] No pool registered for '{id}'");
            return null;
        }

        /// <summary>Get a pooled object and position it.</summary>
        public static GameObject Get(string id, Vector3 position, Quaternion rotation)
        {
            GameObject obj = Get(id);
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }

        /// <summary>Return object to pool.</summary>
        public static void Return(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            if (_poolRoot != null)
                obj.transform.SetParent(_poolRoot);
        }

        /// <summary>Return object after delay.</summary>
        public static void ReturnDelayed(GameObject obj, float delay)
        {
            if (obj == null) return;
            var returner = obj.GetComponent<PoolReturnTimer>();
            if (returner == null) returner = obj.AddComponent<PoolReturnTimer>();
            returner.StartReturn(delay);
        }

        private static void EnsureInit()
        {
            if (!_initialized) Initialize();
        }

        /// <summary>Clear all pools (call on scene unload if needed).</summary>
        public static void ClearAll()
        {
            _pools.Clear();
            if (_poolRoot != null)
            {
                foreach (Transform child in _poolRoot)
                    Object.Destroy(child.gameObject);
            }
        }
    }

    internal class PoolBucket
    {
        private readonly string _id;
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly List<GameObject> _objects = new List<GameObject>();
        private int _index;

        public PoolBucket(string id, GameObject prefab, int initialSize, Transform poolRoot)
        {
            _id = id;
            _prefab = prefab;

            _parent = new GameObject($"Pool_{id}").transform;
            _parent.SetParent(poolRoot);

            for (int i = 0; i < initialSize; i++)
                CreateNew();
        }

        public GameObject Get()
        {
            // Try to find an inactive object
            for (int i = 0; i < _objects.Count; i++)
            {
                int idx = (_index + i) % _objects.Count;
                if (!_objects[idx].activeInHierarchy)
                {
                    _index = (idx + 1) % _objects.Count;
                    return _objects[idx];
                }
            }

            // All in use — expand pool
            return CreateNew();
        }

        private GameObject CreateNew()
        {
            GameObject obj = Object.Instantiate(_prefab, _parent);
            obj.SetActive(false);
            obj.name = $"{_id}_{_objects.Count}";
            _objects.Add(obj);
            return obj;
        }
    }

    /// <summary>Auto-return pooled object after delay.</summary>
    public class PoolReturnTimer : MonoBehaviour
    {
        private float _timer;
        private bool _active;

        public void StartReturn(float delay)
        {
            _timer = delay;
            _active = true;
        }

        private void Update()
        {
            if (!_active) return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _active = false;
                ObjectPooler.Return(gameObject);
            }
        }
    }
}
