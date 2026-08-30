using MonsterMiner.Core;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class PlayerVehicleMount : MonoBehaviour
    {
        const float DismountSideOffset = 2.2f;
        const float CargoDismountSideOffsetFeet = 5f;
        const float CargoMoveSpeedMph = 8f;

        public enum MountMode
        {
            None,
            Driver,
            Cargo
        }

        DriveableTruck currentTruck;
        PlayerController controller;
        Rigidbody rb;
        CapsuleCollider bodyCollider;
        Renderer bodyRenderer;
        MountMode mode = MountMode.None;
        float driverHeadYaw;
        RigidbodyInterpolation storedInterpolation;

        public bool IsMounted => mode != MountMode.None;
        public bool IsDriving => mode == MountMode.Driver;
        public bool IsInCargo => mode == MountMode.Cargo;
        public DriveableTruck CurrentTruck => currentTruck;
        public float DriverHeadYaw => driverHeadYaw;

        public void Initialize()
        {
            controller = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<CapsuleCollider>();
            bodyRenderer = GetComponent<Renderer>();
        }

        public bool TryMountDriver(DriveableTruck truck)
        {
            if (truck == null || IsMounted || truck.HasDriver || controller == null || rb == null)
                return false;

            if (GetComponent<PlayerWingsFlight>()?.IsFlying == true)
                return false;

            currentTruck = truck;
            mode = MountMode.Driver;
            driverHeadYaw = 0f;
            truck.SetDriver(this);

            BeginMountedPhysics(truck, ignoreBedCollisions: true);

            if (bodyCollider != null)
                bodyCollider.enabled = false;
            if (bodyRenderer != null)
                bodyRenderer.enabled = false;

            Transform seat = truck.Seat;
            transform.SetParent(seat, false);
            AlignDriverCameraToSeat();

            controller.ResetViewPitch(8f);
            driverHeadYaw = 0f;

            IndustrialSmallTruckVisualFactory.ApplyEquippedSkin(truck.gameObject);
            truck.SetDriverViewHidden(true);
            GameContext.Instance?.Hud?.ShowMessage("W to throttle, S to brake/reverse. [E] to get out front.");
            GameContext.Instance?.Hud?.ShowCenterMessage("Avoid the rocks!");
            return true;
        }

        public bool TryMountCargo(DriveableTruck truck)
        {
            if (truck == null || IsMounted || truck.HasCargoOccupant || controller == null || rb == null)
                return false;

            if (GetComponent<PlayerWingsFlight>()?.IsFlying == true)
                return false;

            currentTruck = truck;
            mode = MountMode.Cargo;
            truck.SetCargoOccupant(this);

            BeginMountedPhysics(truck, ignoreBedCollisions: false);

            if (bodyCollider != null)
                bodyCollider.enabled = false;
            if (bodyRenderer != null)
                bodyRenderer.enabled = true;

            Transform bed = truck.CargoBed;
            transform.SetParent(bed, false);
            transform.localPosition = truck.CargoEntryLocalPosition;
            transform.localRotation = Quaternion.identity;

            if (controller.Head != null)
                controller.Head.localRotation = Quaternion.identity;

            controller.ResetViewPitch();
            IndustrialSmallTruckVisualFactory.ApplyEquippedSkin(truck.gameObject);
            GameContext.Instance?.Hud?.ShowMessage("Move around the bed. [E] to get out back.");
            return true;
        }

        public bool TryDismount()
        {
            if (!IsMounted || currentTruck == null)
                return false;

            if (IsDriving && !currentTruck.CanDismount)
            {
                GameContext.Instance?.Hud?.ShowMessage("Slow down before getting out front.");
                return false;
            }

            return CompleteDismount();
        }

        public void ForceDismount()
        {
            if (!IsMounted)
                return;

            CompleteDismount();
        }

        public void AddDriverHeadYaw(float deltaDegrees)
        {
            driverHeadYaw = Mathf.Clamp(driverHeadYaw + deltaDegrees, -75f, 75f);
        }

        void BeginMountedPhysics(DriveableTruck truck, bool ignoreBedCollisions)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            storedInterpolation = rb.interpolation;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.isKinematic = true;
            rb.detectCollisions = false;
            SetIgnoreTruckCollisions(truck, true, ignoreBedCollisions);
        }

        void AlignDriverCameraToSeat()
        {
            if (currentTruck == null || currentTruck.Seat == null)
                return;

            Vector3 headLocal = controller != null && controller.Head != null
                ? controller.Head.localPosition
                : new Vector3(0f, 0.65f, 0f);
            transform.localPosition = -headLocal;
            transform.localRotation = Quaternion.identity;
        }

        void LateUpdate()
        {
            if (IsDriving && currentTruck != null)
            {
                AlignDriverCameraToSeat();
                return;
            }

            if (!IsInCargo || currentTruck == null)
                return;

            var stand = currentTruck.CargoEntryLocalPosition;
            var local = transform.localPosition;
            if (Mathf.Abs(local.y - stand.y) > 0.001f)
                transform.localPosition = new Vector3(local.x, stand.y, local.z);
        }

        void FixedUpdate()
        {
            if (IsDriving)
                return;

            if (!IsInCargo || currentTruck == null || controller == null)
                return;

            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude < 0.0001f)
                return;

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            Transform bed = currentTruck.CargoBed;
            Vector3 worldMove = transform.TransformDirection(new Vector3(input.x, 0f, input.z));
            Vector3 localMove = bed.InverseTransformDirection(worldMove) * WorldScale.MilesPerHour(CargoMoveSpeedMph) * Time.fixedDeltaTime;
            Vector3 nextLocal = transform.localPosition + localMove;
            nextLocal = currentTruck.ClampCargoLocalPosition(nextLocal);
            nextLocal.y = currentTruck.CargoEntryLocalPosition.y;
            transform.localPosition = nextLocal;
        }

        bool CompleteDismount()
        {
            if (currentTruck == null)
                return false;

            var truck = currentTruck;
            bool exitedFront = IsDriving;
            currentTruck = null;
            mode = MountMode.None;
            driverHeadYaw = 0f;

            if (exitedFront)
                truck.ClearDriver(this);
            else
                truck.ClearCargoOccupant(this);

            if (exitedFront)
                truck.SetDriverViewHidden(false);

            Vector3 dismountWorldPosition;
            Vector3 lookDirection;

            if (exitedFront)
            {
                lookDirection = truck.transform.forward;
                dismountWorldPosition = truck.transform.position
                    - truck.transform.right * DismountSideOffset
                    + Vector3.up * 0.5f;
            }
            else
            {
                lookDirection = truck.transform.forward;
                dismountWorldPosition = truck.transform.position
                    - truck.transform.right * WorldScale.Feet(CargoDismountSideOffsetFeet);
            }

            SetIgnoreTruckCollisions(truck, false, ignoreBedColliders: true);
            transform.SetParent(null, true);
            if (controller != null)
                transform.position = controller.SnapToFloorWorld(dismountWorldPosition);
            else
                transform.position = dismountWorldPosition;

            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);

            rb.detectCollisions = true;
            rb.isKinematic = false;
            rb.interpolation = storedInterpolation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
            rb.position = transform.position;
            rb.rotation = transform.rotation;

            if (bodyCollider != null)
                bodyCollider.enabled = true;
            if (bodyRenderer != null)
                bodyRenderer.enabled = false;

            if (controller?.Head != null)
                controller.Head.localRotation = Quaternion.identity;

            controller?.ResetViewPitch();
            return true;
        }

        void SetIgnoreTruckCollisions(DriveableTruck truck, bool ignore, bool ignoreBedColliders = true)
        {
            if (truck == null)
                return;

            Transform bed = truck.CargoBed;
            var truckColliders = truck.GetComponentsInChildren<Collider>(true);
            var playerColliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < truckColliders.Length; i++)
            {
                var truckCollider = truckColliders[i];
                if (truckCollider == null || truckCollider.isTrigger)
                    continue;

                if (!ignoreBedColliders && bed != null && truckCollider.transform.IsChildOf(bed))
                    continue;

                for (int j = 0; j < playerColliders.Length; j++)
                {
                    if (playerColliders[j] == null)
                        continue;

                    Physics.IgnoreCollision(truckCollider, playerColliders[j], ignore);
                }
            }
        }
    }
}
