#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Clout.Editor
{
    [InitializeOnLoad]
    public static class _RebuildArenaTrigger
    {
        static _RebuildArenaTrigger()
        {
            EditorApplication.delayCall += Run;
        }

        private static void Run()
        {
            EditorApplication.delayCall -= Run;

            try
            {
                Debug.Log("[RebuildArena] 1/4 — Building clean Test Arena...");
                TestArenaBuilder.BuildTestArenaHeadless();

                Debug.Log("[RebuildArena] 2/4 — Spawning Dealing NPCs...");
                DealingSystemFactory.SpawnDealingNPCsHeadless();

                Debug.Log("[RebuildArena] 3/4 — Spawning Crafting Stations...");
                ProductionSystemFactory.SpawnCraftingStationsHeadless();

                Debug.Log("[RebuildArena] 4/4 — Saving scene...");
                EditorSceneManager.SaveOpenScenes();

                Debug.Log("[RebuildArena] DONE. Humanoid avatar active. Press Play.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RebuildArena] Failed: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                string path = "Assets/_Project/Scripts/Editor/_RebuildArenaTrigger.cs";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    System.IO.File.Delete(path + ".meta");
                    AssetDatabase.Refresh();
                }
            }
        }
    }
}
#endif
