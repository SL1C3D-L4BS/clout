using System.Collections.Generic;
using UnityEngine;

namespace Clout.Utils
{
    /// <summary>
    /// Central item/asset database — ported from Sharp Accent's ResourcesManager.
    /// Upgraded for CLOUT's multi-type item system: weapons, products, ingredients, properties.
    ///
    /// ScriptableObject catalog loaded from Resources. All game items registered here
    /// for fast runtime lookup by ID. Used by inventory, shops, save/load, spawning.
    ///
    /// Usage:
    ///   var pistol = ResourceDatabase.Instance.GetItem<WeaponItem>("pistol_9mm");
    ///   var weed = ResourceDatabase.Instance.GetItem<ProductDefinition>("product_og_kush");
    /// </summary>
    [CreateAssetMenu(menuName = "CLOUT/Utils/Resource Database")]
    public class ResourceDatabase : ScriptableObject
    {
        [Header("All game items — drag ScriptableObjects here")]
        public ScriptableObject[] allItems;

        [System.NonSerialized]
        private Dictionary<string, ScriptableObject> _itemDict = new Dictionary<string, ScriptableObject>();
        [System.NonSerialized]
        private bool _initialized;

        // Singleton loaded from Resources/
        private static ResourceDatabase _instance;
        public static ResourceDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ResourceDatabase>("ResourceDatabase");
                    if (_instance != null) _instance.Init();
                }
                return _instance;
            }
        }

        public void Init()
        {
            if (_initialized) return;
            _itemDict.Clear();

            if (allItems != null)
            {
                for (int i = 0; i < allItems.Length; i++)
                {
                    if (allItems[i] == null) continue;
                    string key = allItems[i].name;
                    if (!_itemDict.ContainsKey(key))
                        _itemDict[key] = allItems[i];
                    else
                        Debug.LogWarning($"[ResourceDatabase] Duplicate item ID: {key}");
                }
            }

            _initialized = true;
            Debug.Log($"[ResourceDatabase] Initialized with {_itemDict.Count} items");
        }

        /// <summary>Get any ScriptableObject by name.</summary>
        public ScriptableObject GetItem(string id)
        {
            if (!_initialized) Init();
            _itemDict.TryGetValue(id, out ScriptableObject item);
            return item;
        }

        /// <summary>Get a typed ScriptableObject by name.</summary>
        public T GetItem<T>(string id) where T : ScriptableObject
        {
            return GetItem(id) as T;
        }

        /// <summary>Get all items of a specific type.</summary>
        public List<T> GetAllOfType<T>() where T : ScriptableObject
        {
            if (!_initialized) Init();
            var results = new List<T>();
            foreach (var item in _itemDict.Values)
            {
                if (item is T typed)
                    results.Add(typed);
            }
            return results;
        }

        /// <summary>Check if an item exists.</summary>
        public bool HasItem(string id)
        {
            if (!_initialized) Init();
            return _itemDict.ContainsKey(id);
        }
    }
}
