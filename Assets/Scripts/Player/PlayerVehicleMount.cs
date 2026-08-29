using MonsterMiner.Core;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class PlayerVehicleMount : MonoBehaviour
    {
        const float DismountSideOffset = 2.2f;
        const float CargoMoveSpeedMph = 8f;
        static readonly Vector3 DriverSeatLocalOffset = new Vector3(0.05f, -0.58f, 0.08f);
        static readonly Vector3 DriverSeatLocalEuler = Vector3.zero;

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

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            if (bodyCollider != null)
                bodyCollider.enabled = false;
            if (bodyRenderer != null)
                bodyRenderer.enabled = true;

            Transform seat = truck.Seat;
            transform.SetParent(seat, false);
            transform.localPosition = DriverSeatLocalOffset;
            transform.localRotation = Quaternion.Euler(DriverSeatLocalEuler);

            controller.ResetViewPitch();
            driverHeadYaw = 0f;

            IndustrialSmallTruckVisualFactory.ApplyEquippedSkin(truck.gameObject);
            GameContext.Instance?.Hud?.ShowMessage("W to throttle, S to brake/reverse. [E] to get out front.");
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

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

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

        void FixedUpdate()
        {
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

            Vector3 exitDirection = exitedFront ? -truck.transform.right : -truck.transform.forward;
            if (exitDirection.sqrMagnitude < 0.01f)
                exitDirection = Vector3.right;
            exitDirection.Normalize();

            Vector3 dismountWorldPosition = truck.transform.position
                + exitDirection * DismountSideOffset
                + Vector3.up * 0.5f;

            transform.SetParent(null, true);
            if (controller != null)
                transform.position = controller.SnapToFloorWorld(dismountWorldPosition);
            else
                transform.position = dismountWorldPosition;

            transform.rotation = Quaternion.LookRotation(
                exitedFront ? truck.transform.forward : exitDirection,
                Vector3.up);

            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (bodyCollider != null)
                bodyCollider.enabled = true;
            if (bodyRenderer != null)
                bodyRenderer.enabled = false;

            if (controller?.Head != null)
                controller.Head.localRotation = Quaternion.identity;

            controller?.ResetViewPitch();
            return true;
        }
    }
}
