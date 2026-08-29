using MonsterMiner.Core;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class DriveableTruck : MonoBehaviour
    {
        const float MaxDriveSpeedMph = 50f;
        const float MaxReverseSpeedMph = 10f;
        const float DriveAcceleration = 24f;
        const float BrakeAcceleration = 36f;
        const float TurnSpeedDegrees = 95f;
        const float DismountSpeedThreshold = 1.75f;

        Transform seat;
        Transform cargoBed;
        Rigidbody rb;
        PlayerVehicleMount driver;
        PlayerVehicleMount cargoOccupant;
        float bottomOffset = 0.8f;

        Vector3 cargoHalfExtents = new Vector3(0.95f, 0.2f, 1.35f);

        public Transform Seat => seat;
        public Transform CargoBed => cargoBed;
        public Vector3 CargoEntryLocalPosition => new Vector3(0f, 0.35f, 0f);
        public bool HasDriver => driver != null;
        public bool HasCargoOccupant => cargoOccupant != null;
        public bool CanDismount => !HasDriver || rb.linearVelocity.magnitude <= DismountSpeedThreshold;

        public void Initialize(Transform driverSeat, Transform bed)
        {
            seat = driverSeat;
            cargoBed = bed;
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            CacheBottomOffset();
            rb.WakeUp();
        }

        public Vector3 ClampCargoLocalPosition(Vector3 localPosition)
        {
            localPosition.x = Mathf.Clamp(localPosition.x, -cargoHalfExtents.x, cargoHalfExtents.x);
            localPosition.z = Mathf.Clamp(localPosition.z, -cargoHalfExtents.z, cargoHalfExtents.z);
            return localPosition;
        }

        public void SetDriver(PlayerVehicleMount mount)
        {
            driver = mount;
            rb.WakeUp();
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
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

            if (throttle && !brake)
            {
                forwardSpeed = Mathf.MoveTowards(
                    forwardSpeed,
                    maxForwardSpeed,
                    DriveAcceleration * Time.fixedDeltaTime);
            }
            else if (brake)
            {
                if (forwardSpeed > 0f)
                {
                    forwardSpeed = Mathf.MoveTowards(
                        forwardSpeed,
                        0f,
                        BrakeAcceleration * Time.fixedDeltaTime);
                }
                else
                {
                    forwardSpeed = Mathf.MoveTowards(
                        forwardSpeed,
                        -maxReverseSpeed,
                        DriveAcceleration * Time.fixedDeltaTime);
                }
            }
            else
            {
                forwardSpeed = Mathf.MoveTowards(
                    forwardSpeed,
                    0f,
                    BrakeAcceleration * 0.65f * Time.fixedDeltaTime);
            }

            if (Mathf.Abs(steer) > 0.01f && Mathf.Abs(forwardSpeed) > 0.35f)
            {
                float turnSign = forwardSpeed >= 0f ? 1f : -1f;
                float turnAmount = steer * TurnSpeedDegrees * turnSign * Time.fixedDeltaTime;
                rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount, 0f));
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
            if (!PlainsGroundSupport.ShouldSupportAt(local, bounds.Radius))
                return;

            float groundY = PlainsWorldBuilder.SamplePlainsWorldY(bounds.transform, local.x, local.z);
            Vector3 pos = rb.position;
            pos.y = groundY + bottomOffset + WorldScale.SpawnDropHeight;
            rb.position = pos;

            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
        }

        void CacheBottomOffset()
        {
            Physics.SyncTransforms();
            float bottomY = FloorAnchor.GetBottomY(gameObject);
            bottomOffset = Mathf.Max(0.2f, transform.position.y - bottomY);
        }
    }
}
