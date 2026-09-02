using MonsterMiner.Core;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class LakeCatalog
    {
        public const float NominalDiameterFeet = 5280f;
        public const float BeachHalfLengthFeet = 150f;
        public const float BeachLengthFeet = BeachHalfLengthFeet * 2f;
        public const float BeachGapFeet = 100f;
        public const float BoatPlayerSpawnShoreInsetFeet = 18f;
        public const float BoatDismountShoreProximityFeet = 10f;
        public const float BoatDismountInlandFeet = 30f;
        public const float WaterSurfaceLocalYOffsetFeet = 0.2f;
        public static float WaterSurfaceLocalYOffset => WorldScale.Feet(WaterSurfaceLocalYOffsetFeet);

        public const float JarlLakeConnectionAngle = Mathf.PI * 0.5f;
        const float LakeSouthShoreAngle = -Mathf.PI * 0.5f;

        public static Vector2 GetJarlNorthShoreContentLocal() =>
            LandQuarry2Boundary.GetEdgeLocalPoint(JarlLakeConnectionAngle);

        public static float GetBeachSouthEdgeZ() => GetJarlNorthShoreContentLocal().y;

        public static float GetBeachNorthEdgeZ() =>
            GetBeachSouthEdgeZ() + WorldScale.Feet(BeachGapFeet);

        public static Vector2 GetBeachCenterContentLocal()
        {
            var jarlNorth = GetJarlNorthShoreContentLocal();
            float centerZ = GetBeachSouthEdgeZ() + WorldScale.Feet(BeachGapFeet * 0.5f);
            return new Vector2(jarlNorth.x, centerZ);
        }

        public static float GetBeachShoreAngle(float contentX)
        {
            var lakeCenter = GetCenterLocal();
            var beachCenter = GetBeachCenterContentLocal();
            float dx = contentX - lakeCenter.x;
            float dz = beachCenter.y - lakeCenter.y;
            if (Mathf.Abs(dx) < 0.01f)
                return dz >= 0f ? Mathf.PI * 0.5f : LakeSouthShoreAngle;

            return Mathf.Atan2(dz, dx);
        }

        public static Vector2 GetCenterLocal()
        {
            var jarlNorth = GetJarlNorthShoreContentLocal();
            float lakeRadius = GetNominalRadiusUnits();
            float beachNorth = GetBeachNorthEdgeZ();
            return new Vector2(jarlNorth.x, beachNorth + lakeRadius);
        }

        public static float GetNominalRadiusUnits() => WorldScale.Feet(NominalDiameterFeet * 0.5f);

        static float? boatLaunchWaterlineContentZ;
        static Vector2? lakeIslandCenterLocal;
        static float lakeIslandRadiusLocal;

        public static void RegisterLakeIsland(Vector2 centerContentLocal, float radiusContentLocal)
        {
            lakeIslandCenterLocal = centerContentLocal;
            lakeIslandRadiusLocal = Mathf.Max(1f, radiusContentLocal);
        }

        public static bool HasLakeIsland => lakeIslandCenterLocal.HasValue && lakeIslandRadiusLocal > 0f;

        public static Vector2 GetLakeIslandCenterLocal() =>
            lakeIslandCenterLocal ?? GetCenterLocal();

        public static float GetLakeIslandRadiusLocal() => lakeIslandRadiusLocal;

        public static bool IsLakeIslandLocal(float localX, float localZ)
        {
            if (!HasLakeIsland)
                return false;

            Vector2 center = lakeIslandCenterLocal.Value;
            float dx = localX - center.x;
            float dz = localZ - center.y;
            return dx * dx + dz * dz <= lakeIslandRadiusLocal * lakeIslandRadiusLocal;
        }

        public static void SetBoatLaunchWaterlineContentZ(float contentZ) =>
            boatLaunchWaterlineContentZ = contentZ;

        public static float GetBoatLaunchWaterlineContentZ() =>
            boatLaunchWaterlineContentZ ?? GetBeachNorthEdgeZ();

        public static float GetSandWaterlineContentZ(float contentX)
        {
            if (boatLaunchWaterlineContentZ.HasValue && IsNearBeachCenterX(contentX))
                return boatLaunchWaterlineContentZ.Value;

            float sandNorthZ = GetBeachNorthEdgeZ();
            float lakeShoreZ = LandQuarry2Boundary.SampleLakeSouthShoreLocalZ(contentX);
            return Mathf.Min(sandNorthZ, lakeShoreZ);
        }

        static bool IsWithinBeachLaunchCorridorX(float localX)
        {
            var beachCenter = GetBeachCenterContentLocal();
            float halfLength = WorldScale.Feet(BeachHalfLengthFeet);
            return Mathf.Abs(localX - beachCenter.x) <= halfLength;
        }

        static bool IsNearBeachCenterX(float contentX)
        {
            return IsWithinBeachLaunchCorridorX(contentX);
        }

        public static Vector3 GetBoatBeachContentLocal(
            float sandHalfThicknessUnits,
            float northOffsetFromWaterlineFeet,
            float verticalOffsetFeet = 0f)
        {
            var beachCenter = GetBeachCenterContentLocal();
            float plainsBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float boatZ = beachCenter.y + WorldScale.Feet(northOffsetFromWaterlineFeet);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(beachCenter.x, boatZ, plainsBase);
            float boatY = groundY + WaterSurfaceLocalYOffset + WorldScale.Feet(verticalOffsetFeet);
            return new Vector3(beachCenter.x, boatY, boatZ);
        }

        public static Vector3 GetBoatSandSpawnContentLocal(
            float sandHalfThicknessUnits,
            float northOffsetFromWaterlineFeet,
            float verticalOffsetFeet,
            float sandInsetFromWaterlineFeet,
            float spawnContentX)
        {
            float waterlineZ = GetSandWaterlineContentZ(spawnContentX);
            float spawnZ = waterlineZ - WorldScale.Feet(sandInsetFromWaterlineFeet);
            float plainsBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(spawnContentX, spawnZ, plainsBase);
            float spawnY = groundY + sandHalfThicknessUnits;
            return new Vector3(spawnContentX, spawnY, spawnZ);
        }

        public static Vector3 ResolveBoatSandSpawnWorld(
            CavernBounds bounds,
            float sandHalfThicknessUnits,
            float northOffsetFromWaterlineFeet,
            float verticalOffsetFeet,
            float sandInsetFromWaterlineFeet = 8f)
        {
            if (bounds == null)
                return Vector3.zero;

            float spawnContentX = GetBoatBeachContentLocal(
                sandHalfThicknessUnits,
                northOffsetFromWaterlineFeet,
                verticalOffsetFeet).x;

            var boatGo = GameObject.Find("WarrensonsBoat");
            if (boatGo != null)
            {
                Vector3 boatWorld = boatGo.transform.position;
                var renderers = boatGo.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds meshBounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        if (renderers[i] != null)
                            meshBounds.Encapsulate(renderers[i].bounds);
                    }

                    boatWorld = meshBounds.center;
                }

                Vector3 boatLocal = bounds.transform.InverseTransformPoint(boatWorld);
                spawnContentX = boatLocal.x;
            }

            Vector3 local = GetBoatSandSpawnContentLocal(
                sandHalfThicknessUnits,
                northOffsetFromWaterlineFeet,
                verticalOffsetFeet,
                sandInsetFromWaterlineFeet,
                spawnContentX);

            float halfHeight = WorldScale.CharacterHeightUnits * 0.5f;
            float groundY = PlainsWorldBuilder.SamplePlainsWorldY(bounds.transform, local.x, local.z);
            return bounds.transform.TransformPoint(new Vector3(
                local.x,
                groundY + halfHeight + bounds.SpawnRestHeight,
                local.z));
        }

        public static float GetWaterSurfaceContentLocalY(float plainsBaseLocalY)
        {
            var beachCenter = GetBeachCenterContentLocal();
            float rootGroundY = PlainsWorldBuilder.SamplePlainsLocalY(
                beachCenter.x,
                GetBeachNorthEdgeZ(),
                plainsBaseLocalY);
            return rootGroundY + WaterSurfaceLocalYOffset;
        }

        public static bool IsLakeLocal(float localX, float localZ)
        {
            if (QuarryCatalog.IsLandQuarry2Local(localX, localZ))
                return false;

            if (IsBeachLocal(localX, localZ))
                return false;

            if (IsLakeIslandLocal(localX, localZ))
                return false;

            return LakeBoundary.ContainsLocal(localX, localZ);
        }

        public static bool IsBeachLocal(float localX, float localZ) =>
            LakeBoundary.IsBeachLocal(localX, localZ);

        public static bool IsOpenWaterLocal(float localX, float localZ)
        {
            if (QuarryCatalog.IsLandQuarry2Local(localX, localZ))
                return false;

            if (IsLakeIslandLocal(localX, localZ))
                return false;

            if (IsBeachLocal(localX, localZ))
                return false;

            if (LandQuarry2Boundary.IsLakeApproachLandLocal(localX, localZ))
                return false;

            if (boatLaunchWaterlineContentZ.HasValue
                && IsWithinBeachLaunchCorridorX(localX)
                && localZ >= boatLaunchWaterlineContentZ.Value)
                return true;

            return LakeBoundary.ContainsLocal(localX, localZ);
        }

        public static bool IsBoatNavigableLocal(float localX, float localZ)
        {
            if (QuarryCatalog.IsLandQuarry2Local(localX, localZ))
                return false;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds != null
                && LakeIslandVisualFactory.BlocksBoatAtContentLocal(localX, localZ, bounds.transform))
                return false;

            if (HasLakeIsland && IsLakeIslandLocal(localX, localZ))
                return true;

            if (IsBoatLaunchCorridorLocal(localX, localZ))
                return true;

            if (IsOpenWaterLocal(localX, localZ))
                return true;

            if (LakeBoundary.IsBeachLocal(localX, localZ))
                return true;

            return LandQuarry2Boundary.IsLakeApproachLandLocal(localX, localZ);
        }

        static bool IsBoatLaunchCorridorLocal(float localX, float localZ)
        {
            if (!IsWithinBeachLaunchCorridorX(localX))
                return false;

            if (localZ < GetBeachSouthEdgeZ() - WorldScale.Feet(5f))
                return false;

            if (LakeBoundary.ContainsLocal(localX, localZ))
                return true;

            return localZ <= GetBeachNorthEdgeZ() + WorldScale.Feet(30f);
        }

        public static Vector2 GetNearestShoreLocal(float localX, float localZ)
        {
            var center = GetCenterLocal();
            float dx = localX - center.x;
            float dz = localZ - center.y;
            float angle = Mathf.Atan2(dz, dx);
            float edge = LakeBoundary.SampleEdgeDistance(angle);
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * edge;
        }

        public static bool IsWalkableLandLocal(float localX, float localZ, Transform boundsTransform)
        {
            if (IsBeachLocal(localX, localZ))
                return true;

            if (LandQuarry2Boundary.IsLakeApproachLandLocal(localX, localZ))
                return true;

            if (boundsTransform != null && IsLakeIslandLocal(localX, localZ))
                return LakeIslandVisualFactory.IsIslandBoatDismountLandLocal(localX, localZ, boundsTransform);

            if (boundsTransform != null
                && LakeIslandVisualFactory.IsOverDryLand(localX, localZ, boundsTransform))
                return true;

            return false;
        }

        public static bool IsNearLakeIslandShoreLocal(
            float localX,
            float localZ,
            Transform boundsTransform,
            float proximityFeet = BoatDismountShoreProximityFeet)
        {
            if (!HasLakeIsland || boundsTransform == null)
                return false;

            Vector2 center = GetLakeIslandCenterLocal();
            float radius = GetLakeIslandRadiusLocal();
            float distToCenter = Vector2.Distance(new Vector2(localX, localZ), center);
            float threshold = WorldScale.Feet(proximityFeet + 12f);

            if (distToCenter > radius + threshold)
                return false;

            if (IsWalkableLandLocal(localX, localZ, boundsTransform))
                return true;

            if (SampleDistanceToIslandDryLandLocal(localX, localZ, boundsTransform) <= threshold)
                return true;

            return distToCenter >= radius - threshold;
        }

        public static bool IsNearBoatDismountShoreLocal(
            float localX,
            float localZ,
            Transform boundsTransform,
            float proximityFeet = BoatDismountShoreProximityFeet)
        {
            if (IsWalkableLandLocal(localX, localZ, boundsTransform))
                return true;

            float threshold = WorldScale.Feet(proximityFeet);

            if (HasLakeIsland
                && IsNearLakeIslandShoreLocal(localX, localZ, boundsTransform, proximityFeet))
                return true;

            if (IsWithinBeachLaunchCorridorX(localX))
            {
                float waterlineZ = GetSandWaterlineContentZ(localX);
                if (Mathf.Abs(localZ - waterlineZ) <= threshold)
                    return true;
            }

            if (LandQuarry2Boundary.IsLakeApproachLandLocal(localX, localZ))
                return true;

            float beachNorthZ = GetBeachNorthEdgeZ();
            if (IsWithinBeachLaunchCorridorX(localX)
                && localZ <= beachNorthZ + threshold
                && localZ >= GetBeachSouthEdgeZ() - threshold)
                return true;

            if (boundsTransform != null
                && TryFindNearestWalkableLandLocal(localX, localZ, boundsTransform, out Vector2 nearestLand))
            {
                float dx = nearestLand.x - localX;
                float dz = nearestLand.y - localZ;
                float landThreshold = HasLakeIsland
                    ? WorldScale.Feet(BoatDismountShoreProximityFeet + 12f)
                    : threshold;
                return dx * dx + dz * dz <= landThreshold * landThreshold;
            }

            return false;
        }

        public static bool TryResolveBoatStopSailingWorldPosition(
            CavernBounds bounds,
            Vector3 boatWorldPosition,
            out Vector3 dismountWorldPosition,
            bool assumeNearShore = false)
        {
            dismountWorldPosition = boatWorldPosition;
            if (bounds == null)
                return false;

            Vector3 boatLocal = bounds.transform.InverseTransformPoint(boatWorldPosition);
            if (!assumeNearShore
                && !IsNearBoatDismountShoreLocal(
                    boatLocal.x,
                    boatLocal.z,
                    bounds.transform,
                    BoatDismountShoreProximityFeet))
            {
                return false;
            }

            if (TryResolveBoatDismountLocal(bounds, boatLocal, out Vector3 dismountLocal, assumeNearShore)
                || TryGetFallbackBoatDismountLocal(bounds, boatLocal, out dismountLocal, assumeNearShore)
                || TryForceBoatDismountFromBoatLocal(bounds, boatLocal, out dismountLocal, assumeNearShore)
                || TryResolveIslandBoatDismountLocal(bounds, boatLocal, out dismountLocal))
            {
                dismountWorldPosition = bounds.transform.TransformPoint(dismountLocal);
                return true;
            }

            return false;
        }

        public static bool TryResolveIslandBoatStopSailingWorldPosition(
            CavernBounds bounds,
            Vector3 boatWorldPosition,
            out Vector3 dismountWorldPosition)
        {
            dismountWorldPosition = boatWorldPosition;
            if (bounds == null)
                return false;

            Vector3 boatLocal = bounds.transform.InverseTransformPoint(boatWorldPosition);
            if (!TryResolveIslandBoatDismountLocal(bounds, boatLocal, out Vector3 dismountLocal))
                return false;

            dismountWorldPosition = bounds.transform.TransformPoint(dismountLocal);
            return true;
        }

        public static bool TryResolveBoatStopSailingWorldPosition(
            CavernBounds bounds,
            Vector3[] referenceWorldPoints,
            int referenceCount,
            out Vector3 dismountWorldPosition,
            bool assumeNearShore = false)
        {
            dismountWorldPosition = Vector3.zero;
            if (bounds == null || referenceWorldPoints == null || referenceCount <= 0)
                return false;

            for (int i = 0; i < referenceCount; i++)
            {
                if (TryResolveBoatStopSailingWorldPosition(
                        bounds,
                        referenceWorldPoints[i],
                        out dismountWorldPosition,
                        assumeNearShore))
                    return true;
            }

            return false;
        }

        public static bool TryForceBoatStopSailingWorldPosition(
            CavernBounds bounds,
            Vector3 boatWorldPosition,
            out Vector3 dismountWorldPosition,
            bool assumeNearShore = false)
        {
            dismountWorldPosition = boatWorldPosition;
            if (bounds == null)
                return false;

            Vector3 boatLocal = bounds.transform.InverseTransformPoint(boatWorldPosition);
            if (!TryForceBoatDismountFromBoatLocal(bounds, boatLocal, out Vector3 dismountLocal, assumeNearShore))
                return false;

            dismountWorldPosition = bounds.transform.TransformPoint(dismountLocal);
            return true;
        }

        public static bool TryResolveBoatDismountLocal(
            CavernBounds bounds,
            Vector3 boatContentLocal,
            out Vector3 dismountContentLocal,
            bool assumeNearShore = false)
        {
            dismountContentLocal = boatContentLocal;
            if (bounds == null)
                return false;

            Transform boundsTransform = bounds.transform;
            if (!assumeNearShore
                && !IsNearBoatDismountShoreLocal(
                    boatContentLocal.x,
                    boatContentLocal.z,
                    boundsTransform,
                    BoatDismountShoreProximityFeet))
            {
                return false;
            }

            if (!TryFindAdjacentLandShoreLocal(
                    boatContentLocal.x,
                    boatContentLocal.z,
                    boundsTransform,
                    out Vector2 shoreLocal))
            {
                return false;
            }

            return TryPlaceBoatDismountInland(shoreLocal, boundsTransform, out dismountContentLocal);
        }

        public static bool TryGetFallbackBoatDismountLocal(
            CavernBounds bounds,
            Vector3 fromContentLocal,
            out Vector3 dismountContentLocal,
            bool assumeNearShore = false)
        {
            dismountContentLocal = fromContentLocal;
            if (bounds == null)
                return false;

            Transform boundsTransform = bounds.transform;
            if (!assumeNearShore
                && !IsNearBoatDismountShoreLocal(
                    fromContentLocal.x,
                    fromContentLocal.z,
                    boundsTransform,
                    BoatDismountShoreProximityFeet))
            {
                return false;
            }

            if (TryFindAdjacentLandShoreLocal(
                    fromContentLocal.x,
                    fromContentLocal.z,
                    boundsTransform,
                    out Vector2 shoreLocal)
                && TryPlaceBoatDismountInland(shoreLocal, boundsTransform, out dismountContentLocal))
            {
                return true;
            }

            float step = WorldScale.Feet(2f);
            float maxSearch = WorldScale.Feet(BoatDismountShoreProximityFeet + 8f);

            if (HasLakeIsland)
            {
                Vector2 center = GetLakeIslandCenterLocal();
                Vector2 toCenter = center - new Vector2(fromContentLocal.x, fromContentLocal.z);
                if (toCenter.sqrMagnitude > 0.0001f)
                {
                    toCenter.Normalize();
                    for (float distance = step; distance <= maxSearch; distance += step)
                    {
                        float sampleX = fromContentLocal.x + toCenter.x * distance;
                        float sampleZ = fromContentLocal.z + toCenter.y * distance;
                        if (!IsWalkableLandLocal(sampleX, sampleZ, boundsTransform))
                            continue;

                        Vector2 inlandDirection = SampleShoreInlandDirectionLocal(
                            sampleX,
                            sampleZ,
                            boundsTransform);
                        if (TryPlaceBoatDismountInland(
                                new Vector2(sampleX, sampleZ),
                                boundsTransform,
                                out dismountContentLocal,
                                inlandDirection))
                            return true;
                    }
                }
            }

            if (IsWithinBeachLaunchCorridorX(fromContentLocal.x))
            {
                float waterlineZ = GetSandWaterlineContentZ(fromContentLocal.x);
                for (float south = step; south <= WorldScale.Feet(12f); south += step)
                {
                    float sampleZ = waterlineZ - south;
                    if (!IsWalkableLandLocal(fromContentLocal.x, sampleZ, boundsTransform))
                        continue;

                    if (TryPlaceBoatDismountInland(
                            new Vector2(fromContentLocal.x, sampleZ),
                            boundsTransform,
                            out dismountContentLocal,
                            new Vector2(0f, -1f)))
                        return true;
                }
            }

            if (TryFindNearestWalkableLandLocal(
                    fromContentLocal.x,
                    fromContentLocal.z,
                    boundsTransform,
                    out Vector2 landLocal)
                && TryPlaceBoatDismountInland(landLocal, boundsTransform, out dismountContentLocal))
                return true;

            return false;
        }

        static bool TryPlaceBoatDismountInland(
            Vector2 shoreLocal,
            Transform boundsTransform,
            out Vector3 dismountContentLocal,
            Vector2? inlandDirectionOverride = null)
        {
            float inland = WorldScale.Feet(BoatDismountInlandFeet);
            Vector2 inlandDirection = inlandDirectionOverride
                ?? SampleShoreInlandDirectionLocal(shoreLocal.x, shoreLocal.y, boundsTransform);
            dismountContentLocal = new Vector3(
                shoreLocal.x + inlandDirection.x * inland,
                0f,
                shoreLocal.y + inlandDirection.y * inland);

            if (FinalizeBoatDismountLocal(boundsTransform, ref dismountContentLocal))
                return true;

            Vector3 best = new Vector3(
                shoreLocal.x + inlandDirection.x * WorldScale.Feet(4f),
                0f,
                shoreLocal.y + inlandDirection.y * WorldScale.Feet(4f));
            bool found = false;

            for (float step = inland; step >= WorldScale.Feet(4f); step -= WorldScale.Feet(2f))
            {
                Vector3 candidate = new Vector3(
                    shoreLocal.x + inlandDirection.x * step,
                    0f,
                    shoreLocal.y + inlandDirection.y * step);
                if (!IsWalkableLandLocal(candidate.x, candidate.z, boundsTransform)
                    || IsOpenWaterLocal(candidate.x, candidate.z))
                    continue;

                best = candidate;
                found = true;
                break;
            }

            dismountContentLocal = best;
            return found;
        }

        static bool FinalizeBoatDismountLocal(Transform boundsTransform, ref Vector3 local)
        {
            if (IsWalkableLandLocal(local.x, local.z, boundsTransform)
                && !IsOpenWaterLocal(local.x, local.z))
                return true;

            Vector2 inland = SampleShoreInlandDirectionLocal(local.x, local.z, boundsTransform);
            for (float step = WorldScale.Feet(2f); step <= WorldScale.Feet(BoatDismountInlandFeet); step += WorldScale.Feet(2f))
            {
                Vector3 candidate = local - new Vector3(inland.x, 0f, inland.y) * step;
                if (IsWalkableLandLocal(candidate.x, candidate.z, boundsTransform)
                    && !IsOpenWaterLocal(candidate.x, candidate.z))
                {
                    local = candidate;
                    return true;
                }
            }

            return false;
        }

        static bool TryFindAdjacentLandShoreLocal(
            float boatX,
            float boatZ,
            Transform boundsTransform,
            out Vector2 shoreLocal)
        {
            shoreLocal = new Vector2(boatX, boatZ);
            if (TryFindNearestWalkableLandLocal(boatX, boatZ, boundsTransform, out shoreLocal))
                return true;

            if (HasLakeIsland)
            {
                Vector2 center = GetLakeIslandCenterLocal();
                Vector2 toCenter = center - new Vector2(boatX, boatZ);
                if (toCenter.sqrMagnitude > 0.0001f)
                {
                    toCenter.Normalize();
                    float step = WorldScale.Feet(1f);
                    float maxSearch = WorldScale.Feet(BoatDismountShoreProximityFeet + 16f);
                    for (float distance = step; distance <= maxSearch; distance += step)
                    {
                        float sampleX = boatX + toCenter.x * distance;
                        float sampleZ = boatZ + toCenter.y * distance;
                        if (IsWalkableLandLocal(sampleX, sampleZ, boundsTransform))
                        {
                            shoreLocal = new Vector2(sampleX, sampleZ);
                            return true;
                        }
                    }
                }
            }

            if (IsWithinBeachLaunchCorridorX(boatX))
            {
                float waterlineZ = GetSandWaterlineContentZ(boatX);
                for (float south = WorldScale.Feet(1f); south <= WorldScale.Feet(16f); south += WorldScale.Feet(1f))
                {
                    float sampleZ = waterlineZ - south;
                    if (IsWalkableLandLocal(boatX, sampleZ, boundsTransform))
                    {
                        shoreLocal = new Vector2(boatX, sampleZ);
                        return true;
                    }
                }
            }

            return false;
        }

        static bool TryFindBoatShoreLocal(
            float boatX,
            float boatZ,
            Transform boundsTransform,
            out Vector2 shoreLocal)
        {
            return TryFindAdjacentLandShoreLocal(boatX, boatZ, boundsTransform, out shoreLocal);
        }

        static bool TryForceBoatDismountFromBoatLocal(
            CavernBounds bounds,
            Vector3 boatContentLocal,
            out Vector3 dismountContentLocal,
            bool assumeNearShore = false)
        {
            dismountContentLocal = boatContentLocal;
            if (bounds == null)
                return false;

            Transform boundsTransform = bounds.transform;
            if (!assumeNearShore
                && !IsNearBoatDismountShoreLocal(
                    boatContentLocal.x,
                    boatContentLocal.z,
                    boundsTransform,
                    BoatDismountShoreProximityFeet))
            {
                return false;
            }

            if (TryFindAdjacentLandShoreLocal(
                    boatContentLocal.x,
                    boatContentLocal.z,
                    boundsTransform,
                    out Vector2 shoreLocal)
                && TryPlaceBoatDismountInland(shoreLocal, boundsTransform, out dismountContentLocal))
            {
                return true;
            }

            float inland = WorldScale.Feet(BoatDismountInlandFeet);
            Vector2 inlandDirection = SampleShoreInlandDirectionFromBoatLocal(
                boatContentLocal.x,
                boatContentLocal.z,
                boundsTransform);
            if (inlandDirection.sqrMagnitude < 0.0001f)
                return false;

            inlandDirection.Normalize();
            if (TryFindNearestWalkableLandLocal(
                    boatContentLocal.x,
                    boatContentLocal.z,
                    boundsTransform,
                    out Vector2 nearestLandLocal))
            {
                Vector2 toLand = nearestLandLocal - new Vector2(boatContentLocal.x, boatContentLocal.z);
                if (toLand.sqrMagnitude > 0.0001f)
                    inlandDirection = toLand.normalized;
            }

            for (float step = inland; step >= WorldScale.Feet(2f); step -= WorldScale.Feet(2f))
            {
                Vector3 candidate = new Vector3(
                    boatContentLocal.x + inlandDirection.x * step,
                    0f,
                    boatContentLocal.z + inlandDirection.y * step);
                if (IsWalkableLandLocal(candidate.x, candidate.z, boundsTransform)
                    && !IsOpenWaterLocal(candidate.x, candidate.z))
                {
                    dismountContentLocal = candidate;
                    return true;
                }
            }

            return false;
        }

        static bool TryResolveIslandBoatDismountLocal(
            CavernBounds bounds,
            Vector3 boatContentLocal,
            out Vector3 dismountContentLocal)
        {
            dismountContentLocal = boatContentLocal;
            if (bounds == null || !HasLakeIsland)
                return false;

            Transform boundsTransform = bounds.transform;
            if (!IsNearLakeIslandShoreLocal(
                    boatContentLocal.x,
                    boatContentLocal.z,
                    boundsTransform,
                    BoatDismountShoreProximityFeet))
                return false;

            Vector2 center = GetLakeIslandCenterLocal();
            Vector2 boatXZ = new Vector2(boatContentLocal.x, boatContentLocal.z);
            Vector2 toCenter = center - boatXZ;
            if (toCenter.sqrMagnitude < 0.0001f)
                toCenter = Vector2.up;
            toCenter.Normalize();

            Vector2 shoreLocal = boatXZ;
            bool foundShore = false;
            float maxMarch = GetLakeIslandRadiusLocal() + WorldScale.Feet(BoatDismountShoreProximityFeet + 4f);
            for (float distance = WorldScale.Feet(1f); distance <= maxMarch; distance += WorldScale.Feet(1f))
            {
                float sampleX = boatContentLocal.x + toCenter.x * distance;
                float sampleZ = boatContentLocal.z + toCenter.y * distance;
                if (!IsWalkableLandLocal(sampleX, sampleZ, boundsTransform))
                    continue;

                shoreLocal = new Vector2(sampleX, sampleZ);
                foundShore = true;
                break;
            }

            if (!foundShore)
                return false;

            Vector2 inlandDirection = SampleShoreInlandDirectionLocal(
                shoreLocal.x,
                shoreLocal.y,
                boundsTransform);
            float inlandMax = WorldScale.Feet(BoatDismountInlandFeet);
            float distToCenter = Vector2.Distance(shoreLocal, center);
            inlandMax = Mathf.Min(inlandMax, distToCenter * 0.65f);
            inlandMax = Mathf.Max(inlandMax, WorldScale.Feet(2f));

            for (float step = inlandMax; step >= WorldScale.Feet(2f); step -= WorldScale.Feet(2f))
            {
                Vector3 candidate = new Vector3(
                    shoreLocal.x + inlandDirection.x * step,
                    0f,
                    shoreLocal.y + inlandDirection.y * step);
                if (IsWalkableLandLocal(candidate.x, candidate.z, boundsTransform))
                {
                    dismountContentLocal = candidate;
                    return true;
                }
            }

            dismountContentLocal = new Vector3(shoreLocal.x, 0f, shoreLocal.y);
            return true;
        }

        static Vector2 SampleShoreInlandDirectionFromBoatLocal(
            float boatX,
            float boatZ,
            Transform boundsTransform)
        {
            if (TryFindAdjacentLandShoreLocal(boatX, boatZ, boundsTransform, out Vector2 shoreLocal))
                return SampleShoreInlandDirectionLocal(shoreLocal.x, shoreLocal.y, boundsTransform);

            if (IsWithinBeachLaunchCorridorX(boatX))
                return new Vector2(0f, -1f);

            if (HasLakeIsland)
            {
                Vector2 center = GetLakeIslandCenterLocal();
                Vector2 toCenter = center - new Vector2(boatX, boatZ);
                if (toCenter.sqrMagnitude > 0.0001f)
                    return toCenter.normalized;
            }

            return SampleShoreInlandDirectionLocal(boatX, boatZ, boundsTransform);
        }

        static bool TryFindNearestWalkableLandLocal(
            float localX,
            float localZ,
            Transform boundsTransform,
            out Vector2 landLocal)
        {
            landLocal = new Vector2(localX, localZ);
            if (IsWalkableLandLocal(localX, localZ, boundsTransform))
                return true;

            float bestDistanceSq = float.PositiveInfinity;
            bool found = false;
            float step = WorldScale.Feet(1f);
            float maxSearch = WorldScale.Feet(BoatDismountShoreProximityFeet + 12f);

            for (float distance = step; distance <= maxSearch; distance += step)
            {
                int samples = Mathf.Max(12, Mathf.CeilToInt(distance * 0.75f));
                for (int i = 0; i < samples; i++)
                {
                    float angle = i * Mathf.PI * 2f / samples;
                    float sampleX = localX + Mathf.Cos(angle) * distance;
                    float sampleZ = localZ + Mathf.Sin(angle) * distance;
                    if (!IsWalkableLandLocal(sampleX, sampleZ, boundsTransform))
                        continue;

                    float distanceSq = (sampleX - localX) * (sampleX - localX)
                        + (sampleZ - localZ) * (sampleZ - localZ);
                    if (distanceSq >= bestDistanceSq)
                        continue;

                    bestDistanceSq = distanceSq;
                    landLocal = new Vector2(sampleX, sampleZ);
                    found = true;
                }

                if (found && distance * distance > bestDistanceSq)
                    break;
            }

            if (found)
                return true;

            if (HasLakeIsland && boundsTransform != null)
            {
                float islandDistance = SampleDistanceToIslandDryLandLocal(localX, localZ, boundsTransform);
                if (islandDistance <= WorldScale.Feet(BoatDismountShoreProximityFeet + 2f))
                {
                    Vector2 center = GetLakeIslandCenterLocal();
                    Vector2 toCenter = center - new Vector2(localX, localZ);
                    if (toCenter.sqrMagnitude > 0.0001f)
                    {
                        toCenter.Normalize();
                        for (float distance = step; distance <= maxSearch; distance += step)
                        {
                            float sampleX = localX + toCenter.x * distance;
                            float sampleZ = localZ + toCenter.y * distance;
                            if (!IsWalkableLandLocal(sampleX, sampleZ, boundsTransform))
                                continue;

                            landLocal = new Vector2(sampleX, sampleZ);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        static Vector2 SampleShoreInlandDirectionLocal(float localX, float localZ, Transform boundsTransform)
        {
            if (boundsTransform != null
                && LakeIslandVisualFactory.IsOverDryLand(localX, localZ, boundsTransform))
            {
                Vector2 center = GetLakeIslandCenterLocal();
                Vector2 toCenter = center - new Vector2(localX, localZ);
                if (toCenter.sqrMagnitude > 0.0001f)
                    return toCenter.normalized;
            }

            if (HasLakeIsland && boundsTransform != null)
            {
                float islandDistance = SampleDistanceToIslandDryLandLocal(localX, localZ, boundsTransform);
                if (islandDistance <= WorldScale.Feet(BoatDismountShoreProximityFeet + 2f))
                {
                    Vector2 center = GetLakeIslandCenterLocal();
                    Vector2 toCenter = center - new Vector2(localX, localZ);
                    if (toCenter.sqrMagnitude > 0.0001f)
                        return toCenter.normalized;
                }
            }

            if (IsBeachLocal(localX, localZ))
                return new Vector2(0f, -1f);

            float waterlineZ = GetSandWaterlineContentZ(localX);
            if (localZ >= waterlineZ - WorldScale.Feet(0.5f))
                return new Vector2(0f, -1f);

            return new Vector2(0f, -1f);
        }

        static float SampleDistanceToIslandDryLandLocal(float localX, float localZ, Transform boundsTransform)
        {
            if (LakeIslandVisualFactory.IsOverDryLand(localX, localZ, boundsTransform))
                return 0f;

            if (!HasLakeIsland)
                return float.PositiveInfinity;

            Vector2 center = GetLakeIslandCenterLocal();
            Vector2 toCenter = new Vector2(center.x - localX, center.y - localZ);
            float step = WorldScale.Feet(1f);
            float maxDistance = GetLakeIslandRadiusLocal() + WorldScale.Feet(BoatDismountShoreProximityFeet);

            if (toCenter.sqrMagnitude > 0.0001f)
            {
                toCenter.Normalize();
                for (float distance = step; distance <= maxDistance; distance += step)
                {
                    float sampleX = localX + toCenter.x * distance;
                    float sampleZ = localZ + toCenter.y * distance;
                    if (LakeIslandVisualFactory.IsOverDryLand(sampleX, sampleZ, boundsTransform))
                        return distance;
                }
            }

            for (int directionIndex = 0; directionIndex < 12; directionIndex++)
            {
                float angle = directionIndex * Mathf.PI * 2f / 12f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                for (float distance = step; distance <= WorldScale.Feet(BoatDismountShoreProximityFeet + 4f); distance += step)
                {
                    float sampleX = localX + direction.x * distance;
                    float sampleZ = localZ + direction.y * distance;
                    if (LakeIslandVisualFactory.IsOverDryLand(sampleX, sampleZ, boundsTransform))
                        return distance;
                }
            }

            return float.PositiveInfinity;
        }
    }
}
