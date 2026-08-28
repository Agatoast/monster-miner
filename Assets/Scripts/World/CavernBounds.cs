using System.Collections.Generic;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public class CavernBounds : MonoBehaviour
    {
        struct SpawnExclusionZone
        {
            public float MinX;
            public float MaxX;
            public float MinZ;
            public float MaxZ;
        }

        readonly List<SpawnExclusionZone> spawnExclusions = new();
        bool cave2Unlocked;
        bool hasSalesmanEggExclusion;
        Vector2 salesmanEggExclusionCenter;
        float salesmanEggExclusionRadius;

        public float Radius = WorldScale.Feet(WorldScale.PlateauNominalRadiusFeet);
        public float Height = 16f;
        public float FloorTopLocalY = 0.25f;
        public float BowlDepth = 0f;
        public float WallThickness = 0.5f;
        public float SpawnRestHeight => WorldScale.SpawnDropHeight;

        public float FloorTopWorldY => SampleFloorWorldY(0f, 0f);

        public float SampleFloorLocalY(float localX, float localZ)
        {
            return PlainsGroundBuilder.SampleGroundLocalY(
                localX,
                localZ,
                Radius,
                FloorTopLocalY,
                BowlDepth,
                PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
        }

        public float SampleFloorWorldY(float localX, float localZ)
        {
            return transform.TransformPoint(new Vector3(localX, SampleFloorLocalY(localX, localZ), localZ)).y;
        }

        public float HalfWidth => Radius;
        public float HalfDepth => Radius;

        public float WalkableRadius => Mathf.Max(1f, Radius - 0.55f);

        public bool IsOnPlateauLocal(float localX, float localZ)
        {
            return PlateauBoundary.IsOnPlateau(localX, localZ, Radius);
        }

        public void SetCave2Unlocked(bool unlocked) => cave2Unlocked = unlocked;

        public void AddSpawnExclusion(float minX, float maxX, float minZ, float maxZ)
        {
            spawnExclusions.Add(new SpawnExclusionZone
            {
                MinX = minX,
                MaxX = maxX,
                MinZ = minZ,
                MaxZ = maxZ
            });
        }

        public void SetSalesmanEggSpawnExclusion(float localX, float localZ, float radiusFeet)
        {
            salesmanEggExclusionCenter = new Vector2(localX, localZ);
            salesmanEggExclusionRadius = WorldScale.Feet(radiusFeet);
            hasSalesmanEggExclusion = true;
        }

        public bool AllowsEggStyleSpawn(float localX, float localZ)
        {
            return !IsInSpawnExclusionLocal(localX, localZ)
                && !IsTooCloseToSalesmanForEggs(localX, localZ);
        }

        public Bounds GetBounds()
        {
            var worldCenter = transform.TransformPoint(new Vector3(0f, Height * 0.5f, 0f));
            var diameter = Radius * 2f;
            return new Bounds(worldCenter, new Vector3(diameter, Height, diameter));
        }

        public Vector3 GetRandomFloorPoint()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float maxDistance = WalkableRadius - 2f;
            float distance = Mathf.Sqrt(Random.value) * maxDistance;
            TryResolveFloorWorldPoint(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, out var floorPoint);
            return floorPoint;
        }

        public bool TryGetRandomClearFloorPoint(
            float clearanceRadius,
            int maxAttempts,
            out Vector3 point,
            float contentHorizontalRadius = 0f,
            bool forEggSpawn = false)
        {
            const float wallMargin = 0.35f;
            float maxDistance = WalkableRadius - 2f;
            if (contentHorizontalRadius > 0f)
            {
                maxDistance = Mathf.Min(
                    maxDistance,
                    Radius - ShellInset - contentHorizontalRadius - wallMargin);
            }

            maxDistance = Mathf.Max(1.5f, maxDistance);

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = Mathf.Sqrt(Random.value) * maxDistance;
                float localX = Mathf.Cos(angle) * distance;
                float localZ = Mathf.Sin(angle) * distance;

                if (!TryResolveFloorWorldPoint(localX, localZ, out var floorPoint))
                    continue;

                if (IsInSpawnExclusionLocal(localX, localZ))
                    continue;

                if (forEggSpawn && IsTooCloseToSalesmanForEggs(localX, localZ))
                    continue;

                if (contentHorizontalRadius > 0f)
                {
                    float pointAngle = Mathf.Atan2(localZ, localX);
                    float plateauEdge = PlateauBoundary.SampleBarrierDistance(pointAngle, Radius);
                    float centerDistance = new Vector2(localX, localZ).magnitude;
                    if (centerDistance + contentHorizontalRadius > plateauEdge - ShellInset)
                        continue;
                }

                if (!IsClearForSpawn(floorPoint, clearanceRadius))
                    continue;

                point = floorPoint;
                return true;
            }

            point = default;
            return false;
        }

        public bool TryGetRandomClearPlateauPoint(
            float clearanceRadius,
            int maxAttempts,
            out Vector3 point,
            float contentHorizontalRadius = 0f,
            bool forEggSpawn = false)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float plateauEdge = PlateauBoundary.SamplePlateauEdgeDistance(angle, Radius);
                float minDistance = WorldScale.Feet(24f) + contentHorizontalRadius;
                float maxDistance = plateauEdge - WorldScale.Feet(8f) - contentHorizontalRadius;
                if (maxDistance <= minDistance)
                    continue;

                float distance = Random.Range(minDistance, maxDistance);
                float localX = Mathf.Cos(angle) * distance;
                float localZ = Mathf.Sin(angle) * distance;

                if (!PlateauBoundary.IsOnPlateau(localX, localZ, Radius))
                    continue;

                if (IsInSpawnExclusionLocal(localX, localZ))
                    continue;

                if (forEggSpawn && IsTooCloseToSalesmanForEggs(localX, localZ))
                    continue;

                if (!TryResolveFloorWorldPoint(localX, localZ, out var floorPoint))
                    continue;

                if (!IsClearForSpawn(floorPoint, clearanceRadius))
                    continue;

                point = floorPoint;
                return true;
            }

            point = default;
            return false;
        }

        public Vector3 GetRandomEdgeFloorPoint(float insetFromWallMin, float insetFromWallMax, float contentHorizontalRadius = 0f)
        {
            for (int attempt = 0; attempt < 48; attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float plateauEdge = PlateauBoundary.SampleBarrierDistance(angle, Radius);
                float maxCenterDistance = Mathf.Max(1.5f, plateauEdge - ShellInset - contentHorizontalRadius - insetFromWallMin);
                float minCenterDistance = Mathf.Max(1.5f, plateauEdge - ShellInset - contentHorizontalRadius - insetFromWallMax);
                float distance = Random.Range(minCenterDistance, maxCenterDistance);
                float localX = Mathf.Cos(angle) * distance;
                float localZ = Mathf.Sin(angle) * distance;

                if (IsInSpawnExclusionLocal(localX, localZ))
                    continue;

                return ResolveFloorWorldPoint(localX, localZ);
            }

            float fallbackAngle = Random.Range(0f, Mathf.PI * 2f);
            float fallbackPlateauEdge = PlateauBoundary.SampleBarrierDistance(fallbackAngle, Radius);
            float fallbackDistance = Mathf.Max(1.5f, fallbackPlateauEdge - ShellInset - contentHorizontalRadius - insetFromWallMax);
            return ResolveFloorWorldPoint(
                Mathf.Cos(fallbackAngle) * fallbackDistance,
                Mathf.Sin(fallbackAngle) * fallbackDistance);
        }

        public float ShellInset => 0.15f;

        Vector3 ResolveFloorWorldPoint(float localX, float localZ)
        {
            if (TryResolveFloorWorldPoint(localX, localZ, out var floorPoint))
                return floorPoint;

            return transform.TransformPoint(new Vector3(localX, SampleFloorLocalY(localX, localZ), localZ));
        }

        public bool TryResolveFloorWorldPoint(float localX, float localZ, out Vector3 floorPoint)
        {
            if (!IsWalkableLocalPoint(localX, localZ))
            {
                floorPoint = default;
                return false;
            }

            float localY = SampleFloorLocalY(localX, localZ);
            floorPoint = transform.TransformPoint(new Vector3(localX, localY, localZ));

            var rayOrigin = transform.TransformPoint(new Vector3(localX, FloorTopLocalY + Height, localZ));
            var hits = Physics.RaycastAll(rayOrigin, Vector3.down, Height + 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            float bestFloorY = float.MinValue;
            Vector3 bestPoint = floorPoint;
            bool foundRaycast = false;

            foreach (var hit in hits)
            {
                if (!FloorColliderUtility.IsFloorCollider(hit.collider))
                    continue;

                var localHit = transform.InverseTransformPoint(hit.point);
                if (!IsWalkableLocalPoint(localHit.x, localHit.z))
                    continue;

                if (hit.point.y <= bestFloorY)
                    continue;

                bestFloorY = hit.point.y;
                bestPoint = hit.point;
                foundRaycast = true;
            }

            if (foundRaycast)
                floorPoint = new Vector3(bestPoint.x, Mathf.Max(floorPoint.y, bestPoint.y), bestPoint.z);

            return true;
        }

        bool IsWalkableLocalPoint(float localX, float localZ)
        {
            if (IsInCave2Zone(localX, localZ))
                return true;

            return PlateauBoundary.IsOnPlateau(localX, localZ, Radius);
        }

        bool IsInCave2Zone(float localX, float localZ)
        {
            if (!cave2Unlocked)
                return false;

            return localZ <= -8.5f
                && localZ >= -34f
                && Mathf.Abs(localX) <= 11.5f;
        }

        bool IsClearForSpawn(Vector3 floorPoint, float clearanceRadius)
        {
            var localPoint = transform.InverseTransformPoint(floorPoint);
            if (IsInSpawnExclusionLocal(localPoint.x, localPoint.z))
                return false;

            float centerY = floorPoint.y + clearanceRadius;
            var center = new Vector3(floorPoint.x, centerY, floorPoint.z);
            var overlaps = Physics.OverlapSphere(center, clearanceRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            foreach (var collider in overlaps)
            {
                if (IsEnvironmentCollider(collider))
                    continue;

                return false;
            }

            return true;
        }

        bool IsInSpawnExclusionLocal(float localX, float localZ)
        {
            for (int i = 0; i < spawnExclusions.Count; i++)
            {
                var zone = spawnExclusions[i];
                if (localX >= zone.MinX
                    && localX <= zone.MaxX
                    && localZ >= zone.MinZ
                    && localZ <= zone.MaxZ)
                    return true;
            }

            return false;
        }

        bool IsTooCloseToSalesmanForEggs(float localX, float localZ)
        {
            if (!hasSalesmanEggExclusion)
                return false;

            return Vector2.Distance(
                new Vector2(localX, localZ),
                salesmanEggExclusionCenter) < salesmanEggExclusionRadius;
        }

        static bool IsEnvironmentCollider(Collider collider)
        {
            if (collider == null)
                return false;

            var transform = collider.transform;
            while (transform != null)
            {
                string name = transform.name;
                if (name == "Floor" || name == "Cave2Floor" || name == "Cave2TunnelFloor"
                    || name == "PlainsGround" || name == "FloorCollision"
                    || name == "PlainsGroundTopCollision" || name == "PlainsGroundSolid"
                    || name == "PlainsGroundCollision" || name == "FloorCenterCap"
                    || name == "PlateauEdgeBarriers" || name == "CliffWalls"
                    || name == "LowerWorldVista")
                    return true;
                if (name.StartsWith("PlainsGroundCollider_") || name.StartsWith("FloorCollider_")
                    || name.StartsWith("PlateauEdgeBarrier_") || name.StartsWith("CliffWall_")
                    || name.StartsWith("Tree_") || name.StartsWith("Copse_")
                    || name.StartsWith("Trunk")
                    || name.StartsWith("Foliage") || name == "PlainsBiome" || name == "TreeCopses")
                    return true;

                transform = transform.parent;
            }

            return false;
        }

        public void Expand(float amount)
        {
            Radius += amount;
        }
    }
}
