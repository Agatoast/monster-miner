using System.Collections;
using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    [DefaultExecutionOrder(50)]
    public class PlayerWingsFlight : MonoBehaviour
    {
        const float LaunchUpFeet = 10f;
        const float LaunchOutFeet = 20f;
        const float LaunchDurationSeconds = 0.75f;
        const float GlideDescentFeetPerSecond = 36f;
        const float GlideForwardPerDropFeet = 0.2f;
        const float LandingClearanceFeet = 3f;

        Rigidbody rb;
        CapsuleCollider bodyCollider;
        PlateauEdgeGuard edgeGuard;
        GameObject equippedVisual;
        GameObject worldWings;
        Coroutine flightRoutine;
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
                if (!flying && progression != null && !progression.HasMinerWingsPermission)
                    GameContext.Instance?.Hud?.ShowMessage("Give the Pentachick Heart to the miner before using the wings.");
                else if (!flying && progression != null && progression.MinerWingsConsumed)
                    GameContext.Instance?.Hud?.ShowMessage("Those wings will not lift you again.");
                return;
            }

            worldWings = wings;
            if (equippedVisual != null)
                Destroy(equippedVisual);

            equippedVisual = null;
            if (worldWings != null && AngelWingsVisualFactory.TryAttachWorldWingsToPlayer(worldWings, transform))
            {
                equippedVisual = worldWings;
                worldWings = null;
            }
            else
            {
                equippedVisual = AngelWingsVisualFactory.CreateEquipped(transform);
                if (worldWings != null)
                    worldWings.SetActive(false);
            }

            if (equippedVisual == null)
            {
                if (worldWings != null)
                    worldWings.SetActive(true);
                worldWings = null;
                GameContext.Instance?.Hud?.ShowMessage("The wings failed to attach.");
                return;
            }

            BeginFlight();
            GameContext.Instance?.Hud?.ShowMessage("The miner's wings lift you into the air.");
        }

        public void CancelFlightAndRestoreWings()
        {
            if (!flying)
                return;

            StopFlightRoutine();
            ClearEquippedVisual();
            if (worldWings != null)
                worldWings.SetActive(true);

            flying = false;
            RestorePlayerPhysics();
            if (edgeGuard != null)
                edgeGuard.enabled = true;
        }

        void BeginFlight()
        {
            flying = true;

            if (edgeGuard != null)
                edgeGuard.enabled = false;

            if (rb != null)
            {
                rb.WakeUp();
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            StopFlightRoutine();
            flightRoutine = StartCoroutine(RunFlightRoutine());
        }

        void StopFlightRoutine()
        {
            if (flightRoutine == null)
                return;

            StopCoroutine(flightRoutine);
            flightRoutine = null;
        }

        IEnumerator RunFlightRoutine()
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
            {
                CancelFlightAndRestoreWings();
                yield break;
            }

            Vector3 launchStart = transform.position;
            Vector3 launchEnd = ComputeLaunchEnd(bounds, launchStart);
            Vector3 glideDirection = launchEnd - launchStart;
            glideDirection.y = 0f;
            if (glideDirection.sqrMagnitude < 0.0001f)
                glideDirection = GetOutwardWorldDirection(bounds, launchStart);
            else
                glideDirection.Normalize();

            float launchElapsed = 0f;
            while (launchElapsed < LaunchDurationSeconds)
            {
                launchElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(launchElapsed / LaunchDurationSeconds);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                SetFlightPosition(Vector3.Lerp(launchStart, launchEnd, eased));
                yield return null;
            }

            SetFlightPosition(launchEnd);
            GameContext.Instance?.Hud?.ShowMessage("The wings spread wide and carry you down from the plateau.");

            float descentSpeed = WorldScale.Feet(GlideDescentFeetPerSecond);
            while (flying)
            {
                float dt = Time.deltaTime;
                float drop = descentSpeed * dt;
                float forward = drop * GlideForwardPerDropFeet;
                Vector3 nextPos = transform.position + glideDirection * forward - Vector3.up * drop;

                Vector3 local = bounds.transform.InverseTransformPoint(nextPos);
                bool stillOnPlateau = PlateauBoundary.IsOnPlateau(local.x, local.z, bounds.Radius);
                float groundY = SampleFlightGroundWorldY(bounds, local.x, local.z, stillOnPlateau);
                float feetY = nextPos.y - GetHalfHeight();
                float clearance = feetY - groundY;

                if (!stillOnPlateau && clearance <= WorldScale.Feet(LandingClearanceFeet))
                {
                    CompleteLanding(bounds, groundY);
                    yield break;
                }

                if (!stillOnPlateau && clearance < WorldScale.Feet(24f))
                    nextPos.y = Mathf.Max(nextPos.y, groundY + GetHalfHeight() + WorldScale.Feet(LandingClearanceFeet));

                SetFlightPosition(nextPos);
                yield return null;
            }
        }

        Vector3 ComputeLaunchEnd(CavernBounds bounds, Vector3 startWorld)
        {
            Vector3 outward = GetOutwardWorldDirection(bounds, startWorld);
            return startWorld + outward * WorldScale.Feet(LaunchOutFeet) + Vector3.up * WorldScale.Feet(LaunchUpFeet);
        }

        static Vector3 GetOutwardWorldDirection(CavernBounds bounds, Vector3 worldPoint)
        {
            Vector3 local = bounds.transform.InverseTransformPoint(worldPoint);
            Vector2 flat = new Vector2(local.x, local.z);
            Vector2 outward = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector2.right;
            return bounds.transform.TransformDirection(new Vector3(outward.x, 0f, outward.y)).normalized;
        }

        void SetFlightPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            if (rb != null)
                rb.position = worldPosition;
        }

        static float SampleFlightGroundWorldY(CavernBounds bounds, float localX, float localZ, bool stillOnPlateau)
        {
            if (stillOnPlateau)
                return bounds.SamplePlateauFloorWorldY(localX, localZ);

            if (PlainsGroundSupport.ShouldSupportAt(new Vector3(localX, 0f, localZ), bounds.Radius))
                return PlainsWorldBuilder.SamplePlainsWorldY(bounds.transform, localX, localZ);

            return CreatureSurfaceSampler.SampleWorldY(bounds, localX, localZ);
        }

        void CompleteLanding(CavernBounds bounds, float _)
        {
            flying = false;
            StopFlightRoutine();

            Vector3 landingPoint = PlainsGroundSupport.ResolvePlainsLandingPoint(
                bounds,
                transform.position,
                GetHalfHeight());

            ClearEquippedVisual();
            if (worldWings != null)
                Destroy(worldWings);
            worldWings = null;

            RestorePlayerPhysics();
            SetFlightPosition(landingPoint);
            if (rb != null)
            {
                rb.useGravity = false;
                rb.WakeUp();
            }

            Physics.SyncTransforms();

            if (edgeGuard != null)
                edgeGuard.enabled = true;

            ParkTruckBesidePlayer(bounds);
            GameContext.Instance?.CaveProgression?.NotifyLandedOnLand();
            GameContext.Instance?.CaveProgression?.ConsumeMinerWings();
            GameContext.Instance?.Hud?.ShowMessage("The wings fade as you touch the ground.");
        }

        void RestorePlayerPhysics()
        {
            if (rb == null)
                return;

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        void ParkTruckBesidePlayer(CavernBounds bounds)
        {
            if (bounds == null)
                return;

            if (GameContext.Instance?.PlayerTruck != null)
                Destroy(GameContext.Instance.PlayerTruck.gameObject);

            Vector3 left = -transform.right;
            left.y = 0f;
            if (left.sqrMagnitude < 0.001f)
                left = Vector3.left;
            left.Normalize();

            Vector3 truckWorld = transform.position + left * WorldScale.Feet(20f);
            Vector3 truckLocal = bounds.transform.InverseTransformPoint(truckWorld);
            truckLocal = PlainsGroundSupport.PushOutwardToSafePlains(truckLocal, bounds.Radius);
            truckWorld = bounds.transform.TransformPoint(new Vector3(truckLocal.x, 0f, truckLocal.z));

            float groundY = PlainsWorldBuilder.SamplePlainsWorldY(bounds.transform, truckLocal.x, truckLocal.z);
            Vector3 floorContact = new Vector3(truckWorld.x, groundY, truckWorld.z);

            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            Vector3 truckForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            Vector3 playerToTruck = floorContact - transform.position;
            playerToTruck.y = 0f;
            if (playerToTruck.sqrMagnitude > 0.01f && Vector3.Dot(truckForward, playerToTruck.normalized) < 0.2f)
                truckForward = playerToTruck.normalized;

            var truck = IndustrialSmallTruckVisualFactory.CreateOnGround(
                bounds.transform,
                floorContact,
                Quaternion.LookRotation(truckForward, Vector3.up));
            if (truck != null && GameContext.Instance != null)
                GameContext.Instance.PlayerTruck = truck;
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

        void OnDisable()
        {
            StopFlightRoutine();
        }
    }
}
