using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Properties;

namespace Clout.World.Districts
{
    /// <summary>
    /// Procedural district generator — builds complete city districts from DistrictDefinition configs.
    ///
    /// Spec v2.0 Step 8: Multi-block generation with road networks, zone-typed building clusters,
    /// POI placement, ambient urban furniture, and NavMesh-ready terrain.
    ///
    /// Pipeline:
    ///   1. Layout road grid (major streets → minor streets → alleys)
    ///   2. Divide blocks into lots via Poisson-disk-like spacing
    ///   3. Assign zone type to each lot (residential/commercial/industrial/waterfront)
    ///   4. Place buildings appropriate to zone type + wealth level
    ///   5. Place player-purchasable properties at designated slots
    ///   6. Populate urban furniture (streetlights, hydrants, dumpsters, parked cars)
    ///   7. Place parks and open spaces
    ///   8. Generate district trigger zone + DistrictManager wiring
    ///
    /// Bay Area architectural language:
    ///   - Painted Ladies row houses (residential zones)
    ///   - Fire-escape apartments (mixed zones)
    ///   - Industrial warehouses with smokestacks (industrial zones)
    ///   - Glass office towers (commercial/downtown zones)
    ///   - Hills modeled via gentle terrain undulation (wealthLevel affects elevation)
    ///
    /// Call GenerateDistrict() from editor (TestArenaBuilder) or at runtime (scene streaming).
    /// All geometry uses ProceduralPropertyBuilder primitives — no external assets required.
    /// </summary>
    public static class ProceduralDistrictGenerator
    {
        // ─── Constants ──────────────────────────────────────────

        private const float ROAD_Y = 0.01f;            // Road surface Y offset
        private const float SIDEWALK_Y = 0.08f;         // Sidewalk raised above road
        private const float SIDEWALK_HEIGHT = 0.15f;     // Curb height
        private const float LOT_PADDING = 2f;            // Meters between buildings
        private const float STREET_LIGHT_SPACING = 20f;  // Meters between lights
        private const float HYDRANT_SPACING = 40f;
        private const float CAR_PROBABILITY = 0.3f;      // Chance of parked car per parking slot

        // Color palettes
        private static readonly Color ROAD_COLOR = new Color(0.22f, 0.22f, 0.24f);
        private static readonly Color SIDEWALK_COLOR = new Color(0.6f, 0.58f, 0.55f);
        private static readonly Color CROSSWALK_COLOR = new Color(0.85f, 0.85f, 0.8f);
        private static readonly Color LANE_MARKING_COLOR = new Color(0.9f, 0.85f, 0.2f);

        // ═══════════════════════════════════════════════════════
        //  MAIN ENTRY POINT
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Generate a complete district from definition.
        /// Returns the root GameObject containing all geometry.
        /// </summary>
        public static GameObject GenerateDistrict(DistrictDefinition def)
        {
            if (def == null)
            {
                Debug.LogError("[DistrictGen] Null DistrictDefinition!");
                return new GameObject("ERROR_NullDistrict");
            }

            GameObject root = new GameObject($"District_{def.districtId}");
            root.transform.position = def.worldCenter;

            float halfW = def.halfExtents.x;
            float halfD = def.halfExtents.z;

            // ─── Phase 1: Ground Plane ──────────────────────────
            GenerateGroundPlane(root.transform, halfW * 2f, halfD * 2f, def);

            // ─── Phase 2: Road Network ──────────────────────────
            var roadNetwork = GenerateRoadNetwork(root.transform, def);

            // ─── Phase 3: City Blocks & Building Lots ───────────
            var blocks = IdentifyBlocks(roadNetwork, halfW, halfD, def);
            PlaceBuildings(root.transform, blocks, def);

            // ─── Phase 4: Player Properties (POIs) ──────────────
            PlacePlayerProperties(root.transform, blocks, def);

            // ─── Phase 5: Urban Furniture ───────────────────────
            PlaceUrbanFurniture(root.transform, roadNetwork, def);

            // ─── Phase 6: Parks & Open Space ────────────────────
            PlaceParks(root.transform, blocks, def);

            // ─── Phase 7: District Trigger Zone ─────────────────
            AttachDistrictTrigger(root, def);

            Debug.Log($"[DistrictGen] Generated '{def.districtName}': " +
                $"{roadNetwork.streetSegments.Count} road segments, " +
                $"{blocks.Count} city blocks");

            return root;
        }

        // ═══════════════════════════════════════════════════════
        //  PHASE 1: GROUND PLANE
        // ═══════════════════════════════════════════════════════

        private static void GenerateGroundPlane(Transform parent, float width, float depth, DistrictDefinition def)
        {
            // Main terrain with district-appropriate ground color
            Color groundColor = GetGroundColor(def);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(parent);
            ground.transform.localPosition = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(width + 10f, 1f, depth + 10f);
            ground.isStatic = true;

            Renderer r = ground.GetComponent<Renderer>();
            if (r != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    Material mat = new Material(shader);
                    mat.color = groundColor;
                    r.sharedMaterial = mat;
                }
            }

            // NavMeshSurface for AI pathfinding
            ground.AddComponent<NavMeshSurface>();
        }

        private static Color GetGroundColor(DistrictDefinition def)
        {
            if (def.industrialRatio > 0.3f)
                return new Color(0.32f, 0.30f, 0.28f); // Industrial grey
            if (def.wealthLevel > 0.7f)
                return new Color(0.38f, 0.40f, 0.36f); // Clean green-grey
            if (def.waterfrontRatio > 0.2f)
                return new Color(0.35f, 0.37f, 0.34f); // Sandy grey
            return new Color(0.35f, 0.36f, 0.33f);      // Standard urban
        }

        // ═══════════════════════════════════════════════════════
        //  PHASE 2: ROAD NETWORK
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Generate a street grid within the district bounds.
        /// Major streets are wider, minor streets fill the gaps.
        /// </summary>
        private static RoadNetwork GenerateRoadNetwork(Transform parent, DistrictDefinition def)
        {
            var network = new RoadNetwork();
            float halfW = def.halfExtents.x;
            float halfD = def.halfExtents.z;
            float sw = def.streetWidth;
            float sidewalkW = def.sidewalkWidth;

            GameObject roadsParent = new GameObject("Roads");
            roadsParent.transform.SetParent(parent);
            roadsParent.transform.localPosition = Vector3.zero;

            // ─── Major N-S Streets ──────────────────────────────
            for (int i = 0; i < def.majorStreetsNS; i++)
            {
                float t = (i + 1f) / (def.majorStreetsNS + 1f);
                float x = Mathf.Lerp(-halfW, halfW, t);

                // Add slight organic offset for non-grid feel
                float offset = Mathf.Sin(x * 0.1f + def.layoutRotation) * 3f;
                x += offset;

                var segment = new StreetSegment
                {
                    start = new Vector3(x, 0, -halfD + 5f),
                    end = new Vector3(x, 0, halfD - 5f),
                    width = sw,
                    isNorthSouth = true
                };
                network.streetSegments.Add(segment);
                network.majorStreetPositionsNS.Add(x);

                GenerateStreetGeometry(roadsParent.transform, segment, sidewalkW);
            }

            // ─── Major E-W Streets ──────────────────────────────
            for (int i = 0; i < def.majorStreetsEW; i++)
            {
                float t = (i + 1f) / (def.majorStreetsEW + 1f);
                float z = Mathf.Lerp(-halfD, halfD, t);

                float offset = Mathf.Cos(z * 0.1f + def.layoutRotation) * 3f;
                z += offset;

                var segment = new StreetSegment
                {
                    start = new Vector3(-halfW + 5f, 0, z),
                    end = new Vector3(halfW - 5f, 0, z),
                    width = sw,
                    isNorthSouth = false
                };
                network.streetSegments.Add(segment);
                network.majorStreetPositionsEW.Add(z);

                GenerateStreetGeometry(roadsParent.transform, segment, sidewalkW);
            }

            // ─── Intersections & Crosswalks ─────────────────────
            foreach (float nsX in network.majorStreetPositionsNS)
            {
                foreach (float ewZ in network.majorStreetPositionsEW)
                {
                    GenerateIntersection(roadsParent.transform, nsX, ewZ, sw, sidewalkW);
                    network.intersections.Add(new Vector3(nsX, 0, ewZ));
                }
            }

            // ─── Minor Streets (alleys) ─────────────────────────
            float minorWidth = sw * 0.5f;
            if (def.density > 0.4f)
            {
                // Add alleys between major streets
                for (int i = 0; i < network.majorStreetPositionsNS.Count - 1; i++)
                {
                    float midX = (network.majorStreetPositionsNS[i] + network.majorStreetPositionsNS[i + 1]) / 2f;
                    if (Random.value > 0.3f)
                    {
                        var alley = new StreetSegment
                        {
                            start = new Vector3(midX, 0, -halfD + 10f),
                            end = new Vector3(midX, 0, halfD - 10f),
                            width = minorWidth,
                            isNorthSouth = true
                        };
                        network.streetSegments.Add(alley);
                        GenerateAlleyGeometry(roadsParent.transform, alley);
                    }
                }
            }

            // ─── Perimeter Road ─────────────────────────────────
            GeneratePerimeterRoad(roadsParent.transform, halfW, halfD, sw * 0.7f, sidewalkW * 0.7f);
            network.hasPerimeterRoad = true;

            return network;
        }

        private static void GenerateStreetGeometry(Transform parent, StreetSegment seg, float sidewalkW)
        {
            Vector3 mid = (seg.start + seg.end) / 2f;
            float length = Vector3.Distance(seg.start, seg.end);

            // Road surface
            Vector3 roadScale = seg.isNorthSouth
                ? new Vector3(seg.width, 0.02f, length)
                : new Vector3(length, 0.02f, seg.width);

            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = seg.isNorthSouth ? $"Road_NS_{mid.x:F0}" : $"Road_EW_{mid.z:F0}";
            road.transform.SetParent(parent);
            road.transform.localPosition = new Vector3(mid.x, ROAD_Y, mid.z);
            road.transform.localScale = roadScale;
            road.isStatic = true;
            Object.DestroyImmediate(road.GetComponent<Collider>());
            SetColor(road, ROAD_COLOR);

            // Center line markings
            Vector3 lineScale = seg.isNorthSouth
                ? new Vector3(0.1f, 0.01f, length * 0.9f)
                : new Vector3(length * 0.9f, 0.01f, 0.1f);

            GameObject centerLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerLine.name = "CenterLine";
            centerLine.transform.SetParent(parent);
            centerLine.transform.localPosition = new Vector3(mid.x, ROAD_Y + 0.015f, mid.z);
            centerLine.transform.localScale = lineScale;
            centerLine.isStatic = true;
            Object.DestroyImmediate(centerLine.GetComponent<Collider>());
            SetColor(centerLine, LANE_MARKING_COLOR);

            // Sidewalks (both sides)
            float sideOffset = (seg.width / 2f) + (sidewalkW / 2f);

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 swPos;
                Vector3 swScale;

                if (seg.isNorthSouth)
                {
                    swPos = new Vector3(mid.x + side * sideOffset, SIDEWALK_Y, mid.z);
                    swScale = new Vector3(sidewalkW, SIDEWALK_HEIGHT, length);
                }
                else
                {
                    swPos = new Vector3(mid.x, SIDEWALK_Y, mid.z + side * sideOffset);
                    swScale = new Vector3(length, SIDEWALK_HEIGHT, sidewalkW);
                }

                GameObject sidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sidewalk.name = $"Sidewalk_{(side < 0 ? "L" : "R")}";
                sidewalk.transform.SetParent(parent);
                sidewalk.transform.localPosition = swPos;
                sidewalk.transform.localScale = swScale;
                sidewalk.isStatic = true;
                Object.DestroyImmediate(sidewalk.GetComponent<Collider>());
                SetColor(sidewalk, SIDEWALK_COLOR);
            }
        }

        private static void GenerateAlleyGeometry(Transform parent, StreetSegment seg)
        {
            Vector3 mid = (seg.start + seg.end) / 2f;
            float length = Vector3.Distance(seg.start, seg.end);

            Vector3 roadScale = seg.isNorthSouth
                ? new Vector3(seg.width, 0.02f, length)
                : new Vector3(length, 0.02f, seg.width);

            GameObject alley = GameObject.CreatePrimitive(PrimitiveType.Cube);
            alley.name = $"Alley_{mid.x:F0}_{mid.z:F0}";
            alley.transform.SetParent(parent);
            alley.transform.localPosition = new Vector3(mid.x, ROAD_Y, mid.z);
            alley.transform.localScale = roadScale;
            alley.isStatic = true;
            Object.DestroyImmediate(alley.GetComponent<Collider>());
            SetColor(alley, ROAD_COLOR * 0.9f); // Slightly darker
        }

        private static void GenerateIntersection(Transform parent, float x, float z, float streetW, float sidewalkW)
        {
            // Intersection pad
            float size = streetW + sidewalkW;
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = $"Intersection_{x:F0}_{z:F0}";
            pad.transform.SetParent(parent);
            pad.transform.localPosition = new Vector3(x, ROAD_Y + 0.005f, z);
            pad.transform.localScale = new Vector3(size, 0.02f, size);
            pad.isStatic = true;
            Object.DestroyImmediate(pad.GetComponent<Collider>());
            SetColor(pad, ROAD_COLOR);

            // Crosswalks (4 directions)
            float cwOffset = streetW / 2f + 0.5f;
            float cwLength = streetW * 0.8f;
            float cwWidth = 0.4f;
            int stripes = 5;

            // N-S crosswalks
            for (int side = -1; side <= 1; side += 2)
            {
                for (int s = 0; s < stripes; s++)
                {
                    float sx = x - cwLength / 2 + (s + 0.5f) * (cwLength / stripes);
                    GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stripe.name = "Crosswalk";
                    stripe.transform.SetParent(parent);
                    stripe.transform.localPosition = new Vector3(sx, ROAD_Y + 0.02f, z + side * cwOffset);
                    stripe.transform.localScale = new Vector3(cwWidth, 0.01f, 2.5f);
                    stripe.isStatic = true;
                    Object.DestroyImmediate(stripe.GetComponent<Collider>());
                    SetColor(stripe, CROSSWALK_COLOR);
                }
            }
        }

        private static void GeneratePerimeterRoad(Transform parent, float halfW, float halfD,
            float roadW, float sidewalkW)
        {
            // Four perimeter road segments
            float inset = roadW / 2f + sidewalkW + 2f;

            // North
            GenerateStreetGeometry(parent, new StreetSegment
            {
                start = new Vector3(-halfW + inset, 0, halfD - inset),
                end = new Vector3(halfW - inset, 0, halfD - inset),
                width = roadW, isNorthSouth = false
            }, sidewalkW);

            // South
            GenerateStreetGeometry(parent, new StreetSegment
            {
                start = new Vector3(-halfW + inset, 0, -halfD + inset),
                end = new Vector3(halfW - inset, 0, -halfD + inset),
                width = roadW, isNorthSouth = false
            }, sidewalkW);

            // East
            GenerateStreetGeometry(parent, new StreetSegment
            {
                start = new Vector3(halfW - inset, 0, -halfD + inset),
                end = new Vector3(halfW - inset, 0, halfD - inset),
                width = roadW, isNorthSouth = true
            }, sidewalkW);

            // West
            GenerateStreetGeometry(parent, new StreetSegment
            {
                start = new Vector3(-halfW + inset, 0, -halfD + inset),
                end = new Vector3(-halfW + inset, 0, halfD - inset),
                width = roadW, isNorthSouth = true
            }, sidewalkW);
        }

        // ═══════════════════════════════════════════════════════
        //  PHASE 3: CITY BLOCKS & BUILDING LOTS
        // ═══════════════════════════════════════════════════════

        private static List<CityBlock> IdentifyBlocks(RoadNetwork network, float halfW, float halfD,
            DistrictDefinition def)
        {
            var blocks = new List<CityBlock>();

            // Sort street positions
            var nsPositions = new List<float>(network.majorStreetPositionsNS);
            var ewPositions = new List<float>(network.majorStreetPositionsEW);
            nsPositions.Sort();
            ewPositions.Sort();

            // Add boundaries
            nsPositions.Insert(0, -halfW + 5f);
            nsPositions.Add(halfW - 5f);
            ewPositions.Insert(0, -halfD + 5f);
            ewPositions.Add(halfD - 5f);

            float streetBuffer = def.streetWidth / 2f + def.sidewalkWidth + LOT_PADDING;

            // Create blocks between intersections
            for (int ix = 0; ix < nsPositions.Count - 1; ix++)
            {
                for (int iz = 0; iz < ewPositions.Count - 1; iz++)
                {
                    float minX = nsPositions[ix] + streetBuffer;
                    float maxX = nsPositions[ix + 1] - streetBuffer;
                    float minZ = ewPositions[iz] + streetBuffer;
                    float maxZ = ewPositions[iz + 1] - streetBuffer;

                    if (maxX - minX < 8f || maxZ - minZ < 8f) continue;

                    // Assign zone type based on position and district composition
                    DistrictZoneType zone = AssignZoneType(
                        (minX + maxX) / 2f, (minZ + maxZ) / 2f,
                        halfW, halfD, def);

                    blocks.Add(new CityBlock
                    {
                        bounds = new Rect(minX, minZ, maxX - minX, maxZ - minZ),
                        zoneType = zone,
                        blockIndex = blocks.Count
                    });
                }
            }

            return blocks;
        }

        private static DistrictZoneType AssignZoneType(float x, float z, float halfW, float halfD,
            DistrictDefinition def)
        {
            // Normalized position within district (0-1 from edge to center)
            float normalizedDist = 1f - Mathf.Max(
                Mathf.Abs(x) / halfW,
                Mathf.Abs(z) / halfD);

            // Waterfront = edge blocks (low Z = south waterfront)
            if (z < -halfD * 0.5f && def.waterfrontRatio > 0.05f)
                return DistrictZoneType.Waterfront;

            // Industrial = northwest corner typically
            if (x < -halfW * 0.3f && z > halfD * 0.2f && Random.value < def.industrialRatio * 2f)
                return DistrictZoneType.Industrial;

            // Commercial = center and along major streets
            if (normalizedDist > 0.5f && Random.value < def.commercialRatio * 1.5f)
                return DistrictZoneType.Commercial;

            // Residential = default fill
            if (Random.value < def.residentialRatio + 0.2f)
                return DistrictZoneType.Residential;

            return DistrictZoneType.Mixed;
        }

        // ═══════════════════════════════════════════════════════
        //  PHASE 3B: BUILDING PLACEMENT
        // ═══════════════════════════════════════════════════════

        private static void PlaceBuildings(Transform parent, List<CityBlock> blocks, DistrictDefinition def)
        {
            GameObject buildingsParent = new GameObject("Buildings");
            buildingsParent.transform.SetParent(parent);
            buildingsParent.transform.localPosition = Vector3.zero;

            foreach (var block in blocks)
            {
                PlaceBuildingsInBlock(buildingsParent.transform, block, def);
            }
        }

        private static void PlaceBuildingsInBlock(Transform parent, CityBlock block, DistrictDefinition def)
        {
            float w = block.bounds.width;
            float d = block.bounds.height; // Rect uses height for y-axis
            float startX = block.bounds.x;
            float startZ = block.bounds.y;

            float wealth = def.wealthLevel;

            switch (block.zoneType)
            {
                case DistrictZoneType.Residential:
                    PlaceResidentialBlock(parent, startX, startZ, w, d, wealth);
                    break;
                case DistrictZoneType.Commercial:
                    PlaceCommercialBlock(parent, startX, startZ, w, d, wealth);
                    break;
                case DistrictZoneType.Industrial:
                    PlaceIndustrialBlock(parent, startX, startZ, w, d);
                    break;
                case DistrictZoneType.Waterfront:
                    PlaceWaterfrontBlock(parent, startX, startZ, w, d, wealth);
                    break;
                case DistrictZoneType.Mixed:
                    PlaceMixedBlock(parent, startX, startZ, w, d, wealth);
                    break;
            }
        }

        private static void PlaceResidentialBlock(Transform parent, float x, float z, float w, float d, float wealth)
        {
            float curX = x + LOT_PADDING;
            float maxX = x + w - LOT_PADDING;

            // Row houses along the front (south face of block)
            if (w > 20f && Random.value > 0.3f)
            {
                int units = Mathf.FloorToInt((maxX - curX) / 5.5f);
                units = Mathf.Clamp(units, 2, 6);
                Vector3 rowPos = new Vector3((curX + maxX) / 2f, 0, z + d * 0.3f);
                ProceduralPropertyBuilder.BuildRowHouses(rowPos, units, wealth);
                return; // Row houses fill the block
            }

            // Individual houses
            while (curX < maxX - 8f)
            {
                float lotWidth = Random.Range(10f, 14f);
                if (curX + lotWidth > maxX) break;

                Vector3 housePos = new Vector3(curX + lotWidth / 2f, 0, z + d * 0.4f);

                if (Random.value > 0.6f && d > 15f)
                {
                    // Apartment building
                    ProceduralPropertyBuilder.BuildApartmentBuilding(housePos, wealth);
                }
                else
                {
                    ProceduralPropertyBuilder.BuildResidentialHouse(housePos, wealth);
                }

                curX += lotWidth + LOT_PADDING;
            }
        }

        private static void PlaceCommercialBlock(Transform parent, float x, float z, float w, float d, float wealth)
        {
            float curX = x + LOT_PADDING;
            float maxX = x + w - LOT_PADDING;

            while (curX < maxX - 8f)
            {
                float roll = Random.value;
                float lotWidth;
                Vector3 buildPos;

                if (roll < 0.3f && wealth > 0.5f)
                {
                    // Office building
                    lotWidth = Random.Range(16f, 22f);
                    if (curX + lotWidth > maxX) break;
                    buildPos = new Vector3(curX + lotWidth / 2f, 0, z + d * 0.4f);
                    ProceduralPropertyBuilder.BuildOfficeBuilding(buildPos, wealth);
                }
                else if (roll < 0.6f)
                {
                    // Convenience store
                    lotWidth = Random.Range(8f, 12f);
                    if (curX + lotWidth > maxX) break;
                    buildPos = new Vector3(curX + lotWidth / 2f, 0, z + d * 0.35f);
                    ProceduralPropertyBuilder.BuildConvenienceStore(buildPos, wealth);
                }
                else
                {
                    // Apartment with ground-floor retail
                    lotWidth = Random.Range(12f, 16f);
                    if (curX + lotWidth > maxX) break;
                    buildPos = new Vector3(curX + lotWidth / 2f, 0, z + d * 0.4f);
                    ProceduralPropertyBuilder.BuildApartmentBuilding(buildPos, wealth);
                }

                curX += lotWidth + LOT_PADDING;
            }
        }

        private static void PlaceIndustrialBlock(Transform parent, float x, float z, float w, float d)
        {
            float curX = x + LOT_PADDING;
            float maxX = x + w - LOT_PADDING;

            while (curX < maxX - 16f)
            {
                float lotWidth = Random.Range(18f, 28f);
                if (curX + lotWidth > maxX) break;

                Vector3 pos = new Vector3(curX + lotWidth / 2f, 0, z + d * 0.4f);

                if (Random.value > 0.7f)
                {
                    // Gas station in industrial area
                    ProceduralPropertyBuilder.BuildGasStation(pos);
                }
                else
                {
                    ProceduralPropertyBuilder.BuildIndustrialBuilding(pos);
                }

                curX += lotWidth + LOT_PADDING;
            }
        }

        private static void PlaceWaterfrontBlock(Transform parent, float x, float z, float w, float d, float wealth)
        {
            // Mix of industrial + commercial along waterfront
            float curX = x + LOT_PADDING;
            float maxX = x + w - LOT_PADDING;

            // Water plane
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = $"Water_{x:F0}_{z:F0}";
            water.transform.SetParent(parent);
            water.transform.localPosition = new Vector3(x + w / 2f, -0.1f, z - 5f);
            water.transform.localScale = new Vector3(w + 5f, 0.02f, 10f);
            water.isStatic = true;
            Object.DestroyImmediate(water.GetComponent<Collider>());
            SetColor(water, new Color(0.15f, 0.25f, 0.4f, 0.8f));

            // Pier/dock platform
            GameObject pier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pier.name = $"Pier_{x:F0}_{z:F0}";
            pier.transform.SetParent(parent);
            pier.transform.localPosition = new Vector3(x + w / 2f, 0.15f, z + 2f);
            pier.transform.localScale = new Vector3(w * 0.6f, 0.3f, 6f);
            pier.isStatic = true;
            Object.DestroyImmediate(pier.GetComponent<Collider>());
            SetColor(pier, new Color(0.45f, 0.35f, 0.25f));

            // Buildings behind pier
            while (curX < maxX - 12f)
            {
                float lotWidth = Random.Range(14f, 22f);
                if (curX + lotWidth > maxX) break;

                Vector3 pos = new Vector3(curX + lotWidth / 2f, 0, z + d * 0.6f);

                if (wealth > 0.5f && Random.value > 0.5f)
                    ProceduralPropertyBuilder.BuildApartmentBuilding(pos, wealth);
                else
                    ProceduralPropertyBuilder.BuildIndustrialBuilding(pos);

                curX += lotWidth + LOT_PADDING;
            }
        }

        private static void PlaceMixedBlock(Transform parent, float x, float z, float w, float d, float wealth)
        {
            // Alternating residential and commercial
            float curX = x + LOT_PADDING;
            float maxX = x + w - LOT_PADDING;
            bool isResidential = Random.value > 0.5f;

            while (curX < maxX - 8f)
            {
                float lotWidth = isResidential ? Random.Range(8f, 14f) : Random.Range(10f, 16f);
                if (curX + lotWidth > maxX) break;

                Vector3 pos = new Vector3(curX + lotWidth / 2f, 0, z + d * 0.4f);

                if (isResidential)
                    ProceduralPropertyBuilder.BuildResidentialHouse(pos, wealth);
                else
                    ProceduralPropertyBuilder.BuildConvenienceStore(pos, wealth);

                curX += lotWidth + LOT_PADDING;
                isResidential = !isResidential;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  PHASE 4: PLAYER PROPERTIES
        // ═══════════════════════════════════════════════════════

        private static void PlacePlayerProperties(Transform parent, List<CityBlock> blocks,
            DistrictDefinition def)
        {
            if (def.availablePropertyTypes == null || def.availablePropertyTypes.Length == 0) return;

            GameObject propertiesParent = new GameObject("PlayerProperties");
            propertiesParent.transform.SetParent(parent);
            propertiesParent.transform.localPosition = Vector3.zero;

            int placed = 0;
            int maxProps = Mathf.Min(def.maxPropertySlots, def.availablePropertyTypes.Length);

            // Spread properties across blocks
            for (int i = 0; i < blocks.Count && placed < maxProps; i++)
            {
                // Skip some blocks to spread properties out
                if (i % Mathf.Max(1, blocks.Count / maxProps) != 0) continue;

                var block = blocks[i];
                PropertyType propType = def.availablePropertyTypes[placed % def.availablePropertyTypes.Length];

                // Position at block edge facing the nearest street
                Vector3 pos = new Vector3(
                    block.bounds.x + block.bounds.width * 0.5f,
                    0,
                    block.bounds.y + block.bounds.height * 0.7f);

                string propName = $"{def.districtName}_{propType}_{placed}";
                var building = ProceduralPropertyBuilder.Build(propType, pos, propName);
                building.transform.SetParent(propertiesParent.transform);

                placed++;
            }

            Debug.Log($"[DistrictGen] Placed {placed} player properties in {def.districtName}");
        }

        // ═══════════════════════════════════════════════════════
        //  PHASE 5: URBAN FURNITURE
        // ═══════════════════════════════════════════════════════

        private static void PlaceUrbanFurniture(Transform parent, RoadNetwork network, DistrictDefinition def)
        {
            GameObject furnitureParent = new GameObject("UrbanFurniture");
            furnitureParent.transform.SetParent(parent);
            furnitureParent.transform.localPosition = Vector3.zero;

            float sidewalkOffset = def.streetWidth / 2f + def.sidewalkWidth * 0.6f;

            // Street lights along major streets
            foreach (var seg in network.streetSegments)
            {
                float length = Vector3.Distance(seg.start, seg.end);
                int lightCount = Mathf.FloorToInt(length / STREET_LIGHT_SPACING);

                for (int i = 0; i < lightCount; i++)
                {
                    float t = (i + 0.5f) / lightCount;
                    Vector3 roadPoint = Vector3.Lerp(seg.start, seg.end, t);

                    // Place on left sidewalk
                    Vector3 lightPos;
                    if (seg.isNorthSouth)
                        lightPos = roadPoint + new Vector3(-sidewalkOffset, 0, 0);
                    else
                        lightPos = roadPoint + new Vector3(0, 0, -sidewalkOffset);

                    var light = ProceduralPropertyBuilder.BuildStreetLight(lightPos);
                    light.transform.SetParent(furnitureParent.transform);
                }
            }

            // Fire hydrants at intersections
            foreach (var intersection in network.intersections)
            {
                Vector3 hydrantPos = intersection + new Vector3(sidewalkOffset + 0.5f, 0, sidewalkOffset + 0.5f);
                var hydrant = ProceduralPropertyBuilder.BuildFireHydrant(hydrantPos);
                hydrant.transform.SetParent(furnitureParent.transform);
            }

            // Dumpsters in alleys
            foreach (var seg in network.streetSegments)
            {
                if (seg.width > def.streetWidth * 0.6f) continue; // Only alleys
                if (Random.value > 0.4f) continue;

                float t = Random.Range(0.2f, 0.8f);
                Vector3 pos = Vector3.Lerp(seg.start, seg.end, t);
                float offset = seg.width / 2f + 1.2f;
                if (seg.isNorthSouth) pos.x += offset;
                else pos.z += offset;

                var dumpster = ProceduralPropertyBuilder.BuildDumpster(pos);
                dumpster.transform.SetParent(furnitureParent.transform);
            }

            // Parked cars along streets
            foreach (var seg in network.streetSegments)
            {
                if (seg.width < def.streetWidth * 0.8f) continue; // Not alleys
                float length = Vector3.Distance(seg.start, seg.end);
                int slots = Mathf.FloorToInt(length / 7f);

                for (int i = 0; i < slots; i++)
                {
                    if (Random.value > CAR_PROBABILITY) continue;

                    float t = (i + 0.5f) / slots;
                    Vector3 roadPoint = Vector3.Lerp(seg.start, seg.end, t);

                    float parkingOffset = seg.width / 2f - 1.5f;
                    float yRot;
                    Vector3 carPos;

                    if (seg.isNorthSouth)
                    {
                        carPos = roadPoint + new Vector3(parkingOffset * (Random.value > 0.5f ? 1 : -1), 0, 0);
                        yRot = 0f;
                    }
                    else
                    {
                        carPos = roadPoint + new Vector3(0, 0, parkingOffset * (Random.value > 0.5f ? 1 : -1));
                        yRot = 90f;
                    }

                    var car = ProceduralPropertyBuilder.BuildParkedCar(carPos, yRot);
                    car.transform.SetParent(furnitureParent.transform);
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        //  PHASE 6: PARKS
        // ═══════════════════════════════════════════════════════

        private static void PlaceParks(Transform parent, List<CityBlock> blocks, DistrictDefinition def)
        {
            // Convert ~10% of blocks to parks (more in wealthy districts)
            float parkChance = 0.08f + def.wealthLevel * 0.12f;
            int parkCount = 0;

            foreach (var block in blocks)
            {
                if (Random.value > parkChance) continue;
                if (block.zoneType == DistrictZoneType.Industrial) continue;
                if (parkCount >= 3) break;

                float size = Mathf.Min(block.bounds.width, block.bounds.height) * 0.8f;
                Vector3 parkPos = new Vector3(
                    block.bounds.x + block.bounds.width / 2f,
                    0,
                    block.bounds.y + block.bounds.height / 2f);

                var park = ProceduralPropertyBuilder.BuildPark(parkPos, size);
                park.transform.SetParent(parent);
                block.zoneType = DistrictZoneType.Park; // Mark as park to prevent building overlap

                parkCount++;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  PHASE 7: DISTRICT TRIGGER ZONE
        // ═══════════════════════════════════════════════════════

        private static void AttachDistrictTrigger(GameObject root, DistrictDefinition def)
        {
            // Large box collider covering entire district
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.up * def.halfExtents.y;
            trigger.size = def.halfExtents * 2f;

            // Attach zone component
            DistrictTriggerZone zone = root.AddComponent<DistrictTriggerZone>();
            zone.districtId = def.districtId;
            zone.districtName = def.districtName;
        }

        // ═══════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════

        private static void SetColor(GameObject obj, Color color)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r == null) return;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader fallback = Shader.Find("Standard");
            Shader shader = urpLit ?? fallback;
            if (shader == null) return;

            Material mat = new Material(shader);
            mat.color = color;

            if (color.a < 1f)
            {
                if (urpLit != null)
                {
                    mat.SetFloat("_Surface", 1);
                    mat.SetFloat("_Blend", 0);
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = 3000;
                }
                else
                {
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = 3000;
                }
            }

            r.sharedMaterial = mat;
        }

        // ═══════════════════════════════════════════════════════
        //  DATA STRUCTURES
        // ═══════════════════════════════════════════════════════

        public class RoadNetwork
        {
            public List<StreetSegment> streetSegments = new List<StreetSegment>();
            public List<float> majorStreetPositionsNS = new List<float>();
            public List<float> majorStreetPositionsEW = new List<float>();
            public List<Vector3> intersections = new List<Vector3>();
            public bool hasPerimeterRoad;
        }

        public class StreetSegment
        {
            public Vector3 start;
            public Vector3 end;
            public float width;
            public bool isNorthSouth;
        }

        public class CityBlock
        {
            public Rect bounds;
            public DistrictZoneType zoneType;
            public int blockIndex;
        }
    }
}
