using System.Collections.Generic;
using MonsterMiner.World;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonsterMiner.Util
{
    public static class LakeIslandVisualFactory
    {
        const string FreeTree01PrefabEditorPath = "Assets/PolyOne/Free Tree/Prefabs/SM_FreeTree_01.prefab";
        const string FreeTree03PrefabEditorPath = "Assets/PolyOne/Free Tree/Prefabs/SM_FreeTree_03.prefab";
        const string FreeTree07PrefabEditorPath = "Assets/PolyOne/Free Tree/Prefabs/SM_FreeTree_07.prefab";

        const float IslandPeakHeightAboveWaterFeet = 3.5f;
        const float IslandEdgeHeightAboveWaterFeet = 0.75f;
        const float IslandPlateauRadiusFraction = 0.78f;
        const float IslandDryLandClearanceAboveWaterFeet = 0.05f;
        const float IslandSurfaceNoiseFeet = 0.15f;
        const int IslandRadialSegments = 48;
        const int IslandRadialRings = 18;
        const float IslandTreeWorldScale = 3.6f;
        const int IslandTreeCount = 11;
        const float IslandTreeOuterDiameterExclusionFeet = 96f;
        const float IslandTreeMinSeparationFeet = 22f;

        static readonly string[] IslandTreePrefabPaths =
        {
            FreeTree01PrefabEditorPath,
            "Assets/PolyOne/Free Tree/Prefabs/SM_FreeTree_02.prefab",
            FreeTree03PrefabEditorPath,
            "Assets/PolyOne/Free Tree/Prefabs/SM_FreeTree_04.prefab",
            "Assets/PolyOne/Free Tree/Prefabs/SM_FreeTree_05.prefab",
            "Assets/PolyOne/Free Tree/Prefabs/SM_FreeTree_06.prefab",
            FreeTree07PrefabEditorPath,
            "Assets/PolyOne/Free Tree/Prefabs/SM_FreeTree_08.prefab",
        };

        static readonly string[] IslandTreeResourcePaths =
        {
            "Models/Trees/SM_FreeTree_01",
            "Models/Trees/SM_FreeTree_02",
            "Models/Trees/SM_FreeTree_03",
            "Models/Trees/SM_FreeTree_04",
            "Models/Trees/SM_FreeTree_05",
            "Models/Trees/SM_FreeTree_06",
            "Models/Trees/SM_FreeTree_07",
            "Models/Trees/SM_FreeTree_08",
        };

        static Collider cachedIslandCollider;

        public static Collider IslandTerrainCollider
        {
            get
            {
                EnsureIslandCached();
                return cachedIslandCollider;
            }
        }

        public static void Create(Transform lakeRoot, Transform contentRoot, float waterLocalY, CavernBounds bounds = null)
        {
            if (lakeRoot == null || contentRoot == null)
                return;

            float islandRadius = LakeCatalog.GetIslandNominalRadiusLocal();
            float islandDiameter = LakeCatalog.GetIslandNominalDiameterLocal();

            var islandRoot = new GameObject("LakeIsland").transform;
            islandRoot.SetParent(lakeRoot, false);
            islandRoot.localPosition = Vector3.zero;
            islandRoot.localRotation = Quaternion.identity;

            var surfaceGo = new GameObject("LakeIslandTerrain");
            surfaceGo.transform.SetParent(islandRoot, false);
            surfaceGo.transform.localPosition = Vector3.zero;
            surfaceGo.transform.localRotation = Quaternion.identity;

            Mesh surfaceMesh = BuildProceduralIslandMesh(islandRadius, islandDiameter, waterLocalY);
            var meshFilter = surfaceGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = surfaceMesh;

            var meshRenderer = surfaceGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = CavernSurfaceMaterialFactory.GetPlainsGrassMaterial();

            var meshCollider = surfaceGo.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = surfaceMesh;
            meshCollider.convex = false;
            cachedIslandCollider = meshCollider;

            LakeCatalog.RegisterLakeIsland(LakeCatalog.GetCenterLocal(), islandRadius);

            CreateIslandTrees(islandRoot, bounds != null ? bounds.transform : contentRoot);
            CreateIslandTaipanSpawn(islandRoot, waterLocalY, islandRadius);
            Physics.SyncTransforms();
        }

        static void CreateIslandTaipanSpawn(Transform islandRoot, float waterLocalY, float islandRadius)
        {
            if (islandRoot == null)
                return;

            var existing = islandRoot.Find("IslandTaipanSpawn");
            if (existing != null)
                Object.Destroy(existing.gameObject);

            var spawnGo = new GameObject("IslandTaipanSpawn");
            spawnGo.transform.SetParent(islandRoot, false);
            float surfaceLocalY = SampleIslandLakeLocalSurfaceY(0f, 0f, waterLocalY, islandRadius);
            spawnGo.transform.localPosition = new Vector3(0f, surfaceLocalY, 0f);
            spawnGo.transform.localRotation = Quaternion.identity;
            spawnGo.AddComponent<IslandTaipanSpawner>();
        }

        static Mesh BuildProceduralIslandMesh(float radius, float diameter, float waterLocalY)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            float peakY = SampleIslandLakeLocalSurfaceY(0f, 0f, waterLocalY, radius);
            vertices.Add(new Vector3(0f, peakY, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));
            const int centerIndex = 0;

            for (int ring = 1; ring <= IslandRadialRings; ring++)
            {
                float ringRadius = diameter * 0.5f * ring / IslandRadialRings;
                for (int segment = 0; segment < IslandRadialSegments; segment++)
                {
                    float angle = segment / (float)IslandRadialSegments * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * ringRadius;
                    float z = Mathf.Sin(angle) * ringRadius;
                    float y = SampleIslandLakeLocalSurfaceY(x, z, waterLocalY, radius);
                    vertices.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(x / diameter + 0.5f, z / diameter + 0.5f));
                }
            }

            int RingVertex(int ring, int segment) => 1 + (ring - 1) * IslandRadialSegments + segment;

            for (int segment = 0; segment < IslandRadialSegments; segment++)
            {
                int nextSegment = (segment + 1) % IslandRadialSegments;
                triangles.Add(centerIndex);
                triangles.Add(RingVertex(1, segment));
                triangles.Add(RingVertex(1, nextSegment));
            }

            for (int ring = 1; ring < IslandRadialRings; ring++)
            {
                for (int segment = 0; segment < IslandRadialSegments; segment++)
                {
                    int nextSegment = (segment + 1) % IslandRadialSegments;
                    int innerA = RingVertex(ring, segment);
                    int innerB = RingVertex(ring, nextSegment);
                    int outerA = RingVertex(ring + 1, segment);
                    int outerB = RingVertex(ring + 1, nextSegment);
                    triangles.Add(innerA);
                    triangles.Add(outerA);
                    triangles.Add(innerB);
                    triangles.Add(innerB);
                    triangles.Add(outerA);
                    triangles.Add(outerB);
                }
            }

            var mesh = new Mesh { name = "LakeIslandSurface" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static float SampleIslandHeightAboveWater(float normalizedDistance01)
        {
            float edge = WorldScale.Feet(IslandEdgeHeightAboveWaterFeet);
            float peak = WorldScale.Feet(IslandPeakHeightAboveWaterFeet);
            if (normalizedDistance01 <= IslandPlateauRadiusFraction)
                return peak;

            float shoreT = Mathf.InverseLerp(IslandPlateauRadiusFraction, 1f, normalizedDistance01);
            return Mathf.Lerp(peak, edge, shoreT);
        }

        static float SampleIslandLakeLocalSurfaceY(
            float lakeLocalX,
            float lakeLocalZ,
            float waterLocalY,
            float radius)
        {
            float dist = new Vector2(lakeLocalX, lakeLocalZ).magnitude;
            float normalized = Mathf.Clamp01(dist / Mathf.Max(radius, 0.001f));
            float aboveWater = SampleIslandHeightAboveWater(normalized);

            float noise = (Mathf.PerlinNoise(lakeLocalX * 0.04f + 12f, lakeLocalZ * 0.04f + 8f) - 0.5f)
                * WorldScale.Feet(IslandSurfaceNoiseFeet);
            return waterLocalY + aboveWater + noise;
        }

        static bool TrySampleIslandSurfaceContentLocalY(float contentX, float contentZ, out float contentY)
        {
            contentY = 0f;
            if (!LakeCatalog.HasLakeIsland)
                return false;

            Vector2 center = LakeCatalog.GetLakeIslandCenterLocal();
            float radius = LakeCatalog.GetLakeIslandRadiusLocal();
            float dx = contentX - center.x;
            float dz = contentZ - center.y;
            float distSq = dx * dx + dz * dz;
            if (distSq > radius * radius)
                return false;

            float dist = Mathf.Sqrt(distSq);
            float normalized = Mathf.Clamp01(dist / Mathf.Max(radius, 0.001f));
            float aboveWater = SampleIslandHeightAboveWater(normalized);

            float noise = (Mathf.PerlinNoise(contentX * 0.04f + 12f, contentZ * 0.04f + 8f) - 0.5f)
                * WorldScale.Feet(IslandSurfaceNoiseFeet);
            aboveWater += noise;

            float plainsBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float waterY = LakeCatalog.GetWaterSurfaceContentLocalY(plainsBase);
            contentY = waterY + aboveWater;
            return true;
        }

        static void CreateIslandTrees(Transform islandRoot, Transform boundsTransform)
        {
            if (islandRoot == null || boundsTransform == null || !LakeCatalog.HasLakeIsland)
                return;

            if (IslandTreePrefabPaths.Length == 0)
                return;

            var propsRoot = new GameObject("LakeIslandProps").transform;
            propsRoot.SetParent(islandRoot, false);
            propsRoot.localPosition = Vector3.zero;
            propsRoot.localRotation = Quaternion.identity;
            propsRoot.localScale = Vector3.one;

            Vector2 center = LakeCatalog.GetLakeIslandCenterLocal();
            float maxPlacementRadius = LakeCatalog.GetIslandNominalRadiusLocal()
                - WorldScale.Feet(IslandTreeOuterDiameterExclusionFeet * 0.5f);
            float minSeparation = WorldScale.Feet(IslandTreeMinSeparationFeet);
            var placedContent = new Vector2[IslandTreeCount];
            int placedCount = 0;
            var rng = new System.Random(System.Environment.TickCount ^ 0x5f3759df);

            for (int treeIndex = 0; treeIndex < IslandTreeCount; treeIndex++)
            {
                bool placed = false;
                for (int attempt = 0; attempt < 64 && !placed; attempt++)
                {
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float placementRadius = Mathf.Sqrt((float)rng.NextDouble()) * maxPlacementRadius;
                    float sampleX = center.x + Mathf.Cos(angle) * placementRadius;
                    float sampleZ = center.y + Mathf.Sin(angle) * placementRadius;
                    if (!IsIslandWalkableLandLocal(sampleX, sampleZ, boundsTransform))
                        continue;

                    bool tooClose = false;
                    for (int i = 0; i < placedCount; i++)
                    {
                        float dx = placedContent[i].x - sampleX;
                        float dz = placedContent[i].y - sampleZ;
                        if (dx * dx + dz * dz < minSeparation * minSeparation)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (tooClose)
                        continue;

                    string prefabPath = IslandTreePrefabPaths[rng.Next(IslandTreePrefabPaths.Length)];
                    GameObject prefab = LoadTreePrefab(prefabPath);
                    if (prefab == null)
                        continue;

                    if (!TrySampleIslandSurfaceWorldY(sampleX, sampleZ, boundsTransform, out float surfaceWorldY))
                        continue;

                    Vector3 worldPosition = boundsTransform.TransformPoint(new Vector3(sampleX, 0f, sampleZ));
                    worldPosition.y = surfaceWorldY;
                    GameObject instance = Object.Instantiate(prefab, propsRoot);
                    instance.transform.SetPositionAndRotation(
                        worldPosition,
                        Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
                    instance.transform.localScale = Vector3.one * IslandTreeWorldScale;
                    KnifeVisualFactory.ApplyUrpMaterials(instance);

                    placedContent[placedCount++] = new Vector2(sampleX, sampleZ);
                    placed = true;
                }
            }
        }

        public static bool BlocksBoatAtContentLocal(float localX, float localZ, Transform boundsTransform) =>
            IsOverDryLand(localX, localZ, boundsTransform);

        public static bool TrySampleWorldY(float localX, float localZ, Transform boundsTransform, out float worldY) =>
            TrySampleIslandSurfaceWorldY(localX, localZ, boundsTransform, out worldY);

        public static bool TrySampleIslandMeshWorldY(
            float localX,
            float localZ,
            Transform boundsTransform,
            out float worldY)
        {
            worldY = 0f;
            if (boundsTransform == null || !LakeCatalog.IsLakeIslandLocal(localX, localZ))
                return false;

            EnsureIslandCached();
            if (cachedIslandCollider != null)
            {
                Vector3 probeWorld = boundsTransform.TransformPoint(new Vector3(localX, 0f, localZ));
                float waterWorldY = SampleWaterWorldY(localX, localZ, boundsTransform);
                Vector3 rayOrigin = new Vector3(probeWorld.x, waterWorldY + WorldScale.Feet(300f), probeWorld.z);
                if (Physics.Raycast(
                        rayOrigin,
                        Vector3.down,
                        out RaycastHit hit,
                        WorldScale.Feet(600f),
                        Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore)
                    && IsIslandSurfaceCollider(hit.collider))
                {
                    worldY = hit.point.y;
                    return true;
                }
            }

            return TrySampleIslandSurfaceWorldY(localX, localZ, boundsTransform, out worldY);
        }

        public static bool IsOverDryLand(float localX, float localZ, Transform boundsTransform)
        {
            if (boundsTransform == null)
                return false;

            if (!TrySampleIslandSurfaceWorldY(localX, localZ, boundsTransform, out float terrainWorldY))
                return false;

            float waterWorldY = SampleWaterWorldY(localX, localZ, boundsTransform);
            return terrainWorldY >= waterWorldY + WorldScale.Feet(IslandDryLandClearanceAboveWaterFeet);
        }

        public static bool IsIslandWalkableLandLocal(float localX, float localZ, Transform boundsTransform)
        {
            if (boundsTransform == null || !LakeCatalog.IsLakeIslandLocal(localX, localZ))
                return false;

            if (!TrySampleIslandSurfaceWorldY(localX, localZ, boundsTransform, out float terrainWorldY))
                return false;

            float waterWorldY = SampleWaterWorldY(localX, localZ, boundsTransform);
            return terrainWorldY >= waterWorldY - WorldScale.Feet(1.5f);
        }

        public static bool IsIslandBoatDismountLandLocal(float localX, float localZ, Transform boundsTransform) =>
            IsIslandWalkableLandLocal(localX, localZ, boundsTransform);

        public static bool TryFindIslandBoatDismountContentLocal(
            float boatLocalX,
            float boatLocalZ,
            Transform boundsTransform,
            out Vector3 dismountContentLocal)
        {
            dismountContentLocal = Vector3.zero;
            if (boundsTransform == null || !LakeCatalog.HasLakeIsland)
                return false;

            Vector2 center = LakeCatalog.GetLakeIslandCenterLocal();
            Vector2 toCenter = center - new Vector2(boatLocalX, boatLocalZ);
            if (toCenter.sqrMagnitude < 0.0001f)
                toCenter = Vector2.up;
            toCenter.Normalize();

            float maxMarch = LakeCatalog.GetLakeIslandRadiusLocal() + WorldScale.Feet(6f);
            for (float distance = WorldScale.Feet(0.5f); distance <= maxMarch; distance += WorldScale.Feet(0.5f))
            {
                float sampleX = boatLocalX + toCenter.x * distance;
                float sampleZ = boatLocalZ + toCenter.y * distance;
                if (!IsIslandBoatDismountLandLocal(sampleX, sampleZ, boundsTransform))
                    continue;

                dismountContentLocal = new Vector3(sampleX, 0f, sampleZ);
                return true;
            }

            return false;
        }

        public static bool TrySampleIslandSurfaceWorldY(
            float localX,
            float localZ,
            Transform boundsTransform,
            out float worldY)
        {
            worldY = 0f;
            if (boundsTransform == null)
                return false;

            if (TrySampleIslandSurfaceContentLocalY(localX, localZ, out float contentY))
            {
                worldY = boundsTransform.TransformPoint(new Vector3(localX, contentY, localZ)).y;
                return true;
            }

            EnsureIslandCached();
            if (cachedIslandCollider == null)
                return false;

            Vector3 probeWorld = boundsTransform.TransformPoint(new Vector3(localX, 0f, localZ));
            float waterWorldY = SampleWaterWorldY(localX, localZ, boundsTransform);
            Vector3 rayOrigin = new Vector3(probeWorld.x, waterWorldY + WorldScale.Feet(300f), probeWorld.z);
            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    WorldScale.Feet(600f),
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore)
                && IsIslandSurfaceCollider(hit.collider))
            {
                worldY = hit.point.y;
                return true;
            }

            return false;
        }

        static bool IsIslandSurfaceCollider(Collider collider)
        {
            if (collider == null)
                return false;

            if (cachedIslandCollider != null && collider == cachedIslandCollider)
                return true;

            var transform = collider.transform;
            while (transform != null)
            {
                if (transform.name == "LakeIslandTerrain")
                    return true;
                transform = transform.parent;
            }

            return false;
        }

        static float SampleWaterWorldY(float localX, float localZ, Transform boundsTransform)
        {
            float plainsBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float waterLocalY = LakeCatalog.GetWaterSurfaceContentLocalY(plainsBase);
            return boundsTransform.TransformPoint(new Vector3(localX, waterLocalY, localZ)).y;
        }

        static void EnsureIslandCached()
        {
            if (cachedIslandCollider != null)
                return;

            var surfaceObject = GameObject.Find("LakeIslandTerrain");
            if (surfaceObject == null)
                return;

            cachedIslandCollider = surfaceObject.GetComponent<Collider>();
        }

        static GameObject LoadTreePrefab(string editorPath)
        {
            GameObject prefab = LoadPrefab(editorPath);
            if (prefab != null)
                return prefab;

            for (int i = 0; i < IslandTreePrefabPaths.Length; i++)
            {
                if (IslandTreePrefabPaths[i] != editorPath)
                    continue;

                if (i >= IslandTreeResourcePaths.Length)
                    return null;

                return Resources.Load<GameObject>(IslandTreeResourcePaths[i]);
            }

            return null;
        }

        static GameObject LoadPrefab(string editorPath)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(editorPath);
#else
            return null;
#endif
        }
    }
}
