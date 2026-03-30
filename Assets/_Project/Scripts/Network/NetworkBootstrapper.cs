using UnityEngine;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;

namespace Clout.Network
{
    /// <summary>
    /// Network initialization — creates and configures FishNet with Tugboat transport.
    ///
    /// Singleplayer: auto-starts as Host (server + client).
    /// Multiplayer: starts as Server or Client based on configuration.
    /// Co-op: up to 4 players on a single host.
    /// </summary>
    public class NetworkBootstrapper : MonoBehaviour
    {
        [Header("Network Configuration")]
        public bool autoStartAsHost = true;
        public string serverAddress = "localhost";
        public ushort serverPort = 7770;

        [Header("References")]
        public NetworkManager networkManager;

        private bool _networkReady;

        private void Awake()
        {
            if (FindObjectsByType<NetworkBootstrapper>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            if (networkManager == null)
                networkManager = FindAnyObjectByType<NetworkManager>();

            if (networkManager == null)
            {
                Debug.Log("[Clout Network] No NetworkManager found — running in OFFLINE mode.");
                _networkReady = false;
                return;
            }

            _networkReady = true;

            Tugboat tug = networkManager.GetComponentInChildren<Tugboat>();
            if (tug != null)
            {
                tug.SetPort(serverPort);
                tug.SetClientAddress(serverAddress);
            }
        }

        private void Start()
        {
            if (_networkReady && autoStartAsHost)
                StartHost();
        }

        public void StartHost()
        {
            if (networkManager == null) return;
            networkManager.ServerManager.StartConnection();
            networkManager.ClientManager.StartConnection();
            Debug.Log("[Clout Network] Started as HOST (server + client)");
        }

        public void StartServer()
        {
            if (networkManager == null) return;
            networkManager.ServerManager.StartConnection();
            Debug.Log("[Clout Network] Started as SERVER");
        }

        public void StartClient()
        {
            if (networkManager == null) return;

            Tugboat tug = networkManager.GetComponentInChildren<Tugboat>();
            if (tug != null)
                tug.SetClientAddress(serverAddress);

            networkManager.ClientManager.StartConnection();
            Debug.Log($"[Clout Network] Started as CLIENT → {serverAddress}:{serverPort}");
        }

        public void StopNetwork()
        {
            if (networkManager == null) return;

            if (networkManager.IsServerStarted)
                networkManager.ServerManager.StopConnection(true);

            if (networkManager.IsClientStarted)
                networkManager.ClientManager.StopConnection();
        }
    }
}
