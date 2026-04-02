#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Clout.Core;
using Clout.Player;
using Clout.Empire.Economy;
using Clout.Empire.Crafting;
using Clout.World.NPCs;
using Clout.UI.Economy;

namespace Clout.Editor
{
    /// <summary>
    /// Creates economy system components and spawns ShopKeeper NPCs into the test arena.
    ///
    /// Menu: Clout > Setup > Create Economy System (creates SOs if needed)
    /// Menu: Clout > Spawn Shop NPCs (adds shops to current scene)
    ///
    /// Creates:
    /// - 1 Ingredient Supplier shop (sells crafting ingredients)
    /// - 1 General Store (sells consumables/tools)
    /// - 1 Fence Shop (buys product at wholesale)
    /// - ShopUI overlay (auto-subscribes to all shops)
    /// - Wires CashManager + TransactionLedger if not in scene
    /// </summary>
    public static class EconomySystemFactory
    {
        private const string INGREDIENT_PATH = "Assets/_Project/ScriptableObjects/Ingredients";

        [MenuItem("Clout/Setup/Create Economy System", false, 204)]
        public static void CreateEconomySystem()
        {
            CreateEconomySystemHeadless();

            EditorUtility.DisplayDialog("Clout — Economy System",
                "Economy system wired into scene:\n\n" +
                "Shops:\n" +
                "• Chemo's Supply — Ingredient supplier (orange)\n" +
                "• Pawn It — General store (white)\n" +
                "• Big Tony's — Fence / product buyer (magenta)\n\n" +
                "Systems:\n" +
                "• CashManager (dirty/clean wallet)\n" +
                "• TransactionLedger (financial records)\n" +
                "• ShopUI (auto-subscribes to all shops)\n\n" +
                "Approach shop NPCs and press [E] to trade.",
                "Done");
        }

        public static void CreateEconomySystemHeadless()
        {
            // Ensure economy singleton exists in scene
            EnsureEconomySingleton();

            // Spawn shops
            SpawnShopNPCs();
        }

        [MenuItem("Clout/Spawn Shop NPCs", false, 102)]
        public static void SpawnShopNPCsMenu()
        {
            EnsureEconomySingleton();
            SpawnShopNPCs();

            EditorUtility.DisplayDialog("Clout — Shop NPCs",
                "Spawned 3 shop NPCs into current scene:\n\n" +
                "• Chemo's Supply (orange) — Ingredients\n" +
                "• Pawn It (white) — General goods\n" +
                "• Big Tony's (magenta) — Fence\n\n" +
                "Approach and press [E] to interact.",
                "Done");
        }

        // ═══════════════════════════════════════════════════════
        //  ECONOMY SINGLETON
        // ═══════════════════════════════════════════════════════

        private static void EnsureEconomySingleton()
        {
            // CashManager
            if (Object.FindAnyObjectByType<CashManager>() == null)
            {
                GameObject econObj = GameObject.Find("EconomySystem");
                if (econObj == null)
                    econObj = new GameObject("EconomySystem");

                econObj.AddComponent<CashManager>();
                econObj.AddComponent<TransactionLedger>();
                econObj.AddComponent<EconomyManager>();

                Debug.Log("[Clout] Economy singleton created: CashManager + TransactionLedger + EconomyManager.");
            }

            // ShopUI
            if (Object.FindAnyObjectByType<ShopUI>() == null)
            {
                GameObject shopUIObj = new GameObject("ShopUI");
                shopUIObj.AddComponent<ShopUI>();
                Debug.Log("[Clout] ShopUI created.");
            }
        }

        // ═══════════════════════════════════════════════════════
        //  SPAWN SHOP NPCs
        // ═══════════════════════════════════════════════════════

        private static void SpawnShopNPCs()
        {
            // Ensure production system SOs exist (for ingredient references)
            string pseudoPath = $"{INGREDIENT_PATH}/ING_Pseudoephedrine.asset";
            if (!System.IO.File.Exists(pseudoPath))
            {
                ProductionSystemFactory.CreateProductionSystemHeadless();
            }

            // Load ingredient SOs for shop listings
            IngredientDefinition pseudo = AssetDatabase.LoadAssetAtPath<IngredientDefinition>(
                $"{INGREDIENT_PATH}/ING_Pseudoephedrine.asset");
            IngredientDefinition redPhos = AssetDatabase.LoadAssetAtPath<IngredientDefinition>(
                $"{INGREDIENT_PATH}/ING_Red Phosphorus.asset");
            IngredientDefinition bakingSoda = AssetDatabase.LoadAssetAtPath<IngredientDefinition>(
                $"{INGREDIENT_PATH}/ING_Baking Soda.asset");
            IngredientDefinition acetone = AssetDatabase.LoadAssetAtPath<IngredientDefinition>(
                $"{INGREDIENT_PATH}/ING_Acetone.asset");
            IngredientDefinition cocaPaste = AssetDatabase.LoadAssetAtPath<IngredientDefinition>(
                $"{INGREDIENT_PATH}/ING_Coca Paste.asset");
            IngredientDefinition seeds = AssetDatabase.LoadAssetAtPath<IngredientDefinition>(
                $"{INGREDIENT_PATH}/ING_Cannabis Seeds.asset");
            IngredientDefinition nutrients = AssetDatabase.LoadAssetAtPath<IngredientDefinition>(
                $"{INGREDIENT_PATH}/ING_Nutrient Solution.asset");

            // ─── 1. INGREDIENT SUPPLIER ────────────────────────
            SpawnShopKeeper(
                "Shop_Chemos_Supply",
                new Vector3(-20f, 0f, -10f),
                new Color(1f, 0.6f, 0.2f), // Orange
                "Chemo's Supply",
                ShopType.IngredientSupplier,
                new ShopListing[]
                {
                    MakeListing("pseudoephedrine", "Pseudoephedrine", pseudo?.basePurchasePrice ?? 15f, 20, 0.3f, "Cold medicine base. Essential for crystal."),
                    MakeListing("red_phosphorus", "Red Phosphorus", redPhos?.basePurchasePrice ?? 25f, 10, 0.2f, "Volatile catalyst. Handle with care."),
                    MakeListing("baking_soda", "Baking Soda", bakingSoda?.basePurchasePrice ?? 5f, 50, 0.5f, "Cheap cutting agent. Stretches product."),
                    MakeListing("acetone", "Acetone", acetone?.basePurchasePrice ?? 12f, 15, 0.3f, "Industrial solvent. Many uses."),
                    MakeListing("coca_paste", "Coca Paste", cocaPaste?.basePurchasePrice ?? 80f, 5, 0.1f, "Imported base. Premium ingredient."),
                    MakeListing("cannabis_seeds", "Cannabis Seeds", seeds?.basePurchasePrice ?? 10f, 30, 0.4f, "Grow your own. Patience required."),
                    MakeListing("nutrient_solution", "Nutrient Solution", nutrients?.basePurchasePrice ?? 8f, 25, 0.4f, "Hydroponic feed. Boosts yield."),
                }
            );

            // ─── 2. GENERAL STORE ──────────────────────────────
            SpawnShopKeeper(
                "Shop_Pawn_It",
                new Vector3(20f, 0f, -10f),
                new Color(0.9f, 0.9f, 0.9f), // White
                "Pawn It",
                ShopType.GeneralStore,
                new ShopListing[]
                {
                    MakeListing("burner_phone", "Burner Phone", 50f, 10, 0.2f, "Untraceable. Good for one-time deals."),
                    MakeListing("lockpick_set", "Lockpick Set", 75f, 5, 0.1f, "Gets you into places you shouldn't be."),
                    MakeListing("bandages", "Bandages", 20f, 20, 0.5f, "Basic first aid. Heals minor wounds."),
                    MakeListing("energy_drink", "Energy Drink", 10f, 30, 0.6f, "Temporary stamina boost."),
                    MakeListing("duffle_bag", "Duffle Bag", 100f, 3, 0.1f, "Increases carry capacity."),
                }
            );

            // ─── 3. FENCE SHOP ─────────────────────────────────
            SpawnShopKeeper(
                "Shop_Big_Tonys",
                new Vector3(0f, 0f, -20f),
                new Color(0.9f, 0.3f, 0.7f), // Magenta
                "Big Tony's",
                ShopType.FenceShop,
                new ShopListing[0], // Fence doesn't sell — only buys
                0.55f // 55% of street value
            );

            Debug.Log("[Clout] Shop NPCs spawned: Chemo's Supply + Pawn It + Big Tony's.");
        }

        // ═══════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════

        private static ShopListing MakeListing(string itemId, string displayName,
            float price, int maxStock, float restockRate, string description)
        {
            return new ShopListing
            {
                itemId = itemId,
                displayName = displayName,
                price = price,
                maxStock = maxStock,
                restockRate = restockRate,
                description = description
            };
        }

        private static void SpawnShopKeeper(string name, Vector3 position, Color color,
            string shopName, ShopType shopType, ShopListing[] listings,
            float fenceBuyRate = 0.5f)
        {
            GameObject boxManPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Placeholder/Models/boxMan.fbx");

            GameObject npc = new GameObject(name);
            npc.transform.position = position;

            // Model
            if (boxManPrefab != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(boxManPrefab);
                model.name = "Model";
                model.transform.SetParent(npc.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                Material mat = EditorShaderHelper.CreateMaterial(color);
                foreach (Renderer r in renderers) r.sharedMaterial = mat;
            }
            else
            {
                GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                model.name = "Model";
                model.transform.SetParent(npc.transform);
                model.transform.localPosition = new Vector3(0, 1f, 0);
                Object.DestroyImmediate(model.GetComponent<Collider>());
                Renderer r = model.GetComponent<Renderer>();
                Material mat = EditorShaderHelper.CreateMaterial(color);
                r.sharedMaterial = mat;
            }

            // Collider
            CapsuleCollider col = npc.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1f, 0);
            col.radius = 0.35f;
            col.height = 2f;

            // ShopKeeper component
            ShopKeeper shop = npc.AddComponent<ShopKeeper>();
            shop.shopName = shopName;
            shop.shopType = shopType;
            shop.listings = listings;
            shop.fenceBuyRate = fenceBuyRate;
            shop.restockInterval = 120f; // 2 min for testing (faster cycle)

            // Shop type indicator above head
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.name = "ShopIndicator";
            indicator.transform.SetParent(npc.transform);
            indicator.transform.localPosition = new Vector3(0, 2.4f, 0);
            indicator.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            indicator.transform.localRotation = Quaternion.Euler(45f, 45f, 0f); // Diamond shape
            Object.DestroyImmediate(indicator.GetComponent<Collider>());
            Renderer ir = indicator.GetComponent<Renderer>();
            Material im = EditorShaderHelper.CreateMaterial(color);
            ir.sharedMaterial = im;

            Debug.Log($"[Clout] Shop NPC spawned: {shopName} ({shopType}) at {position}");
        }
    }
}
#endif
