using System;
using MonsterMiner.Core;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class FloorColliderUtility
    {
        public static bool IsFloorCollider(Collider collider)
        {
            return IsWalkSurfaceCollider(collider) || IsBoatFloorCollider(collider);
        }

        public static bool IsWalkSurfaceCollider(Collider collider)
        {
            if (collider == null)
                return false;

            var transform = collider.transform;
            while (transform != null)
            {
                string name = transform.name;
                if (name == "Floor" || name == "Cave2Floor" || name == "Cave2TunnelFloor"
                    || name == "FloorCollision" || name == "PlainsGroundTopCollision" || name == "PlainsGroundCollision"
                    || name == "FloorCenterCap"
                    || name == "PlainsGround"
                    || name == "PlainsColliderGrid" || name == "PlateauCliffWalls")
                    return true;
                if (name.StartsWith("FloorCollider_") || name.StartsWith("PlainsGroundCollider_")
                    || name.StartsWith("PlainsGroundCell_") || name.StartsWith("Quarry2FloorCollider_"))
                    return true;
                if (name == "Quarry2FloorCollision" || name == "Quarry2FloorCenterCap"
                    || name == "Quarry2SnowApronCollision")
                    return true;
                if (name == "LakeBeachCollision" || name.StartsWith("LakeBeachCollider_")
                    || name == "LakeIslandTerrain")
                    return true;
                transform = transform.parent;
            }

            return collider.GetComponent<Terrain>() != null;
        }

        public static bool IsBoatFloorCollider(Collider collider)
        {
            if (collider == null)
                return false;

            var transform = collider.transform;
            while (transform != null)
            {
                string name = transform.name;
                if (name == "BoatWalkDeck" || name == "BoatFloorWalkCollider"
                    || name == "WalkDeckCollision" || name == "BoatDeck"
                    || name.StartsWith("WalkDeckCell"))
                    return true;
                transform = transform.parent;
            }

            return false;
        }

        public static bool IsPlainsFloorCollider(Collider collider)
        {
            if (collider == null)
                return false;

            var transform = collider.transform;
            while (transform != null)
            {
                string name = transform.name;
                if (name == "PlainsGroundTopCollision" || name == "PlainsGroundCollision"
                    || name == "PlainsGround" || name == "PlainsColliderGrid")
                    return true;
                if (name.StartsWith("PlainsGroundCollider_") || name.StartsWith("PlainsGroundCell_"))
                    return true;
                transform = transform.parent;
            }

            return false;
        }

        public static bool IsLakeIslandTerrainCollider(Collider collider)
        {
            if (collider == null)
                return false;

            var transform = collider.transform;
            while (transform != null)
            {
                if (transform.name == "LakeIslandTerrain")
                    return true;
                transform = transform.parent;
            }

            return false;
        }

        public static bool TryResolveFloorPoint(Vector3 worldPoint, float rayHeight, float maxDistance, out Vector3 floorPoint)
        {
            return TryResolveWalkFloorPoint(worldPoint, rayHeight, maxDistance, GameContext.Instance?.CavernBounds, out floorPoint);
        }

        public static bool TryResolveWalkFloorPoint(
            Vector3 worldPoint,
            float rayHeight,
            float maxDistance,
            CavernBounds bounds,
            out Vector3 floorPoint)
        {
            floorPoint = worldPoint;

            float localX = 0f;
            float localZ = 0f;
            bool hasLocal = bounds != null;
            if (hasLocal)
            {
                Vector3 local = bounds.transform.InverseTransformPoint(worldPoint);
                localX = local.x;
                localZ = local.z;
            }

            if (TryRaycastFirstWalkSurface(worldPoint, rayHeight, maxDistance, bounds, out floorPoint))
                return true;

            bool onIsland = hasLocal
                && LakeCatalog.IsWalkableLandLocal(localX, localZ, bounds.transform)
                && LakeCatalog.IsLakeIslandLocal(localX, localZ);

            bool onBeach = hasLocal && LakeCatalog.IsBeachLocal(localX, localZ);
            bool onLakeApproach = hasLocal && LandQuarry2Boundary.IsLakeApproachLandLocal(localX, localZ);
            return TryResolveWalkFloorFallback(
                bounds,
                localX,
                localZ,
                worldPoint,
                onIsland,
                onBeach,
                onLakeApproach,
                out floorPoint);
        }

        static bool TryRaycastFirstWalkSurface(
            Vector3 worldPoint,
            float rayHeight,
            float maxDistance,
            CavernBounds bounds,
            out Vector3 floorPoint)
        {
            floorPoint = worldPoint;
            var origin = worldPoint + Vector3.up * rayHeight;
            var hits = Physics.RaycastAll(origin, Vector3.down, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
                return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            float localX = 0f;
            float localZ = 0f;
            bool hasLocal = bounds != null;
            if (hasLocal)
            {
                Vector3 local = bounds.transform.InverseTransformPoint(worldPoint);
                localX = local.x;
                localZ = local.z;
            }

            bool onIsland = hasLocal
                && LakeCatalog.IsWalkableLandLocal(localX, localZ, bounds.transform)
                && LakeCatalog.IsLakeIslandLocal(localX, localZ);

            for (int i = 0; i < hits.Length; i++)
            {
                if (!IsWalkSurfaceCollider(hits[i].collider))
                    continue;
                if (IsBoatFloorCollider(hits[i].collider))
                    continue;
                if (hits[i].normal.y < 0.35f)
                    continue;
                if (onIsland && !IsLakeIslandTerrainCollider(hits[i].collider))
                    continue;

                floorPoint = hits[i].point;
                return true;
            }

            return false;
        }

        static bool TryResolveWalkFloorFallback(
            CavernBounds bounds,
            float localX,
            float localZ,
            Vector3 worldPoint,
            bool onIsland,
            bool onBeach,
            bool onLakeApproach,
            out Vector3 floorPoint)
        {
            floorPoint = worldPoint;
            if (bounds == null)
                return false;

            float surfaceY;
            if (onIsland
                && LakeIslandVisualFactory.TrySampleWorldY(localX, localZ, bounds.transform, out surfaceY))
            {
                floorPoint = new Vector3(worldPoint.x, surfaceY, worldPoint.z);
                return true;
            }

            if (LandQuarry2Boundary.IsSnowGroundLocal(localX, localZ))
            {
                float lowerBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
                float localY = LandQuarry2Boundary.SampleSnowFloorLocalY(localX, localZ, lowerBase);
                surfaceY = bounds.transform.TransformPoint(new Vector3(localX, localY, localZ)).y;
                floorPoint = new Vector3(worldPoint.x, surfaceY, worldPoint.z);
                return true;
            }

            if (onBeach || onLakeApproach || LakeCatalog.IsWalkableLandLocal(localX, localZ, bounds.transform))
            {
                surfaceY = PlainsWorldBuilder.SamplePlainsWorldY(bounds.transform, localX, localZ);
                floorPoint = new Vector3(worldPoint.x, surfaceY, worldPoint.z);
                return true;
            }

            return false;
        }
    }
}
