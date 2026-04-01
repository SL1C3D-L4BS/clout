#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Clout.Editor
{
    /// <summary>
    /// Self-deleting trigger — rebuilds the test arena with all Phase 2 systems
    /// including Step 6 Worker System (WorkerManager, RecruitmentManager, HireUI,
    /// WorkerManagementUI, DealerAI, CookAI, GuardAI).
    /// </summary>
    [InitializeOnLoad]
    public static class _RebuildArenaTrigger
    {
        static _RebuildArenaTrigger()
        {
            EditorApplication.delayCall += RunOnce;
        }

        private static void RunOnce()
        {
            EditorApplication.delayCall -= RunOnce;

            Debug.Log("[Clout] _RebuildArenaTrigger: rebuilding test arena (Step 6 Worker System)...");

            try
            {
                TestArenaBuilder.BuildTestArenaHeadless();
                Debug.Log("[Clout] _RebuildArenaTrigger: arena rebuild complete.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Clout] _RebuildArenaTrigger failed: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                string path = "Assets/_Project/Scripts/Editor/_RebuildArenaTrigger.cs";
                if (System.IO.File.Exists(path))
                {
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.Refresh();
                    Debug.Log("[Clout] _RebuildArenaTrigger: trigger script removed.");
                }
            }
        }
    }
}
#endif
