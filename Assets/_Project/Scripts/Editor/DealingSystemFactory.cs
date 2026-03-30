#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Clout.Core;
using Clout.Empire.Crafting;
using Clout.Empire.Dealing;
using Clout.World.NPCs;
using Clout.UI.Dealing;
using Clout.Player;

namespace Clout.Editor
{
    /// <summary>
    /// Creates all dealing system ScriptableObjects and wires NPCs into the test arena.
    ///
    /// Menu: Clout > Setup > Create Dealing System
    ///
    /// Creates:
    /// - 5 ProductDefinition SOs (Weed, Crystal, Powder, Pills, Shroom)
    /// - 2 SupplierDefinition SOs (Street Connect, Mid-Level Connect)
    /// - Adds DealManager + DealUI to the test arena
    /// - Spawns 3 CustomerAI NPCs with DealInteraction
    /// - Spawns 1 SupplierNPC
    /// - Adds ProductInventory to the player
    ///
    /// Menu: Clout > Spawn Dealing NPCs (adds to current scene)
    /// </summary>
    public static class DealingSystemFactory
    {
        private const string PRODUCT_PATH = "Assets/_Project/ScriptableObjects/Products";
        private const string SUPPLIER_PATH = "Assets/_Project/ScriptableObjects/Suppliers";

        // ═══════════════════════════════════════════════════════
        //  PRODUCT SOs
        // ═══════════════════════════════════════════════════════

        [MenuItem("Clout/Setup/Create Dealing System", false, 202)]
        public static void CreateDealingSystem()
        {
            CreateDealingSystemHeadless();

            EditorUtility.DisplayDialog("Clout — Dealing System",
                "Created dealing system assets:\n\n" +
                "Products:\n" +
                "• Weed (Cannabis)\n" +
                "• Crystal (Methamphetamine)\n" +
                "• Powder (Cocaine)\n" +
                "• Pills (MDMA)\n" +
                "• Shroom (LSD)\n\n" +
                "Suppliers:\n" +
                "• Lil' D — Street Connect\n" +
                "• Mr. Kim — Mid-Level Connect\n\n" +
                "Use 'Clout > Spawn Dealing NPCs' to add to current scene.",
                "Done");
        }

        public static void CreateDealingSystemHeadless()
        {
            EnsureFolder(PRODUCT_PATH);
            EnsureFolder(SUPPLIER_PATH);

            // Create products
            var weed = CreateProduct("Weed", "Locally grown. Low risk, low reward.",
                ProductType.Cannabis, 40f, 25f, 10f,
                0.4f, 0.6f, 120f,
                0.1f, 0.05f, 0.3f,
                new Color(0.2f, 0.7f, 0.2f));

            var crystal = CreateProduct("Crystal", "Cooked in labs. High risk, high reward.",
                ProductType.Methamphetamine, 120f, 80f, 40f,
                0.7f, 0.4f, 180f,
                0.5f, 0.1f, 0.6f,
                new Color(0.8f, 0.9f, 1f));

            var powder = CreateProduct("Powder", "Imported product. Premium clientele.",
                ProductType.Cocaine, 150f, 100f, 60f,
                0.6f, 0.7f, 90f,
                0.4f, 0.08f, 0.2f,
                Color.white);

            var pills = CreateProduct("Pills", "Party drug. Nightclub circuit.",
                ProductType.MDMA, 80f, 50f, 25f,
                0.3f, 0.8f, 240f,
                0.2f, 0.05f, 0.1f,
                new Color(1f, 0.4f, 0.7f));

            var shroom = CreateProduct("Shroom", "Grown in dark rooms. Niche market.",
                ProductType.LSD, 60f, 35f, 15f,
                0.2f, 0.7f, 300f,
                0.15f, 0.03f, 0.05f,
                new Color(0.6f, 0.4f, 0.8f));

            // Create suppliers
            CreateSupplier("Lil D", "Street-level connect. Unreliable but cheap.",
                0, 0.6f, 300f, 0.02f, new SupplierProduct[]
                {
                    new SupplierProduct { product = weed, maxQuantityPerRestock = 20, wholesalePrice = 25f, qualityFloor = 0.1f, qualityCeiling = 0.5f },
                    new SupplierProduct { product = pills, maxQuantityPerRestock = 10, wholesalePrice = 40f, qualityFloor = 0.1f, qualityCeiling = 0.4f }
                });

            CreateSupplier("Mr Kim", "Mid-level. Reliable, better quality. Needs CLOUT.",
                1, 0.85f, 600f, 0.05f, new SupplierProduct[]
                {
                    new SupplierProduct { product = weed, maxQuantityPerRestock = 40, wholesalePrice = 30f, qualityFloor = 0.3f, qualityCeiling = 0.7f },
                    new SupplierProduct { product = crystal, maxQuantityPerRestock = 15, wholesalePrice = 70f, qualityFloor = 0.3f, qualityCeiling = 0.6f },
                    new SupplierProduct { product = powder, maxQuantityPerRestock = 10, wholesalePrice = 90f, qualityFloor = 0.4f, qualityCeiling = 0.7f },
                    new SupplierProduct { product = pills, maxQuantityPerRestock = 20, wholesalePrice = 45f, qualityFloor = 0.3f, qualityCeiling = 0.6f }
                });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Clout] Dealing system assets created: 5 products, 2 suppliers.");
        }

        // ═══════════════════════════════════════════════════════
        //  SPAWN NPCs INTO CURRENT SCENE
        // ═══════════════════════════════════════════════════════

        [MenuItem("Clout/Spawn Dealing NPCs", false, 101)]
        public static void SpawnDealingNPCs()
        {
            SpawnDealingNPCsHeadless();
            EditorUtility.DisplayDialog("Clout — Dealing NPCs",
                "Spawned into current scene:\n\n" +
                "• 3 Customer NPCs (green — seeking product)\n" +
                "• 1 Supplier NPC (cyan — Lil' D)\n" +
                "• DealManager + DealUI\n" +
                "• ProductInventory on Player\n\n" +
                "Player starts with $500 + 5 Weed.\n" +
                "Approach green NPCs to deal. Approach cyan NPC to buy.",
                "Done");
        }

        public static void SpawnDealingNPCsHeadless()
        {
            // Ensure SOs exist
            string weedPath = $"{PRODUCT_PATH}/PROD_Weed.asset";
            if (!System.IO.File.Exists(weedPath))
                CreateDealingSystemHeadless();

            // Find player
            PlayerStateManager player = Object.FindAnyObjectByType<PlayerStateManager>();
            if (player == null)
            {
                Debug.LogWarning("[Clout] No player found in scene. Build test arena first.");
                return;
            }

            // Add ProductInventory to player if missing
            ProductInventory productInv = player.GetComponent<ProductInventory>();
            if (productInv == null)
                productInv = player.gameObject.AddComponent<ProductInventory>();

            // Give player starting cash and product
            player.cash = 500f;
            ProductDefinition weed = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Weed.asset");
            // Note: Runtime AddProduct happens at play time. We wire the reference.

            // Create DealManager
            GameObject dealManagerObj = new GameObject("DealManager");
            DealManager dealMgr = dealManagerObj.AddComponent<DealManager>();

            // Create DealUI
            DealUI dealUI = dealManagerObj.AddComponent<DealUI>();
            dealUI.dealManager = dealMgr;

            // Create SupplierUI
            SupplierUI supplierUI = dealManagerObj.AddComponent<SupplierUI>();

            // Dealing Bootstrapper — gives player starting product at runtime
            DealingBootstrapper bootstrapper = dealManagerObj.AddComponent<DealingBootstrapper>();
            ProductDefinition weedProd = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Weed.asset");
            ProductDefinition pillsProd = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Pills.asset");
            bootstrapper.startingProducts = new ProductDefinition[] { weedProd, pillsProd };
            bootstrapper.startingQuantities = new int[] { 10, 5 };
            bootstrapper.startingQualities = new float[] { 0.4f, 0.3f };
            bootstrapper.startingCash = 500f;

            // === CUSTOMER NPCs ===
            SpawnCustomerNPC("Customer_Stoner_Steve", new Vector3(8f, 0f, 6f),
                ProductType.Cannabis, 60f, 0.3f, 0.5f, 0.1f, new Color(0.3f, 0.8f, 0.3f));

            SpawnCustomerNPC("Customer_Party_Pete", new Vector3(-10f, 0f, 12f),
                ProductType.MDMA, 100f, 0.5f, 0.4f, 0.05f, new Color(0.3f, 0.9f, 0.5f));

            SpawnCustomerNPC("Customer_Wall_Street_Wendy", new Vector3(5f, 0f, 18f),
                ProductType.Cocaine, 200f, 0.7f, 0.3f, 0.0f, new Color(0.4f, 1f, 0.6f));

            // === SUPPLIER NPC ===
            SupplierDefinition supplierData = AssetDatabase.LoadAssetAtPath<SupplierDefinition>(
                $"{SUPPLIER_PATH}/SUPP_Lil D.asset");
            SpawnSupplierNPC("Supplier_Lil_D", new Vector3(-15f, 0f, -5f),
                supplierData, supplierUI, new Color(0.3f, 0.8f, 0.9f));

            // Wire SupplierUI into scene's supplier
            Debug.Log("[Clout] Dealing NPCs spawned: 3 customers + 1 supplier + DealManager.");
        }

        // ─── HELPERS ───────────────────────────────────────────

        private static ProductDefinition CreateProduct(string name, string desc,
            ProductType type, float streetValue, float bulkValue, float prodCost,
            float addiction, float satisfaction, float effectDur,
            float detectability, float weight, float odor,
            Color color)
        {
            string path = $"{PRODUCT_PATH}/PROD_{name}.asset";
            ProductDefinition existing = AssetDatabase.LoadAssetAtPath<ProductDefinition>(path);
            if (existing != null) return existing;

            ProductDefinition prod = ScriptableObject.CreateInstance<ProductDefinition>();
            prod.productName = name;
            prod.description = desc;
            prod.productType = type;
            prod.baseStreetValue = streetValue;
            prod.baseBulkValue = bulkValue;
            prod.productionCost = prodCost;
            prod.addictionRate = addiction;
            prod.satisfactionRate = satisfaction;
            prod.effectDuration = effectDur;
            prod.detectability = detectability;
            prod.weight = weight;
            prod.odor = odor;
            prod.productColor = color;

            // Standard quality tiers
            prod.qualityTiers = new QualityTier[]
            {
                new QualityTier { tierName = "Trash",   minQuality = 0f,   priceMultiplier = 0.5f, tierColor = Color.gray },
                new QualityTier { tierName = "Street",  minQuality = 0.2f, priceMultiplier = 1.0f, tierColor = Color.white },
                new QualityTier { tierName = "Mid",     minQuality = 0.4f, priceMultiplier = 1.5f, tierColor = Color.yellow },
                new QualityTier { tierName = "Fire",    minQuality = 0.6f, priceMultiplier = 2.5f, tierColor = new Color(1f, 0.5f, 0f) },
                new QualityTier { tierName = "Pure",    minQuality = 0.85f, priceMultiplier = 4.0f, tierColor = Color.cyan },
            };

            AssetDatabase.CreateAsset(prod, path);
            EditorUtility.SetDirty(prod);
            return prod;
        }

        private static SupplierDefinition CreateSupplier(string name, string desc,
            int requiredRank, float reliability, float restockInterval, float bustRisk,
            SupplierProduct[] catalog)
        {
            string safeName = name.Replace("'", "").Replace(" ", " ");
            string path = $"{SUPPLIER_PATH}/SUPP_{safeName}.asset";
            SupplierDefinition existing = AssetDatabase.LoadAssetAtPath<SupplierDefinition>(path);
            if (existing != null) return existing;

            SupplierDefinition supp = ScriptableObject.CreateInstance<SupplierDefinition>();
            supp.supplierName = name;
            supp.description = desc;
            supp.requiredCloutRank = requiredRank;
            supp.reliability = reliability;
            supp.restockInterval = restockInterval;
            supp.bustRisk = bustRisk;
            supp.catalog = catalog;

            AssetDatabase.CreateAsset(supp, path);
            EditorUtility.SetDirty(supp);
            return supp;
        }

        private static void SpawnCustomerNPC(string name, Vector3 position,
            ProductType preferred, float budget, float qualPref, float priceSens,
            float addiction, Color color)
        {
            // Check for boxMan
            GameObject boxManPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Placeholder/Models/boxMan.fbx");

            GameObject npc = new GameObject(name);
            npc.transform.position = position;
            npc.tag = "Civilian";

            // Model
            GameObject model = null;
            if (boxManPrefab != null)
            {
                model = (GameObject)PrefabUtility.InstantiatePrefab(boxManPrefab);
                model.name = "Model";
                model.transform.SetParent(npc.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
                foreach (Renderer r in renderers) r.sharedMaterial = mat;
            }
            else
            {
                model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                model.name = "Model";
                model.transform.SetParent(npc.transform);
                model.transform.localPosition = new Vector3(0, 1f, 0);
                Object.DestroyImmediate(model.GetComponent<Collider>());
                Renderer r = model.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
                r.sharedMaterial = mat;
            }

            // Collider
            CapsuleCollider col = npc.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1f, 0);
            col.radius = 0.35f;
            col.height = 2f;

            // NavMeshAgent for wandering
            NavMeshAgent agent = npc.AddComponent<NavMeshAgent>();
            agent.speed = 1.5f;
            agent.stoppingDistance = 0.5f;
            agent.radius = 0.4f;
            agent.height = 2f;

            // Customer AI
            CustomerAI customer = npc.AddComponent<CustomerAI>();
            customer.customerName = name.Replace("Customer_", "").Replace("_", " ");
            customer.preferredProduct = preferred;
            customer.maxWillingToPay = budget;
            customer.qualityPreference = qualPref;
            customer.pricesSensitivity = priceSens;
            customer.addictionLevel = addiction;
            customer.purchaseInterval = 60f; // For testing — faster cycle
            customer.currentState = CustomerState.Seeking; // Start seeking immediately

            // Deal Interaction
            DealInteraction dealInteract = npc.AddComponent<DealInteraction>();

            // Seeking indicator (floating sphere above head)
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "SeekingIndicator";
            indicator.transform.SetParent(npc.transform);
            indicator.transform.localPosition = new Vector3(0, 2.5f, 0);
            indicator.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            Object.DestroyImmediate(indicator.GetComponent<Collider>());
            Renderer ir = indicator.GetComponent<Renderer>();
            Material im = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            im.color = Color.green;
            im.SetFloat("_Surface", 1); // Transparent
            ir.sharedMaterial = im;
            dealInteract.seekingIndicator = indicator;

            Debug.Log($"[Clout] Customer NPC spawned: {customer.customerName} at {position}");
        }

        private static void SpawnSupplierNPC(string name, Vector3 position,
            SupplierDefinition data, SupplierUI ui, Color color)
        {
            GameObject boxManPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Placeholder/Models/boxMan.fbx");

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
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
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
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
                r.sharedMaterial = mat;
            }

            // Collider
            CapsuleCollider col = npc.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1f, 0);
            col.radius = 0.35f;
            col.height = 2f;

            // Supplier NPC
            SupplierNPC supplier = npc.AddComponent<SupplierNPC>();
            supplier.supplierData = data;

            // Wire UI
            if (ui != null)
                supplier.OnSupplierOpened += ui.Open;

            // Indicator — cyan diamond above head
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "SupplierIndicator";
            indicator.transform.SetParent(npc.transform);
            indicator.transform.localPosition = new Vector3(0, 2.5f, 0);
            indicator.transform.localScale = new Vector3(0.3f, 0.45f, 0.3f);
            Object.DestroyImmediate(indicator.GetComponent<Collider>());
            Renderer ir = indicator.GetComponent<Renderer>();
            Material im = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            im.color = new Color(0, 1f, 1f);
            ir.sharedMaterial = im;

            Debug.Log($"[Clout] Supplier NPC spawned: {data?.supplierName ?? name} at {position}");
        }

        // ─── STARTUP PRODUCT FOR TESTING ───────────────────────

        /// <summary>
        /// Runtime helper — gives the player starting product for testing.
        /// Call from a MonoBehaviour.Start() or add to GameBootstrapper.
        /// </summary>
        [MenuItem("Clout/Debug/Give Player Starting Product", false, 300)]
        public static void GivePlayerStartingProduct()
        {
            PlayerStateManager player = Object.FindAnyObjectByType<PlayerStateManager>();
            if (player == null) { Debug.Log("[Clout] No player found."); return; }

            ProductInventory inv = player.GetComponent<ProductInventory>();
            if (inv == null) inv = player.gameObject.AddComponent<ProductInventory>();

            ProductDefinition weed = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Weed.asset");
            ProductDefinition pills = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Pills.asset");

            if (weed != null) inv.AddProduct(weed, 10, 0.4f);
            if (pills != null) inv.AddProduct(pills, 5, 0.3f);

            player.cash = 500f;

            Debug.Log("[Clout] Player given: 10x Weed (Street), 5x Pills (Street), $500 cash.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
