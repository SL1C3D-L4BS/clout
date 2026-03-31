using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using Clout.Network;

namespace Clout.Core
{
    /// <summary>
    /// Game entry point — ensures all singletons and essential managers exist.
    /// Runs before any other MonoBehaviour via Script Execution Order or [RuntimeInitializeOnLoadMethod].
    ///
    /// CRITICAL: Must create FishNet NetworkManager BEFORE scene loads, because
    /// all NetworkBehaviour components (StateManager, RuntimeStats, etc.) need
    /// a NetworkManager to exist during their Awake() — FishNet's IL weaving
    /// injects code that looks for the singleton.
    ///
    /// Singleplayer runs as Host (server + client on same machine).
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Bootstrap Config")]
        [SerializeField] private bool autoStartAsHost = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // Ensure GameBootstrapper exists
            if (FindAnyObjectByType<GameBootstrapper>() == null)
            {
                GameObject go = new GameObject("[Clout] Bootstrapper");
                go.AddComponent<GameBootstrapper>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // ALWAYS ensure NetworkManager exists — FishNet NetworkBehaviour components
            // break silently without one. Singleplayer = host mode.
            EnsureNetworkManager();
            EnsureNetworkBootstrapper();

            Debug.Log("[Clout] Game bootstrapper initialized (NetworkManager ensured).");
        }

        /// <summary>
        /// Create a FishNet NetworkManager if one doesn't exist.
        /// This MUST happen before any NetworkBehaviour.Awake() runs.
        /// </summary>
        private void EnsureNetworkManager()
        {
            if (FindAnyObjectByType<NetworkManager>() != null) return;

            GameObject nmObj = new GameObject("[FishNet] NetworkManager");
            DontDestroyOnLoad(nmObj);

            // Add NetworkManager
            nmObj.AddComponent<NetworkManager>();

            // Add Tugboat transport (FishNet's built-in UDP transport)
            Tugboat transport = nmObj.AddComponent<Tugboat>();

            Debug.Log("[Clout] FishNet NetworkManager created (singleplayer/host mode).");
        }

        private void EnsureNetworkBootstrapper()
        {
            if (FindAnyObjectByType<NetworkBootstrapper>() != null) return;

            GameObject go = new GameObject("[Clout] NetworkBootstrapper");
            var nb = go.AddComponent<NetworkBootstrapper>();
            nb.autoStartAsHost = autoStartAsHost;
            DontDestroyOnLoad(go);
        }
    }
}
