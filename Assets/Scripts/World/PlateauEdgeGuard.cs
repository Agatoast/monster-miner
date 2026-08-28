using MonsterMiner.Core;
using UnityEngine;

namespace MonsterMiner.World
{
    public class PlateauEdgeGuard : MonoBehaviour
    {
        const string WarningMessage = "Be careful, you are going to fall!";

        static readonly float WarningDistance = WorldScale.Feet(24f);

        Rigidbody rb;
        CavernBounds bounds;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            if (!TryGetLocalPosition(out var local) || IsBelowPlateau(local))
                return;

            if (PlateauBoundary.IsNearPlateauEdge(local.x, local.z, bounds.Radius, WarningDistance))
                GameContext.Instance?.Hud?.ShowCenterMessage(WarningMessage);
        }

        void FixedUpdate()
        {
            if (!TryGetLocalPosition(out var local) || IsBelowPlateau(local))
                return;

            if (!ShouldStayOnPlateau(local, out var clamped))
                return;

            float distance = new Vector2(local.x, local.z).magnitude;
            if (distance < 0.001f)
                return;

            Vector2 outward = new Vector2(local.x, local.z) / distance;
            Vector3 worldClamped = bounds.transform.TransformPoint(new Vector3(clamped.x, local.y, clamped.y));
            transform.position = worldClamped;

            if (rb == null)
                return;

            Vector3 velocity = rb.linearVelocity;
            Vector3 outwardWorld = bounds.transform.TransformDirection(new Vector3(outward.x, 0f, outward.y)).normalized;
            float outwardSpeed = Vector3.Dot(new Vector3(velocity.x, 0f, velocity.z), outwardWorld);
            if (outwardSpeed > 0f)
            {
                velocity -= outwardWorld * outwardSpeed;
                rb.linearVelocity = velocity;
            }

            if (velocity.y < -0.5f)
            {
                velocity.y = 0f;
                rb.linearVelocity = velocity;
            }

            GameContext.Instance?.Hud?.ShowCenterMessage(WarningMessage);
        }

        bool ShouldStayOnPlateau(Vector3 local, out Vector2 clamped)
        {
            clamped = new Vector2(local.x, local.z);
            float distance = clamped.magnitude;
            if (distance < 0.001f)
                return false;

            float angle = Mathf.Atan2(local.z, local.x);
            float maxDistance = PlateauBoundary.SampleBarrierDistance(angle, bounds.Radius);
            bool pastBarrier = distance > maxDistance;
            bool leftPlateauTop = !PlateauBoundary.IsOnPlateau(local.x, local.z, bounds.Radius);
            if (!pastBarrier && !leftPlateauTop)
                return false;

            if (pastBarrier)
                clamped = clamped.normalized * maxDistance;
            else
                clamped = clamped.normalized * PlateauBoundary.SamplePlateauEdgeDistance(angle, bounds.Radius);

            return true;
        }

        bool TryGetLocalPosition(out Vector3 local)
        {
            local = default;
            if (bounds == null)
                bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null || Player.PlayerController.IsGameplayBlocked())
                return false;

            local = bounds.transform.InverseTransformPoint(transform.position);
            return true;
        }

        static bool IsBelowPlateau(Vector3 local)
        {
            return local.y < PlainsBiomeVisualFactory.PlainsSurfaceLocalY - WorldScale.Feet(20f);
        }
    }
}
