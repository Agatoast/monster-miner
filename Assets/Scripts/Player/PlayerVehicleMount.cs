using MonsterMiner.Core;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    [DefaultExecutionOrder(110)]
    public class PlayerVehicleMount : MonoBehaviour
    {
        const float DismountSideOffset = 2.2f;
        const float CargoDismountSideOffsetFeet = 5f;
        const float CargoMoveSpeedMph = 8f;
        public const float BoatHelmViewPitch = 0f;
        const float BoatDriverLocalYawDegrees = 0f;
        static readonly Vector3[] BoatStopSailingReferenceBuffer = new Vector3[16];

        public enum MountMode
        {
            None,
            Driver,
            Cargo,
            BoatDriver,
            BoatCargo
        }

        DriveableTruck currentTruck;
        DriveableBoat currentBoat;
        ICargoVehicle currentCargo;
        PlayerController controller;
        Rigidbody rb;
        CapsuleCollider bodyCollider;
        Renderer bodyRenderer;
        MountMode mode = MountMode.None;
        float driverHeadYaw;
        Vector3? boatDeckReturnLocal;
        RigidbodyInterpolation storedInterpolation;
        DriveableBoat pendingBoatCollisionHost;
        Transform pendingBoatCargoBed;

        public bool IsMounted => mode != MountMode.None;
        public bool IsDriving => mode == MountMode.Driver || mode == MountMode.BoatDriver;
        public bool IsDrivingBoat => mode == MountMode.BoatDriver;
        public bool IsInCargo => mode == MountMode.Cargo;
        public bool IsInBoatCargo => mode == MountMode.BoatCargo;
        public DriveableTruck CurrentTruck => currentTruck;
        public DriveableBoat CurrentBoat => currentBoat;
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
            currentBoat = null;
            currentCargo = null;
            mode = MountMode.Driver;
            driverHeadYaw = 0f;
            truck.SetDriver(this);

            BeginMountedPhysics(truck.HostTransform, truck.CargoBed, ignoreBedCollisions: true);
            pendingBoatCollisionHost = null;
            pendingBoatCargoBed = null;

            if (bodyCollider != null)
                bodyCollider.enabled = false;
            if (bodyRenderer != null)
                bodyRenderer.enabled = false;

            transform.SetParent(truck.Seat, false);
            AlignDriverCameraToSeat(truck.Seat);

            controller.ResetViewPitch(8f);
            driverHeadYaw = 0f;

            IndustrialSmallTruckVisualFactory.ApplyEquippedSkin(truck.gameObject);
            truck.SetDriverViewHidden(true);
            GameContext.Instance?.Hud?.ShowMessage("W to throttle, S to brake/reverse. [E] to get out front.");
            GameContext.Instance?.Hud?.ShowCenterMessage("Avoid the rocks!");
            return true;
        }

        public bool TryMountBoatDriver(DriveableBoat boat)
        {
            if (boat == null || IsMounted || boat.HasDriver || controller == null || rb == null)
                return false;

            if (GetComponent<PlayerWingsFlight>()?.IsFlying == true)
                return false;

            currentBoat = boat;
            currentTruck = null;
            currentCargo = null;
            mode = MountMode.BoatDriver;
            driverHeadYaw = 0f;
            boat.SetDriver(this);

            BeginMountedPhysics(boat.HostTransform, boat.CargoBed, ignoreBedCollisions: true);
            pendingBoatCollisionHost = null;
            pendingBoatCargoBed = null;

            if (bodyCollider != null)
                bodyCollider.enabled = false;
            if (bodyRenderer != null)
                bodyRenderer.enabled = false;

            transform.SetParent(boat.Helm, false);
            AlignBoatDriverToHelm(boat.Helm);
            SyncMountedRigidbody();

            controller.ResetViewPitch(0f);
            driverHeadYaw = 0f;
            GetComponent<PlayerHands>()?.EnterBoatSailView();
            return true;
        }

        public bool TryMountBoatDriverFromDeck(DriveableBoat boat)
        {
            if (boat == null || !IsInBoatCargo || currentBoat != boat || boat.HasDriver || controller == null || rb == null)
                return false;

            if (GetComponent<PlayerWingsFlight>()?.IsFlying == true)
                return false;

            boatDeckReturnLocal = transform.localPosition;
            currentBoat = boat;
            boat.ClearCargoOccupant(this);
            currentCargo = null;
            currentTruck = null;
            mode = MountMode.BoatDriver;
            driverHeadYaw = 0f;
            boat.SetDriver(this);

            BeginMountedPhysics(boat.HostTransform, boat.CargoBed, ignoreBedCollisions: true);
            pendingBoatCollisionHost = null;
            pendingBoatCargoBed = null;

            if (bodyCollider != null)
                bodyCollider.enabled = false;
            if (bodyRenderer != null)
                bodyRenderer.enabled = false;

            transform.SetParent(boat.Helm, false);
            AlignBoatDriverToHelm(boat.Helm);
            SyncMountedRigidbody();

            controller.ResetViewPitch(0f);
            driverHeadYaw = 0f;
            GetComponent<PlayerHands>()?.EnterBoatSailView();
            return true;
        }

        public bool TryReturnBoatDriverToDeck(DriveableBoat boat)
        {
            if (boat == null || !IsDrivingBoat || currentBoat != boat || controller == null || rb == null)
                return false;

            boat.ClearDriver(this);
            currentCargo = boat;
            currentTruck = null;
            mode = MountMode.BoatCargo;
            driverHeadYaw = 0f;
            boat.SetCargoOccupant(this);

            SetIgnoreHostCollisions(boat.HostTransform, boat.CargoBed, true, ignoreBedColliders: false);

            if (bodyRenderer != null)
                bodyRenderer.enabled = false;

            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            transform.SetParent(boat.CargoBed, false);
            Vector3 deckLocal = boatDeckReturnLocal ?? boat.CargoBed.InverseTransformPoint(boat.Helm.position);
            boatDeckReturnLocal = null;
            deckLocal.y = boat.CargoEntryLocalPosition.y;
            deckLocal = boat.ClampCargoLocalPosition(deckLocal);
            transform.localPosition = deckLocal;
            transform.localRotation = Quaternion.identity;
            Physics.SyncTransforms();
            rb.position = transform.position;
            rb.rotation = transform.rotation;

            if (bodyCollider != null)
                bodyCollider.enabled = false;

            if (controller.Head != null)
                controller.Head.localRotation = Quaternion.identity;

            controller.ResetViewPitch();
            return true;
        }

        public bool TryMountCargo(DriveableTruck truck)
        {
            return TryMountCargoVehicle(truck);
        }

        public bool TryMountBoatCargo(DriveableBoat boat)
        {
            return TryMountCargoVehicle(boat);
        }

        bool TryMountCargoVehicle(ICargoVehicle vehicle)
        {
            if (vehicle == null || IsMounted || vehicle.HasCargoOccupant || controller == null || rb == null)
                return false;

            if (GetComponent<PlayerWingsFlight>()?.IsFlying == true)
                return false;

            currentTruck = vehicle as DriveableTruck;
            currentBoat = vehicle as DriveableBoat;
            currentCargo = vehicle;
            mode = currentBoat != null ? MountMode.BoatCargo : MountMode.Cargo;
            vehicle.SetCargoOccupant(this);

            BeginMountedPhysics(vehicle.HostTransform, vehicle.CargoBed, ignoreBedCollisions: false);
            pendingBoatCollisionHost = null;
            pendingBoatCargoBed = null;

            if (bodyCollider != null)
                bodyCollider.enabled = false;
            if (bodyRenderer != null)
                bodyRenderer.enabled = currentBoat == null;

            transform.SetParent(vehicle.CargoBed, false);
            transform.localPosition = vehicle.ClampCargoLocalPosition(vehicle.CargoEntryLocalPosition);
            transform.localRotation = Quaternion.identity;
            Physics.SyncTransforms();
            rb.position = transform.position;
            rb.rotation = transform.rotation;

            if (controller.Head != null)
                controller.Head.localRotation = Quaternion.identity;

            controller.ResetViewPitch();
            if (currentTruck != null)
                IndustrialSmallTruckVisualFactory.ApplyEquippedSkin(currentTruck.gameObject);

            if (currentTruck != null)
                GameContext.Instance?.Hud?.ShowMessage("Move around the bed. [E] to get out back.");
            return true;
        }

        public bool TryDismount()
        {
            if (!IsMounted)
                return false;

            if (IsDrivingBoat && currentBoat != null)
            {
                if (!currentBoat.IsNearDismountShore)
                {
                    GameContext.Instance?.Hud?.ShowMessage(
                        $"Sail within {LakeCatalog.BoatDismountShoreProximityFeet:0} ft of land to stop.");
                    return false;
                }

                if (!TryResolveBoatLandWorldPosition(currentBoat, out _))
                    return false;

                currentBoat.StopForDismount();
                return CompleteDismount();
            }

            if (IsDriving && currentTruck != null && !currentTruck.CanDismount)
            {
                GameContext.Instance?.Hud?.ShowMessage("Slow down before getting out front.");
                return false;
            }

            if (IsInBoatCargo && currentBoat != null && !currentBoat.IsNearDismountShore)
            {
                GameContext.Instance?.Hud?.ShowMessage(
                    $"Sail within {LakeCatalog.BoatDismountShoreProximityFeet:0} ft of land to get off.");
                return false;
            }

            if (IsInBoatCargo && currentBoat != null)
            {
                if (!TryResolveBoatLandWorldPosition(currentBoat, out _))
                    return false;

                currentBoat.StopForDismount();
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

        void BeginMountedPhysics(Transform host, Transform cargoBed, bool ignoreBedCollisions)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            storedInterpolation = rb.interpolation;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.isKinematic = true;
            rb.detectCollisions = false;
            SetIgnoreHostCollisions(host, cargoBed, true, ignoreBedCollisions);
        }

        void AlignDriverCameraToSeat(Transform seat)
        {
            if (seat == null)
                return;

            Vector3 headLocal = controller != null && controller.Head != null
                ? controller.Head.localPosition
                : new Vector3(0f, 0.65f, 0f);
            transform.localPosition = -headLocal;
            transform.localRotation = Quaternion.identity;
        }

        void AlignBoatDriverToHelm(Transform helm)
        {
            AlignDriverCameraToSeat(helm);
            transform.localRotation = Quaternion.Euler(BoatHelmViewPitch, BoatDriverLocalYawDegrees, 0f);
        }

        void SyncMountedRigidbody()
        {
            Physics.SyncTransforms();
            rb.position = transform.position;
            rb.rotation = transform.rotation;
        }

        void ResetStandingLocalRig()
        {
            if (controller?.Head != null)
            {
                controller.Head.localPosition = new Vector3(0f, 0.65f, 0f);
                controller.Head.localRotation = Quaternion.identity;
            }
        }

        void LateUpdate()
        {
            if (IsDrivingBoat && currentBoat != null)
            {
                AlignBoatDriverToHelm(currentBoat.Helm);
                SyncMountedRigidbody();
                return;
            }

            if (IsDriving && currentTruck != null)
            {
                AlignDriverCameraToSeat(currentTruck.Seat);
                return;
            }

            if (currentCargo == null || (!IsInCargo && !IsInBoatCargo))
                return;

            var stand = currentCargo.CargoEntryLocalPosition;
            var local = transform.localPosition;
            Vector3 clamped = currentCargo.ClampCargoLocalPosition(local);
            if ((clamped - local).sqrMagnitude > 0.000001f || Mathf.Abs(local.y - stand.y) > 0.001f)
                transform.localPosition = new Vector3(clamped.x, stand.y, clamped.z);
        }

        void FixedUpdate()
        {
            if (pendingBoatCollisionHost != null)
            {
                if (!pendingBoatCollisionHost.ContainsWorldPointInHullXZ(transform.position))
                {
                    SetIgnoreHostCollisions(
                        pendingBoatCollisionHost.HostTransform,
                        pendingBoatCargoBed,
                        false,
                        ignoreBedColliders: true);
                    pendingBoatCollisionHost = null;
                    pendingBoatCargoBed = null;
                }
            }

            if (IsDriving || IsDrivingBoat)
                return;

            if (currentCargo == null || controller == null || (!IsInCargo && !IsInBoatCargo))
                return;

            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude < 0.0001f)
                return;

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            Transform bed = currentCargo.CargoBed;
            Vector3 worldMove = transform.TransformDirection(new Vector3(input.x, 0f, input.z));
            Vector3 localMove = bed.InverseTransformDirection(worldMove) * WorldScale.MilesPerHour(CargoMoveSpeedMph) * Time.fixedDeltaTime;
            Vector3 nextLocal = transform.localPosition + localMove;
            nextLocal = currentCargo.ClampCargoLocalPosition(nextLocal);
            nextLocal.y = currentCargo.CargoEntryLocalPosition.y;
            transform.localPosition = nextLocal;
        }

        bool CompleteDismount()
        {
            var truck = currentTruck;
            var boat = currentBoat;
            var cargo = currentCargo;
            bool exitedFront = mode == MountMode.Driver;
            bool exitedHelm = mode == MountMode.BoatDriver;
            bool exitedCargo = mode == MountMode.Cargo || mode == MountMode.BoatCargo;
            bool boatLandDismount = boat != null && (exitedHelm || exitedCargo);
            bool deferBoatDeckColliders = boatLandDismount;
            bool resolvedBoatLand = false;
            Vector3 boatLandWorld = Vector3.zero;

            if (boatLandDismount && boat != null)
            {
                resolvedBoatLand = TryResolveBoatLandWorldPosition(boat, out boatLandWorld);
                if (!resolvedBoatLand)
                    return false;

                var bounds = GameContext.Instance?.CavernBounds;
                boat.TryPushWorldPointClearOfHullXZ(ref boatLandWorld, bounds);
            }

            currentTruck = null;
            currentBoat = null;
            currentCargo = null;
            mode = MountMode.None;
            driverHeadYaw = 0f;
            boatDeckReturnLocal = null;

            transform.SetParent(null, true);
            transform.localScale = Vector3.one;
            ResetStandingLocalRig();

            if (exitedFront && truck != null)
                truck.ClearDriver(this);
            else if (exitedHelm && boat != null)
                boat.ClearDriver(this, enableWalkDeckColliders: !deferBoatDeckColliders);
            else if (exitedCargo && cargo != null)
                cargo.ClearCargoOccupant(this);

            if (exitedFront && truck != null)
                truck.SetDriverViewHidden(false);

            Transform host = truck != null ? truck.HostTransform : boat != null ? boat.HostTransform : cargo?.HostTransform;
            Vector3 dismountWorldPosition;
            Vector3 lookDirection;

            if (exitedFront && truck != null)
            {
                lookDirection = truck.transform.forward;
                dismountWorldPosition = truck.transform.position
                    - truck.transform.right * DismountSideOffset
                    + Vector3.up * 0.5f;
            }
            else if (exitedHelm && boat != null)
            {
                lookDirection = boat.Helm != null ? boat.Helm.forward : boat.BowForwardWorld;
                dismountWorldPosition = transform.position;
            }
            else if (exitedCargo && boat != null)
            {
                lookDirection = boat.BowForwardWorld;
                dismountWorldPosition = transform.position;
            }
            else if (exitedCargo && host != null)
            {
                lookDirection = host.forward;
                dismountWorldPosition = transform.position
                    - host.right * WorldScale.Feet(CargoDismountSideOffsetFeet);
            }
            else
            {
                lookDirection = transform.forward;
                dismountWorldPosition = transform.position;
            }

            controller?.ResetViewPitch();

            if (boatLandDismount && controller != null)
            {
                var bounds = GameContext.Instance?.CavernBounds;
                float probeY = bounds != null
                    ? bounds.transform.position.y + WorldScale.Feet(400f)
                    : transform.position.y + WorldScale.Feet(400f);
                dismountWorldPosition = controller.SnapToFloorWorld(
                    new Vector3(boatLandWorld.x, probeY, boatLandWorld.z));
                controller.ResetPlainsMovementState();
            }
            else if (controller != null)
            {
                dismountWorldPosition = controller.SnapToFloorWorld(dismountWorldPosition);
            }

            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.0001f)
                lookDirection.Normalize();
            else
                lookDirection = Vector3.forward;

            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            transform.SetPositionAndRotation(
                dismountWorldPosition,
                Quaternion.LookRotation(lookDirection, Vector3.up));
            SyncMountedRigidbody();

            if (bodyCollider != null)
                bodyCollider.enabled = true;

            GetComponent<PlainsGroundSupport>()?.SnapSupportedFeetToGroundNow();
            SyncMountedRigidbody();

            GetComponent<LakeTraversalGuard>()?.RefreshValidPosition();

            if (deferBoatDeckColliders && boat != null)
            {
                boat.EnableWalkDeckColliders();
                pendingBoatCollisionHost = boat;
                pendingBoatCargoBed = boat.CargoBed;
            }
            else if (host != null)
            {
                SetIgnoreHostCollisions(host, cargo?.CargoBed, false, ignoreBedColliders: true);
            }

            rb.interpolation = storedInterpolation;
            rb.detectCollisions = true;
            rb.isKinematic = false;

            if (bodyRenderer != null)
                bodyRenderer.enabled = false;

            GetComponent<PlayerHands>()?.ExitBoatSailView();
            return true;
        }

        bool TryResolveBoatLandWorldPosition(DriveableBoat boat, out Vector3 landWorldPosition)
        {
            landWorldPosition = Vector3.zero;
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null || boat == null)
                return false;

            int referenceCount = boat.FillStopSailingReferenceWorldPoints(BoatStopSailingReferenceBuffer);
            if (LakeCatalog.TryResolveBoatStopSailingWorldPosition(
                    bounds,
                    BoatStopSailingReferenceBuffer,
                    referenceCount,
                    out landWorldPosition,
                    assumeNearShore: true))
                return true;

            for (int i = 0; i < referenceCount; i++)
            {
                if (LakeCatalog.TryForceBoatStopSailingWorldPosition(
                        bounds,
                        BoatStopSailingReferenceBuffer[i],
                        out landWorldPosition,
                        assumeNearShore: true))
                    return true;
            }

            if (LakeCatalog.TryResolveIslandBoatStopSailingWorldPosition(
                    bounds,
                    boat.HostTransform.position,
                    out landWorldPosition))
                return true;

            GameContext.Instance?.Hud?.ShowMessage("Could not step onto land.");
            return false;
        }

        void SetIgnoreHostCollisions(Transform host, Transform cargoBed, bool ignore, bool ignoreBedColliders = true)
        {
            if (host == null)
                return;

            var hostColliders = host.GetComponentsInChildren<Collider>(true);
            var playerColliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < hostColliders.Length; i++)
            {
                var hostCollider = hostColliders[i];
                if (hostCollider == null || hostCollider.isTrigger)
                    continue;

                if (!ignoreBedColliders && cargoBed != null && hostCollider.transform.IsChildOf(cargoBed))
                    continue;

                for (int j = 0; j < playerColliders.Length; j++)
                {
                    if (playerColliders[j] == null)
                        continue;

                    Physics.IgnoreCollision(hostCollider, playerColliders[j], ignore);
                }
            }
        }
    }
}
