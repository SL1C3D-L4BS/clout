using UnityEngine;
using Clout.Core;
using Clout.Empire.Properties;

namespace Clout.World
{
    /// <summary>
    /// Procedural building generator — creates property structures from Unity primitives.
    /// Replaces Synty assets for Phase 2 prototyping. Each PropertyType gets a distinct
    /// silhouette, scale, and color scheme so players can visually identify them.
    ///
    /// Geometry is intentionally simple (cubes, cylinders) but architecturally proportioned
    /// using golden ratio and real-world building dimensions for visual credibility.
    ///
    /// Call Build() at edit-time from PropertySystemFactory or at runtime from PropertyManager.
    /// </summary>
    public static class ProceduralPropertyBuilder
    {
        // Golden ratio for pleasing proportions
        private const float PHI = 1.618f;

        // Real-world unit scale: 1 Unity unit = 1 meter
        private const float FLOOR_HEIGHT = 3.2f;       // Standard floor-to-floor
        private const float DOOR_HEIGHT = 2.4f;
        private const float DOOR_WIDTH = 1.2f;
        private const float WINDOW_SIZE = 1.0f;
        private const float WALL_THICKNESS = 0.25f;

        /// <summary>
        /// Build a procedural property structure for the given type.
        /// Returns the root GameObject with all geometry as children.
        /// </summary>
        public static GameObject Build(PropertyType type, Vector3 position, string propertyName)
        {
            return type switch
            {
                PropertyType.Safehouse => BuildSafehouse(position, propertyName),
                PropertyType.Lab => BuildLab(position, propertyName),
                PropertyType.Growhouse => BuildGrowhouse(position, propertyName),
                PropertyType.Storefront => BuildStorefront(position, propertyName),
                PropertyType.Warehouse => BuildWarehouse(position, propertyName),
                PropertyType.Nightclub => BuildNightclub(position, propertyName),
                PropertyType.AutoShop => BuildAutoShop(position, propertyName),
                PropertyType.Restaurant => BuildRestaurant(position, propertyName),
                _ => BuildGenericBuilding(position, propertyName, 8f, 6f, 1, new Color(0.5f, 0.5f, 0.5f))
            };
        }

        // ═══════════════════════════════════════════════════════
        //  BUILDING TYPES
        // ═══════════════════════════════════════════════════════

        private static GameObject BuildSafehouse(Vector3 pos, string name)
        {
            // Small residential — 2-story apartment / motel room
            float width = 7f;
            float depth = 5f;
            int floors = 2;
            Color wallColor = new Color(0.65f, 0.58f, 0.50f); // Warm beige
            Color roofColor = new Color(0.35f, 0.28f, 0.22f); // Dark brown
            Color trimColor = new Color(0.45f, 0.55f, 0.45f); // Sage green

            GameObject root = new GameObject(name);
            root.transform.position = pos;

            // Main structure
            BuildBox(root.transform, "Walls", Vector3.up * (floors * FLOOR_HEIGHT / 2),
                new Vector3(width, floors * FLOOR_HEIGHT, depth), wallColor);

            // Pitched roof
            BuildPitchedRoof(root.transform, floors * FLOOR_HEIGHT,
                width, depth, 1.8f, roofColor);

            // Front door
            BuildDoor(root.transform, new Vector3(0, DOOR_HEIGHT / 2, depth / 2 + 0.01f),
                trimColor);

            // Windows — 2 per floor, front face
            for (int f = 0; f < floors; f++)
            {
                float y = f * FLOOR_HEIGHT + FLOOR_HEIGHT * 0.6f;
                BuildWindow(root.transform, new Vector3(-width * 0.25f, y, depth / 2 + 0.02f), trimColor);
                BuildWindow(root.transform, new Vector3(width * 0.25f, y, depth / 2 + 0.02f), trimColor);
            }

            // Front steps
            BuildBox(root.transform, "Steps", new Vector3(0, 0.15f, depth / 2 + 0.5f),
                new Vector3(2f, 0.3f, 1f), new Color(0.5f, 0.5f, 0.5f));

            // Collision
            AddBuildingCollider(root, width, floors * FLOOR_HEIGHT + 1.8f, depth);

            // Interaction trigger
            AddInteractionTrigger(root, width + 3f, floors * FLOOR_HEIGHT, depth + 3f);

            return root;
        }

        private static GameObject BuildLab(Vector3 pos, string name)
        {
            // Industrial lab — low, wide, secretive. Think Breaking Bad.
            float width = 12f;
            float depth = 8f;
            Color wallColor = new Color(0.55f, 0.55f, 0.58f); // Industrial grey
            Color roofColor = new Color(0.35f, 0.35f, 0.38f);
            Color accentColor = new Color(0.3f, 0.6f, 0.8f); // Chemical blue

            GameObject root = new GameObject(name);
            root.transform.position = pos;

            // Main structure — low ceiling, industrial
            BuildBox(root.transform, "Walls", Vector3.up * (FLOOR_HEIGHT / 2),
                new Vector3(width, FLOOR_HEIGHT, depth), wallColor);

            // Flat roof with raised edge (parapet)
            BuildBox(root.transform, "Roof", new Vector3(0, FLOOR_HEIGHT + 0.1f, 0),
                new Vector3(width + 0.3f, 0.2f, depth + 0.3f), roofColor);
            BuildBox(root.transform, "Parapet_F", new Vector3(0, FLOOR_HEIGHT + 0.5f, depth / 2),
                new Vector3(width + 0.3f, 0.8f, WALL_THICKNESS), roofColor);
            BuildBox(root.transform, "Parapet_B", new Vector3(0, FLOOR_HEIGHT + 0.5f, -depth / 2),
                new Vector3(width + 0.3f, 0.8f, WALL_THICKNESS), roofColor);

            // Ventilation units on roof (cubes + cylinders)
            BuildBox(root.transform, "Vent_1", new Vector3(-3f, FLOOR_HEIGHT + 0.8f, -1f),
                new Vector3(1.5f, 1.2f, 1.5f), new Color(0.4f, 0.4f, 0.42f));
            BuildCylinder(root.transform, "Exhaust_1", new Vector3(2f, FLOOR_HEIGHT + 1.2f, 1f),
                0.3f, 2f, new Color(0.45f, 0.45f, 0.45f));
            BuildCylinder(root.transform, "Exhaust_2", new Vector3(3f, FLOOR_HEIGHT + 1.0f, 1f),
                0.25f, 1.6f, new Color(0.45f, 0.45f, 0.45f));

            // Roll-up door (garage style)
            BuildBox(root.transform, "GarageDoor", new Vector3(-width * 0.3f, 1.5f, depth / 2 + 0.01f),
                new Vector3(3.5f, 3f, 0.1f), accentColor);

            // Small personnel door
            BuildDoor(root.transform, new Vector3(width * 0.3f, DOOR_HEIGHT / 2, depth / 2 + 0.01f),
                new Color(0.3f, 0.3f, 0.3f));

            // Hazard stripe on wall
            BuildBox(root.transform, "HazardStripe", new Vector3(0, FLOOR_HEIGHT * 0.85f, depth / 2 + 0.02f),
                new Vector3(width, 0.15f, 0.05f), Color.yellow);

            AddBuildingCollider(root, width, FLOOR_HEIGHT + 1f, depth);
            AddInteractionTrigger(root, width + 3f, FLOOR_HEIGHT, depth + 3f);

            return root;
        }

        private static GameObject BuildGrowhouse(Vector3 pos, string name)
        {
            // Greenhouse / converted house — distinctive green tint
            float width = 9f;
            float depth = 7f;
            Color wallColor = new Color(0.55f, 0.60f, 0.52f); // Greenish
            Color roofColor = new Color(0.3f, 0.45f, 0.3f); // Forest green
            Color glassColor = new Color(0.5f, 0.8f, 0.5f, 0.7f);

            GameObject root = new GameObject(name);
            root.transform.position = pos;

            // Main structure
            BuildBox(root.transform, "Walls", Vector3.up * (FLOOR_HEIGHT / 2),
                new Vector3(width, FLOOR_HEIGHT, depth), wallColor);

            // Greenhouse extension (glass roof section)
            BuildBox(root.transform, "Greenhouse", new Vector3(width * 0.35f, FLOOR_HEIGHT * 0.4f, 0),
                new Vector3(width * 0.4f, FLOOR_HEIGHT * 0.8f, depth), glassColor);

            // Pitched roof on main section
            BuildPitchedRoof(root.transform, FLOOR_HEIGHT,
                width * 0.65f, depth, 1.5f, roofColor);

            // Door
            BuildDoor(root.transform, new Vector3(-width * 0.15f, DOOR_HEIGHT / 2, depth / 2 + 0.01f),
                new Color(0.35f, 0.45f, 0.35f));

            // Grow lights visible through windows (bright green glow)
            BuildWindow(root.transform, new Vector3(-width * 0.3f, FLOOR_HEIGHT * 0.55f, depth / 2 + 0.02f),
                new Color(0.2f, 0.9f, 0.2f));

            AddBuildingCollider(root, width, FLOOR_HEIGHT + 1.5f, depth);
            AddInteractionTrigger(root, width + 3f, FLOOR_HEIGHT, depth + 3f);

            return root;
        }

        private static GameObject BuildStorefront(Vector3 pos, string name)
        {
            // Street-level shop — classic retail with large front window
            float width = 8f;
            float depth = 10f;
            Color wallColor = new Color(0.7f, 0.65f, 0.58f); // Warm tan
            Color awningColor = new Color(0.75f, 0.2f, 0.15f); // Red awning
            Color signColor = new Color(0.9f, 0.85f, 0.7f);

            GameObject root = new GameObject(name);
            root.transform.position = pos;

            // Main structure — 2 story
            BuildBox(root.transform, "Walls", Vector3.up * FLOOR_HEIGHT,
                new Vector3(width, FLOOR_HEIGHT * 2, depth), wallColor);

            // Flat roof
            BuildBox(root.transform, "Roof", new Vector3(0, FLOOR_HEIGHT * 2 + 0.1f, 0),
                new Vector3(width + 0.2f, 0.15f, depth + 0.2f), new Color(0.3f, 0.3f, 0.32f));

            // Storefront window (large glass panel)
            BuildBox(root.transform, "ShopWindow", new Vector3(0, FLOOR_HEIGHT * 0.5f, depth / 2 + 0.02f),
                new Vector3(width * 0.65f, FLOOR_HEIGHT * 0.6f, 0.08f), new Color(0.6f, 0.7f, 0.8f, 0.5f));

            // Awning
            BuildBox(root.transform, "Awning", new Vector3(0, FLOOR_HEIGHT * 0.9f, depth / 2 + 1f),
                new Vector3(width + 0.5f, 0.08f, 2f), awningColor);

            // Sign above awning
            BuildBox(root.transform, "Sign", new Vector3(0, FLOOR_HEIGHT * 1.05f, depth / 2 + 0.5f),
                new Vector3(width * 0.7f, 0.6f, 0.1f), signColor);

            // Door (center)
            BuildDoor(root.transform, new Vector3(0, DOOR_HEIGHT / 2, depth / 2 + 0.01f),
                new Color(0.4f, 0.3f, 0.25f));

            // Second floor windows
            float y2 = FLOOR_HEIGHT + FLOOR_HEIGHT * 0.5f;
            BuildWindow(root.transform, new Vector3(-width * 0.25f, y2, depth / 2 + 0.02f), wallColor * 0.8f);
            BuildWindow(root.transform, new Vector3(width * 0.25f, y2, depth / 2 + 0.02f), wallColor * 0.8f);

            AddBuildingCollider(root, width, FLOOR_HEIGHT * 2, depth);
            AddInteractionTrigger(root, width + 3f, FLOOR_HEIGHT * 2, depth + 3f);

            return root;
        }

        private static GameObject BuildWarehouse(Vector3 pos, string name)
        {
            // Large industrial warehouse — big, utilitarian
            float width = 18f;
            float depth = 12f;
            Color wallColor = new Color(0.5f, 0.48f, 0.45f);
            Color roofColor = new Color(0.42f, 0.40f, 0.38f);
            Color doorColor = new Color(0.6f, 0.55f, 0.3f); // Yellowish industrial

            GameObject root = new GameObject(name);
            root.transform.position = pos;

            // Main structure — tall single story
            float height = FLOOR_HEIGHT * 1.8f;
            BuildBox(root.transform, "Walls", Vector3.up * (height / 2),
                new Vector3(width, height, depth), wallColor);

            // Arched roof (approximated with stretched box)
            BuildBox(root.transform, "Roof", new Vector3(0, height + 0.6f, 0),
                new Vector3(width * 0.95f, 1.2f, depth + 0.3f), roofColor);

            // Large loading doors x2
            BuildBox(root.transform, "LoadingDoor_L", new Vector3(-width * 0.25f, 2f, depth / 2 + 0.01f),
                new Vector3(4f, 4f, 0.12f), doorColor);
            BuildBox(root.transform, "LoadingDoor_R", new Vector3(width * 0.25f, 2f, depth / 2 + 0.01f),
                new Vector3(4f, 4f, 0.12f), doorColor);

            // Personnel door
            BuildDoor(root.transform, new Vector3(0, DOOR_HEIGHT / 2, depth / 2 + 0.01f),
                new Color(0.35f, 0.35f, 0.35f));

            // Loading dock platform
            BuildBox(root.transform, "Dock", new Vector3(0, 0.6f, depth / 2 + 1.5f),
                new Vector3(width * 0.8f, 1.2f, 3f), new Color(0.55f, 0.55f, 0.55f));

            AddBuildingCollider(root, width, height + 1.2f, depth);
            AddInteractionTrigger(root, width + 4f, height, depth + 4f);

            return root;
        }

        private static GameObject BuildNightclub(Vector3 pos, string name)
        {
            // Flashy nightclub — dark exterior with neon accent colors
            float width = 14f;
            float depth = 10f;
            Color wallColor = new Color(0.15f, 0.12f, 0.18f); // Dark purple-black
            Color neonColor = new Color(0.9f, 0.1f, 0.9f); // Neon magenta
            Color neonBlue = new Color(0.1f, 0.4f, 0.9f);

            GameObject root = new GameObject(name);
            root.transform.position = pos;

            // Main structure — tall, imposing
            float height = FLOOR_HEIGHT * 2;
            BuildBox(root.transform, "Walls", Vector3.up * (height / 2),
                new Vector3(width, height, depth), wallColor);

            // Flat roof with neon trim
            BuildBox(root.transform, "Roof", new Vector3(0, height + 0.1f, 0),
                new Vector3(width + 0.2f, 0.15f, depth + 0.2f), wallColor);
            BuildBox(root.transform, "NeonTrim_F", new Vector3(0, height + 0.25f, depth / 2),
                new Vector3(width + 0.4f, 0.1f, 0.08f), neonColor);
            BuildBox(root.transform, "NeonTrim_B", new Vector3(0, height + 0.25f, -depth / 2),
                new Vector3(width + 0.4f, 0.1f, 0.08f), neonBlue);

            // VIP entrance
            BuildBox(root.transform, "Entrance", new Vector3(0, height * 0.35f, depth / 2 + 1f),
                new Vector3(4f, height * 0.7f, 2f), new Color(0.1f, 0.08f, 0.12f));

            // Neon sign
            BuildBox(root.transform, "NeonSign", new Vector3(0, height * 0.85f, depth / 2 + 0.15f),
                new Vector3(width * 0.5f, 0.8f, 0.08f), neonColor);

            // Velvet rope posts (cylinders)
            BuildCylinder(root.transform, "RopePost_L", new Vector3(-2f, 0.5f, depth / 2 + 2.5f),
                0.08f, 1f, neonColor);
            BuildCylinder(root.transform, "RopePost_R", new Vector3(2f, 0.5f, depth / 2 + 2.5f),
                0.08f, 1f, neonColor);

            // Bouncer area (dark box)
            BuildBox(root.transform, "BouncerArea", new Vector3(3f, 1f, depth / 2 + 2.5f),
                new Vector3(1.5f, 2f, 1.5f), new Color(0.08f, 0.06f, 0.1f));

            AddBuildingCollider(root, width, height, depth);
            AddInteractionTrigger(root, width + 4f, height, depth + 4f);

            return root;
        }

        private static GameObject BuildAutoShop(Vector3 pos, string name)
        {
            // Auto repair / chop shop — garages and oil stains
            float width = 14f;
            float depth = 9f;
            Color wallColor = new Color(0.58f, 0.55f, 0.50f);
            Color garageDoorColor = new Color(0.35f, 0.45f, 0.55f); // Steel blue
            Color signColor = new Color(0.8f, 0.3f, 0.1f); // Orange

            GameObject root = new GameObject(name);
            root.transform.position = pos;

            float height = FLOOR_HEIGHT * 1.4f;
            BuildBox(root.transform, "Walls", Vector3.up * (height / 2),
                new Vector3(width, height, depth), wallColor);

            // Flat roof
            BuildBox(root.transform, "Roof", new Vector3(0, height + 0.1f, 0),
                new Vector3(width + 0.2f, 0.15f, depth + 0.2f), new Color(0.35f, 0.33f, 0.3f));

            // 3 garage bays
            for (int i = 0; i < 3; i++)
            {
                float x = -width * 0.3f + i * (width * 0.3f);
                BuildBox(root.transform, $"GarageDoor_{i}", new Vector3(x, 1.75f, depth / 2 + 0.01f),
                    new Vector3(3.2f, 3.5f, 0.12f), garageDoorColor);
            }

            // Office door on the side
            BuildDoor(root.transform, new Vector3(width * 0.42f, DOOR_HEIGHT / 2, depth / 2 + 0.01f),
                new Color(0.4f, 0.35f, 0.3f));

            // Sign
            BuildBox(root.transform, "Sign", new Vector3(0, height * 0.9f, depth / 2 + 0.15f),
                new Vector3(width * 0.6f, 0.7f, 0.1f), signColor);

            // Oil stain on ground
            BuildBox(root.transform, "OilStain", new Vector3(0, 0.01f, depth / 2 + 2f),
                new Vector3(6f, 0.02f, 4f), new Color(0.15f, 0.12f, 0.1f));

            AddBuildingCollider(root, width, height, depth);
            AddInteractionTrigger(root, width + 3f, height, depth + 3f);

            return root;
        }

        private static GameObject BuildRestaurant(Vector3 pos, string name)
        {
            // Restaurant / food front — warm, inviting exterior
            float width = 10f;
            float depth = 8f;
            Color wallColor = new Color(0.72f, 0.55f, 0.40f); // Terracotta
            Color awningColor = new Color(0.15f, 0.35f, 0.15f); // Dark green
            Color trimColor = new Color(0.85f, 0.80f, 0.65f); // Cream

            GameObject root = new GameObject(name);
            root.transform.position = pos;

            // Main structure — 2 story
            float height = FLOOR_HEIGHT * 2;
            BuildBox(root.transform, "Walls", Vector3.up * (height / 2),
                new Vector3(width, height, depth), wallColor);

            // Flat roof
            BuildBox(root.transform, "Roof", new Vector3(0, height + 0.1f, 0),
                new Vector3(width + 0.2f, 0.15f, depth + 0.2f), new Color(0.4f, 0.35f, 0.3f));

            // Large front window
            BuildBox(root.transform, "DiningWindow", new Vector3(-width * 0.2f, FLOOR_HEIGHT * 0.45f, depth / 2 + 0.02f),
                new Vector3(width * 0.45f, FLOOR_HEIGHT * 0.5f, 0.08f), new Color(0.7f, 0.75f, 0.6f, 0.5f));

            // Awning over entrance
            BuildBox(root.transform, "Awning", new Vector3(width * 0.2f, FLOOR_HEIGHT * 0.85f, depth / 2 + 1.2f),
                new Vector3(4f, 0.08f, 2.4f), awningColor);

            // Door
            BuildDoor(root.transform, new Vector3(width * 0.2f, DOOR_HEIGHT / 2, depth / 2 + 0.01f),
                new Color(0.3f, 0.2f, 0.15f));

            // Outdoor seating area (table + chairs as boxes)
            BuildBox(root.transform, "Table_1", new Vector3(-2f, 0.4f, depth / 2 + 3f),
                new Vector3(0.8f, 0.05f, 0.8f), trimColor);
            BuildCylinder(root.transform, "TableLeg_1", new Vector3(-2f, 0.2f, depth / 2 + 3f),
                0.05f, 0.4f, new Color(0.3f, 0.3f, 0.3f));

            // Second floor windows
            float y2 = FLOOR_HEIGHT + FLOOR_HEIGHT * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                float x = -width * 0.3f + i * (width * 0.3f);
                BuildWindow(root.transform, new Vector3(x, y2, depth / 2 + 0.02f), trimColor);
            }

            AddBuildingCollider(root, width, height, depth);
            AddInteractionTrigger(root, width + 3f, height, depth + 3f);

            return root;
        }

        private static GameObject BuildGenericBuilding(Vector3 pos, string name,
            float width, float depth, int floors, Color color)
        {
            GameObject root = new GameObject(name);
            root.transform.position = pos;

            float height = floors * FLOOR_HEIGHT;
            BuildBox(root.transform, "Walls", Vector3.up * (height / 2),
                new Vector3(width, height, depth), color);

            BuildBox(root.transform, "Roof", new Vector3(0, height + 0.1f, 0),
                new Vector3(width + 0.2f, 0.15f, depth + 0.2f), color * 0.7f);

            BuildDoor(root.transform, new Vector3(0, DOOR_HEIGHT / 2, depth / 2 + 0.01f),
                color * 0.5f);

            AddBuildingCollider(root, width, height, depth);
            AddInteractionTrigger(root, width + 3f, height, depth + 3f);

            return root;
        }

        // ═══════════════════════════════════════════════════════
        //  PRIMITIVE BUILDERS
        // ═══════════════════════════════════════════════════════

        private static GameObject BuildBox(Transform parent, string name, Vector3 localPos,
            Vector3 scale, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            obj.isStatic = true;

            // Remove auto-collider (we use a single building collider)
            Object.DestroyImmediate(obj.GetComponent<Collider>());

            SetColor(obj, color);
            return obj;
        }

        private static GameObject BuildCylinder(Transform parent, string name, Vector3 localPos,
            float radius, float height, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = new Vector3(radius * 2, height / 2, radius * 2);
            obj.isStatic = true;

            Object.DestroyImmediate(obj.GetComponent<Collider>());

            SetColor(obj, color);
            return obj;
        }

        private static void BuildPitchedRoof(Transform parent, float wallHeight,
            float width, float depth, float peakHeight, Color color)
        {
            // Approximate pitched roof with a stretched cube rotated — quick and effective
            // Two angled planes meeting at the ridge
            float roofWidth = width * 0.55f;
            float roofThickness = 0.12f;
            float angle = Mathf.Atan2(peakHeight, width / 2) * Mathf.Rad2Deg;
            float slopeLength = Mathf.Sqrt(peakHeight * peakHeight + (width / 2) * (width / 2));

            // Left slope
            GameObject leftRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftRoof.name = "Roof_Left";
            leftRoof.transform.SetParent(parent);
            leftRoof.transform.localPosition = new Vector3(-width * 0.2f, wallHeight + peakHeight * 0.45f, 0);
            leftRoof.transform.localEulerAngles = new Vector3(0, 0, angle);
            leftRoof.transform.localScale = new Vector3(slopeLength, roofThickness, depth + 0.6f);
            leftRoof.isStatic = true;
            Object.DestroyImmediate(leftRoof.GetComponent<Collider>());
            SetColor(leftRoof, color);

            // Right slope
            GameObject rightRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightRoof.name = "Roof_Right";
            rightRoof.transform.SetParent(parent);
            rightRoof.transform.localPosition = new Vector3(width * 0.2f, wallHeight + peakHeight * 0.45f, 0);
            rightRoof.transform.localEulerAngles = new Vector3(0, 0, -angle);
            rightRoof.transform.localScale = new Vector3(slopeLength, roofThickness, depth + 0.6f);
            rightRoof.isStatic = true;
            Object.DestroyImmediate(rightRoof.GetComponent<Collider>());
            SetColor(rightRoof, color);
        }

        private static void BuildDoor(Transform parent, Vector3 pos, Color color)
        {
            BuildBox(parent, "Door", pos, new Vector3(DOOR_WIDTH, DOOR_HEIGHT, 0.08f), color);

            // Door frame
            BuildBox(parent, "DoorFrame_L", pos + new Vector3(-DOOR_WIDTH / 2 - 0.08f, 0, 0),
                new Vector3(0.12f, DOOR_HEIGHT + 0.12f, 0.12f), color * 0.7f);
            BuildBox(parent, "DoorFrame_R", pos + new Vector3(DOOR_WIDTH / 2 + 0.08f, 0, 0),
                new Vector3(0.12f, DOOR_HEIGHT + 0.12f, 0.12f), color * 0.7f);
            BuildBox(parent, "DoorFrame_T", pos + new Vector3(0, DOOR_HEIGHT / 2 + 0.08f, 0),
                new Vector3(DOOR_WIDTH + 0.32f, 0.12f, 0.12f), color * 0.7f);
        }

        private static void BuildWindow(Transform parent, Vector3 pos, Color frameColor)
        {
            // Glass pane
            BuildBox(parent, "WindowGlass", pos,
                new Vector3(WINDOW_SIZE, WINDOW_SIZE, 0.04f), new Color(0.5f, 0.6f, 0.7f, 0.4f));

            // Frame
            float f = 0.06f;
            BuildBox(parent, "WinFrame_T", pos + new Vector3(0, WINDOW_SIZE / 2 + f / 2, 0),
                new Vector3(WINDOW_SIZE + f * 2, f, 0.06f), frameColor * 0.6f);
            BuildBox(parent, "WinFrame_B", pos + new Vector3(0, -WINDOW_SIZE / 2 - f / 2, 0),
                new Vector3(WINDOW_SIZE + f * 2, f, 0.06f), frameColor * 0.6f);
        }

        // ═══════════════════════════════════════════════════════
        //  COLLIDERS & TRIGGERS
        // ═══════════════════════════════════════════════════════

        private static void AddBuildingCollider(GameObject root, float width, float height, float depth)
        {
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, height / 2, 0);
            col.size = new Vector3(width, height, depth);
        }

        private static void AddInteractionTrigger(GameObject root, float width, float height, float depth)
        {
            GameObject triggerObj = new GameObject("InteractionZone");
            triggerObj.transform.SetParent(root.transform);
            triggerObj.transform.localPosition = new Vector3(0, height / 2, 0);

            BoxCollider trigger = triggerObj.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(width, height, depth);
        }

        // ═══════════════════════════════════════════════════════
        //  MATERIAL
        // ═══════════════════════════════════════════════════════

        private static void SetColor(GameObject obj, Color color)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r == null) return;

            // Try URP Lit shader, fall back to Standard
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader fallback = Shader.Find("Standard");
            Shader shader = urpLit != null ? urpLit : fallback;

            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.color = color;

                // Handle transparency
                if (color.a < 1f)
                {
                    if (urpLit != null)
                    {
                        mat.SetFloat("_Surface", 1); // Transparent
                        mat.SetFloat("_Blend", 0); // Alpha
                        mat.SetOverrideTag("RenderType", "Transparent");
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.renderQueue = 3000;
                    }
                    else
                    {
                        mat.SetFloat("_Mode", 3); // Transparent
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.renderQueue = 3000;
                    }
                }

                r.sharedMaterial = mat;
            }
        }
    }
}
