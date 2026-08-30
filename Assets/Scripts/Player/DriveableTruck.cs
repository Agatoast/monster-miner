using MonsterMiner.Combat;
using MonsterMiner.Core;
using MonsterMiner.Util;
using MonsterMiner.World;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterMiner.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class DriveableTruck : MonoBehaviour
    {
        const float MaxDriveSpeedMph = 50f;
        const float MaxReverseSpeedMph = 10f;
        const float DriveAccelerationMphPerSecond = 10f;
        const float TurnSpeedDegrees = 95f;
        const float DismountSpeedThreshold = 1.75f;
        const float CreatureHitMinSpeedMph = 4f;
        const float RockHitDamage = 10f;
        const float RockHitShakeDuration = 0.45f;
        const float RockHitShakeIntensity = 0.42f;
        const float RockHitCooldownSeconds = 0.6f;

        float lastRockHitTime;
        readonly HashSet<Collider> ignoredObstacleColliders = new HashSet<Collider>();

        Transform seat;
        Transform cargoBed;
        Rigidbody rb;
        PlayerVehicleMount driver;
        PlayerVehicleMount cargoOccupant;
        float bottomOffset = 0.8f;
        Color bodyColor = new Color(0.78f, 0.12f, 0.10f);
        bool bodyColorCached;
        bool driverViewHidden;
        Renderer[] bodyRenderers;

        Vector3 cargoHalfExtents = new Vector3(0.95f, 0.2f, 1.35f);
        float cargoBedTopLocalY = 0.15f;

        public Transform Seat => seat;
        public Transform CargoBed => cargoBed;
        public Color BodyColor => bodyColor;
        public Vector3 CargoEntryLocalPosition => new Vector3(0f, cargoBedTopLocalY + WorldScale.CharacterHeightUnits * 0.5f, 0f);
        public bool HasDriver => driver != null;
        public bool HasCargoOccupant => cargoOccupant != null;
        public bool CanDismount => !HasDriver || rb.linearVelocity.magnitude <= DismountSpeedThreshold;

        public float DisplaySpeedMph
        {
            get
            {
                if (rb == null)
                    return 0f;

                float forwardUnits = Vector3.Dot(rb.linearVelocity, transform.forward);
                float oneMph = WorldScale.MilesPerHour(1f);
                if (oneMph <= 0.0001f)
                    return 0f;

                return Mathf.Max(0f, forwardUnits / oneMph);
            }
        }

        public void Initialize(Transform driverSeat, Transform bed)
        {
            seat = driverSeat;
            cargoBed = bed;
            CacheCargoBedTopLocalY();
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationY
                | RigidbodyConstraints.FreezeRotationZ;
            CacheBottomOffset();
            RegisterExistingPassThroughObstacles();
            rb.WakeUp();
        }

        public static void RegisterPassThroughObstacle(Collider obstacleCollider)
        {
            GameContext.Instance?.PlayerTruck?.IgnoreObstacleCollision(obstacleCollider);
        }

        public Vector3 ClampCargoLocalPosition(Vector3 localPosition)
        {
            localPosition.x = Mathf.Clamp(localPosition.x, -cargoHalfExtents.x, cargoHalfExtents.x);
            localPosition.z = Mathf.Clamp(localPosition.z, -cargoHalfExtents.z, cargoHalfExtents.z);
            localPosition.y = CargoEntryLocalPosition.y;
            return localPosition;
        }

        void CacheCargoBedTopLocalY()
        {
            if (cargoBed == null)
                return;

            var bedCollider = cargoBed.GetComponent<BoxCollider>();
            if (bedCollider == null)
                return;

            cargoBedTopLocalY = bedCollider.center.y + bedCollider.size.y * 0.5f;
        }

        public void SetDriver(PlayerVehicleMount mount)
        {
            driver = mount;
            rb.WakeUp();
        }

        public void CacheBodyColor()
        {
            bodyColorCached = true;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.name.Contains("Tyre"))
                    continue;

                var material = renderer.sharedMaterial;
                if (material == null)
                    continue;

                if (material.HasProperty("_BaseColor"))
                {
                    bodyColor = material.GetColor("_BaseColor");
                    return;
                }

                bodyColor = material.color;
                return;
            }
        }

        public void SetDriverViewHidden(bool hidden)
        {
            if (driverViewHidden == hidden)
                return;

            if (!bodyColorCached)
                CacheBodyColor();

            if (bodyRenderers == null)
                bodyRenderers = GetComponentsInChildren<Renderer>(true);

            driverViewHidden = hidden;
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] != null)
                    bodyRenderers[i].enabled = !hidden;
            }
        }

        public void ClearDriver(PlayerVehicleMount mount)
        {
            if (driver == mount)
                driver = null;
        }

        public void SetCargoOccupant(PlayerVehicleMount mount)
        {
            cargoOccupant = mount;
        }

        public void ClearCargoOccupant(PlayerVehicleMount mount)
        {
            if (cargoOccupant == mount)
                cargoOccupant = null;
        }

        void FixedUpdate()
        {
            if (rb == null)
                return;

            if (HasDriver && seat != null)
                DriveWithInput();
            else
                StickToPlainsGround();

            if (HasDriver)
                ScanTruckObstacleHits();
        }

        void DriveWithInput()
        {
            if (PlayerController.IsGameplayBlocked())
                return;

            bool throttle = Input.GetKey(KeyCode.W);
            bool brake = Input.GetKey(KeyCode.S);
            float steer = Input.GetAxisRaw("Horizontal");
            float maxForwardSpeed = WorldScale.MilesPerHour(MaxDriveSpeedMph);
            float maxReverseSpeed = WorldScale.MilesPerHour(MaxReverseSpeedMph);
            float driveAcceleration = WorldScale.MilesPerHour(DriveAccelerationMphPerSecond);
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

            if (throttle && !brake)
            {
                forwardSpeed = Mathf.MoveTowards(
                    forwardSpeed,
                    maxForwardSpeed,
                    driveAcceleration * Time.fixedDeltaTime);
            }
            else if (brake)
            {
                if (forwardSpeed > 0f)
                {
                    forwardSpeed = Mathf.MoveTowards(
                        forwardSpeed,
                        0f,
                        driveAcceleration * Time.fixedDeltaTime);
                }
                else
                {
                    forwardSpeed = Mathf.MoveTowards(
                        forwardSpeed,
                        -maxReverseSpeed,
                        driveAcceleration * Time.fixedDeltaTime);
                }
            }

            rb.angularVelocity = Vector3.zero;
            if (Mathf.Abs(steer) > 0.01f && Mathf.Abs(forwardSpeed) > 0.35f)
            {
                float turnSign = forwardSpeed >= 0f ? 1f : -1f;
                float turnAmount = steer * TurnSpeedDegrees * turnSign * Time.fixedDeltaTime;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount, 0f));
            }
            else
            {
                rb.constraints = RigidbodyConstraints.FreezeRotationX
                    | RigidbodyConstraints.FreezeRotationY
                    | RigidbodyConstraints.FreezeRotationZ;
            }

            Vector3 flatForward = transform.forward * forwardSpeed;
            rb.linearVelocity = new Vector3(flatForward.x, 0f, flatForward.z);
            StickToPlainsGround();
        }

        void StickToPlainsGround()
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return;

            Vector3 local = bounds.transform.InverseTransformPoint(transform.position);
            if (LakeCatalog.IsOpenWaterLocal(local.x, local.z))
                return;

            if (!PlainsGroundSupport.ShouldSupportAt(local, bounds.Radius))
                return;

            float groundY = PlainsWorldBuilder.SamplePlainsWorldY(bounds.transform, local.x, local.z);
            Vector3 pos = rb.position;
            pos.y = groundY + bottomOffset + WorldScale.SpawnDropHeight;
            rb.position = pos;

            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
            rb.angularVelocity = Vector3.zero;
        }

        void ScanTruckObstacleHits()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
                return;

            Vector3 center = transform.TransformPoint(box.center);
            Vector3 halfExtents = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
            var hits = Physics.OverlapBox(
                center,
                halfExtents,
                transform.rotation,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.IsChildOf(transform))
                    continue;

                HandleObstacleHit(hits[i]);
            }
        }

        void HandleObstacleHit(Collider hit)
        {
            if (hit == null)
                return;

            if (TruckObstacleUtility.TryGetRock(hit, out _))
            {
                TryHitRock();
                return;
            }

            IgnoreObstacleCollision(hit);

            if (!IsMovingFastEnoughToHit())
                return;

            Vector3 launchVelocity = GetTruckLaunchVelocity();

            if (TruckObstacleUtility.TryGetTree(hit, out var tree) && tree.TryLaunchFromTruck(launchVelocity))
                return;

            var egg = hit.GetComponentInParent<MonsterEgg>();
            if (egg != null && egg.TryInstantHatchFromTruck(launchVelocity))
                return;

            var monster = hit.GetComponentInParent<Monster>();
            if (monster == null || monster.IsInAir || monster.IsCarried)
                return;

            monster.LaunchFromTruck(launchVelocity);
        }

        void IgnoreObstacleCollision(Collider hit)
        {
            if (hit == null || TruckObstacleUtility.TryGetRock(hit, out _))
                return;

            Transform obstacleRoot = ResolvePassThroughRoot(hit);
            if (obstacleRoot == null)
                return;

            var obstacleColliders = obstacleRoot.GetComponentsInChildren<Collider>(true);
            var truckColliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < obstacleColliders.Length; i++)
            {
                var obstacleCollider = obstacleColliders[i];
                if (obstacleCollider == null || obstacleCollider.isTrigger)
                    continue;

                if (!ignoredObstacleColliders.Add(obstacleCollider))
                    continue;

                for (int j = 0; j < truckColliders.Length; j++)
                {
                    var truckCollider = truckColliders[j];
                    if (truckCollider == null || truckCollider.isTrigger)
                        continue;

                    Physics.IgnoreCollision(truckCollider, obstacleCollider, true);
                }
            }
        }

        Transform ResolvePassThroughRoot(Collider hit)
        {
            if (TruckObstacleUtility.TryGetTree(hit, out var tree))
                return tree.transform;

            var egg = hit.GetComponentInParent<MonsterEgg>();
            if (egg != null)
                return egg.transform;

            var monster = hit.GetComponentInParent<Monster>();
            if (monster != null)
                return monster.transform;

            return hit.transform;
        }

        void RegisterExistingPassThroughObstacles()
        {
            foreach (var tree in FindObjectsByType<PlainsTreeObstacle>(FindObjectsSortMode.None))
            {
                var collider = tree.GetComponent<Collider>();
                if (collider != null)
                    IgnoreObstacleCollision(collider);
            }

            foreach (var egg in FindObjectsByType<MonsterEgg>(FindObjectsSortMode.None))
            {
                var collider = egg.GetComponent<Collider>();
                if (collider != null)
                    IgnoreObstacleCollision(collider);
            }

            foreach (var monster in FindObjectsByType<Monster>(FindObjectsSortMode.None))
            {
                var collider = monster.GetComponent<Collider>();
                if (collider != null)
                    IgnoreObstacleCollision(collider);
            }
        }

        void TryHitRock()
        {
            if (!IsMovingFastEnoughToHit())
                return;

            if (Time.time - lastRockHitTime < RockHitCooldownSeconds)
                return;

            lastRockHitTime = Time.time;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            GameContext.Instance?.Hud?.ShowCenterMessage("Don't Hit Rocks!", 0.22f);
            DamageOccupants(RockHitDamage);
            ShakeOccupants();
        }

        void DamageOccupants(float damage)
        {
            ApplyDamageToMount(driver, damage);
            ApplyDamageToMount(cargoOccupant, damage);
        }

        void ShakeOccupants()
        {
            ApplyShakeToMount(driver);
            ApplyShakeToMount(cargoOccupant);
        }

        static void ApplyDamageToMount(PlayerVehicleMount mount, float damage)
        {
            if (mount == null)
                return;

            mount.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        }

        static void ApplyShakeToMount(PlayerVehicleMount mount)
        {
            if (mount == null)
                return;

            mount.GetComponent<PlayerCameraShake>()?.BeginViolentShake(RockHitShakeDuration, RockHitShakeIntensity);
        }

        Vector3 GetTruckLaunchVelocity()
        {
            if (rb == null)
                return transform.forward * 0.01f;

            return rb.linearVelocity;
        }

        bool IsMovingFastEnoughToHit()
        {
            return rb != null
                && rb.linearVelocity.sqrMagnitude >= WorldScale.MilesPerHour(CreatureHitMinSpeedMph)
                    * WorldScale.MilesPerHour(CreatureHitMinSpeedMph);
        }

        void CacheBottomOffset()
        {
            Physics.SyncTransforms();
            float bottomY = FloorAnchor.GetBottomY(gameObject);
            bottomOffset = Mathf.Max(0.2f, transform.position.y - bottomY);
        }
    }
}
