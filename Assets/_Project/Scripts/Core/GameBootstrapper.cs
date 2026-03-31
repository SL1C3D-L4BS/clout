using UnityEngine;

namespace Clout.Core
{
    /// <summary>
    /// Game entry point — ensures all singletons and essential managers exist.
    /// Phase 2 singleplayer — FishNet NetworkManager creation removed.
    /// FishNet bootstrapping will be restored in Phase 4 multiplayer.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
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
            Debug.Log("[Clout] Game bootstrapper initialized (singleplayer mode).");
        }
    }
}
