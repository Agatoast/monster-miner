using MonsterMiner.Player;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public class PlainsTreeObstacle : MonoBehaviour
    {
        const float FallenColliderPadding = 0.06f;
        const float FallenGroundRestOffset = 0.02f;

        bool isInAir;
        bool isFallen;

        public bool IsInAir => isInAir;
        public bool IsFallen => isFallen;

        public void Configure(float trunkHeight, float foliageScale)
        {
            EnsureCollider(trunkHeight, foliageScale);
        }

        public void EnsureCollider()
        {
            EnsureCollider(2.4f, 1.8f);
        }

        void EnsureCollider(float trunkHeight, float foliageScale)
        {
            if (isFallen || GetComponent<Collider>() != null)
                return;

            float height = trunkHeight + foliageScale * 1.2f;
            float radius = Mathf.Max(0.28f, foliageScale * 0.42f);
            var capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, height * 0.5f, 0f);
            capsule.height = height;
            capsule.radius = radius;
            capsule.direction = 1;
        }

        public bool TryLaunchFromTruck(Vector3 truckVelocity)
        {
            if (isInAir || isFallen)
                return false;

            TruckLaunchPhysics.LaunchTree(this, truckVelocity);
            return true;
        }

        public void BeginAirborne()
        {
            isInAir = true;
            transform.SetParent(null, true);
        }

        public void CompleteFallLanding(Vector3 landPoint, Vector3 launchDirection)
        {
            isInAir = false;
            isFallen = true;

            var rb = GetComponent<Rigidbody>();
            Vector3 settledPosition = rb != null ? rb.position : transform.position;
            if (rb != null)
                Destroy(rb);

            transform.position = new Vector3(landPoint.x, settledPosition.y, landPoint.z);
            ApplyFallenOrientation(launchDirection);
            ConfigureFallenCollider();
            SnapFallenTreeToGround(landPoint);
        }

        void ApplyFallenOrientation(Vector3 launchDirection)
        {
            Vector3 flatDir = launchDirection;
            flatDir.y = 0f;
            if (flatDir.sqrMagnitude < 0.01f)
                flatDir = transform.forward;
            flatDir.Normalize();

            Vector3 fallAxis = Vector3.Cross(Vector3.up, flatDir);
            if (fallAxis.sqrMagnitude < 0.01f)
                fallAxis = Vector3.right;
            fallAxis.Normalize();

            transform.rotation = Quaternion.AngleAxis(90f, fallAxis);
        }

        void SnapFallenTreeToGround(Vector3 landPoint)
        {
            Physics.SyncTransforms();

            float bottomY = GetRendererBottomY();
            Vector3 bottomProbe = GetRendererBottomCenter();
            float floorY = ResolveFallenGroundY(bottomProbe, landPoint);
            transform.position += Vector3.up * (floorY + FallenGroundRestOffset - bottomY);
            Physics.SyncTransforms();

            bottomY = GetRendererBottomY();
            float targetBottomY = floorY + FallenGroundRestOffset;
            if (Mathf.Abs(bottomY - targetBottomY) > 0.02f)
            {
                transform.position += Vector3.up * (targetBottomY - bottomY);
                Physics.SyncTransforms();
            }
        }

        static float ResolveFallenGroundY(Vector3 bottomProbe, Vector3 landPoint)
        {
            if (FloorColliderUtility.TryResolveFloorPoint(bottomProbe + Vector3.up * 2f, 8f, 64f, out var floorHit))
                return floorHit.y;

            return FloorAnchor.ResolveFloorSurfaceY(landPoint);
        }

        float GetRendererBottomY()
        {
            float bottomY = float.MaxValue;
            bool found = false;

            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                bottomY = Mathf.Min(bottomY, renderer.bounds.min.y);
                found = true;
            }

            return found ? bottomY : transform.position.y;
        }

        Vector3 GetRendererBottomCenter()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return transform.position;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        void ConfigureFallenCollider()
        {
            foreach (var existing in GetComponents<Collider>())
                Destroy(existing);

            if (!TryGetLocalRendererBounds(out var center, out var size))
                return;

            var box = gameObject.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
            box.enabled = true;
            box.isTrigger = false;
            DriveableTruck.RegisterPassThroughObstacle(box);
        }

        static bool TryGetLocalRendererBounds(Transform root, out Vector3 center, out Vector3 size)
        {
            center = Vector3.zero;
            size = Vector3.one * 0.5f;

            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return false;

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                worldBounds.Encapsulate(renderers[i].bounds);

            Vector3 extents = worldBounds.extents;
            Vector3 worldCenter = worldBounds.center;
            Vector3 localMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 localMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = worldCenter + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 local = root.InverseTransformPoint(corner);
                        localMin = Vector3.Min(localMin, local);
                        localMax = Vector3.Max(localMax, local);
                    }
                }
            }

            center = (localMin + localMax) * 0.5f;
            size = localMax - localMin + Vector3.one * FallenColliderPadding;
            return size.sqrMagnitude > 0.001f;
        }

        bool TryGetLocalRendererBounds(out Vector3 center, out Vector3 size)
        {
            return TryGetLocalRendererBounds(transform, out center, out size);
        }
    }
}
