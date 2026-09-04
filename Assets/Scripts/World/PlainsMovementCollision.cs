using MonsterMiner.Inventory;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class PlainsMovementCollision
    {
        const float SkinWidth = 0.02f;
        const int MaxDepenetrationPasses = 6;

        public static Vector3 ResolvePosition(
            Transform self,
            CapsuleCollider capsule,
            Rigidbody rb,
            Vector3 current,
            Vector3 worldDelta)
        {
            if (capsule == null)
                return current + worldDelta;

            Quaternion rotation = self != null ? self.rotation : Quaternion.identity;
            float radius = GetRadius(capsule);
            GetCapsulePoints(capsule, current, rotation, radius, out Vector3 point1, out Vector3 point2);

            current = Depenetrate(self, capsule, current, rotation, point1, point2, radius);
            current = Sweep(self, capsule, current, rotation, worldDelta, radius);
            GetCapsulePoints(capsule, current, rotation, radius, out point1, out point2);
            current = Depenetrate(self, capsule, current, rotation, point1, point2, radius);
            return current;
        }

        public static Vector3 ResolvePosition(
            Transform self,
            Collider bodyCollider,
            Rigidbody rb,
            Vector3 current,
            Vector3 worldDelta)
        {
            if (bodyCollider is CapsuleCollider capsule)
                return ResolvePosition(self, capsule, rb, current, worldDelta);

            if (bodyCollider == null)
                return current + worldDelta;

            Quaternion rotation = self != null ? self.rotation : Quaternion.identity;
            BuildBoundsCapsule(bodyCollider, current, rotation, out Vector3 point1, out Vector3 point2, out float radius);

            current = Depenetrate(self, bodyCollider, current, rotation, point1, point2, radius);
            current = Sweep(self, bodyCollider, current, rotation, worldDelta, radius);
            BuildBoundsCapsule(bodyCollider, current, rotation, out point1, out point2, out radius);
            current = Depenetrate(self, bodyCollider, current, rotation, point1, point2, radius);
            return current;
        }

        static Vector3 Sweep(
            Transform self,
            Collider bodyCollider,
            Vector3 current,
            Quaternion rotation,
            Vector3 worldDelta,
            float radius)
        {
            Vector3 horizontal = new Vector3(worldDelta.x, 0f, worldDelta.z);
            Vector3 vertical = new Vector3(0f, worldDelta.y, 0f);

            if (horizontal.sqrMagnitude > 0.0000001f)
            {
                current = SweepAxis(self, bodyCollider, current, rotation, new Vector3(horizontal.x, 0f, 0f), radius);
                current = SweepAxis(self, bodyCollider, current, rotation, new Vector3(0f, 0f, horizontal.z), radius);
            }

            if (Mathf.Abs(vertical.y) > 0.0000001f)
                current = SweepAxis(self, bodyCollider, current, rotation, vertical, radius);

            return current;
        }

        static Vector3 SweepAxis(
            Transform self,
            Collider bodyCollider,
            Vector3 current,
            Quaternion rotation,
            Vector3 delta,
            float radius)
        {
            if (delta.sqrMagnitude <= 0.0000001f)
                return current;

            Vector3 point1;
            Vector3 point2;
            if (bodyCollider is CapsuleCollider capsule)
                GetCapsulePoints(capsule, current, rotation, radius, out point1, out point2);
            else
                BuildBoundsCapsule(bodyCollider, current, rotation, out point1, out point2, out radius);

            Vector3 direction = delta.normalized;
            float distance = delta.magnitude;
            float closest = distance;
            var hits = Physics.CapsuleCastAll(
                point1,
                point2,
                radius,
                direction,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                ref RaycastHit hit = ref hits[i];
                if (!ShouldBlockMovement(hit.collider, self))
                    continue;

                closest = Mathf.Min(closest, hit.distance);
            }

            distance = closest;

            distance = Mathf.Max(0f, distance - SkinWidth);
            return current + direction * distance;
        }

        static Vector3 Depenetrate(
            Transform self,
            Collider bodyCollider,
            Vector3 current,
            Quaternion rotation,
            Vector3 point1,
            Vector3 point2,
            float radius)
        {
            for (int pass = 0; pass < MaxDepenetrationPasses; pass++)
            {
                bool moved = false;
                var overlaps = Physics.OverlapCapsule(point1, point2, radius, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < overlaps.Length; i++)
                {
                    Collider other = overlaps[i];
                    if (!ShouldBlockMovement(other, self))
                        continue;

                    if (!Physics.ComputePenetration(
                            bodyCollider,
                            current,
                            rotation,
                            other,
                            other.transform.position,
                            other.transform.rotation,
                            out Vector3 direction,
                            out float distance))
                        continue;

                    if (distance <= 0.0001f)
                        continue;

                    current += direction * (distance + SkinWidth);
                    moved = true;

                    if (bodyCollider is CapsuleCollider capsule)
                        GetCapsulePoints(capsule, current, rotation, radius, out point1, out point2);
                    else
                        BuildBoundsCapsule(bodyCollider, current, rotation, out point1, out point2, out radius);
                }

                if (!moved)
                    break;
            }

            return current;
        }

        static float GetRadius(CapsuleCollider capsule)
        {
            float scale = Mathf.Max(Mathf.Abs(capsule.transform.lossyScale.x), Mathf.Abs(capsule.transform.lossyScale.z));
            return Mathf.Max(0.05f, capsule.radius * scale - SkinWidth);
        }

        static void GetCapsulePoints(
            CapsuleCollider capsule,
            Vector3 position,
            Quaternion rotation,
            float radius,
            out Vector3 point1,
            out Vector3 point2)
        {
            Vector3 axis = rotation * Vector3.up;
            float scaleY = Mathf.Abs(capsule.transform.lossyScale.y);
            float height = capsule.height * scaleY;
            float half = height * 0.5f;
            Vector3 worldCenter = position + rotation * Vector3.Scale(capsule.center, capsule.transform.lossyScale);
            float stem = Mathf.Max(0f, half - radius);
            point1 = worldCenter + axis * stem;
            point2 = worldCenter - axis * stem;
        }

        static void BuildBoundsCapsule(
            Collider bodyCollider,
            Vector3 position,
            Quaternion rotation,
            out Vector3 point1,
            out Vector3 point2,
            out float radius)
        {
            Vector3 offset = position - bodyCollider.transform.position;
            Bounds bounds = bodyCollider.bounds;
            bounds.center += offset;

            radius = Mathf.Max(0.05f, Mathf.Max(bounds.extents.x, bounds.extents.z) - SkinWidth);
            float halfHeight = Mathf.Max(radius, bounds.extents.y - SkinWidth);
            point1 = bounds.center + Vector3.up * (halfHeight - radius);
            point2 = bounds.center - Vector3.up * (halfHeight - radius);
        }

        static bool ShouldBlockMovement(Collider collider, Transform self)
        {
            if (collider == null || collider.isTrigger)
                return false;

            Transform hitTransform = collider.transform;
            if (self != null && (hitTransform == self || hitTransform.IsChildOf(self)))
                return false;

            if (FloorColliderUtility.IsWalkSurfaceCollider(collider))
                return false;

            if (collider.GetComponentInParent<MonsterEgg>() != null)
                return false;

            if (collider.GetComponentInParent<WorldPickup>() != null)
                return false;

            return true;
        }
    }
}
