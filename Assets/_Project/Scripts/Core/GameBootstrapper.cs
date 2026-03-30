using UnityEngine;
using Clout.Network;

namespace Clout.Core
{
    /// <summary>
    /// Game entry point — ensures all singletons and essential managers exist.
    /// Runs before any other MonoBehaviour via Script Execution Order or [RuntimeInitializeOnLoadMethod].
    ///
    /// Clout architecture: scene-agnostic bootstrapper that creates persistent managers
    /// for networking, audio, save/load, and game state.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Bootstrap Config")]
        [SerializeField] private bool ensureNetwork = true;
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

            if (ensureNetwork)
                EnsureNetworkBootstrapper();

            Debug.Log("[Clout] Game bootstrapper initialized.");
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
