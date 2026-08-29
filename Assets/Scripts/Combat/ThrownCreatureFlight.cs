using MonsterMiner.Core;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Combat
{
    public class ThrownCreatureFlight : MonoBehaviour
    {
        const float NonAggressiveGravityScale = 1f / 6f;
        const float DefaultGravityScale = 1f;
        const float MaxFlightSeconds = 8f;

        Monster monster;
        Rigidbody body;
        Vector3 velocity;
        float gravityY;
        float elapsed;

        public static void Begin(Monster thrownMonster, Vector3 startPoint, Vector3 targetPoint, bool slowFall)
        {
            if (thrownMonster == null)
                return;

            var existing = thrownMonster.GetComponent<ThrownCreatureFlight>();
            if (existing != null)
                Destroy(existing);

            var flight = thrownMonster.gameObject.AddComponent<ThrownCreatureFlight>();
            flight.Initialize(thrownMonster, startPoint, targetPoint, slowFall);
        }

        void Initialize(Monster thrownMonster, Vector3 startPoint, Vector3 targetPoint, bool slowFall)
        {
            monster = thrownMonster;
            body = monster.GetComponent<Rigidbody>();
            float gravityScale = slowFall ? NonAggressiveGravityScale : DefaultGravityScale;

            gravityY = Physics.gravity.y * gravityScale;
            monster.BeginAirborneThrow();

            if (body != null)
            {
                body.useGravity = false;
                body.linearVelocity = Vector3.zero;
                body.MovePosition(startPoint);
            }
            else
            {
                monster.transform.position = startPoint;
            }

            float duration = EstimateDuration(startPoint, targetPoint);
            velocity = ComputeBallisticVelocity(startPoint, targetPoint, gravityY, duration);
        }

        void FixedUpdate()
        {
            if (monster == null)
            {
                Destroy(this);
                return;
            }

            elapsed += Time.fixedDeltaTime;
            if (elapsed >= MaxFlightSeconds)
            {
                LandAtCurrentPosition();
                return;
            }

            velocity += Vector3.up * gravityY * Time.fixedDeltaTime;
            Vector3 current = body != null ? body.position : monster.transform.position;
            Vector3 next = current + velocity * Time.fixedDeltaTime;

            if (TryResolveLandingPoint(next, out var landPoint))
            {
                if (body != null)
                    body.MovePosition(landPoint);
                else
                    monster.transform.position = landPoint;

                monster.CompleteThrowLanding(landPoint);
                Destroy(this);
                return;
            }

            if (body != null)
                body.MovePosition(next);
            else
                monster.transform.position = next;
        }

        void OnDestroy()
        {
            if (body != null)
                body.useGravity = true;
        }

        void LandAtCurrentPosition()
        {
            Vector3 current = body != null ? body.position : monster.transform.position;
            if (!TryResolveLandingPoint(current, out var landPoint))
                landPoint = current;

            monster.CompleteThrowLanding(landPoint);
            Destroy(this);
        }

        bool TryResolveLandingPoint(Vector3 probe, out Vector3 landPoint)
        {
            landPoint = probe;
            if (velocity.y > 0f)
                return false;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds != null)
            {
                Vector3 local = bounds.transform.InverseTransformPoint(probe);
                if (bounds.TryResolveFloorWorldPoint(local.x, local.z, out var floorPoint))
                {
                    if (probe.y <= floorPoint.y + 0.08f)
                    {
                        landPoint = floorPoint + Vector3.up * 0.03f;
                        return true;
                    }

                    return false;
                }
            }

            if (FloorAnchor.TryResolveFloorPoint(probe + Vector3.up * 8f, 24f, 48f, out var hit))
            {
                if (probe.y <= hit.y + 0.08f)
                {
                    landPoint = hit + Vector3.up * 0.03f;
                    return true;
                }
            }

            return false;
        }

        static float EstimateDuration(Vector3 start, Vector3 target)
        {
            float flatDistance = Vector3.Distance(
                new Vector3(start.x, 0f, start.z),
                new Vector3(target.x, 0f, target.z));
            return Mathf.Clamp(flatDistance / WorldScale.Feet(35f), 0.75f, 3.5f);
        }

        static Vector3 ComputeBallisticVelocity(Vector3 start, Vector3 target, float gravityY, float duration)
        {
            if (duration <= 0.01f)
                return Vector3.zero;

            Vector3 delta = target - start;
            return new Vector3(
                delta.x / duration,
                (delta.y - 0.5f * gravityY * duration * duration) / duration,
                delta.z / duration);
        }
    }
}
