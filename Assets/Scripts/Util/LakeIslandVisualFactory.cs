using MonsterMiner.Core;
using MonsterMiner.World;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonsterMiner.Util
{
    public static class LakeIslandVisualFactory
    {
        const string TerrainEditorPath = "Assets/Island/Terrain/New Terrain.asset";
        const string TerrainMaterialEditorPath = "Assets/Island/Terrain/New Terrain Material.mat";
        const string FallbackTerrainEditorPath =
            "Assets/Free Island Collection/Environment/Terrain/Terrains/Terrain 1.asset";
        const string TerrainResourcePath = "Island/LakeIslandTerrain";

        const string DockPrefabEditorPath = "Assets/Island/Prefabs/Dock.prefab";
        const string ChestPrefabEditorPath = "Assets/Island/Prefabs/Chest.prefab";
        const string FreeTree01PrefabEditorPath = "Assets/PolyOne/Free Tree/Prefabs/SM_FreeTree_01.prefab";
        const string FreeTree03PrefabEditorPath = "Assets/PolyOne/Free Tree/Prefabs/SM_FreeTree_03.prefab";
        const string FreeTree07PrefabEditorPath = "Assets/PolyOne/Free Tree/Prefabs/SM_FreeTree_07.prefab";

        const float IslandNominalDiameterFeet = 100f;
        const float IslandTerrainBaseBelowWaterFeet = 3f;
        const float IslandDryLandClearanceAboveWaterFeet = 0.35f;
        const float IslandVerticalScaleMultiplier = 0.72f;
        const float IslandHeightFlattenStrength = 0.82f;
        const float IslandPlateauRadiusFraction = 0.48f;
        const float IslandCliffOuterRadiusFraction = 0.64f;
        const float IslandShoreRadiusFraction = 0.84f;
        const float IslandUnderwaterHeightFraction = 0.06f;
        const float IslandTreeWorldScale = 3.6f;

        static readonly IslandPropPlacement[] IslandProps =
        {
            new IslandPropPlacement(DockPrefabEditorPath, new Vector3(-9.7f, 4.89f, 23.15f), Quaternion.Euler(0f, 60.6f, 0f)),
            new IslandPropPlacement(ChestPrefabEditorPath, new Vector3(2.61f, 6f, 4.12f), Quaternion.Euler(-8.1f, -37.4f, 4.3f)),
        };

        static readonly string[] IslandTreePrefabPaths =
        {
            FreeTree01PrefabEditorPath,
            FreeTree03PrefabEditorPath,
            FreeTree07PrefabEditorPath,
        };

        static readonly float[] IslandTreeHeadingDegrees = { 24f, -112f, 68f };
        static readonly float[] IslandTreeDomeRadiusFractions = { 0.34f, 0.5f, 0.22f };
        static readonly float[] IslandTreeAngleDegrees = { 35f, 158f, 266f };

        static Terrain cachedTerrain;
        static TerrainCollider cachedTerrainCollider;

        readonly struct IslandPropPlacement
        {
            public readonly string EditorPath;
            public readonly Vector3 DemoLocalPosition;
            public readonly Quaternion DemoLocalRotation;
            public readonly float UniformScaleMultiplier;

            public IslandPropPlacement(
                string editorPath,
                Vector3 demoLocalPosition,
                Quaternion demoLocalRotation,
                float uniformScaleMultiplier = 1f)
            {
                EditorPath = editorPath;
                DemoLocalPosition = demoLocalPosition;
                DemoLocalRotation = demoLocalRotation;
                UniformScaleMultiplier = uniformScaleMultiplier;
            }
        }

        public static Terrain Create(Transform lakeRoot, Transform contentRoot, float waterLocalY, CavernBounds bounds = null)
        {
            if (lakeRoot == null || contentRoot == null)
                return null;

            TerrainData source = LoadTerrainData();
            if (source == null)
            {
                Debug.LogWarning("Monster Miner: lake island terrain asset missing.");
                return null;
            }

            TerrainData instance = Object.Instantiate(source);
            instance.name = "LakeIslandTerrain";
            FlattenIslandHeightmap(instance, IslandHeightFlattenStrength);

            var islandRoot = new GameObject("LakeIsland").transform;
            islandRoot.SetParent(lakeRoot, false);
            islandRoot.localPosition = Vector3.zero;
            islandRoot.localRotation = Quaternion.identity;

            var islandGo = Terrain.CreateTerrainGameObject(instance);
            islandGo.name = "LakeIslandTerrain";
            islandGo.transform.SetParent(islandRoot, false);

            Vector3 terrainSize = instance.size;
            float targetDiameter = WorldScale.Feet(IslandNominalDiameterFeet);
            float horizontalScale = targetDiameter / Mathf.Max(terrainSize.x, terrainSize.z);
            float verticalScale = horizontalScale * IslandVerticalScaleMultiplier;

            islandGo.transform.localScale = new Vector3(horizontalScale, verticalScale, horizontalScale);
            islandGo.transform.localPosition = new Vector3(
                -terrainSize.x * horizontalScale * 0.5f,
                waterLocalY - WorldScale.Feet(IslandTerrainBaseBelowWaterFeet),
                -terrainSize.z * horizontalScale * 0.5f);

            var terrain = islandGo.GetComponent<Terrain>();
            if (terrain != null)
            {
                terrain.drawTreesAndFoliage = false;
                terrain.allowAutoConnect = false;
                Material terrainMaterial = LoadTerrainMaterial();
                if (terrainMaterial != null)
                    terrain.materialTemplate = terrainMaterial;
            }

            var terrainCollider = islandGo.GetComponent<TerrainCollider>();
            if (terrainCollider != null)
            {
                terrainCollider.enabled = true;
                cachedTerrainCollider = terrainCollider;
            }

            cachedTerrain = terrain;

            Vector3 terrainCenterWorld = terrain.transform.TransformPoint(
                new Vector3(terrain.terrainData.size.x * 0.5f, 0f, terrain.terrainData.size.z * 0.5f));
            Vector3 terrainCenterContent = contentRoot.InverseTransformPoint(terrainCenterWorld);
            Vector3 terrainScale = terrain.transform.lossyScale;
            float islandRadius = Mathf.Max(
                terrain.terrainData.size.x * terrainScale.x,
                terrain.terrainData.size.z * terrainScale.z) * 0.5f;
            LakeCatalog.RegisterLakeIsland(
                new Vector2(terrainCenterContent.x, terrainCenterContent.z),
                islandRadius);

            CreateIslandProps(islandRoot, horizontalScale);
            CreateIslandTreesOnDryLand(islandRoot, bounds != null ? bounds.transform : contentRoot);

            return terrain;
        }

        static void FlattenIslandHeightmap(TerrainData terrainData, float flattenStrength)
        {
            if (terrainData == null)
                return;

            int resolution = terrainData.heightmapResolution;
            if (resolution <= 1)
                return;

            float[,] heights = terrainData.GetHeights(0, 0, resolution, resolution);
            float center = (resolution - 1) * 0.5f;
            float maxDist = center;
            float peak = 0f;

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                    peak = Mathf.Max(peak, heights[z, x]);
            }

            float plateauHeight = peak * 0.82f;
            float cliffBaseHeight = peak * 0.24f;

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dx = (x - center) / maxDist;
                    float dz = (z - center) / maxDist;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    float height = heights[z, x];

                    if (dist <= IslandPlateauRadiusFraction)
                    {
                        float blend = 1f - dist / IslandPlateauRadiusFraction;
                        blend = blend * blend;
                        height = Mathf.Lerp(height, plateauHeight, blend * flattenStrength);
                    }
                    else if (dist <= IslandCliffOuterRadiusFraction)
                    {
                        float cliffT = Mathf.InverseLerp(
                            IslandPlateauRadiusFraction,
                            IslandCliffOuterRadiusFraction,
                            dist);
                        cliffT = cliffT * cliffT * cliffT;
                        height = Mathf.Lerp(plateauHeight, cliffBaseHeight, cliffT);
                    }
                    else if (dist <= IslandShoreRadiusFraction)
                    {
                        float shoreT = Mathf.InverseLerp(
                            IslandCliffOuterRadiusFraction,
                            IslandShoreRadiusFraction,
                            dist);
                        shoreT = shoreT * shoreT;
                        height = Mathf.Lerp(
                            cliffBaseHeight,
                            peak * IslandUnderwaterHeightFraction,
                            shoreT);
                    }
                    else
                    {
                        float outerT = Mathf.InverseLerp(IslandShoreRadiusFraction, 1f, dist);
                        height = Mathf.Lerp(
                            peak * IslandUnderwaterHeightFraction,
                            peak * IslandUnderwaterHeightFraction * 0.35f,
                            outerT);
                    }

                    heights[z, x] = Mathf.Clamp01(height);
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        static void CreateIslandProps(Transform islandRoot, float horizontalScale)
        {
            var propsRoot = new GameObject("LakeIslandProps").transform;
            propsRoot.SetParent(islandRoot, false);
            propsRoot.localPosition = Vector3.zero;
            propsRoot.localRotation = Quaternion.identity;
            propsRoot.localScale = Vector3.one;

            for (int i = 0; i < IslandProps.Length; i++)
            {
                GameObject prefab = LoadPrefab(IslandProps[i].EditorPath);
                if (prefab == null)
                    continue;

                Vector3 scaledPosition = IslandProps[i].DemoLocalPosition * horizontalScale;
                if (cachedTerrain != null)
                {
                    Vector3 worldPosition = islandRoot.TransformPoint(scaledPosition);
                    if (TrySampleIslandSurfaceWorldYAtWorld(worldPosition, out float surfaceWorldY))
                        scaledPosition.y = islandRoot.InverseTransformPoint(new Vector3(worldPosition.x, surfaceWorldY, worldPosition.z)).y;
                    else
                        scaledPosition.y = cachedTerrain.SampleHeight(worldPosition);
                }

                GameObject instance = Object.Instantiate(prefab, propsRoot);
                instance.transform.localPosition = scaledPosition;
                instance.transform.localRotation = IslandProps[i].DemoLocalRotation;
                instance.transform.localScale = Vector3.one * horizontalScale * IslandProps[i].UniformScaleMultiplier;
                KnifeVisualFactory.ApplyUrpMaterials(instance);
            }
        }

        static void CreateIslandTreesOnDryLand(Transform islandRoot, Transform boundsTransform)
        {
            if (islandRoot == null || boundsTransform == null || cachedTerrain == null)
                return;

            var propsRoot = islandRoot.Find("LakeIslandProps");
            if (propsRoot == null)
            {
                propsRoot = new GameObject("LakeIslandProps").transform;
                propsRoot.SetParent(islandRoot, false);
                propsRoot.localPosition = Vector3.zero;
                propsRoot.localRotation = Quaternion.identity;
                propsRoot.localScale = Vector3.one;
            }

            for (int i = 0; i < IslandTreePrefabPaths.Length; i++)
            {
                if (!TryFindDryLandDomeContentLocal(
                        boundsTransform,
                        IslandTreeAngleDegrees[i],
                        IslandTreeDomeRadiusFractions[i],
                        out Vector3 contentLocal))
                    continue;

                GameObject prefab = LoadPrefab(IslandTreePrefabPaths[i]);
                if (prefab == null)
                    continue;

                Vector3 worldPosition = boundsTransform.TransformPoint(contentLocal);
                if (!TrySampleIslandSurfaceWorldYAtWorld(worldPosition, out float surfaceWorldY))
                    continue;

                worldPosition.y = surfaceWorldY;
                GameObject instance = Object.Instantiate(prefab, propsRoot);
                instance.transform.SetPositionAndRotation(
                    worldPosition,
                    Quaternion.Euler(0f, IslandTreeHeadingDegrees[i], 0f));
                instance.transform.localScale = Vector3.one * IslandTreeWorldScale;
                KnifeVisualFactory.ApplyUrpMaterials(instance);
            }
        }

        static bool TryFindDryLandDomeContentLocal(
            Transform boundsTransform,
            float angleDegrees,
            float radiusFraction,
            out Vector3 contentLocal)
        {
            contentLocal = Vector3.zero;
            if (boundsTransform == null || !LakeCatalog.HasLakeIsland)
                return false;

            Vector2 center = LakeCatalog.GetLakeIslandCenterLocal();
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            float step = WorldScale.Feet(1f);
            float maxRadius = LakeCatalog.GetLakeIslandRadiusLocal();
            float dryEdgeDistance = 0f;

            for (float distance = step; distance <= maxRadius; distance += step)
            {
                float sampleX = center.x + direction.x * distance;
                float sampleZ = center.y + direction.y * distance;
                if (IsOverDryLand(sampleX, sampleZ, boundsTransform))
                    dryEdgeDistance = distance;
                else if (dryEdgeDistance > 0f)
                    break;
            }

            if (dryEdgeDistance <= step)
                return false;

            float placeDistance = dryEdgeDistance * Mathf.Clamp(radiusFraction, 0.25f, 0.9f);
            contentLocal = new Vector3(
                center.x + direction.x * placeDistance,
                0f,
                center.y + direction.y * placeDistance);

            return IsOverDryLand(contentLocal.x, contentLocal.z, boundsTransform);
        }

        public static bool BlocksBoatAtContentLocal(float localX, float localZ, Transform boundsTransform) =>
            IsOverDryLand(localX, localZ, boundsTransform);

        public static bool TrySampleWorldY(float localX, float localZ, Transform boundsTransform, out float worldY)
        {
            worldY = 0f;
            if (boundsTransform == null)
                return false;

            return TrySampleIslandSurfaceWorldY(localX, localZ, boundsTransform, out worldY);
        }

        public static bool IsOverDryLand(float localX, float localZ, Transform boundsTransform)
        {
            if (cachedTerrain == null || boundsTransform == null)
                return false;

            if (!TrySampleIslandSurfaceWorldY(localX, localZ, boundsTransform, out float terrainWorldY))
                return false;

            float waterWorldY = SampleWaterWorldY(localX, localZ, boundsTransform);
            return terrainWorldY >= waterWorldY + WorldScale.Feet(IslandDryLandClearanceAboveWaterFeet);
        }

        public static bool IsIslandBoatDismountLandLocal(float localX, float localZ, Transform boundsTransform)
        {
            if (cachedTerrain == null || boundsTransform == null)
                return false;

            if (IsOverDryLand(localX, localZ, boundsTransform))
                return true;

            if (!LakeCatalog.IsLakeIslandLocal(localX, localZ))
                return false;

            if (!TrySampleIslandSurfaceWorldY(localX, localZ, boundsTransform, out float terrainWorldY))
                return false;

            float waterWorldY = SampleWaterWorldY(localX, localZ, boundsTransform);
            return terrainWorldY >= waterWorldY - WorldScale.Feet(0.25f);
        }

        public static bool TrySampleIslandSurfaceWorldY(
            float localX,
            float localZ,
            Transform boundsTransform,
            out float worldY)
        {
            worldY = 0f;
            if (cachedTerrain == null || boundsTransform == null)
                return false;

            Vector3 probeWorld = boundsTransform.TransformPoint(new Vector3(localX, 0f, localZ));
            float waterWorldY = SampleWaterWorldY(localX, localZ, boundsTransform);
            Vector3 rayOrigin = new Vector3(probeWorld.x, waterWorldY + WorldScale.Feet(300f), probeWorld.z);
            float rayLength = WorldScale.Feet(600f);

            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    rayLength,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore)
                && IsIslandTerrainCollider(hit.collider))
            {
                worldY = hit.point.y;
                return true;
            }

            worldY = cachedTerrain.SampleHeight(probeWorld);
            return IsIslandTerrainWorldPoint(probeWorld, worldY);
        }

        static bool TrySampleIslandSurfaceWorldYAtWorld(Vector3 probeWorld, out float worldY)
        {
            worldY = 0f;
            if (cachedTerrain == null)
                return false;

            float rayTop = cachedTerrain.transform.position.y + WorldScale.Feet(500f);
            Vector3 rayOrigin = new Vector3(probeWorld.x, rayTop, probeWorld.z);
            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    WorldScale.Feet(800f),
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore)
                && IsIslandTerrainCollider(hit.collider))
            {
                worldY = hit.point.y;
                return true;
            }

            return false;
        }

        static bool IsIslandTerrainCollider(Collider collider)
        {
            if (collider == null)
                return false;

            if (cachedTerrainCollider != null && collider == cachedTerrainCollider)
                return true;

            return cachedTerrain != null && collider.GetComponent<Terrain>() == cachedTerrain;
        }

        static bool IsIslandTerrainWorldPoint(Vector3 probeWorld, float sampledWorldY)
        {
            if (cachedTerrain == null)
                return false;

            Vector3 local = cachedTerrain.transform.InverseTransformPoint(probeWorld);
            Vector3 size = cachedTerrain.terrainData.size;
            return local.x >= 0f && local.z >= 0f
                && local.x <= size.x
                && local.z <= size.z;
        }

        static float SampleWaterWorldY(float localX, float localZ, Transform boundsTransform)
        {
            float plainsBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float waterLocalY = LakeCatalog.GetWaterSurfaceContentLocalY(plainsBase);
            return boundsTransform.TransformPoint(new Vector3(localX, waterLocalY, localZ)).y;
        }

        static TerrainData LoadTerrainData()
        {
#if UNITY_EDITOR
            var islandTerrain = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainEditorPath);
            if (islandTerrain != null)
                return islandTerrain;

            return AssetDatabase.LoadAssetAtPath<TerrainData>(FallbackTerrainEditorPath);
#else
            return Resources.Load<TerrainData>(TerrainResourcePath);
#endif
        }

        static Material LoadTerrainMaterial()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialEditorPath);
#else
            return null;
#endif
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
