using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Clout.Save
{
    /// <summary>
    /// Save/Load system — ported from Sharp Accent's Serialization.cs.
    /// Upgraded from BinaryFormatter (security risk) to JSON serialization.
    ///
    /// Supports:
    /// - Multiple save slots (3)
    /// - Auto-save on events (property exit, deal complete, timed interval)
    /// - Manual save/load
    /// - Save versioning for backward compatibility
    /// - Serializable wrappers for Unity types (Vector3, Quaternion)
    ///
    /// Phase 2: Player state, cash, inventory, properties, workers
    /// Phase 3+: Full world state, NPC states, economy, territory
    /// </summary>
    public static class SaveManager
    {
        public const int MAX_SAVE_SLOTS = 3;
        public const int SAVE_VERSION = 1;

        private static string SaveDirectory =>
            Path.Combine(Application.persistentDataPath, "Saves");

        /// <summary>Save game state to a slot (0-2).</summary>
        public static bool Save(CloutSaveData data, int slot = 0)
        {
            if (slot < 0 || slot >= MAX_SAVE_SLOTS)
            {
                Debug.LogError($"[SaveManager] Invalid slot {slot}");
                return false;
            }

            try
            {
                if (!Directory.Exists(SaveDirectory))
                    Directory.CreateDirectory(SaveDirectory);

                data.saveVersion = SAVE_VERSION;
                data.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                data.playTime += Time.realtimeSinceStartup;

                string json = JsonUtility.ToJson(data, true);
                string path = GetSavePath(slot);
                File.WriteAllText(path, json);

                Debug.Log($"[SaveManager] Saved to slot {slot}: {path}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e.Message}");
                return false;
            }
        }

        /// <summary>Load game state from a slot.</summary>
        public static CloutSaveData Load(int slot = 0)
        {
            string path = GetSavePath(slot);
            if (!File.Exists(path))
            {
                Debug.Log($"[SaveManager] No save found in slot {slot}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                CloutSaveData data = JsonUtility.FromJson<CloutSaveData>(json);

                if (data.saveVersion != SAVE_VERSION)
                    Debug.LogWarning($"[SaveManager] Save version mismatch: {data.saveVersion} vs {SAVE_VERSION}");

                Debug.Log($"[SaveManager] Loaded slot {slot} — {data.saveTimestamp}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed: {e.Message}");
                return null;
            }
        }

        /// <summary>Check if a save slot has data.</summary>
        public static bool HasSave(int slot) =>
            File.Exists(GetSavePath(slot));

        /// <summary>Delete a save slot.</summary>
        public static bool DeleteSave(int slot)
        {
            string path = GetSavePath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            return false;
        }

        /// <summary>Get metadata for all save slots (for UI display).</summary>
        public static SaveSlotInfo[] GetAllSlotInfo()
        {
            var slots = new SaveSlotInfo[MAX_SAVE_SLOTS];
            for (int i = 0; i < MAX_SAVE_SLOTS; i++)
            {
                slots[i] = new SaveSlotInfo { slot = i, hasSave = HasSave(i) };
                if (slots[i].hasSave)
                {
                    var data = Load(i);
                    if (data != null)
                    {
                        slots[i].timestamp = data.saveTimestamp;
                        slots[i].cloutScore = data.cloutScore;
                        slots[i].cloutRank = data.cloutRank;
                        slots[i].dirtyMoney = data.dirtyMoney;
                        slots[i].playTime = data.playTime;
                    }
                }
            }
            return slots;
        }

        private static string GetSavePath(int slot) =>
            Path.Combine(SaveDirectory, $"clout_save_{slot}.json");
    }

    // ─────────────────────────────────────────────────────────
    //  SAVE DATA STRUCTURES
    // ─────────────────────────────────────────────────────────

    [Serializable]
    public class CloutSaveData
    {
        // Meta
        public int saveVersion;
        public string saveTimestamp;
        public float playTime;

        // Player
        public SerializableVector3 playerPosition;
        public SerializableVector3 playerRotation;
        public int playerHealth;
        public float playerStamina;
        public string currentDistrict;

        // Empire - Money
        public float dirtyMoney;
        public float cleanMoney;

        // Empire - Reputation
        public int cloutScore;
        public string cloutRank;
        public float streetRep;
        public float civilianRep;

        // Empire - Wanted
        public float currentHeat;
        public int wantedLevel;

        // Empire - Inventory
        public List<SavedProductStack> products = new List<SavedProductStack>();
        public List<SavedWeaponSlot> weapons = new List<SavedWeaponSlot>();

        // Empire - Properties
        public List<SavedProperty> ownedProperties = new List<SavedProperty>();

        // Empire - Workers
        public List<SavedWorker> hiredWorkers = new List<SavedWorker>();
    }

    [Serializable]
    public struct SavedProductStack
    {
        public string productId;
        public int quantity;
        public int qualityTier;
        public float potency;
    }

    [Serializable]
    public struct SavedWeaponSlot
    {
        public string weaponId;
        public bool isEquipped;
        public bool isRightHand;
        public int currentAmmo;
    }

    [Serializable]
    public struct SavedProperty
    {
        public string propertyId;
        public string districtId;
        public int upgradeLevel;
        public List<SavedProductStack> stash;
    }

    [Serializable]
    public struct SavedWorker
    {
        public string workerId;
        public string workerName;
        public string assignedPropertyId;
        public string workerType; // Dealer, Cook, Guard
        public float loyalty;
        public float skill;
    }

    [Serializable]
    public struct SaveSlotInfo
    {
        public int slot;
        public bool hasSave;
        public string timestamp;
        public int cloutScore;
        public string cloutRank;
        public float dirtyMoney;
        public float playTime;
    }

    // ─────────────────────────────────────────────────────────
    //  SERIALIZABLE UNITY TYPES
    //  Ported from Sharp Accent's SaveableVector3
    // ─────────────────────────────────────────────────────────

    [Serializable]
    public struct SerializableVector3
    {
        public float x, y, z;

        public SerializableVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
        public Vector3 ToVector3() => new Vector3(x, y, z);

        public static implicit operator SerializableVector3(Vector3 v) => new SerializableVector3(v);
        public static implicit operator Vector3(SerializableVector3 v) => v.ToVector3();
    }

    [Serializable]
    public struct SerializableQuaternion
    {
        public float x, y, z, w;

        public SerializableQuaternion(Quaternion q) { x = q.x; y = q.y; z = q.z; w = q.w; }
        public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);

        public static implicit operator SerializableQuaternion(Quaternion q) => new SerializableQuaternion(q);
        public static implicit operator Quaternion(SerializableQuaternion q) => q.ToQuaternion();
    }
}
