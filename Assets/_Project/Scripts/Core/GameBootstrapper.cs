using UnityEngine;
using Clout.Empire.Economy;
using Clout.Empire.Economy.Laundering;
using Clout.Forensics;
using Clout.UI.Economy;
using Clout.UI.Forensics;
using Clout.UI.Laundering;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void PostSceneBootstrap()
        {
            // Ensure Phase 3+ systems exist after scene fully loaded
            EnsurePhase3Systems();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("[Clout] Game bootstrapper initialized (singleplayer mode).");
        }

        /// <summary>
        /// Auto-create Phase 3 managers if they don't exist in the current scene.
        /// Allows scenes built before Step 11 to still function without rebuilding.
        /// </summary>
        private static void EnsurePhase3Systems()
        {
            // Step 11: Money Laundering Pipeline
            if (LaunderingManager.Instance == null && FindAnyObjectByType<LaunderingManager>() == null)
            {
                GameObject launderObj = new GameObject("[Clout] LaunderingSystem");
                launderObj.AddComponent<LaunderingManager>();
                launderObj.AddComponent<LaunderingUI>();
                Debug.Log("[Clout] Auto-created LaunderingManager + LaunderingUI (Phase 3 Step 11).");
            }
            else if (FindAnyObjectByType<LaunderingUI>() == null)
            {
                var mgr = FindAnyObjectByType<LaunderingManager>();
                if (mgr != null)
                {
                    mgr.gameObject.AddComponent<LaunderingUI>();
                    Debug.Log("[Clout] Auto-created LaunderingUI on existing LaunderingManager.");
                }
            }

            // Step 12: Signature & Forensics System
            if (SignatureDatabase.Instance == null && FindAnyObjectByType<SignatureDatabase>() == null)
            {
                GameObject forensicObj = new GameObject("[Clout] ForensicsSystem");
                forensicObj.AddComponent<SignatureDatabase>();
                forensicObj.AddComponent<ForensicLabAI>();
                forensicObj.AddComponent<ForensicsUI>();
                Debug.Log("[Clout] Auto-created SignatureDatabase + ForensicLabAI + ForensicsUI (Phase 3 Step 12).");
            }
            else
            {
                // Ensure sub-components exist even if database was already created
                var db = FindAnyObjectByType<SignatureDatabase>();
                if (db != null)
                {
                    if (FindAnyObjectByType<ForensicLabAI>() == null)
                        db.gameObject.AddComponent<ForensicLabAI>();
                    if (FindAnyObjectByType<ForensicsUI>() == null)
                        db.gameObject.AddComponent<ForensicsUI>();
                }
            }

            // Step 13: Advanced Economy & Market Simulator
            if (MarketSimulator.Instance == null && FindAnyObjectByType<MarketSimulator>() == null)
            {
                GameObject marketObj = new GameObject("[Clout] MarketSystem");
                marketObj.AddComponent<MarketSimulator>();
                marketObj.AddComponent<CommodityTracker>();
                marketObj.AddComponent<MarketAnalysisUI>();
                Debug.Log("[Clout] Auto-created MarketSimulator + CommodityTracker + MarketAnalysisUI (Phase 3 Step 13).");
            }
            else
            {
                var sim = FindAnyObjectByType<MarketSimulator>();
                if (sim != null)
                {
                    if (FindAnyObjectByType<CommodityTracker>() == null)
                        sim.gameObject.AddComponent<CommodityTracker>();
                    if (FindAnyObjectByType<MarketAnalysisUI>() == null)
                        sim.gameObject.AddComponent<MarketAnalysisUI>();
                }
            }
        }
    }
}
