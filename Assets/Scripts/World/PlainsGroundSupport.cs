using MonsterMiner.Core;
using MonsterMiner.Player;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    [DefaultExecutionOrder(150)]
    public class PlainsGroundSupport : MonoBehaviour
    {
        const float PopUpToleranceFeet = 8f;

        Rigidbody rb;
        CapsuleCollider capsule;
        DriveableTruck truck;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            truck = GetComponent<DriveableTruck>();
        }

        void FixedUpdate()
        {
            if (rb == null || PlayerController.IsGameplayBlocked())
                return;

            if (GetComponent<PlayerWingsFlight>()?.IsFlying == true)
                return;

            if (truck != null && truck.HasDriver)
                return;

            var mount = GetComponent<PlayerVehicleMount>();
            if (mount != null && mount.IsMounted)
                return;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return;

            Vector3 local = bounds.transform.InverseTransformPoint(rb.position);
            if (!ShouldSupportAt(local, bounds.Radius))
            {
                if (truck == null)
                    rb.useGravity = true;
                return;
            }

            if (truck == null)
                rb.useGravity = false;

            float groundY = SampleSupportGroundWorldY(bounds, local.x, local.z);
            float bottomOffset = GetBottomOffset();
            float targetCenterY = groundY + bottomOffset + WorldScale.SpawnDropHeight;
            float feetY = rb.position.y - bottomOffset;

            if (feetY < groundY - WorldScale.Feet(PopUpToleranceFeet))
            {
                local = PushOutwardToSafePlains(local, bounds.Radius);
                groundY = SampleSupportGroundWorldY(bounds, local.x, local.z);
                targetCenterY = groundY + bottomOffset + WorldScale.SpawnDropHeight;
            }

            var player = GetComponent<PlayerController>();
            bool inPlainsJump = player != null && player.IsInPlainsJump;
            bool belowGround = feetY < groundY + WorldScale.SpawnDropHeight;
            bool hovering = !inPlainsJump && feetY > targetCenterY + WorldScale.Feet(0.25f);
            if (belowGround || hovering)
            {
                Vector3 pos = rb.position;
                pos.y = targetCenterY;
                rb.position = pos;
                transform.position = pos;
                Physics.SyncTransforms();
            }

            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
        }

        public void SnapSupportedFeetToGroundNow()
        {
            if (rb == null || capsule == null)
                return;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return;

            Vector3 local = bounds.transform.InverseTransformPoint(rb.position);
            if (!ShouldSupportAt(local, bounds.Radius))
                return;

            float groundY = SampleSupportGroundWorldY(bounds, local.x, local.z);
            float bottomOffset = GetBottomOffset();
            float targetCenterY = groundY + bottomOffset + WorldScale.SpawnDropHeight;
            Vector3 pos = rb.position;
            pos.y = targetCenterY;
            rb.position = pos;
            transform.position = pos;
            Physics.SyncTransforms();

            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
        }

        public static bool ShouldSupportAt(Vector3 local, float plateauRadius)
        {
            if (LandQuarry2Boundary.IsSnowGroundLocal(local.x, local.z))
                return true;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds != null
                && LakeCatalog.IsWalkableLandLocal(local.x, local.z, bounds.transform))
                return true;

            if (LakeCatalog.IsOpenWaterLocal(local.x, local.z))
                return false;

            if (LakeCatalog.IsBeachLocal(local.x, local.z))
                return true;

            if (PlateauBoundary.IsOnPlateau(local.x, local.z, plateauRadius))
                return false;

            float angle = Mathf.Atan2(local.z, local.x);
            float distance = new Vector2(local.x, local.z).magnitude;
            float edge = PlateauBoundary.SamplePlateauEdgeDistance(angle, plateauRadius);
            return distance >= edge - WorldScale.Feet(3f);
        }

        public static bool IsOnPlains(CavernBounds bounds, Vector3 worldPosition)
        {
            if (bounds == null)
                return false;

            Vector3 local = bounds.transform.InverseTransformPoint(worldPosition);
            if (ShouldSupportAt(local, bounds.Radius))
                return true;

            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            return local.y <= plainsBaseY + WorldScale.Feet(80f);
        }

        public static float SampleSupportGroundWorldY(CavernBounds bounds, float localX, float localZ)
        {
            if (bounds == null)
                return 0f;

            if (bounds.TryResolveFloorWorldPoint(localX, localZ, out Vector3 floorPoint))
                return floorPoint.y;

            if (LandQuarry2Boundary.IsSnowGroundLocal(localX, localZ))
            {
                float lowerBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
                float localY = LandQuarry2Boundary.SampleSnowFloorLocalY(localX, localZ, lowerBase);
                return bounds.transform.TransformPoint(new Vector3(localX, localY, localZ)).y;
            }

            if (bounds != null
                && LakeCatalog.IsWalkableLandLocal(localX, localZ, bounds.transform)
                && LakeIslandVisualFactory.TrySampleWorldY(localX, localZ, bounds.transform, out float islandWorldY))
            {
                return islandWorldY;
            }

            if (LakeCatalog.IsBeachLocal(localX, localZ)
                || LandQuarry2Boundary.IsLakeApproachLandLocal(localX, localZ))
            {
                return PlainsWorldBuilder.SamplePlainsWorldY(bounds.transform, localX, localZ);
            }

            if (!ShouldSupportAt(new Vector3(localX, 0f, localZ), bounds.Radius))
                return CreatureSurfaceSampler.SampleWorldY(bounds, localX, localZ);

            return PlainsWorldBuilder.SamplePlainsWorldY(bounds.transform, localX, localZ);
        }

        public static Vector3 ResolvePlainsLandingPoint(CavernBounds bounds, Vector3 worldPoint, float halfHeight)
        {
            if (bounds == null)
                return worldPoint;

            Vector3 local = bounds.transform.InverseTransformPoint(worldPoint);
            local = PushOutwardToSafePlains(local, bounds.Radius);
            float groundY = PlainsWorldBuilder.SamplePlainsWorldY(bounds.transform, local.x, local.z);
            return bounds.transform.TransformPoint(new Vector3(
                local.x,
                groundY + halfHeight + bounds.SpawnRestHeight,
                local.z));
        }

        public static Vector3 SnapWorldPointToPlains(CavernBounds bounds, Vector3 worldPoint, float halfHeight)
        {
            return ResolvePlainsLandingPoint(bounds, worldPoint, halfHeight);
        }

        public static Vector3 PushOutwardToSafePlains(Vector3 local, float plateauRadius)
        {
            float angle = Mathf.Atan2(local.z, local.x);
            float minRadius = PlateauWallGeometry.GetWallBaseOutwardRadius(angle, plateauRadius) + WorldScale.Feet(80f);
            Vector2 flat = new Vector2(local.x, local.z);
            if (flat.magnitude >= minRadius)
                return local;

            Vector2 dir = flat.sqrMagnitude > 0.01f
                ? flat.normalized
                : new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            flat = dir * minRadius;
            return new Vector3(flat.x, local.y, flat.y);
        }

        float GetBottomOffset()
        {
            if (capsule != null)
            {
                float scaleY = Mathf.Abs(transform.lossyScale.y);
                return (capsule.height * 0.5f - capsule.center.y) * scaleY;
            }

            return Mathf.Max(0.05f, transform.position.y - FloorAnchor.GetBottomY(gameObject));
        }
    }
}
