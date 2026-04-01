using UnityEngine;
using Clout.Core;
using Clout.Empire.Properties;
using Clout.World.Police;

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
        //  AMBIENT CITY BUILDINGS (Step 8 — District Generation)
        //  Non-purchasable background structures that fill districts
        //  with visual variety and urban atmosphere.
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Residential house — small 1-2 story home with yard. Bay Area Victorian/Edwardian style.
        /// </summary>
        public static GameObject BuildResidentialHouse(Vector3 pos, float wealthLevel = 0.5f)
        {
            float width = Random.Range(6f, 8f);
            float depth = Random.Range(5f, 7f);
            int floors = Random.value > 0.4f ? 2 : 1;
            float height = floors * FLOOR_HEIGHT;

            // Wealth-modulated colors — wealthier = cleaner, brighter
            float wb = 0.4f + wealthLevel * 0.35f;
            Color wallColor = Color.HSVToRGB(
                Random.Range(0.02f, 0.12f), // Warm hues
                Random.Range(0.15f, 0.35f) * (1f - wealthLevel * 0.3f),
                wb);
            Color roofColor = new Color(wb * 0.55f, wb * 0.45f, wb * 0.4f);
            Color trimColor = Color.HSVToRGB(Random.Range(0f, 0.15f), 0.1f, wb + 0.1f);

            GameObject root = new GameObject($"House_{pos.x:F0}_{pos.z:F0}");
            root.transform.position = pos;
            root.isStatic = true;

            // Main structure
            BuildBox(root.transform, "Walls", Vector3.up * (height / 2),
                new Vector3(width, height, depth), wallColor);

            // Pitched roof — Victorian signature
            BuildPitchedRoof(root.transform, height, width, depth,
                Random.Range(1.4f, 2.2f), roofColor);

            // Front door
            BuildDoor(root.transform, new Vector3(Random.Range(-width * 0.15f, width * 0.15f),
                DOOR_HEIGHT / 2, depth / 2 + 0.01f), trimColor * 0.6f);

            // Windows per floor
            for (int f = 0; f < floors; f++)
            {
                float y = f * FLOOR_HEIGHT + FLOOR_HEIGHT * 0.6f;
                int winCount = Random.Range(1, 3);
                for (int w = 0; w < winCount; w++)
                {
                    float x = -width * 0.3f + w * (width * 0.35f);
                    BuildWindow(root.transform, new Vector3(x, y, depth / 2 + 0.02f), trimColor);
                }
            }

            // Front steps
            BuildBox(root.transform, "Steps", new Vector3(0, 0.2f, depth / 2 + 0.6f),
                new Vector3(1.8f, 0.4f, 1.2f), new Color(0.55f, 0.55f, 0.55f));

            // Fence for wealthier homes
            if (wealthLevel > 0.4f)
            {
                float fenceY = 0.5f;
                float fenceDepth = depth + 4f;
                Color fenceColor = wealthLevel > 0.7f
                    ? new Color(0.9f, 0.9f, 0.85f) // White picket
                    : new Color(0.35f, 0.3f, 0.25f); // Wood
                BuildBox(root.transform, "Fence_L", new Vector3(-width / 2 - 1f, fenceY, 0),
                    new Vector3(0.08f, 1f, fenceDepth), fenceColor);
                BuildBox(root.transform, "Fence_R", new Vector3(width / 2 + 1f, fenceY, 0),
                    new Vector3(0.08f, 1f, fenceDepth), fenceColor);
                BuildBox(root.transform, "Fence_F", new Vector3(0, fenceY, depth / 2 + 2f),
                    new Vector3(width + 2f, 1f, 0.08f), fenceColor);
            }

            AddBuildingCollider(root, width, height + 2.2f, depth);
            return root;
        }

        /// <summary>
        /// Apartment building — 3-5 story multi-unit. Bay Area style with fire escapes.
        /// </summary>
        public static GameObject BuildApartmentBuilding(Vector3 pos, float wealthLevel = 0.5f)
        {
            float width = Random.Range(12f, 18f);
            float depth = Random.Range(10f, 14f);
            int floors = Random.Range(3, 6);
            float height = floors * FLOOR_HEIGHT;

            float wb = 0.35f + wealthLevel * 0.3f;
            Color wallColor = Color.HSVToRGB(
                Random.Range(0.0f, 0.08f),
                Random.Range(0.1f, 0.25f),
                wb);
            Color trimColor = new Color(wb * 0.7f, wb * 0.7f, wb * 0.75f);
            Color fireEscapeColor = new Color(0.2f, 0.2f, 0.22f);

            GameObject root = new GameObject($"Apartment_{pos.x:F0}_{pos.z:F0}");
            root.transform.position = pos;
            root.isStatic = true;

            // Main structure
            BuildBox(root.transform, "Walls", Vector3.up * (height / 2),
                new Vector3(width, height, depth), wallColor);

            // Flat roof with parapet
            BuildBox(root.transform, "Roof", new Vector3(0, height + 0.1f, 0),
                new Vector3(width + 0.3f, 0.2f, depth + 0.3f), wallColor * 0.6f);
            BuildBox(root.transform, "Parapet_F", new Vector3(0, height + 0.5f, depth / 2),
                new Vector3(width + 0.3f, 0.8f, WALL_THICKNESS), wallColor * 0.65f);

            // Front entrance
            BuildDoor(root.transform, new Vector3(0, DOOR_HEIGHT / 2, depth / 2 + 0.01f),
                trimColor * 0.5f);
            // Awning over entrance
            BuildBox(root.transform, "Awning", new Vector3(0, DOOR_HEIGHT + 0.3f, depth / 2 + 0.8f),
                new Vector3(3f, 0.08f, 1.6f), trimColor);

            // Windows — grid pattern
            for (int f = 0; f < floors; f++)
            {
                float y = f * FLOOR_HEIGHT + FLOOR_HEIGHT * 0.6f;
                int winCount = Mathf.FloorToInt(width / 3f);
                for (int w = 0; w < winCount; w++)
                {
                    float x = -width * 0.4f + w * (width * 0.8f / Mathf.Max(1, winCount - 1));
                    BuildWindow(root.transform, new Vector3(x, y, depth / 2 + 0.02f), trimColor);
                }
            }

            // Fire escape — Bay Area signature
            if (floors >= 3)
            {
                float feX = -width * 0.35f;
                for (int f = 1; f < floors; f++)
                {
                    float y = f * FLOOR_HEIGHT;
                    // Platform
                    BuildBox(root.transform, $"FireEscape_Plat_{f}",
                        new Vector3(feX, y, depth / 2 + 1f),
                        new Vector3(2.5f, 0.08f, 1.2f), fireEscapeColor);
                    // Railing
                    BuildBox(root.transform, $"FireEscape_Rail_{f}",
                        new Vector3(feX, y + 0.5f, depth / 2 + 1.55f),
                        new Vector3(2.5f, 0.05f, 0.05f), fireEscapeColor);
                    // Ladder
                    BuildBox(root.transform, $"FireEscape_Ladder_{f}",
                        new Vector3(feX + 1f, y - FLOOR_HEIGHT * 0.5f, depth / 2 + 1f),
                        new Vector3(0.05f, FLOOR_HEIGHT, 0.05f), fireEscapeColor);
                }
            }

            // Roof elements — water tower or AC units
            if (Random.value > 0.5f)
            {
                BuildCylinder(root.transform, "WaterTower",
                    new Vector3(width * 0.25f, height + 2f, -depth * 0.2f),
                    1.2f, 2.5f, new Color(0.45f, 0.35f, 0.3f));
                // Tower legs
                for (int i = 0; i < 4; i++)
                {
                    float lx = width * 0.25f + Mathf.Cos(i * 90f * Mathf.Deg2Rad) * 0.8f;
                    float lz = -depth * 0.2f + Mathf.Sin(i * 90f * Mathf.Deg2Rad) * 0.8f;
                    BuildCylinder(root.transform, $"TowerLeg_{i}",
                        new Vector3(lx, height + 0.5f, lz), 0.08f, 1f, fireEscapeColor);
                }
            }
            else
            {
                BuildBox(root.transform, "AC_Unit_1",
                    new Vector3(-width * 0.2f, height + 0.6f, 0),
                    new Vector3(1.5f, 1f, 1.5f), new Color(0.5f, 0.5f, 0.52f));
                BuildBox(root.transform, "AC_Unit_2",
                    new Vector3(width * 0.15f, height + 0.6f, depth * 0.2f),
                    new Vector3(1.2f, 0.8f, 1.2f), new Color(0.48f, 0.48f, 0.5f));
            }

            AddBuildingCollider(root, width, height + 1f, depth);
            return root;
        }

        /// <summary>
        /// Convenience store / bodega — small commercial with signage and ice cooler.
        /// </summary>
        public static GameObject BuildConvenienceStore(Vector3 pos, float wealthLevel = 0.5f)
        {
            float width = Random.Range(6f, 9f);
            float depth = Random.Range(6f, 8f);
            float height = FLOOR_HEIGHT;

            Color wallColor = new Color(0.7f, 0.65f, 0.55f);
            Color signColor = Color.HSVToRGB(Random.Range(0f, 1f), 0.7f, 0.8f);
            Color awningColor = Color.HSVToRGB(Random.Range(0f, 1f), 0.5f, 0.5f);

            GameObject root = new GameObject($"Store_{pos.x:F0}_{pos.z:F0}");
            root.transform.position = pos;
            root.isStatic = true;

            BuildBox(root.transform, "Walls", Vector3.up * (height / 2),
                new Vector3(width, height, depth), wallColor);

            // Flat roof
            BuildBox(root.transform, "Roof", new Vector3(0, height + 0.08f, 0),
                new Vector3(width + 0.2f, 0.15f, depth + 0.2f), wallColor * 0.7f);

            // Large store window
            BuildBox(root.transform, "StoreWindow",
                new Vector3(-width * 0.2f, height * 0.45f, depth / 2 + 0.02f),
                new Vector3(width * 0.5f, height * 0.55f, 0.08f),
                new Color(0.6f, 0.7f, 0.8f, 0.5f));

            // Door
            BuildDoor(root.transform, new Vector3(width * 0.25f, DOOR_HEIGHT / 2, depth / 2 + 0.01f),
                new Color(0.35f, 0.35f, 0.4f));

            // Awning
            BuildBox(root.transform, "Awning",
                new Vector3(0, height * 0.88f, depth / 2 + 1.2f),
                new Vector3(width + 0.4f, 0.06f, 2.4f), awningColor);

            // Sign
            BuildBox(root.transform, "Sign",
                new Vector3(0, height + 0.5f, depth / 2 + 0.1f),
                new Vector3(width * 0.7f, 0.8f, 0.12f), signColor);

            // Ice cooler outside
            BuildBox(root.transform, "IceCooler",
                new Vector3(width * 0.4f, 0.5f, depth / 2 + 1f),
                new Vector3(0.8f, 1f, 0.6f), new Color(0.8f, 0.85f, 0.9f));

            // Newspaper stand
            BuildBox(root.transform, "NewspaperStand",
                new Vector3(-width * 0.4f, 0.5f, depth / 2 + 1.2f),
                new Vector3(0.5f, 1f, 0.4f), new Color(0.6f, 0.2f, 0.15f));

            AddBuildingCollider(root, width, height, depth);
            return root;
        }

        /// <summary>
        /// Gas station — canopy structure with pumps and mini-mart.
        /// </summary>
        public static GameObject BuildGasStation(Vector3 pos)
        {
            float width = 20f;
            float depth = 16f;

            Color canopyColor = new Color(0.85f, 0.85f, 0.88f);
            Color pumpColor = new Color(0.75f, 0.15f, 0.15f);
            Color miniMartColor = new Color(0.65f, 0.62f, 0.58f);

            GameObject root = new GameObject($"GasStation_{pos.x:F0}_{pos.z:F0}");
            root.transform.position = pos;
            root.isStatic = true;

            // Canopy structure
            BuildBox(root.transform, "Canopy",
                new Vector3(0, 4.5f, 0),
                new Vector3(12f, 0.25f, 8f), canopyColor);

            // Canopy pillars (4 corners)
            float px = 5f, pz = 3.2f;
            BuildCylinder(root.transform, "Pillar_FL", new Vector3(-px, 2.25f, pz), 0.25f, 4.5f, canopyColor * 0.9f);
            BuildCylinder(root.transform, "Pillar_FR", new Vector3(px, 2.25f, pz), 0.25f, 4.5f, canopyColor * 0.9f);
            BuildCylinder(root.transform, "Pillar_BL", new Vector3(-px, 2.25f, -pz), 0.25f, 4.5f, canopyColor * 0.9f);
            BuildCylinder(root.transform, "Pillar_BR", new Vector3(px, 2.25f, -pz), 0.25f, 4.5f, canopyColor * 0.9f);

            // Gas pumps (3 islands)
            for (int i = 0; i < 3; i++)
            {
                float x = -3.5f + i * 3.5f;
                BuildBox(root.transform, $"Pump_{i}",
                    new Vector3(x, 0.8f, 0),
                    new Vector3(0.5f, 1.6f, 0.4f), pumpColor);
                // Pump screen
                BuildBox(root.transform, $"PumpScreen_{i}",
                    new Vector3(x, 1.3f, 0.22f),
                    new Vector3(0.35f, 0.3f, 0.05f), new Color(0.1f, 0.15f, 0.1f));
            }

            // Mini-mart building
            float mmW = 7f, mmD = 5f;
            BuildBox(root.transform, "MiniMart_Walls",
                new Vector3(width * 0.3f, FLOOR_HEIGHT / 2, -depth * 0.2f),
                new Vector3(mmW, FLOOR_HEIGHT, mmD), miniMartColor);
            BuildBox(root.transform, "MiniMart_Roof",
                new Vector3(width * 0.3f, FLOOR_HEIGHT + 0.08f, -depth * 0.2f),
                new Vector3(mmW + 0.2f, 0.15f, mmD + 0.2f), miniMartColor * 0.7f);
            // Big glass window
            BuildBox(root.transform, "MiniMart_Window",
                new Vector3(width * 0.3f, FLOOR_HEIGHT * 0.45f, -depth * 0.2f + mmD / 2 + 0.02f),
                new Vector3(mmW * 0.6f, FLOOR_HEIGHT * 0.5f, 0.08f),
                new Color(0.6f, 0.7f, 0.8f, 0.5f));
            BuildDoor(root.transform,
                new Vector3(width * 0.3f - mmW * 0.25f, DOOR_HEIGHT / 2, -depth * 0.2f + mmD / 2 + 0.01f),
                new Color(0.35f, 0.35f, 0.4f));

            // Price sign (tall pole)
            BuildBox(root.transform, "PriceSign_Pole",
                new Vector3(-width * 0.35f, 4f, depth * 0.35f),
                new Vector3(0.3f, 8f, 0.3f), new Color(0.5f, 0.5f, 0.55f));
            BuildBox(root.transform, "PriceSign_Board",
                new Vector3(-width * 0.35f, 7f, depth * 0.35f),
                new Vector3(2.5f, 3f, 0.2f), canopyColor);

            // Ground markings (pump lanes)
            BuildBox(root.transform, "GroundPad",
                new Vector3(0, 0.02f, 0),
                new Vector3(13f, 0.04f, 9f), new Color(0.3f, 0.3f, 0.32f));

            AddBuildingCollider(root, width, 5f, depth);
            return root;
        }

        /// <summary>
        /// City park — open green space with benches, trees (cylinders + spheres), and paths.
        /// </summary>
        public static GameObject BuildPark(Vector3 pos, float size = 25f)
        {
            Color grassColor = new Color(0.25f, 0.45f, 0.2f);
            Color pathColor = new Color(0.6f, 0.55f, 0.45f);
            Color benchColor = new Color(0.45f, 0.3f, 0.2f);
            Color trunkColor = new Color(0.35f, 0.25f, 0.15f);
            Color foliageColor = new Color(0.2f, 0.5f, 0.2f);

            GameObject root = new GameObject($"Park_{pos.x:F0}_{pos.z:F0}");
            root.transform.position = pos;

            // Grass ground
            BuildBox(root.transform, "Grass",
                new Vector3(0, 0.02f, 0),
                new Vector3(size, 0.04f, size), grassColor);

            // Walking paths (cross pattern)
            BuildBox(root.transform, "Path_NS", new Vector3(0, 0.04f, 0),
                new Vector3(2f, 0.03f, size * 0.9f), pathColor);
            BuildBox(root.transform, "Path_EW", new Vector3(0, 0.04f, 0),
                new Vector3(size * 0.9f, 0.03f, 2f), pathColor);

            // Trees — trunks (cylinders) + foliage (spheres)
            int treeCount = Mathf.FloorToInt(size / 5f);
            for (int i = 0; i < treeCount; i++)
            {
                float tx = Random.Range(-size * 0.4f, size * 0.4f);
                float tz = Random.Range(-size * 0.4f, size * 0.4f);

                // Skip if too close to paths
                if (Mathf.Abs(tx) < 2f || Mathf.Abs(tz) < 2f) continue;

                float trunkHeight = Random.Range(3f, 5f);
                float canopyRadius = Random.Range(1.5f, 3f);
                Color leafColor = Color.HSVToRGB(
                    Random.Range(0.22f, 0.38f), // Green hue range
                    Random.Range(0.4f, 0.7f),
                    Random.Range(0.3f, 0.55f));

                BuildCylinder(root.transform, $"Trunk_{i}",
                    new Vector3(tx, trunkHeight / 2, tz),
                    0.15f, trunkHeight, trunkColor);

                // Foliage — sphere approximated by scaled cylinder + box
                GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                foliage.name = $"Foliage_{i}";
                foliage.transform.SetParent(root.transform);
                foliage.transform.localPosition = new Vector3(tx, trunkHeight + canopyRadius * 0.5f, tz);
                foliage.transform.localScale = Vector3.one * canopyRadius * 2f;
                foliage.isStatic = true;
                Object.DestroyImmediate(foliage.GetComponent<Collider>());
                SetColor(foliage, leafColor);
            }

            // Benches (4 around the center)
            Vector3[] benchPositions = {
                new Vector3(3f, 0, 3f), new Vector3(-3f, 0, 3f),
                new Vector3(3f, 0, -3f), new Vector3(-3f, 0, -3f)
            };
            for (int i = 0; i < benchPositions.Length; i++)
            {
                Vector3 bp = benchPositions[i];
                // Seat
                BuildBox(root.transform, $"Bench_Seat_{i}",
                    bp + new Vector3(0, 0.45f, 0),
                    new Vector3(1.5f, 0.08f, 0.45f), benchColor);
                // Back
                BuildBox(root.transform, $"Bench_Back_{i}",
                    bp + new Vector3(0, 0.7f, -0.2f),
                    new Vector3(1.5f, 0.5f, 0.06f), benchColor);
                // Legs
                BuildBox(root.transform, $"Bench_Leg_L_{i}",
                    bp + new Vector3(-0.6f, 0.22f, 0),
                    new Vector3(0.06f, 0.45f, 0.45f), new Color(0.3f, 0.3f, 0.3f));
                BuildBox(root.transform, $"Bench_Leg_R_{i}",
                    bp + new Vector3(0.6f, 0.22f, 0),
                    new Vector3(0.06f, 0.45f, 0.45f), new Color(0.3f, 0.3f, 0.3f));
            }

            // Fountain in center (if park is large enough)
            if (size >= 20f)
            {
                BuildCylinder(root.transform, "Fountain_Base",
                    new Vector3(0, 0.3f, 0), 2f, 0.6f, new Color(0.55f, 0.55f, 0.58f));
                BuildCylinder(root.transform, "Fountain_Column",
                    new Vector3(0, 1.2f, 0), 0.3f, 1.8f, new Color(0.55f, 0.55f, 0.58f));
                BuildCylinder(root.transform, "Fountain_Bowl",
                    new Vector3(0, 1.8f, 0), 1f, 0.3f, new Color(0.55f, 0.55f, 0.58f));
                // Water (semi-transparent blue)
                BuildCylinder(root.transform, "Fountain_Water",
                    new Vector3(0, 0.35f, 0), 1.8f, 0.1f, new Color(0.2f, 0.35f, 0.6f, 0.6f));
            }

            return root;
        }

        /// <summary>
        /// Industrial building — large corrugated warehouse/factory with pipes and smokestacks.
        /// </summary>
        public static GameObject BuildIndustrialBuilding(Vector3 pos)
        {
            float width = Random.Range(16f, 24f);
            float depth = Random.Range(12f, 18f);
            float height = FLOOR_HEIGHT * Random.Range(1.5f, 2.5f);

            Color wallColor = new Color(
                Random.Range(0.4f, 0.55f),
                Random.Range(0.38f, 0.5f),
                Random.Range(0.36f, 0.48f));
            Color roofColor = wallColor * 0.75f;
            Color pipeColor = new Color(0.55f, 0.5f, 0.45f);

            GameObject root = new GameObject($"Industrial_{pos.x:F0}_{pos.z:F0}");
            root.transform.position = pos;
            root.isStatic = true;

            // Main structure
            BuildBox(root.transform, "Walls", Vector3.up * (height / 2),
                new Vector3(width, height, depth), wallColor);

            // Metal roof (slightly raised arch)
            BuildBox(root.transform, "Roof", new Vector3(0, height + 0.4f, 0),
                new Vector3(width * 0.95f, 0.8f, depth + 0.3f), roofColor);

            // Loading bay
            BuildBox(root.transform, "LoadingDoor",
                new Vector3(-width * 0.3f, 2f, depth / 2 + 0.01f),
                new Vector3(4.5f, 4f, 0.12f), new Color(0.5f, 0.45f, 0.3f));

            // Smokestacks
            int stacks = Random.Range(1, 4);
            for (int i = 0; i < stacks; i++)
            {
                float sx = Random.Range(-width * 0.3f, width * 0.3f);
                float sz = Random.Range(-depth * 0.3f, depth * 0.3f);
                float stackH = Random.Range(3f, 6f);
                BuildCylinder(root.transform, $"Smokestack_{i}",
                    new Vector3(sx, height + stackH / 2, sz),
                    Random.Range(0.3f, 0.6f), stackH, pipeColor);
            }

            // External pipes along side wall
            float pipeY = height * 0.6f;
            BuildCylinder(root.transform, "Pipe_Side",
                new Vector3(width / 2 + 0.3f, pipeY, 0), 0.12f, depth * 0.7f, pipeColor);
            // Rotate pipe horizontal
            root.transform.Find("Pipe_Side").localEulerAngles = new Vector3(0, 0, 90);

            // Corrugated texture (horizontal stripes)
            for (int s = 0; s < 3; s++)
            {
                float sy = height * (0.2f + s * 0.3f);
                BuildBox(root.transform, $"CorrugationStripe_{s}",
                    new Vector3(0, sy, depth / 2 + 0.02f),
                    new Vector3(width, 0.05f, 0.03f), wallColor * 0.85f);
            }

            AddBuildingCollider(root, width, height + 1f, depth);
            return root;
        }

        /// <summary>
        /// Row houses / townhomes — connected units, very Bay Area. Painted Ladies style.
        /// </summary>
        public static GameObject BuildRowHouses(Vector3 pos, int unitCount = 4, float wealthLevel = 0.5f)
        {
            float unitWidth = Random.Range(4.5f, 6f);
            float totalWidth = unitWidth * unitCount;
            float depth = Random.Range(8f, 11f);
            float height = FLOOR_HEIGHT * Random.Range(2, 4);

            GameObject root = new GameObject($"RowHouses_{pos.x:F0}_{pos.z:F0}");
            root.transform.position = pos;
            root.isStatic = true;

            for (int u = 0; u < unitCount; u++)
            {
                float x = -totalWidth / 2 + unitWidth * (u + 0.5f);

                // Each unit gets a unique color — Painted Ladies!
                Color unitColor = Color.HSVToRGB(
                    Random.Range(0f, 1f),
                    0.15f + wealthLevel * 0.25f,
                    0.5f + wealthLevel * 0.3f);
                Color trimC = unitColor * 1.15f;

                // Slight height variation per unit
                float unitH = height + Random.Range(-0.5f, 0.8f);

                // Walls
                BuildBox(root.transform, $"Unit_{u}_Walls",
                    new Vector3(x, unitH / 2, 0),
                    new Vector3(unitWidth - 0.15f, unitH, depth), unitColor);

                // Cornice/crown molding at top
                BuildBox(root.transform, $"Unit_{u}_Cornice",
                    new Vector3(x, unitH + 0.15f, depth / 2),
                    new Vector3(unitWidth, 0.3f, 0.3f), trimC);

                // Bay window (projecting) — classic SF
                if (wealthLevel > 0.3f && Random.value > 0.3f)
                {
                    float bayY = FLOOR_HEIGHT * 1.5f;
                    BuildBox(root.transform, $"Unit_{u}_BayWindow",
                        new Vector3(x, bayY, depth / 2 + 0.6f),
                        new Vector3(unitWidth * 0.6f, FLOOR_HEIGHT * 0.7f, 1.2f),
                        new Color(unitColor.r * 0.95f, unitColor.g * 0.95f, unitColor.b * 0.95f));
                    // Bay window glass
                    BuildBox(root.transform, $"Unit_{u}_BayGlass",
                        new Vector3(x, bayY, depth / 2 + 1.22f),
                        new Vector3(unitWidth * 0.55f, FLOOR_HEIGHT * 0.55f, 0.06f),
                        new Color(0.6f, 0.7f, 0.8f, 0.5f));
                }

                // Door
                BuildDoor(root.transform,
                    new Vector3(x, DOOR_HEIGHT / 2, depth / 2 + 0.01f), trimC * 0.5f);

                // Steps
                BuildBox(root.transform, $"Unit_{u}_Steps",
                    new Vector3(x, 0.3f, depth / 2 + 0.8f),
                    new Vector3(1.5f, 0.6f, 1.6f), new Color(0.55f, 0.55f, 0.55f));

                // Upper windows
                for (int f = 1; f < Mathf.FloorToInt(unitH / FLOOR_HEIGHT); f++)
                {
                    float wy = f * FLOOR_HEIGHT + FLOOR_HEIGHT * 0.6f;
                    BuildWindow(root.transform, new Vector3(x, wy, depth / 2 + 0.02f), trimC);
                }
            }

            // Shared pitched roof
            BuildPitchedRoof(root.transform, height, totalWidth, depth, 1.8f,
                new Color(0.3f, 0.28f, 0.25f));

            AddBuildingCollider(root, totalWidth, height + 2f, depth);
            return root;
        }

        /// <summary>
        /// Office building — glass and steel, 4-8 stories. Downtown commercial.
        /// </summary>
        public static GameObject BuildOfficeBuilding(Vector3 pos, float wealthLevel = 0.7f)
        {
            float width = Random.Range(14f, 22f);
            float depth = Random.Range(12f, 18f);
            int floors = Random.Range(4, 9);
            float height = floors * FLOOR_HEIGHT;

            Color frameColor = new Color(0.35f, 0.38f, 0.42f);
            Color glassColor = new Color(0.45f, 0.55f, 0.65f, 0.7f);

            GameObject root = new GameObject($"Office_{pos.x:F0}_{pos.z:F0}");
            root.transform.position = pos;
            root.isStatic = true;

            // Steel frame structure
            BuildBox(root.transform, "Frame", Vector3.up * (height / 2),
                new Vector3(width, height, depth), frameColor);

            // Glass curtain wall — front face
            for (int f = 0; f < floors; f++)
            {
                float y = f * FLOOR_HEIGHT + FLOOR_HEIGHT * 0.5f;
                BuildBox(root.transform, $"Glass_F_{f}",
                    new Vector3(0, y, depth / 2 + 0.05f),
                    new Vector3(width * 0.9f, FLOOR_HEIGHT * 0.7f, 0.08f), glassColor);
                // Floor separator
                BuildBox(root.transform, $"FloorLine_F_{f}",
                    new Vector3(0, f * FLOOR_HEIGHT, depth / 2 + 0.06f),
                    new Vector3(width * 0.92f, 0.12f, 0.05f), frameColor * 0.7f);
            }

            // Side glass
            for (int f = 0; f < floors; f++)
            {
                float y = f * FLOOR_HEIGHT + FLOOR_HEIGHT * 0.5f;
                BuildBox(root.transform, $"Glass_R_{f}",
                    new Vector3(width / 2 + 0.05f, y, 0),
                    new Vector3(0.08f, FLOOR_HEIGHT * 0.7f, depth * 0.9f), glassColor);
            }

            // Flat roof
            BuildBox(root.transform, "Roof", new Vector3(0, height + 0.1f, 0),
                new Vector3(width + 0.2f, 0.2f, depth + 0.2f), frameColor * 0.8f);

            // Lobby entrance — recessed with overhang
            BuildBox(root.transform, "Lobby_Overhang",
                new Vector3(0, FLOOR_HEIGHT * 0.95f, depth / 2 + 2f),
                new Vector3(width * 0.4f, 0.15f, 4f), frameColor);

            // Revolving door
            BuildCylinder(root.transform, "RevolvingDoor",
                new Vector3(0, 1.2f, depth / 2 + 0.5f),
                0.8f, 2.4f, new Color(0.5f, 0.55f, 0.6f, 0.6f));

            // HVAC on roof
            BuildBox(root.transform, "HVAC",
                new Vector3(width * 0.2f, height + 0.8f, -depth * 0.2f),
                new Vector3(3f, 1.5f, 3f), new Color(0.5f, 0.5f, 0.52f));

            AddBuildingCollider(root, width, height, depth);
            return root;
        }

        // ═══════════════════════════════════════════════════════
        //  URBAN FURNITURE (Streetlights, hydrants, mailboxes, etc.)
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Street light pole with lamp head.
        /// </summary>
        public static GameObject BuildStreetLight(Vector3 pos)
        {
            GameObject root = new GameObject($"StreetLight_{pos.x:F0}_{pos.z:F0}");
            root.transform.position = pos;
            root.isStatic = true;

            Color poleColor = new Color(0.25f, 0.25f, 0.28f);
            Color lampColor = new Color(0.95f, 0.9f, 0.7f);

            BuildCylinder(root.transform, "Pole", new Vector3(0, 3f, 0), 0.08f, 6f, poleColor);
            // Arm
            BuildBox(root.transform, "Arm", new Vector3(0.5f, 5.8f, 0),
                new Vector3(1f, 0.06f, 0.06f), poleColor);
            // Lamp head
            BuildBox(root.transform, "Lamp", new Vector3(0.8f, 5.6f, 0),
                new Vector3(0.5f, 0.15f, 0.25f), lampColor);

            return root;
        }

        /// <summary>
        /// Fire hydrant.
        /// </summary>
        public static GameObject BuildFireHydrant(Vector3 pos)
        {
            GameObject root = new GameObject($"Hydrant_{pos.x:F0}_{pos.z:F0}");
            root.transform.position = pos;
            root.isStatic = true;

            BuildCylinder(root.transform, "Body", new Vector3(0, 0.3f, 0),
                0.12f, 0.6f, new Color(0.8f, 0.15f, 0.1f));
            BuildCylinder(root.transform, "Cap", new Vector3(0, 0.65f, 0),
                0.15f, 0.1f, new Color(0.8f, 0.15f, 0.1f));

            // Side nozzle
            BuildCylinder(root.transform, "Nozzle", new Vector3(0.12f, 0.35f, 0),
                0.04f, 0.12f, new Color(0.75f, 0.2f, 0.1f));

            return root;
        }

        /// <summary>
        /// Dumpster — rectangular container.
        /// </summary>
        public static GameObject BuildDumpster(Vector3 pos)
        {
            GameObject root = new GameObject($"Dumpster_{pos.x:F0}_{pos.z:F0}");
            root.transform.position = pos;
            root.isStatic = true;

            Color color = new Color(0.15f, 0.35f, 0.2f); // Dark green
            BuildBox(root.transform, "Body", new Vector3(0, 0.6f, 0),
                new Vector3(1.8f, 1.2f, 1.2f), color);
            // Lid
            BuildBox(root.transform, "Lid", new Vector3(0, 1.22f, 0),
                new Vector3(1.85f, 0.05f, 1.25f), color * 0.8f);

            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.6f, 0);
            col.size = new Vector3(1.8f, 1.2f, 1.2f);

            return root;
        }

        /// <summary>
        /// Parked car — simple box approximation.
        /// </summary>
        public static GameObject BuildParkedCar(Vector3 pos, float yRotation = 0f)
        {
            Color bodyColor = Color.HSVToRGB(Random.Range(0f, 1f), Random.Range(0.2f, 0.7f), Random.Range(0.3f, 0.8f));
            Color wheelColor = new Color(0.15f, 0.15f, 0.15f);
            Color windowColor = new Color(0.3f, 0.35f, 0.45f, 0.7f);

            GameObject root = new GameObject($"Car_{pos.x:F0}_{pos.z:F0}");
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0, yRotation, 0);
            root.isStatic = true;

            // Body
            BuildBox(root.transform, "Body", new Vector3(0, 0.6f, 0),
                new Vector3(2f, 0.7f, 4.5f), bodyColor);
            // Cabin
            BuildBox(root.transform, "Cabin", new Vector3(0, 1.1f, -0.2f),
                new Vector3(1.7f, 0.6f, 2f), bodyColor * 0.95f);
            // Windshield
            BuildBox(root.transform, "Windshield", new Vector3(0, 1.15f, 0.82f),
                new Vector3(1.6f, 0.5f, 0.06f), windowColor);
            // Rear window
            BuildBox(root.transform, "RearWindow", new Vector3(0, 1.15f, -1.22f),
                new Vector3(1.6f, 0.5f, 0.06f), windowColor);

            // Wheels (4)
            float wx = 0.85f, wz = 1.3f;
            BuildCylinder(root.transform, "Wheel_FL", new Vector3(-wx, 0.25f, wz), 0.25f, 0.2f, wheelColor);
            BuildCylinder(root.transform, "Wheel_FR", new Vector3(wx, 0.25f, wz), 0.25f, 0.2f, wheelColor);
            BuildCylinder(root.transform, "Wheel_BL", new Vector3(-wx, 0.25f, -wz), 0.25f, 0.2f, wheelColor);
            BuildCylinder(root.transform, "Wheel_BR", new Vector3(wx, 0.25f, -wz), 0.25f, 0.2f, wheelColor);

            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.7f, 0);
            col.size = new Vector3(2f, 1.4f, 4.5f);

            return root;
        }

        // ═══════════════════════════════════════════════════════
        //  POLICE STATIONS (Spec v2.0 Section 29)
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Build a standard police precinct — 2-story institutional building with
        /// badge emblem, barred windows, dispatch antenna, and parking bay.
        /// Attaches PoliceStation component with configured spawn/arrest points.
        /// </summary>
        public static GameObject BuildPoliceStation(Vector3 position, string stationName)
        {
            float width = 16f;
            float depth = 10f;
            int floors = 2;
            float height = floors * FLOOR_HEIGHT;

            Color wallColor = new Color(0.55f, 0.55f, 0.62f);    // Institutional blue-grey
            Color roofColor = new Color(0.30f, 0.30f, 0.35f);    // Dark slate
            Color trimColor = new Color(0.20f, 0.30f, 0.55f);    // Police blue
            Color doorColor = new Color(0.25f, 0.25f, 0.30f);    // Steel
            Color badgeColor = new Color(0.85f, 0.75f, 0.20f);   // Gold badge
            Color barColor = new Color(0.3f, 0.3f, 0.3f);        // Window bars

            GameObject root = new GameObject(stationName);
            root.transform.position = position;

            // ── Main building ──────────────────────────────────
            BuildBox(root.transform, "Walls", Vector3.up * (height / 2),
                new Vector3(width, height, depth), wallColor);

            // Flat institutional roof with parapet
            BuildBox(root.transform, "Roof", new Vector3(0, height + 0.1f, 0),
                new Vector3(width + 0.3f, 0.2f, depth + 0.3f), roofColor);
            BuildBox(root.transform, "Parapet_F", new Vector3(0, height + 0.5f, depth / 2),
                new Vector3(width + 0.3f, 0.8f, WALL_THICKNESS), roofColor);
            BuildBox(root.transform, "Parapet_B", new Vector3(0, height + 0.5f, -depth / 2),
                new Vector3(width + 0.3f, 0.8f, WALL_THICKNESS), roofColor);
            BuildBox(root.transform, "Parapet_L", new Vector3(-width / 2, height + 0.5f, 0),
                new Vector3(WALL_THICKNESS, 0.8f, depth + 0.3f), roofColor);
            BuildBox(root.transform, "Parapet_R", new Vector3(width / 2, height + 0.5f, 0),
                new Vector3(WALL_THICKNESS, 0.8f, depth + 0.3f), roofColor);

            // ── Blue stripe — police accent band ───────────────
            BuildBox(root.transform, "BlueStripe_F", new Vector3(0, height * 0.42f, depth / 2 + 0.02f),
                new Vector3(width, 0.25f, 0.05f), trimColor);
            BuildBox(root.transform, "BlueStripe_B", new Vector3(0, height * 0.42f, -depth / 2 - 0.02f),
                new Vector3(width, 0.25f, 0.05f), trimColor);

            // ── Entrance — double doors with overhang ──────────
            // Concrete canopy over entrance
            BuildBox(root.transform, "Canopy", new Vector3(0, FLOOR_HEIGHT * 0.95f, depth / 2 + 1.5f),
                new Vector3(5f, 0.2f, 3f), new Color(0.45f, 0.45f, 0.50f));
            // Canopy support pillars
            BuildCylinder(root.transform, "Pillar_L", new Vector3(-2f, FLOOR_HEIGHT * 0.47f, depth / 2 + 2.5f),
                0.2f, FLOOR_HEIGHT * 0.95f, new Color(0.50f, 0.50f, 0.55f));
            BuildCylinder(root.transform, "Pillar_R", new Vector3(2f, FLOOR_HEIGHT * 0.47f, depth / 2 + 2.5f),
                0.2f, FLOOR_HEIGHT * 0.95f, new Color(0.50f, 0.50f, 0.55f));

            // Double doors
            BuildBox(root.transform, "Door_L", new Vector3(-0.65f, DOOR_HEIGHT / 2, depth / 2 + 0.01f),
                new Vector3(DOOR_WIDTH, DOOR_HEIGHT, 0.08f), doorColor);
            BuildBox(root.transform, "Door_R", new Vector3(0.65f, DOOR_HEIGHT / 2, depth / 2 + 0.01f),
                new Vector3(DOOR_WIDTH, DOOR_HEIGHT, 0.08f), doorColor);
            // Door frame
            BuildBox(root.transform, "DoorFrame_Top", new Vector3(0, DOOR_HEIGHT + 0.06f, depth / 2 + 0.01f),
                new Vector3(DOOR_WIDTH * 2 + 0.4f, 0.12f, 0.12f), doorColor * 0.7f);

            // ── Front steps ────────────────────────────────────
            BuildBox(root.transform, "Steps_1", new Vector3(0, 0.1f, depth / 2 + 0.8f),
                new Vector3(4f, 0.2f, 1.6f), new Color(0.5f, 0.5f, 0.5f));
            BuildBox(root.transform, "Steps_2", new Vector3(0, 0.3f, depth / 2 + 0.4f),
                new Vector3(4f, 0.2f, 0.8f), new Color(0.5f, 0.5f, 0.5f));

            // ── Badge emblem on facade ─────────────────────────
            // Shield shape approximated with overlapping box + cylinder
            BuildBox(root.transform, "Badge_Back", new Vector3(0, height * 0.75f, depth / 2 + 0.03f),
                new Vector3(1.8f, 2.2f, 0.08f), trimColor);
            BuildCylinder(root.transform, "Badge_Star", new Vector3(0, height * 0.76f, depth / 2 + 0.08f),
                0.5f, 0.08f, badgeColor);

            // ── Barred windows (1st floor — detention level) ───
            for (int i = 0; i < 3; i++)
            {
                float x = -width * 0.3f + i * (width * 0.3f);
                // Skip center window on ground floor (door is there)
                if (i == 1) continue;

                float y = FLOOR_HEIGHT * 0.6f;
                BuildWindow(root.transform, new Vector3(x, y, depth / 2 + 0.02f), barColor);
                // Vertical bars over window
                for (int b = 0; b < 3; b++)
                {
                    float bx = x - 0.3f + b * 0.3f;
                    BuildBox(root.transform, $"Bar_{i}_{b}",
                        new Vector3(bx, y, depth / 2 + 0.05f),
                        new Vector3(0.04f, WINDOW_SIZE, 0.04f), barColor);
                }
            }

            // ── 2nd floor windows (office level — no bars) ─────
            float y2 = FLOOR_HEIGHT + FLOOR_HEIGHT * 0.55f;
            for (int i = 0; i < 4; i++)
            {
                float x = -width * 0.35f + i * (width * 0.23f);
                BuildWindow(root.transform, new Vector3(x, y2, depth / 2 + 0.02f), trimColor);
            }

            // ── Radio antenna on roof ──────────────────────────
            BuildCylinder(root.transform, "Antenna_Mast", new Vector3(width * 0.3f, height + 2.5f, -depth * 0.3f),
                0.06f, 4.5f, new Color(0.5f, 0.5f, 0.5f));
            // Antenna crossbars
            BuildBox(root.transform, "Antenna_Cross1", new Vector3(width * 0.3f, height + 3.5f, -depth * 0.3f),
                new Vector3(1.5f, 0.06f, 0.06f), new Color(0.5f, 0.5f, 0.5f));
            BuildBox(root.transform, "Antenna_Cross2", new Vector3(width * 0.3f, height + 4f, -depth * 0.3f),
                new Vector3(1f, 0.06f, 0.06f), new Color(0.5f, 0.5f, 0.5f));

            // ── Satellite dish on roof ─────────────────────────
            BuildCylinder(root.transform, "Dish_Base", new Vector3(-width * 0.3f, height + 0.5f, depth * 0.2f),
                0.15f, 0.6f, new Color(0.45f, 0.45f, 0.45f));
            BuildCylinder(root.transform, "Dish_Bowl", new Vector3(-width * 0.3f, height + 1.0f, depth * 0.2f),
                0.6f, 0.1f, new Color(0.8f, 0.8f, 0.82f));

            // ── Parking bay (side of building) ─────────────────
            BuildBox(root.transform, "ParkingPad", new Vector3(width / 2 + 3f, 0.02f, 0),
                new Vector3(5f, 0.04f, depth * 0.8f), new Color(0.25f, 0.25f, 0.28f));
            // Parking stripe
            BuildBox(root.transform, "ParkStripe_1", new Vector3(width / 2 + 1.5f, 0.03f, -depth * 0.2f),
                new Vector3(0.1f, 0.02f, 3f), Color.white);
            BuildBox(root.transform, "ParkStripe_2", new Vector3(width / 2 + 4.5f, 0.03f, -depth * 0.2f),
                new Vector3(0.1f, 0.02f, 3f), Color.white);

            // ── Flagpole ───────────────────────────────────────
            BuildCylinder(root.transform, "Flagpole", new Vector3(-width * 0.4f, 3f, depth / 2 + 3f),
                0.05f, 6f, new Color(0.6f, 0.6f, 0.6f));
            BuildBox(root.transform, "Flag", new Vector3(-width * 0.4f + 0.6f, 5.5f, depth / 2 + 3f),
                new Vector3(1.2f, 0.7f, 0.02f), new Color(0.1f, 0.2f, 0.6f));

            // ── Colliders ──────────────────────────────────────
            AddBuildingCollider(root, width, height + 1f, depth);

            // ── PoliceStation component ────────────────────────
            PoliceStation ps = root.AddComponent<PoliceStation>();
            ps.stationName = stationName;
            ps.spawnOffset = new Vector3(width / 2 + 3f, 0f, 0f);  // Spawn in parking bay
            ps.arrestPoint = new Vector3(0f, 0f, depth / 2 + 2f);  // Arrest at front entrance

            return root;
        }

        /// <summary>
        /// Build a police station with attached jail wing — larger complex with
        /// cell block extension, exercise yard, razor wire fencing, watchtower,
        /// and sally port (secure vehicle entrance).
        /// </summary>
        public static GameObject BuildPoliceStationWithJail(Vector3 position, string stationName)
        {
            // Build the main station first
            GameObject root = BuildPoliceStation(position, stationName);

            float mainDepth = 10f;

            Color cellColor = new Color(0.48f, 0.48f, 0.52f);    // Colder grey
            Color fenceColor = new Color(0.4f, 0.4f, 0.4f);      // Chain-link grey
            Color wireColor = new Color(0.55f, 0.55f, 0.55f);    // Razor wire
            Color yardColor = new Color(0.35f, 0.38f, 0.34f);    // Concrete yard
            Color towerColor = new Color(0.45f, 0.45f, 0.50f);

            // ── Jail wing (extends from rear of station) ───────
            float jailWidth = 12f;
            float jailDepth = 14f;
            float jailHeight = FLOOR_HEIGHT * 1.5f; // Lower, more oppressive

            BuildBox(root.transform, "JailWing_Walls", new Vector3(0, jailHeight / 2, -mainDepth / 2 - jailDepth / 2),
                new Vector3(jailWidth, jailHeight, jailDepth), cellColor);

            // Jail flat roof
            BuildBox(root.transform, "JailWing_Roof", new Vector3(0, jailHeight + 0.1f, -mainDepth / 2 - jailDepth / 2),
                new Vector3(jailWidth + 0.2f, 0.15f, jailDepth + 0.2f), cellColor * 0.75f);

            // ── Cell windows (small, barred) ───────────────────
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    float x = -jailWidth * 0.35f + col * (jailWidth * 0.23f);
                    float z = -mainDepth / 2 - jailDepth * 0.3f - row * jailDepth * 0.35f;

                    // Small slit window
                    BuildBox(root.transform, $"CellWindow_{row}_{col}",
                        new Vector3(x, jailHeight * 0.7f, jailWidth / 2 + 0.02f),
                        new Vector3(0.5f, 0.3f, 0.04f), new Color(0.4f, 0.45f, 0.5f, 0.5f));

                    // Heavy bars
                    for (int b = 0; b < 2; b++)
                    {
                        BuildBox(root.transform, $"CellBar_{row}_{col}_{b}",
                            new Vector3(x - 0.12f + b * 0.24f, jailHeight * 0.7f, jailWidth / 2 + 0.04f),
                            new Vector3(0.05f, 0.35f, 0.05f), new Color(0.25f, 0.25f, 0.25f));
                    }
                }
            }

            // ── Exercise yard ──────────────────────────────────
            float yardX = jailWidth / 2 + 5f;
            float yardZ = -mainDepth / 2 - jailDepth / 2;
            float yardSize = 10f;

            // Yard surface
            BuildBox(root.transform, "Yard_Floor", new Vector3(yardX, 0.02f, yardZ),
                new Vector3(yardSize, 0.04f, yardSize), yardColor);

            // Perimeter fence (4 walls)
            float fenceHeight = 3.5f;
            float fenceThick = 0.08f;
            BuildBox(root.transform, "Fence_N", new Vector3(yardX, fenceHeight / 2, yardZ + yardSize / 2),
                new Vector3(yardSize, fenceHeight, fenceThick), fenceColor);
            BuildBox(root.transform, "Fence_S", new Vector3(yardX, fenceHeight / 2, yardZ - yardSize / 2),
                new Vector3(yardSize, fenceHeight, fenceThick), fenceColor);
            BuildBox(root.transform, "Fence_E", new Vector3(yardX + yardSize / 2, fenceHeight / 2, yardZ),
                new Vector3(fenceThick, fenceHeight, yardSize), fenceColor);
            BuildBox(root.transform, "Fence_W", new Vector3(yardX - yardSize / 2, fenceHeight / 2, yardZ),
                new Vector3(fenceThick, fenceHeight, yardSize), fenceColor);

            // Razor wire on top of fence
            float wireY = fenceHeight + 0.2f;
            BuildCylinder(root.transform, "Wire_N", new Vector3(yardX, wireY, yardZ + yardSize / 2),
                0.15f, 0.3f, wireColor);
            BuildCylinder(root.transform, "Wire_S", new Vector3(yardX, wireY, yardZ - yardSize / 2),
                0.15f, 0.3f, wireColor);
            BuildCylinder(root.transform, "Wire_E", new Vector3(yardX + yardSize / 2, wireY, yardZ),
                0.15f, 0.3f, wireColor);

            // ── Watchtower ─────────────────────────────────────
            float towerX = yardX + yardSize / 2;
            float towerZ = yardZ + yardSize / 2;
            float towerHeight = 8f;

            // Tower legs (4 cylinders)
            float legSpacing = 1.2f;
            for (int lx = 0; lx < 2; lx++)
            {
                for (int lz = 0; lz < 2; lz++)
                {
                    float legX = towerX - legSpacing / 2 + lx * legSpacing;
                    float legZ = towerZ - legSpacing / 2 + lz * legSpacing;
                    BuildCylinder(root.transform, $"TowerLeg_{lx}_{lz}",
                        new Vector3(legX, towerHeight / 2, legZ),
                        0.12f, towerHeight, towerColor);
                }
            }

            // Tower cabin
            BuildBox(root.transform, "TowerCabin", new Vector3(towerX, towerHeight, towerZ),
                new Vector3(3f, 2.5f, 3f), towerColor);

            // Tower roof
            BuildBox(root.transform, "TowerRoof", new Vector3(towerX, towerHeight + 1.4f, towerZ),
                new Vector3(3.5f, 0.15f, 3.5f), towerColor * 0.7f);

            // Searchlight
            BuildCylinder(root.transform, "Searchlight", new Vector3(towerX, towerHeight + 1.8f, towerZ),
                0.2f, 0.4f, new Color(0.9f, 0.9f, 0.7f));

            // ── Sally port (secure vehicle entrance) ───────────
            float sallyX = -jailWidth / 2 - 2.5f;
            float sallyZ = -mainDepth / 2 - jailDepth * 0.3f;

            // Garage-style secure entrance
            BuildBox(root.transform, "SallyPort_Walls", new Vector3(sallyX, 2f, sallyZ),
                new Vector3(5f, 4f, 6f), cellColor * 0.9f);
            BuildBox(root.transform, "SallyPort_Roof", new Vector3(sallyX, 4.1f, sallyZ),
                new Vector3(5.3f, 0.15f, 6.3f), cellColor * 0.75f);
            // Rolling gate
            BuildBox(root.transform, "SallyGate", new Vector3(sallyX, 1.75f, sallyZ + 3.01f),
                new Vector3(3.5f, 3.5f, 0.1f), new Color(0.35f, 0.35f, 0.4f));

            // ── Collider for jail wing ─────────────────────────
            // Add a second collider for the jail extension
            BoxCollider jailCol = root.AddComponent<BoxCollider>();
            jailCol.center = new Vector3(0, jailHeight / 2, -mainDepth / 2 - jailDepth / 2);
            jailCol.size = new Vector3(jailWidth, jailHeight, jailDepth);

            // Update PoliceStation arrest point to jail sally port
            PoliceStation ps = root.GetComponent<PoliceStation>();
            if (ps != null)
            {
                ps.arrestPoint = new Vector3(sallyX, 0f, sallyZ + 4f);
            }

            return root;
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
