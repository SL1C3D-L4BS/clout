#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Clout.Editor
{
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
            Debug.Log("[Clout] _RebuildArenaTrigger: rebuilding (Police tag fix + Phone UI)...");
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
                }
            }
        }
    }
}
#endif
