using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Clout.Core
{
    /// <summary>
    /// Sits in Bootstrap.unity. Loads Main.unity additively once the network layer is ready.
    ///
    /// Bootstrap is index 0 — Unity loads it first and it persists forever.
    /// Main is index 1 — loaded additively on top so Bootstrap managers remain alive.
    /// </summary>
    public class BootstrapSceneLoader : MonoBehaviour
    {
        [Header("Scene Config")]
        [Tooltip("Scene name to load additively after bootstrap. Must be in Build Settings.")]
        public string mainSceneName = "Main";

        [Tooltip("Seconds to wait before loading (gives NetworkManager time to initialize).")]
        public float loadDelay = 0.1f;

        private void Start()
        {
            // Don't load if Main is already loaded (e.g., playing from Main scene in Editor)
            Scene main = SceneManager.GetSceneByName(mainSceneName);
            if (main.isLoaded) return;

            StartCoroutine(LoadMainScene());
        }

        private IEnumerator LoadMainScene()
        {
            yield return new WaitForSeconds(loadDelay);

            AsyncOperation op = SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Additive);
            op.allowSceneActivation = true;

            yield return op;

            // Set Main as the active scene so new GameObjects go there
            Scene loaded = SceneManager.GetSceneByName(mainSceneName);
            if (loaded.IsValid())
            {
                SceneManager.SetActiveScene(loaded);
                Debug.Log($"[Clout] '{mainSceneName}' loaded additively and set active.");
            }
        }
    }
}
