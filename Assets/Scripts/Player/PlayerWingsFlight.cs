using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class PlayerWingsFlight : MonoBehaviour
    {
        const float DescentMph = 32f;
        const float OutwardGlideMph = 16f;
        const float AirControlMph = 11f;
        const float LandingDistanceFromWallFeet = 100f;

        Rigidbody rb;
        CapsuleCollider bodyCollider;
        PlateauEdgeGuard edgeGuard;
        GameObject equippedVisual;
        GameObject worldWings;
        bool flying;

        public bool IsFlying => flying;

        public void Initialize()
        {
            rb = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<CapsuleCollider>();
            edgeGuard = GetComponent<PlateauEdgeGuard>();
        }

        public void EquipFromWorld(GameObject wings)
        {
            var progression = GameContext.Instance?.CaveProgression;
            if (flying || progression == null || !progression.CanEquipMinerWings)
            {
                if (!flying && CaveProgression.HasPentachickHeartInInventory())
                    GameContext.Instance?.Hud?.ShowMessage("Give the Pentachick Heart to the miner before using the wings.");
                return;
            }

            worldWings = wings;
            if (worldWings != null)
                worldWings.SetActive(false);

            if (equippedVisual != null)
                Destroy(equippedVisual);

            equippedVisual = AngelWingsVisualFactory.CreateEquipped(transform);
            flying = true;
            if (edgeGuard != null)
                edgeGuard.enabled = false;
            if (rb != null)
                rb.useGravity = false;

            GameContext.Instance?.Hud?.ShowMessage("The miner's wings carry you away from the plateau.");
        }

        public void CancelFlightAndRestoreWings()
        {
            if (!flying)
                return;

            ClearEquippedVisual();
            if (worldWings != null)
                worldWings.SetActive(true);

            flying = false;
            if (rb != null)
                rb.useGravity = true;
            if (edgeGuard != null)
                edgeGuard.enabled = true;
        }

        void FixedUpdate()
        {
            if (!flying || rb == null || PlayerController.IsGameplayBlocked())
                return;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return;

            Vector3 local = bounds.transform.InverseTransformPoint(transform.position);
            bool stillOnPlateau = PlateauBoundary.IsOnPlateau(local.x, local.z, bounds.Radius);
            float angle = Mathf.Atan2(local.z, local.x);
            float landingRadius = GetLandingRadius(bounds, angle);
            Vector2 flat = new Vector2(local.x, local.z);
            float distance = flat.magnitude;
            Vector2 outward = distance > 0.001f ? flat / distance : Vector2.down;
            Vector3 outwardWorld = bounds.transform.TransformDirection(new Vector3(outward.x, 0f, outward.y));

            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            Vector3 airMove = transform.TransformDirection(input) * WorldScale.MilesPerHour(AirControlMph);
            Vector3 velocity = rb.linearVelocity;

            if (stillOnPlateau)
            {
                float glideSpeed = WorldScale.MilesPerHour(OutwardGlideMph);
                velocity.x = outwardWorld.x * glideSpeed + airMove.x;
                velocity.z = outwardWorld.z * glideSpeed + airMove.z;
                velocity.y = 0f;
            }
            else
            {
                velocity.y = -WorldScale.MilesPerHour(DescentMph);
                velocity.x = airMove.x;
                velocity.z = airMove.z;

                if (distance < landingRadius)
                {
                    float glideSpeed = WorldScale.MilesPerHour(OutwardGlideMph);
                    velocity.x += outwardWorld.x * glideSpeed;
                    velocity.z += outwardWorld.z * glideSpeed;
                }
            }

            rb.linearVelocity = velocity;

            if (!stillOnPlateau && HasReachedLandingZone(bounds, local, landingRadius))
                CompleteLanding(bounds, local);
        }

        static float GetLandingRadius(CavernBounds bounds, float angle)
        {
            return BluffSlopeBuilder.GetBluffBaseOutwardRadius(angle, bounds.Radius)
                + WorldScale.Feet(LandingDistanceFromWallFeet);
        }

        bool HasReachedLandingZone(CavernBounds bounds, Vector3 local, float landingRadius)
        {
            float distance = new Vector2(local.x, local.z).magnitude;
            if (distance < landingRadius)
                return false;

            float groundY = SampleLowerGroundWorldY(bounds, local.x, local.z);
            return transform.position.y - GetHalfHeight() <= groundY + 0.6f;
        }

        void CompleteLanding(CavernBounds bounds, Vector3 local)
        {
            Vector3 probe = bounds.transform.TransformPoint(local);
            Vector3 landed = transform.position;
            if (FloorAnchor.TryResolveFloorPoint(probe, 64f, 256f, out var floorPoint))
                landed = new Vector3(floorPoint.x, floorPoint.y + GetHalfHeight() + 0.02f, floorPoint.z);
            else
            {
                float groundY = SampleLowerGroundWorldY(bounds, local.x, local.z);
                landed.y = groundY + GetHalfHeight() + 0.02f;
            }

            transform.position = landed;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.useGravity = true;
            }

            ClearEquippedVisual();
            if (worldWings != null)
                Destroy(worldWings);
            worldWings = null;
            flying = false;
            if (edgeGuard != null)
                edgeGuard.enabled = true;

            GameContext.Instance?.CaveProgression?.ConsumeMinerWings();
            GameContext.Instance?.Hud?.ShowMessage("The wings fade as you touch the ground.");
        }

        static float SampleLowerGroundWorldY(CavernBounds bounds, float localX, float localZ)
        {
            float lowerBase = LowerWorldBuilder.GetLowerGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float localY = LowerWorldBuilder.SampleLowerPlainsLocalY(localX, localZ, lowerBase);
            return bounds.transform.TransformPoint(new Vector3(localX, localY, localZ)).y;
        }

        float GetHalfHeight()
        {
            if (bodyCollider != null)
                return (bodyCollider.height * 0.5f) * transform.lossyScale.y;
            return WorldScale.CharacterHeightUnits * 0.5f;
        }

        void ClearEquippedVisual()
        {
            if (equippedVisual == null)
                return;

            Destroy(equippedVisual);
            equippedVisual = null;
        }
    }
}
