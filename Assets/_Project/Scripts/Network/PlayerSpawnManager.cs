using UnityEngine;
using FishNet;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Managing;

namespace Clout.Network
{
    /// <summary>
    /// Server-side player spawn manager. Spawns player prefab with ownership
    /// when clients connect. Supports 4-player co-op.
    /// </summary>
    public class PlayerSpawnManager : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        public NetworkObject playerPrefab;

        [Tooltip("Spawn points in the scene. Falls back to origin if none set.")]
        public Transform[] spawnPoints;

        private int spawnIndex;
        private NetworkManager networkManager;

        private void Start()
        {
            networkManager = InstanceFinder.NetworkManager;
            if (networkManager == null)
            {
                enabled = false; // No NetworkManager — disable gracefully for offline play
                return;
            }

            networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
        }

        private void OnDestroy()
        {
            if (networkManager != null)
                networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
        }

        private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            if (!asServer) return;
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawnManager] No player prefab assigned!");
                return;
            }

            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int idx = spawnIndex % spawnPoints.Length;
                spawnPos = spawnPoints[idx].position;
                spawnRot = spawnPoints[idx].rotation;
                spawnIndex++;
            }

            NetworkObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
            networkManager.ServerManager.Spawn(player, conn);

            Debug.Log($"[Clout] Spawned player for connection {conn.ClientId} at {spawnPos}");
        }
    }
}
