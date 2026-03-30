#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Clout.Core;
using Clout.Empire.Crafting;
using Clout.Empire.Dealing;
using Clout.Player;
using Clout.UI.Production;

namespace Clout.Editor
{
    /// <summary>
    /// Creates all production system ScriptableObjects and wires stations into the test arena.
    ///
    /// Menu: Clout > Setup > Create Production System
    ///
    /// Creates:
    /// - 8 IngredientDefinition SOs (base chemicals, additives, cutting agents)
    /// - 5 RecipeDefinition SOs (one per product type)
    /// - Editor tool to spawn crafting stations into scenes
    ///
    /// Menu: Clout > Spawn Crafting Stations (adds to current scene)
    /// </summary>
    public static class ProductionSystemFactory
    {
        private const string INGREDIENT_PATH = "Assets/_Project/ScriptableObjects/Ingredients";
        private const string RECIPE_PATH = "Assets/_Project/ScriptableObjects/Recipes";
        private const string PRODUCT_PATH = "Assets/_Project/ScriptableObjects/Products";

        // ═══════════════════════════════════════════════════════
        //  CREATE ALL PRODUCTION SOs
        // ═══════════════════════════════════════════════════════

        [MenuItem("Clout/Setup/Create Production System", false, 203)]
        public static void CreateProductionSystem()
        {
            CreateProductionSystemHeadless();

            EditorUtility.DisplayDialog("Clout — Production System",
                "Created production system assets:\n\n" +
                "Ingredients:\n" +
                "• Pseudoephedrine (Base Chemical)\n" +
                "• Red Phosphorus (Catalyst)\n" +
                "• Iodine (Catalyst)\n" +
                "• Baking Soda (Cutting Agent)\n" +
                "• Coca Paste (Base Chemical)\n" +
                "• Acetone (Solvent/Catalyst)\n" +
                "• Cannabis Seeds (Agricultural)\n" +
                "• Nutrient Solution (Additive)\n\n" +
                "Recipes:\n" +
                "• Basic Weed Grow (Cannabis)\n" +
                "• Crystal Cook (Methamphetamine)\n" +
                "• Powder Process (Cocaine)\n" +
                "• Press Pills (MDMA)\n" +
                "• Grow Shrooms (LSD)\n\n" +
                "Use 'Clout > Spawn Crafting Stations' to add to current scene.",
                "Done");
        }

        public static void CreateProductionSystemHeadless()
        {
            EnsureFolder(INGREDIENT_PATH);
            EnsureFolder(RECIPE_PATH);

            // ── Ingredients ──────────────────────────────────────

            // Base chemicals
            var pseudo = CreateIngredient("Pseudoephedrine", "Cold medicine extract. Core meth precursor.",
                IngredientCategory.BaseChemical, 1.2f, 0.3f, 720f, 15f, 25f, false, false,
                new Color(0.9f, 0.9f, 0.95f),
                new AdditiveEffect[] {
                    new AdditiveEffect { effectName = "Purity Boost", qualityModifier = 0.1f, valueModifier = 0.15f }
                });

            var redPhos = CreateIngredient("Red Phosphorus", "Industrial chemical. Dangerous catalyst.",
                IngredientCategory.Catalyst, 1.5f, 0.7f, 2160f, 25f, 40f, false, false,
                new Color(0.8f, 0.2f, 0.2f),
                new AdditiveEffect[] {
                    new AdditiveEffect { effectName = "Reaction Catalyst", qualityModifier = 0.05f, toxicityModifier = 0.1f }
                });

            var iodine = CreateIngredient("Iodine", "Medical-grade iodine. Reaction catalyst.",
                IngredientCategory.Catalyst, 1.3f, 0.2f, 4320f, 12f, 20f, true, false,
                new Color(0.4f, 0.2f, 0.1f),
                new AdditiveEffect[] {
                    new AdditiveEffect { effectName = "Stabilizer", qualityModifier = 0.03f }
                });

            var bakingSoda = CreateIngredient("Baking Soda", "Common cutting agent. Stretches product, kills quality.",
                IngredientCategory.Cutting, 0.3f, 0.0f, 8760f, 2f, 3f, true, false,
                Color.white,
                new AdditiveEffect[] {
                    new AdditiveEffect { effectName = "Cut Product", qualityModifier = -0.2f, valueModifier = -0.3f }
                });

            var cocaPaste = CreateIngredient("Coca Paste", "Imported base material. Foundation for cocaine processing.",
                IngredientCategory.BaseChemical, 1.0f, 0.1f, 336f, 80f, 120f, false, false,
                new Color(0.9f, 0.85f, 0.7f),
                new AdditiveEffect[] {
                    new AdditiveEffect { effectName = "Base Potency", qualityModifier = 0.08f, valueModifier = 0.1f }
                });

            var acetone = CreateIngredient("Acetone", "Industrial solvent. Speeds reactions, increases risk.",
                IngredientCategory.Catalyst, 1.4f, 0.5f, 4320f, 8f, 15f, true, false,
                new Color(0.8f, 0.8f, 0.9f),
                new AdditiveEffect[] {
                    new AdditiveEffect { effectName = "Speed Catalyst", qualityModifier = 0.02f, toxicityModifier = 0.05f }
                });

            var cannabisSeeds = CreateIngredient("Cannabis Seeds", "High-yield strain seeds. Requires grow time.",
                IngredientCategory.Agricultural, 0.8f, 0.0f, 8760f, 10f, 15f, false, true,
                new Color(0.3f, 0.5f, 0.2f),
                new AdditiveEffect[] {
                    new AdditiveEffect { effectName = "Organic Base" }
                });

            var nutrients = CreateIngredient("Nutrient Solution", "Hydroponic growth enhancer. Improves yield quality.",
                IngredientCategory.Additive, 1.1f, 0.0f, 2160f, 5f, 8f, true, false,
                new Color(0.2f, 0.7f, 0.3f),
                new AdditiveEffect[] {
                    new AdditiveEffect { effectName = "Growth Enhancer", qualityModifier = 0.1f, valueModifier = 0.05f }
                });

            // ── Recipes ──────────────────────────────────────────

            // Load product SOs (created by DealingSystemFactory)
            var weedProd = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Weed.asset");
            var crystalProd = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Crystal.asset");
            var powderProd = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Powder.asset");
            var pillsProd = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Pills.asset");
            var shroomProd = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Shroom.asset");

            // Warn if products don't exist
            if (weedProd == null)
            {
                Debug.LogWarning("[Clout] Product SOs not found. Run 'Clout > Setup > Create Dealing System' first.");
                // Create them if possible
                DealingSystemFactory.CreateDealingSystemHeadless();
                weedProd = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Weed.asset");
                crystalProd = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Crystal.asset");
                powderProd = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Powder.asset");
                pillsProd = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Pills.asset");
                shroomProd = AssetDatabase.LoadAssetAtPath<ProductDefinition>($"{PRODUCT_PATH}/PROD_Shroom.asset");
            }

            // Recipe: Basic Weed Grow
            CreateRecipe("Basic Weed Grow",
                "Grow cannabis from seeds. Low risk, low skill, moderate time.",
                ProductType.Cannabis, weedProd,
                new IngredientSlot[] {
                    new IngredientSlot { ingredient = cannabisSeeds, quantity = 2 }
                },
                new IngredientSlot[] {
                    new IngredientSlot { ingredient = nutrients, quantity = 1 }
                },
                outputQuantity: 8, baseQuality: 0.35f,
                craftingTime: 30f, requiredSkill: 0,
                PropertyType.Lab, // Growhouse ideally, but Lab works for Phase 2
                explosionRisk: 0f, fumeRisk: 0.05f,
                marketValueMult: 1f);

            // Recipe: Crystal Cook
            CreateRecipe("Crystal Cook",
                "Cook methamphetamine. High risk, high reward. Requires chemistry knowledge.",
                ProductType.Methamphetamine, crystalProd,
                new IngredientSlot[] {
                    new IngredientSlot { ingredient = pseudo, quantity = 3 },
                    new IngredientSlot { ingredient = redPhos, quantity = 1 },
                    new IngredientSlot { ingredient = iodine, quantity = 1 }
                },
                new IngredientSlot[] {
                    new IngredientSlot { ingredient = acetone, quantity = 1 },
                    new IngredientSlot { ingredient = bakingSoda, quantity = 2 }
                },
                outputQuantity: 5, baseQuality: 0.4f,
                craftingTime: 45f, requiredSkill: 1,
                PropertyType.Lab,
                explosionRisk: 0.08f, fumeRisk: 0.15f,
                marketValueMult: 1.2f);

            // Recipe: Powder Process
            CreateRecipe("Powder Process",
                "Refine coca paste into cocaine. Premium product, premium ingredients.",
                ProductType.Cocaine, powderProd,
                new IngredientSlot[] {
                    new IngredientSlot { ingredient = cocaPaste, quantity = 2 },
                    new IngredientSlot { ingredient = acetone, quantity = 2 }
                },
                new IngredientSlot[] {
                    new IngredientSlot { ingredient = bakingSoda, quantity = 3 }
                },
                outputQuantity: 4, baseQuality: 0.45f,
                craftingTime: 35f, requiredSkill: 1,
                PropertyType.Lab,
                explosionRisk: 0.02f, fumeRisk: 0.1f,
                marketValueMult: 1.3f);

            // Recipe: Press Pills
            CreateRecipe("Press Pills",
                "Press MDMA tablets. Party circuit staple. Clean and consistent.",
                ProductType.MDMA, pillsProd,
                new IngredientSlot[] {
                    new IngredientSlot { ingredient = pseudo, quantity = 2 },
                    new IngredientSlot { ingredient = iodine, quantity = 2 }
                },
                new IngredientSlot[] {
                    new IngredientSlot { ingredient = nutrients, quantity = 1 }
                },
                outputQuantity: 10, baseQuality: 0.38f,
                craftingTime: 25f, requiredSkill: 0,
                PropertyType.Lab,
                explosionRisk: 0.01f, fumeRisk: 0.05f,
                marketValueMult: 1.1f);

            // Recipe: Grow Shrooms
            CreateRecipe("Grow Shrooms",
                "Cultivate psilocybin mushrooms. Low risk, niche market. Patience required.",
                ProductType.LSD, shroomProd,
                new IngredientSlot[] {
                    new IngredientSlot { ingredient = cannabisSeeds, quantity = 1 }, // Spore substitute
                    new IngredientSlot { ingredient = nutrients, quantity = 2 }
                },
                null, // No optional additives
                outputQuantity: 6, baseQuality: 0.5f,
                craftingTime: 40f, requiredSkill: 0,
                PropertyType.Lab,
                explosionRisk: 0f, fumeRisk: 0.02f,
                marketValueMult: 1f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Clout] Production system assets created: 8 ingredients, 5 recipes.");
        }

        // ═══════════════════════════════════════════════════════
        //  SPAWN CRAFTING STATIONS INTO CURRENT SCENE
        // ═══════════════════════════════════════════════════════

        [MenuItem("Clout/Spawn Crafting Stations", false, 102)]
        public static void SpawnCraftingStations()
        {
            SpawnCraftingStationsHeadless();
            EditorUtility.DisplayDialog("Clout — Crafting Stations",
                "Spawned into current scene:\n\n" +
                "• 1 Lab Table (all 5 recipes)\n" +
                "• ProductionManager\n" +
                "• CraftingUI\n" +
                "• IngredientInventory on Player\n" +
                "• Starting ingredients given via bootstrapper\n\n" +
                "Approach the cyan station marker to cook.\n" +
                "Press [E] to interact.",
                "Done");
        }

        public static void SpawnCraftingStationsHeadless()
        {
            // Ensure SOs exist
            string ingredientPath = $"{INGREDIENT_PATH}/ING_Pseudoephedrine.asset";
            if (!System.IO.File.Exists(ingredientPath))
                CreateProductionSystemHeadless();

            // Find player
            PlayerStateManager player = Object.FindAnyObjectByType<PlayerStateManager>();
            if (player == null)
            {
                Debug.LogWarning("[Clout] No player found in scene. Build test arena first.");
                return;
            }

            // Add IngredientInventory to player if missing
            IngredientInventory ingredientInv = player.GetComponent<IngredientInventory>();
            if (ingredientInv == null)
                ingredientInv = player.gameObject.AddComponent<IngredientInventory>();

            // Ensure ProductInventory exists too
            ProductInventory productInv = player.GetComponent<ProductInventory>();
            if (productInv == null)
                productInv = player.gameObject.AddComponent<ProductInventory>();

            // Create ProductionManager
            GameObject pmObj = new GameObject("ProductionManager");
            ProductionManager pm = pmObj.AddComponent<ProductionManager>();

            // Create CraftingUI
            CraftingUI craftingUI = pmObj.AddComponent<CraftingUI>();
            craftingUI.productionManager = pm;

            // Load all recipe SOs
            var weedRecipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>($"{RECIPE_PATH}/RCP_Basic Weed Grow.asset");
            var crystalRecipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>($"{RECIPE_PATH}/RCP_Crystal Cook.asset");
            var powderRecipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>($"{RECIPE_PATH}/RCP_Powder Process.asset");
            var pillsRecipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>($"{RECIPE_PATH}/RCP_Press Pills.asset");
            var shroomRecipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>($"{RECIPE_PATH}/RCP_Grow Shrooms.asset");

            RecipeDefinition[] allRecipes = new RecipeDefinition[]
            {
                weedRecipe, crystalRecipe, powderRecipe, pillsRecipe, shroomRecipe
            };

            // Spawn Lab Table station
            SpawnStation("Lab_Table_01", new Vector3(-8f, 0f, -8f),
                "Lab Table", StationType.BasicLab, allRecipes,
                new Color(0f, 0.8f, 0.9f));

            // Crafting Bootstrapper — gives player starting ingredients at runtime
            CraftingBootstrapper bootstrapper = pmObj.AddComponent<CraftingBootstrapper>();

            Debug.Log("[Clout] Crafting stations spawned: 1 lab table + ProductionManager + CraftingUI.");
        }

        // ─── Spawn a station ─────────────────────────────────────

        private static void SpawnStation(string name, Vector3 position,
            string displayName, StationType type, RecipeDefinition[] recipes,
            Color indicatorColor)
        {
            GameObject boxManPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Placeholder/Models/boxMan.fbx");

            GameObject station = new GameObject(name);
            station.transform.position = position;

            // Visual — table-like shape
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = "StationModel";
            model.transform.SetParent(station.transform);
            model.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            model.transform.localScale = new Vector3(2f, 1f, 1.2f);
            Object.DestroyImmediate(model.GetComponent<Collider>());
            Renderer r = model.GetComponent<Renderer>();
            Material mat = EditorShaderHelper.CreateMaterial(new Color(0.35f, 0.3f, 0.25f));
            r.sharedMaterial = mat;

            // Lab equipment on top (small cubes)
            SpawnLabProp(station.transform, new Vector3(-0.5f, 1.1f, 0f), new Vector3(0.3f, 0.4f, 0.3f),
                new Color(0.6f, 0.8f, 0.9f)); // Beaker
            SpawnLabProp(station.transform, new Vector3(0.3f, 1.05f, 0.2f), new Vector3(0.25f, 0.3f, 0.25f),
                new Color(0.9f, 0.3f, 0.2f)); // Burner
            SpawnLabProp(station.transform, new Vector3(0f, 1.15f, -0.3f), new Vector3(0.4f, 0.5f, 0.2f),
                new Color(0.5f, 0.5f, 0.6f)); // Equipment

            // Collider for interaction
            BoxCollider col = station.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(2.5f, 1.5f, 1.8f);

            // Station component
            CraftingStation cs = station.AddComponent<CraftingStation>();
            cs.stationName = displayName;
            cs.stationId = name;
            cs.stationType = type;
            cs.availableRecipes = recipes;
            cs.maxConcurrentBatches = 1;
            cs.speedMultiplier = 1f;
            cs.qualityBonus = 0f;

            // Indicator — floating icon above station
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "StationIndicator";
            indicator.transform.SetParent(station.transform);
            indicator.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            indicator.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            Object.DestroyImmediate(indicator.GetComponent<Collider>());
            Renderer ir = indicator.GetComponent<Renderer>();
            Material im = EditorShaderHelper.CreateMaterial(indicatorColor);
            ir.sharedMaterial = im;

            Debug.Log($"[Clout] Crafting station spawned: {displayName} at {position}");
        }

        private static void SpawnLabProp(Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prop.name = "LabProp";
            prop.transform.SetParent(parent);
            prop.transform.localPosition = localPos;
            prop.transform.localScale = scale;
            Object.DestroyImmediate(prop.GetComponent<Collider>());
            Renderer r = prop.GetComponent<Renderer>();
            Material mat = EditorShaderHelper.CreateMaterial(color);
            r.sharedMaterial = mat;
        }

        // ═══════════════════════════════════════════════════════
        //  SO CREATION HELPERS
        // ═══════════════════════════════════════════════════════

        private static IngredientDefinition CreateIngredient(
            string name, string desc, IngredientCategory category,
            float potency, float toxicity, float shelfLife,
            float purchasePrice, float streetPrice,
            bool isLegal, bool requiresGrowth,
            Color color, AdditiveEffect[] effects)
        {
            string path = $"{INGREDIENT_PATH}/ING_{name}.asset";
            IngredientDefinition existing = AssetDatabase.LoadAssetAtPath<IngredientDefinition>(path);
            if (existing != null) return existing;

            IngredientDefinition ing = ScriptableObject.CreateInstance<IngredientDefinition>();
            ing.ingredientName = name;
            ing.description = desc;
            ing.category = category;
            ing.potency = potency;
            ing.toxicity = toxicity;
            ing.shelf_life = shelfLife;
            ing.basePurchasePrice = purchasePrice;
            ing.baseStreetPrice = streetPrice;
            ing.isLegal = isLegal;
            ing.requiresGrowth = requiresGrowth;
            ing.tintColor = color;
            ing.effects = effects;

            AssetDatabase.CreateAsset(ing, path);
            EditorUtility.SetDirty(ing);
            return ing;
        }

        private static RecipeDefinition CreateRecipe(
            string name, string desc, ProductType type,
            ProductDefinition outputProduct,
            IngredientSlot[] ingredients, IngredientSlot[] additives,
            int outputQuantity, float baseQuality,
            float craftingTime, int requiredSkill,
            PropertyType facilityType,
            float explosionRisk, float fumeRisk,
            float marketValueMult)
        {
            string path = $"{RECIPE_PATH}/RCP_{name}.asset";
            RecipeDefinition existing = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
            if (existing != null) return existing;

            RecipeDefinition recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            recipe.recipeName = name;
            recipe.description = desc;
            recipe.productType = type;
            recipe.outputProduct = outputProduct;
            recipe.ingredients = ingredients;
            recipe.optionalAdditives = additives;
            recipe.outputQuantity = outputQuantity;
            recipe.baseQuality = baseQuality;
            recipe.craftingTime = craftingTime;
            recipe.requiredSkillLevel = requiredSkill;
            recipe.requiredFacility = facilityType;
            recipe.explosionRisk = explosionRisk;
            recipe.fumeDetectionRisk = fumeRisk;
            recipe.marketValueMultiplier = marketValueMult;

            AssetDatabase.CreateAsset(recipe, path);
            EditorUtility.SetDirty(recipe);
            return recipe;
        }

        // ─── Utility ─────────────────────────────────────────────

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
