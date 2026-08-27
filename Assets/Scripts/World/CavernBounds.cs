using System.Collections.Generic;
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

        public float Radius = 12f;
        public float Height = 16f;
        public float FloorTopLocalY = 0.25f;
        public float WallThickness = 0.5f;
        public float SpawnRestHeight = 0.01f;

        public float FloorTopWorldY => transform.TransformPoint(new Vector3(0f, FloorTopLocalY, 0f)).y;

        public float HalfWidth => Radius;
        public float HalfDepth => Radius;

        public float WalkableRadius => Mathf.Max(1f, Radius - 0.55f);

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

        public bool TryGetRandomClearFloorPoint(float clearanceRadius, int maxAttempts, out Vector3 point, float contentHorizontalRadius = 0f)
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

                if (contentHorizontalRadius > 0f)
                {
                    var localPoint = transform.InverseTransformPoint(floorPoint);
                    float centerDistance = new Vector2(localPoint.x, localPoint.z).magnitude;
                    if (centerDistance + contentHorizontalRadius > Radius - ShellInset)
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

        public Vector3 GetRandomEdgeFloorPoint(float insetFromWallMin, float insetFromWallMax, float contentHorizontalRadius = 0f)
        {
            float maxCenterDistance = Mathf.Max(1.5f, Radius - ShellInset - contentHorizontalRadius - insetFromWallMin);
            float minCenterDistance = Mathf.Max(1.5f, Radius - ShellInset - contentHorizontalRadius - insetFromWallMax);

            for (int attempt = 0; attempt < 48; attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = Random.Range(minCenterDistance, maxCenterDistance);
                float localX = Mathf.Cos(angle) * distance;
                float localZ = Mathf.Sin(angle) * distance;

                if (IsInSpawnExclusionLocal(localX, localZ))
                    continue;

                return ResolveFloorWorldPoint(localX, localZ);
            }

            float fallbackAngle = Random.Range(0f, Mathf.PI * 2f);
            float fallbackDistance = minCenterDistance;
            return ResolveFloorWorldPoint(
                Mathf.Cos(fallbackAngle) * fallbackDistance,
                Mathf.Sin(fallbackAngle) * fallbackDistance);
        }

        public float ShellInset => 0.15f;

        Vector3 ResolveFloorWorldPoint(float localX, float localZ)
        {
            TryResolveFloorWorldPoint(localX, localZ, out var floorPoint);
            return floorPoint;
        }

        bool TryResolveFloorWorldPoint(float localX, float localZ, out Vector3 floorPoint)
        {
            var rayOrigin = transform.TransformPoint(new Vector3(localX, FloorTopLocalY + Height, localZ));
            var hits = Physics.RaycastAll(rayOrigin, Vector3.down, Height + 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            floorPoint = default;
            bool foundFloor = false;
            float bestFloorY = float.MinValue;

            foreach (var hit in hits)
            {
                if (!IsFloorCollider(hit.collider))
                    continue;

                var localHit = transform.InverseTransformPoint(hit.point);
                if (!IsWalkableLocalPoint(localHit.x, localHit.z))
                    continue;

                if (hit.point.y <= bestFloorY)
                    continue;

                bestFloorY = hit.point.y;
                floorPoint = hit.point;
                foundFloor = true;
            }

            if (foundFloor)
                return true;

            floorPoint = transform.TransformPoint(new Vector3(localX, FloorTopLocalY, localZ));
            return IsWalkableLocalPoint(localX, localZ);
        }

        bool IsWalkableLocalPoint(float localX, float localZ)
        {
            if (IsInCave2Zone(localX, localZ))
                return true;

            return new Vector2(localX, localZ).magnitude <= Radius + 0.25f;
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

        static bool IsFloorCollider(Collider collider)
        {
            if (collider == null)
                return false;

            var transform = collider.transform;
            while (transform != null)
            {
                if (transform.name == "Floor" || transform.name == "Cave2Floor" || transform.name == "Cave2TunnelFloor")
                    return true;
                transform = transform.parent;
            }

            return false;
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
                    || name == "Ceiling" || name == "WallCylinder" || name == "WallCollision")
                    return true;
                if (name.StartsWith("WallCollider_"))
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
