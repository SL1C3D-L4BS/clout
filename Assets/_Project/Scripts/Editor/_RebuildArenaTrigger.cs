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
                Debug.Log("[RebuildArena] 1/5 — Building Test Arena...");
                TestArenaBuilder.BuildTestArenaHeadless();

                Debug.Log("[RebuildArena] 2/5 — Spawning Economy System (shops)...");
                EconomySystemFactory.CreateEconomySystemHeadless();

                Debug.Log("[RebuildArena] 3/5 — Spawning Dealing NPCs...");
                DealingSystemFactory.SpawnDealingNPCsHeadless();

                Debug.Log("[RebuildArena] 4/5 — Spawning Crafting Stations...");
                ProductionSystemFactory.SpawnCraftingStationsHeadless();

                Debug.Log("[RebuildArena] 5/5 — Saving scene...");
                EditorSceneManager.SaveOpenScenes();

                Debug.Log("[RebuildArena] DONE. Press Play — WASD, E=interact, shops are live.");
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
