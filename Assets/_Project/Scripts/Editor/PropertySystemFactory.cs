#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Clout.Core;
using Clout.Empire.Properties;
using Clout.Empire.Economy;
using Clout.UI.Properties;
using Clout.World;

namespace Clout.Editor
{
    /// <summary>
    /// Creates property ScriptableObjects and spawns procedural buildings into the test arena.
    ///
    /// Menu: Clout > Setup > Create Property System (creates SOs)
    /// Menu: Clout > Spawn Properties (adds buildings to current scene)
    ///
    /// Creates:
    /// - 8 PropertyDefinition SOs (one per property type)
    /// - Procedural buildings for each property
    /// - PropertyManager singleton if not present
    /// - PropertyUI for browsing and purchasing
    /// - Property components on each building for interaction
    /// </summary>
    public static class PropertySystemFactory
    {
        private const string PROPERTY_PATH = "Assets/_Project/ScriptableObjects/Properties";

        [MenuItem("Clout/Setup/Create Property System", false, 205)]
        public static void CreatePropertySystem()
        {
            CreatePropertySystemHeadless();

            EditorUtility.DisplayDialog("Clout — Property System",
                "Created property system:\n\n" +
                "Properties:\n" +
                "• Motel Room (Safehouse — $2,000)\n" +
                "• Basement Lab (Lab — $15,000)\n" +
                "• Garden House (Growhouse — $8,000)\n" +
                "• Smoke Screen (Storefront — $25,000)\n" +
                "• Eastside Warehouse (Warehouse — $40,000)\n" +
                "• Club Void (Nightclub — $80,000)\n" +
                "• Chop Shop (AutoShop — $30,000)\n" +
                "• Mama's Kitchen (Restaurant — $45,000)\n\n" +
                "Use 'Clout > Spawn Properties' to add buildings to scene.",
                "Done");
        }

        public static void CreatePropertySystemHeadless()
        {
            EnsureFolder(PROPERTY_PATH);
            CreateAllPropertySOs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Clout] Property system SOs created: 8 properties.");
        }

        [MenuItem("Clout/Spawn Properties", false, 103)]
        public static void SpawnProperties()
        {
            SpawnPropertiesHeadless();

            EditorUtility.DisplayDialog("Clout — Properties Spawned",
                "Spawned 8 procedural buildings into scene:\n\n" +
                "• Motel Room (NW — beige)\n" +
                "• Basement Lab (NE — grey)\n" +
                "• Garden House (E — green)\n" +
                "• Smoke Screen (SE — tan)\n" +
                "• Eastside Warehouse (S — grey)\n" +
                "• Club Void (SW — dark purple)\n" +
                "• Chop Shop (W — steel blue)\n" +
                "• Mama's Kitchen (N — terracotta)\n\n" +
                "Walk up and press [E] to interact.\n" +
                "Press [P] to open Property Browser.",
                "Done");
        }

        public static void SpawnPropertiesHeadless()
        {
            // Ensure SOs exist
            string motelPath = $"{PROPERTY_PATH}/PROP_Motel_Room.asset";
            if (!System.IO.File.Exists(motelPath))
                CreatePropertySystemHeadless();

            // Ensure PropertyManager exists
            if (Object.FindAnyObjectByType<PropertyManager>() == null)
            {
                GameObject mgrObj = GameObject.Find("EconomySystem");
                if (mgrObj == null) mgrObj = new GameObject("EconomySystem");

                if (mgrObj.GetComponent<CashManager>() == null)
                    mgrObj.AddComponent<CashManager>();
                if (mgrObj.GetComponent<TransactionLedger>() == null)
                    mgrObj.AddComponent<TransactionLedger>();
                if (mgrObj.GetComponent<EconomyManager>() == null)
                    mgrObj.AddComponent<EconomyManager>();

                mgrObj.AddComponent<PropertyManager>();
                Debug.Log("[Clout] PropertyManager added to EconomySystem.");
            }

            // Ensure PropertyUI exists
            PropertyUI propUI = Object.FindAnyObjectByType<PropertyUI>();
            if (propUI == null)
            {
                GameObject uiObj = new GameObject("PropertyUI");
                propUI = uiObj.AddComponent<PropertyUI>();
            }

            // Container for all property buildings
            GameObject container = new GameObject("Properties");

            // Load all property SOs
            var motel = LoadProp("PROP_Motel_Room");
            var lab = LoadProp("PROP_Basement_Lab");
            var grow = LoadProp("PROP_Garden_House");
            var store = LoadProp("PROP_Smoke_Screen");
            var warehouse = LoadProp("PROP_Eastside_Warehouse");
            var club = LoadProp("PROP_Club_Void");
            var auto = LoadProp("PROP_Chop_Shop");
            var restaurant = LoadProp("PROP_Mamas_Kitchen");

            // ═══════════════════════════════════════════════════════
            //  PROCEDURAL PLACEMENT MATH
            // ═══════════════════════════════════════════════════════
            //
            //  Arena: 160x160m, walls at ±78m, wall thickness = 1m
            //  Wall inner face = 77.5m from center
            //  Buildings placed along inner perimeter, 2 per wall edge
            //  Each building faces INWARD (toward center)
            //
            //  Building footprints (from ProceduralPropertyBuilder):
            //    Safehouse:  7w × 5d     Lab:        12w × 8d
            //    Growhouse:  9w × 7d     Storefront:  8w × 10d
            //    Warehouse: 18w × 12d    Nightclub:  14w × 10d
            //    AutoShop:  14w × 9d     Restaurant: 10w × 8d
            //
            //  Placement formula per wall:
            //    Cardinal axis = wallInner - margin - depth/2
            //    Lateral offset = ±(wallInner * 0.2) to spread buildings apart
            //    Y rotation faces front door toward center
            //
            //  Layout (top-down, +Z = North):
            //
            //         Wall_North (z=+78)
            //    ┌─────────────────────────┐
            //    │  Restaurant    Motel     │ ← face south (Y=180)
            //    │                          │
            //    │ Nightclub    ┌──────┐ Lab│ ← face east/west (Y=90/270)
            //    │              │COMBAT│    │
            //    │ AutoShop     └──────┘Grow│ ← face east/west (Y=90/270)
            //    │                          │
            //    │  Warehouse   Storefront  │ ← face north (Y=0)
            //    └─────────────────────────┘
            //         Wall_South (z=-78)

            const float WALL_INNER = 77.5f;  // 78 - 0.5 (half wall thickness)
            const float MARGIN = 3f;          // Gap between building back and wall
            const float LATERAL = 16f;        // Lateral offset along wall for spacing

            // ─── NORTH WALL — buildings face south (Y=180°) ──────────
            // Depth runs along Z; back at pos.z + depth/2 near north wall
            // pos.z = WALL_INNER - MARGIN - depth/2
            float northRestZ = WALL_INNER - MARGIN - 8f / 2f;   // Restaurant depth=8 → z=70.5
            float northMotelZ = WALL_INNER - MARGIN - 5f / 2f;  // Motel depth=5    → z=72

            SpawnPropertyBuilding(restaurant, new Vector3(LATERAL, 0, northRestZ), 180f, container.transform, propUI);
            SpawnPropertyBuilding(motel, new Vector3(-LATERAL, 0, northMotelZ), 180f, container.transform, propUI);

            // ─── EAST WALL — buildings face west (Y=270°) ────────────
            // Rotated 270°: depth maps to world X, back at pos.x + depth/2 near east wall
            // pos.x = WALL_INNER - MARGIN - depth/2
            float eastLabX = WALL_INNER - MARGIN - 8f / 2f;     // Lab depth=8      → x=70.5
            float eastGrowX = WALL_INNER - MARGIN - 7f / 2f;    // Growhouse depth=7→ x=71

            SpawnPropertyBuilding(lab, new Vector3(eastLabX, 0, LATERAL), 270f, container.transform, propUI);
            SpawnPropertyBuilding(grow, new Vector3(eastGrowX, 0, -LATERAL), 270f, container.transform, propUI);

            // ─── SOUTH WALL — buildings face north (Y=0°) ────────────
            // Depth runs along Z; back at pos.z - depth/2 near south wall
            // pos.z = -(WALL_INNER - MARGIN - depth/2)
            float southWarehZ = -(WALL_INNER - MARGIN - 12f / 2f); // Warehouse depth=12 → z=-68.5
            float southStoreZ = -(WALL_INNER - MARGIN - 10f / 2f); // Storefront depth=10→ z=-69.5

            SpawnPropertyBuilding(warehouse, new Vector3(-LATERAL, 0, southWarehZ), 0f, container.transform, propUI);
            SpawnPropertyBuilding(store, new Vector3(LATERAL, 0, southStoreZ), 0f, container.transform, propUI);

            // ─── WEST WALL — buildings face east (Y=90°) ─────────────
            // Rotated 90°: depth maps to world X, back at pos.x - depth/2 near west wall
            // pos.x = -(WALL_INNER - MARGIN - depth/2)
            float westClubX = -(WALL_INNER - MARGIN - 10f / 2f);  // Nightclub depth=10 → x=-69.5
            float westAutoX = -(WALL_INNER - MARGIN - 9f / 2f);   // AutoShop depth=9   → x=-70

            SpawnPropertyBuilding(club, new Vector3(westClubX, 0, LATERAL), 90f, container.transform, propUI);
            SpawnPropertyBuilding(auto, new Vector3(westAutoX, 0, -LATERAL), 90f, container.transform, propUI);

            // ─── STREET GRID — visual connection between properties ──
            BuildStreetRing(container.transform);

            Debug.Log("[Clout] 8 procedural property buildings spawned inside arena perimeter.");
        }

        // ═══════════════════════════════════════════════════════
        //  PROPERTY SO CREATION
        // ═══════════════════════════════════════════════════════

        private static void CreateAllPropertySOs()
        {
            // ─── 1. SAFEHOUSE ────────────────────────────────
            CreatePropertySO("Motel Room", "Cheap motel on the edge of town. Roach-infested but private. " +
                "A roof over your head and a place to stash your product.",
                PropertyType.Safehouse, 2000f, 50f, 0f,
                20f, 2, 0.05f, 0.01f, false, "Northside",
                new PropertyUpgrade[]
                {
                    MakeUpgrade("Better Lock", "Reinforced door. Harder to break in.", 500f, 0,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.SecurityLevel, value = 0.2f }),
                    MakeUpgrade("Hidden Compartment", "Secret stash behind the wall.", 1000f, 2,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.StorageCapacity, value = 30f }),
                    MakeUpgrade("Blackout Curtains", "Nobody sees what happens inside.", 300f, 0,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.PoliceVisibility, value = -0.03f }),
                });

            // ─── 2. LAB ──────────────────────────────────────
            CreatePropertySO("Basement Lab", "Converted basement with basic lab equipment. " +
                "Fumes are a problem. Ventilation upgrade recommended.",
                PropertyType.Lab, 15000f, 200f, 0f,
                50f, 3, 0.3f, 0.08f, true, "Industrial District",
                new PropertyUpgrade[]
                {
                    MakeUpgrade("Ventilation System", "Industrial fans. Reduces fume detection.", 3000f, 4,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.PoliceVisibility, value = -0.15f }),
                    MakeUpgrade("Pro Equipment", "Better burners, flasks, scales. Higher purity.", 5000f, 8,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.CraftingQuality, value = 0.15f }),
                    MakeUpgrade("Speed Press", "Automated pressing. Faster batch cycles.", 4000f, 6,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.CraftingSpeed, value = 0.3f }),
                    MakeUpgrade("Security Cameras", "See who's coming before they see you.", 2000f, 2,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.SecurityLevel, value = 0.3f }),
                    MakeUpgrade("Expanded Storage", "Industrial shelving. More product capacity.", 2500f, 4,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.StorageCapacity, value = 80f }),
                });

            // ─── 3. GROWHOUSE ────────────────────────────────
            CreatePropertySO("Garden House", "Suburban house converted for indoor cultivation. " +
                "Hydroponics and grow lights ready to go.",
                PropertyType.Growhouse, 8000f, 150f, 0f,
                40f, 2, 0.2f, 0.05f, false, "Suburbs",
                new PropertyUpgrade[]
                {
                    MakeUpgrade("LED Grow Lights", "Full spectrum. Better yield and quality.", 2000f, 4,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.CraftingQuality, value = 0.1f }),
                    MakeUpgrade("Hydroponic System", "Soil-free growing. Faster cycles.", 3500f, 6,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.CraftingSpeed, value = 0.25f }),
                    MakeUpgrade("Carbon Filters", "Eliminates odor. Hard to detect from outside.", 1500f, 2,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.PoliceVisibility, value = -0.12f }),
                    MakeUpgrade("Extra Grow Room", "Convert the attic. Double capacity.", 4000f, 8,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.StorageCapacity, value = 60f }),
                });

            // ─── 4. STOREFRONT ───────────────────────────────
            CreatePropertySO("Smoke Screen", "Corner smoke shop — legitimate front business. " +
                "Launders dirty money through daily sales. The taxman sees nothing.",
                PropertyType.Storefront, 25000f, 300f, 500f,
                30f, 3, 0.1f, 0.02f, false, "Downtown",
                new PropertyUpgrade[]
                {
                    MakeUpgrade("Back Room", "Hidden room behind the counter. Product storage.", 3000f, 4,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.StorageCapacity, value = 50f }),
                    MakeUpgrade("Premium Stock", "Better merchandise. Higher legitimate revenue.", 5000f, 6,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.DailyRevenue, value = 200f }),
                    MakeUpgrade("Loyal Manager", "Hired help who asks no questions.", 2000f, 2,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.EmployeeSlots, value = 1 }),
                    MakeUpgrade("Security System", "Cameras and alarm. Deters theft and police.", 2500f, 4,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.SecurityLevel, value = 0.25f }),
                });

            // ─── 5. WAREHOUSE ────────────────────────────────
            CreatePropertySO("Eastside Warehouse", "Massive industrial storage. " +
                "Loading docks, forklifts, and room for serious weight.",
                PropertyType.Warehouse, 40000f, 500f, 200f,
                200f, 4, 0.15f, 0.04f, false, "Industrial District",
                new PropertyUpgrade[]
                {
                    MakeUpgrade("Climate Control", "Temperature-regulated storage. Product stays fresh.", 5000f, 6,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.StorageCapacity, value = 100f }),
                    MakeUpgrade("Loading Bay", "Faster logistics. Reduces transfer time.", 8000f, 8,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.CraftingSpeed, value = 0.2f }),
                    MakeUpgrade("Guard Post", "Armed security. Deters raids and theft.", 6000f, 4,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.SecurityLevel, value = 0.4f }),
                    MakeUpgrade("Shell Company", "Paperwork says it's a furniture business.", 10000f, 12,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.PoliceVisibility, value = -0.1f }),
                    MakeUpgrade("Reduced Overhead", "Negotiate better utility rates.", 3000f, 2,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.UpkeepReduction, value = 0.15f }),
                });

            // ─── 6. NIGHTCLUB ────────────────────────────────
            CreatePropertySO("Club Void", "Underground nightclub. Loud music, dark corners. " +
                "Perfect for laundering big money and building street reputation.",
                PropertyType.Nightclub, 80000f, 1000f, 2000f,
                40f, 5, 0.25f, 0.06f, false, "Entertainment District",
                new PropertyUpgrade[]
                {
                    MakeUpgrade("VIP Lounge", "Exclusive area. Higher revenue from premium guests.", 15000f, 12,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.DailyRevenue, value = 800f }),
                    MakeUpgrade("Sound System", "World-class audio. Packs the house every night.", 10000f, 8,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.DailyRevenue, value = 500f }),
                    MakeUpgrade("Back Office", "Private dealing room behind the bar.", 5000f, 4,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.StorageCapacity, value = 30f }),
                    MakeUpgrade("Bouncers", "Muscle at the door. No uninvited guests.", 4000f, 2,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.SecurityLevel, value = 0.35f }),
                    MakeUpgrade("Creative Accounting", "Launder more per day. Books look clean.", 20000f, 16,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.DailyRevenue, value = 1000f }),
                });

            // ─── 7. AUTO SHOP ────────────────────────────────
            CreatePropertySO("Chop Shop", "Automotive repair on the surface. Vehicle stripping underneath. " +
                "Good income from 'parts' and vehicle modification.",
                PropertyType.AutoShop, 30000f, 400f, 600f,
                60f, 3, 0.2f, 0.04f, false, "Southside",
                new PropertyUpgrade[]
                {
                    MakeUpgrade("Hydraulic Lift", "Professional equipment. Better service income.", 4000f, 4,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.DailyRevenue, value = 200f }),
                    MakeUpgrade("Paint Booth", "Respray vehicles. Extra income stream.", 6000f, 8,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.DailyRevenue, value = 300f }),
                    MakeUpgrade("Hidden Bay", "Underground bay for hot vehicles. Extra storage.", 8000f, 10,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.StorageCapacity, value = 40f }),
                    MakeUpgrade("Mechanic Crew", "Skilled workers. More throughput.", 3000f, 4,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.EmployeeSlots, value = 2 }),
                });

            // ─── 8. RESTAURANT ───────────────────────────────
            CreatePropertySO("Mamas Kitchen", "Family-style Italian restaurant. " +
                "Great cover business. Launders money through food sales. Everybody loves Mama's cooking.",
                PropertyType.Restaurant, 45000f, 600f, 1200f,
                25f, 4, 0.08f, 0.02f, false, "Little Italy",
                new PropertyUpgrade[]
                {
                    MakeUpgrade("Wine Cellar", "Premium wines. Higher ticket prices.", 8000f, 6,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.DailyRevenue, value = 400f }),
                    MakeUpgrade("Private Dining", "Back room for 'business meetings'. Storage + deals.", 6000f, 8,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.StorageCapacity, value = 20f }),
                    MakeUpgrade("Catering Van", "Mobile food service. Cover for deliveries.", 5000f, 6,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.PoliceVisibility, value = -0.05f }),
                    MakeUpgrade("Expanded Kitchen", "More seats, more revenue, more laundering.", 12000f, 12,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.DailyRevenue, value = 600f }),
                    MakeUpgrade("Reduced Waste", "Better inventory management. Lower costs.", 3000f, 4,
                        new PropertyUpgradeEffect { type = PropertyUpgradeType.UpkeepReduction, value = 0.1f }),
                });
        }

        // ═══════════════════════════════════════════════════════
        //  SPAWN INTO SCENE
        // ═══════════════════════════════════════════════════════

        private static void SpawnPropertyBuilding(PropertyDefinition def, Vector3 pos, float yRotation,
            Transform parent, PropertyUI propUI)
        {
            if (def == null) return;

            // Generate procedural building at position
            GameObject building = ProceduralPropertyBuilder.Build(def.propertyType, pos, def.propertyName);
            building.transform.SetParent(parent);

            // Rotate to face center — all child geometry uses localPosition so this works cleanly
            building.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

            // Attach Property component (not owned yet — for-sale state)
            Property prop = building.AddComponent<Property>();
            prop.SetDefinition(def);

            // Register with PropertyUI as for-sale
            if (propUI != null)
            {
                propUI.propertiesForSale.Add(new PropertyForSale
                {
                    definition = def,
                    worldPosition = pos
                });
            }

            // Store world position on definition
            def.worldPosition = pos;
            EditorUtility.SetDirty(def);

            Debug.Log($"[Clout] Property: {def.propertyName} ({def.propertyType}) at {pos} facing Y={yRotation}°");
        }

        /// <summary>
        /// Builds a procedural street ring connecting the property district.
        /// Asphalt-colored ground strips forming a rectangular loop around the combat zone.
        /// </summary>
        private static void BuildStreetRing(Transform parent)
        {
            Color asphalt = new Color(0.22f, 0.22f, 0.25f);
            Color lineColor = new Color(0.75f, 0.70f, 0.30f); // Yellow road marking

            // Street dimensions
            const float STREET_WIDTH = 8f;
            const float STREET_Y = 0.02f;  // Just above ground to prevent z-fighting
            const float RING_DIST = 48f;   // Distance of street ring from center

            // North-South streets (east & west sides)
            BuildStreetSegment(parent, "Street_East",
                new Vector3(RING_DIST, STREET_Y, 0f),
                new Vector3(STREET_WIDTH, 0.04f, RING_DIST * 2f + STREET_WIDTH), asphalt);
            BuildStreetSegment(parent, "Street_West",
                new Vector3(-RING_DIST, STREET_Y, 0f),
                new Vector3(STREET_WIDTH, 0.04f, RING_DIST * 2f + STREET_WIDTH), asphalt);

            // East-West streets (north & south sides)
            BuildStreetSegment(parent, "Street_North",
                new Vector3(0f, STREET_Y, RING_DIST),
                new Vector3(RING_DIST * 2f + STREET_WIDTH, 0.04f, STREET_WIDTH), asphalt);
            BuildStreetSegment(parent, "Street_South",
                new Vector3(0f, STREET_Y, -RING_DIST),
                new Vector3(RING_DIST * 2f + STREET_WIDTH, 0.04f, STREET_WIDTH), asphalt);

            // Center road markings (dashed yellow lines)
            for (int i = -5; i <= 5; i++)
            {
                float z = i * (RING_DIST * 2f / 10f);
                BuildStreetSegment(parent, $"Marking_E_{i}",
                    new Vector3(RING_DIST, STREET_Y + 0.01f, z),
                    new Vector3(0.15f, 0.02f, 3f), lineColor);
                BuildStreetSegment(parent, $"Marking_W_{i}",
                    new Vector3(-RING_DIST, STREET_Y + 0.01f, z),
                    new Vector3(0.15f, 0.02f, 3f), lineColor);
            }
            for (int i = -5; i <= 5; i++)
            {
                float x = i * (RING_DIST * 2f / 10f);
                BuildStreetSegment(parent, $"Marking_N_{i}",
                    new Vector3(x, STREET_Y + 0.01f, RING_DIST),
                    new Vector3(3f, 0.02f, 0.15f), lineColor);
                BuildStreetSegment(parent, $"Marking_S_{i}",
                    new Vector3(x, STREET_Y + 0.01f, -RING_DIST),
                    new Vector3(3f, 0.02f, 0.15f), lineColor);
            }

            // Sidewalks between buildings and streets (lighter concrete)
            Color sidewalk = new Color(0.55f, 0.55f, 0.52f);
            const float SW_WIDTH = 2.5f;

            BuildStreetSegment(parent, "Sidewalk_E_Inner",
                new Vector3(RING_DIST - STREET_WIDTH / 2f - SW_WIDTH / 2f, STREET_Y, 0f),
                new Vector3(SW_WIDTH, 0.06f, RING_DIST * 2f), sidewalk);
            BuildStreetSegment(parent, "Sidewalk_E_Outer",
                new Vector3(RING_DIST + STREET_WIDTH / 2f + SW_WIDTH / 2f, STREET_Y, 0f),
                new Vector3(SW_WIDTH, 0.06f, RING_DIST * 2f), sidewalk);

            BuildStreetSegment(parent, "Sidewalk_W_Inner",
                new Vector3(-RING_DIST + STREET_WIDTH / 2f + SW_WIDTH / 2f, STREET_Y, 0f),
                new Vector3(SW_WIDTH, 0.06f, RING_DIST * 2f), sidewalk);
            BuildStreetSegment(parent, "Sidewalk_W_Outer",
                new Vector3(-RING_DIST - STREET_WIDTH / 2f - SW_WIDTH / 2f, STREET_Y, 0f),
                new Vector3(SW_WIDTH, 0.06f, RING_DIST * 2f), sidewalk);

            BuildStreetSegment(parent, "Sidewalk_N_Inner",
                new Vector3(0f, STREET_Y, RING_DIST - STREET_WIDTH / 2f - SW_WIDTH / 2f),
                new Vector3(RING_DIST * 2f, 0.06f, SW_WIDTH), sidewalk);
            BuildStreetSegment(parent, "Sidewalk_N_Outer",
                new Vector3(0f, STREET_Y, RING_DIST + STREET_WIDTH / 2f + SW_WIDTH / 2f),
                new Vector3(RING_DIST * 2f, 0.06f, SW_WIDTH), sidewalk);

            BuildStreetSegment(parent, "Sidewalk_S_Inner",
                new Vector3(0f, STREET_Y, -RING_DIST + STREET_WIDTH / 2f + SW_WIDTH / 2f),
                new Vector3(RING_DIST * 2f, 0.06f, SW_WIDTH), sidewalk);
            BuildStreetSegment(parent, "Sidewalk_S_Outer",
                new Vector3(0f, STREET_Y, -RING_DIST - STREET_WIDTH / 2f - SW_WIDTH / 2f),
                new Vector3(RING_DIST * 2f, 0.06f, SW_WIDTH), sidewalk);

            Debug.Log("[Clout] Street grid built: 4 roads + markings + sidewalks.");
        }

        private static void BuildStreetSegment(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = name;
            seg.transform.SetParent(parent);
            seg.transform.position = pos;
            seg.transform.localScale = scale;
            seg.isStatic = true;

            // Remove collider — streets don't block movement
            Object.DestroyImmediate(seg.GetComponent<Collider>());

            Renderer r = seg.GetComponent<Renderer>();
            Material mat = EditorShaderHelper.CreateMaterial(color);
            r.sharedMaterial = mat;
        }

        // ═══════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════

        private static PropertyDefinition CreatePropertySO(string name, string desc,
            PropertyType type, float price, float upkeep, float revenue,
            float storage, int employees, float visibility, float raidChance,
            bool requiresCover, string district, PropertyUpgrade[] upgrades)
        {
            string safeName = name.Replace(" ", "_").Replace("'", "");
            string path = $"{PROPERTY_PATH}/PROP_{safeName}.asset";
            PropertyDefinition existing = AssetDatabase.LoadAssetAtPath<PropertyDefinition>(path);
            if (existing != null) return existing;

            PropertyDefinition prop = ScriptableObject.CreateInstance<PropertyDefinition>();
            prop.propertyName = name;
            prop.description = desc;
            prop.propertyType = type;
            prop.purchasePrice = price;
            prop.dailyUpkeep = upkeep;
            prop.dailyRevenue = revenue;
            prop.maxStorage = storage;
            prop.maxEmployeeSlots = employees;
            prop.policeVisibility = visibility;
            prop.raidChance = raidChance;
            prop.requiresCoverBusiness = requiresCover;
            prop.districtName = district;
            prop.availableUpgrades = upgrades;

            AssetDatabase.CreateAsset(prop, path);
            EditorUtility.SetDirty(prop);
            return prop;
        }

        private static PropertyUpgrade MakeUpgrade(string name, string desc, float cost, float hours,
            params PropertyUpgradeEffect[] effects)
        {
            return new PropertyUpgrade
            {
                upgradeName = name,
                description = desc,
                cost = cost,
                constructionTime = hours,
                effects = effects
            };
        }

        private static PropertyDefinition LoadProp(string soName)
        {
            return AssetDatabase.LoadAssetAtPath<PropertyDefinition>($"{PROPERTY_PATH}/{soName}.asset");
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
