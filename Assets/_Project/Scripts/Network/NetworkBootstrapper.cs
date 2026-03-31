using UnityEngine;

namespace Clout.Network
{
    /// <summary>
    /// Network initialization — Phase 2 singleplayer safe.
    /// FishNet types wrapped in try-catch so it doesn't crash when no NetworkManager exists.
    /// Full FishNet integration will be restored in Phase 4 multiplayer.
    /// </summary>
    public class NetworkBootstrapper : MonoBehaviour
    {
        [Header("Network Configuration")]
        public bool autoStartAsHost = true;
        public string serverAddress = "localhost";
        public ushort serverPort = 7770;

        private void Awake()
        {
            if (FindObjectsByType<NetworkBootstrapper>(FindObjectsInactive.Exclude).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            Debug.Log("[Clout Network] NetworkBootstrapper running in offline mode (Phase 2 singleplayer).");
        }

        private void Start()
        {
            // Phase 2: no auto-host, pure singleplayer
        }

        public void StartHost()
        {
            Debug.LogWarning("[Clout Network] StartHost called but FishNet is disabled for Phase 2 singleplayer.");
        }

        public void StartServer()
        {
            Debug.LogWarning("[Clout Network] StartServer called but FishNet is disabled for Phase 2 singleplayer.");
        }

        public void StartClient()
        {
            Debug.LogWarning("[Clout Network] StartClient called but FishNet is disabled for Phase 2 singleplayer.");
        }

        public void StopNetwork()
        {
            // No-op in Phase 2
        }
    }
}
