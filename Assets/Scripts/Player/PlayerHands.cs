using MonsterMiner.Core;
using MonsterMiner.Inventory;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.Player
{
    [DefaultExecutionOrder(200)]
    public class PlayerHands : MonoBehaviour
    {
        const float SwingDuration = 0.45f;
        const float SwingAngle = 55f;
        static readonly Vector3 PickaxeScreenOffset = new Vector3(-25f, 140f, 35f);
        static readonly Vector3 KnifeScreenOffset = new Vector3(-45f, 90f, 35f);
        static readonly Vector3 PistolScreenOffset = new Vector3(-40f, 45f, -40f);
        static readonly Vector3 SpearScreenOffset = new Vector3(20f, 10f, 0f);
        static readonly Vector3 MachineGunScreenOffset = new Vector3(-30f, -95f, 45f);
        static readonly Vector3 ShotgunScreenOffset = new Vector3(-108f, 100f, -8f);
        static readonly Vector3 RifleScreenOffset = new Vector3(-116f, 8f, 48f);
        const float SpearUnderFingers = 0.08f;
        static readonly Vector3 PickaxeSwingPivotPixelOffset = new Vector3(0f, 20f, 0f);
        const float PickaxeScreenVerticalTwistDegrees = 90f;
        const float KnifeScreenVerticalTwistDegrees = 90f;
        static readonly Vector3 FallbackAnchorLocalPosition = new Vector3(0.18f, -0.15f, 0.35f);
        static readonly Vector3 FallbackLeftAnchorLocalPosition = new Vector3(-0.18f, -0.15f, 0.35f);
        static readonly Vector3 HeldItemLocalPosition = new Vector3(0f, -0.03f, 0.06f);
        static readonly Vector3 HeldPebbleLocalPosition = new Vector3(0f, -0.04f, 0.08f);
        static readonly Vector3 LeftHandScreenOffset = new Vector3(0f, 120f, -100f);

        VrHandsVisualFactory hands;
        Transform heldItemAnchor;
        Transform leftHeldItemAnchor;
        Transform heldVisual;
        PlayerController controller;
        float swingTimer;
        bool swinging;
        bool holdingPickaxe;
        bool holdingKnife;
        bool holdingPistol;
        bool holdingSpear;
        bool holdingShotgun;
        bool holdingRifle;
        bool holdingMachineGun;
        bool holdingLeftHandItem;
        bool drivingPoseActive;

        bool HoldingThrustWeapon => holdingKnife || holdingSpear;

        public Transform LeftHandAnchor => hands?.LeftHandAnchor;

        public bool TryGetRightHandScreenPoint(out Vector2 guiPoint)
        {
            guiPoint = default;
            var camera = controller?.ViewCamera;
            if (camera == null)
                return false;

            Transform anchor = hands != null && hands.HasMesh ? hands.RightHandAnchor : null;
            if (anchor == null)
                return false;

            Vector3 screenPoint = camera.WorldToScreenPoint(anchor.position);
            if (screenPoint.z <= 0f)
                return false;

            guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            return true;
        }

        public void Initialize(PlayerController playerController)
        {
            controller = playerController;
            var cam = controller.ViewCamera.transform;
            hands = new VrHandsVisualFactory(cam);
            heldItemAnchor = hands.HasMesh ? hands.RightHandAnchor : CreateFallbackAnchor(cam, FallbackAnchorLocalPosition);

            var ctx = GameContext.Instance;
            if (ctx?.Inventory != null)
            {
                ctx.Inventory.OnInventoryChanged += RefreshHeldItem;
                ctx.Inventory.OnSelectedChanged += OnSelectedChanged;
                ctx.Inventory.OnGlovesChanged += RefreshGloves;
                RefreshGloves();
                RefreshHeldItem();
            }
        }

        void OnDestroy()
        {
            var ctx = GameContext.Instance;
            if (ctx?.Inventory == null)
                return;

            ctx.Inventory.OnInventoryChanged -= RefreshHeldItem;
            ctx.Inventory.OnSelectedChanged -= OnSelectedChanged;
            ctx.Inventory.OnGlovesChanged -= RefreshGloves;
        }

        void OnSelectedChanged(int _) => RefreshHeldItem();

        void LateUpdate()
        {
            if (IsDrivingCab())
            {
                ApplyDrivingPose();
                return;
            }

            if (drivingPoseActive)
            {
                EndDrivingPose();
                return;
            }

            if (heldVisual == null || hands == null || !hands.HasMesh || controller?.ViewCamera == null)
                return;

            if (holdingLeftHandItem)
            {
                ApplyHeldLeftHandItemScreenOffset();
                return;
            }

            if (HoldingThrustWeapon)
            {
                if (swinging)
                    hands.ApplyKnifeStab(controller.ViewCamera, GetKnifeStabCurve());

                if (holdingSpear)
                    ApplyHeldSpearOffset();
                else
                    ApplyHeldKnifeOffset();
                return;
            }

            if (holdingMachineGun)
            {
                ApplyHeldMachineGunOffset();
                return;
            }

            if (holdingShotgun)
            {
                ApplyHeldShotgunOffset();
                return;
            }

            if (holdingRifle)
            {
                ApplyHeldRifleOffset();
                return;
            }

            if (holdingPistol)
            {
                ApplyHeldPistolOffset();
                return;
            }

            if (!holdingPickaxe)
                return;

            ApplyHeldPickaxeOffset();
        }

        void ApplyHeldKnifeOffset()
        {
            ApplyHeldThrustWeaponOffset(
                KnifeVisualFactory.HeldMeshLocalPosition,
                KnifeVisualFactory.HeldMeshLocalRotation,
                KnifeScreenOffset);
        }

        void ApplyHeldPistolOffset()
        {
            ApplyHeldThrustWeaponOffset(
                PistolVisualFactory.HeldMeshLocalPosition,
                PistolVisualFactory.HeldMeshLocalRotation,
                PistolScreenOffset);
        }

        void ApplyHeldSpearOffset()
        {
            var camera = controller.ViewCamera;
            Transform grip = hands.SpearGripAnchor;
            Quaternion restRotation = Quaternion.AngleAxis(
                KnifeScreenVerticalTwistDegrees,
                camera.transform.up) * grip.rotation * SpearVisualFactory.HeldMeshLocalRotation;

            Vector3 pinch = grip.position;
            Vector3 screen = camera.WorldToScreenPoint(pinch);
            if (screen.z > 0.001f)
            {
                screen.x += SpearScreenOffset.x;
                screen.y += SpearScreenOffset.y;
                pinch = camera.ScreenToWorldPoint(screen);
            }

            pinch += camera.transform.forward * SpearUnderFingers;
            Vector3 position = pinch - restRotation * SpearVisualFactory.ScaledMeshCenter;
            heldVisual.SetPositionAndRotation(position, restRotation);
        }

        void ApplyHeldThrustWeaponOffset(Vector3 meshLocal, Quaternion meshLocalRot, Vector3 screenOffset)
        {
            var camera = controller.ViewCamera;
            Transform grip = hands.KnifeGripAnchor;

            Vector3 gripPointWorld = grip.TransformPoint(meshLocal);
            Quaternion gripPointRotation = grip.rotation * meshLocalRot;
            Vector3 worldOffset = hands.GetScreenWorldOffset(camera, gripPointWorld, screenOffset);
            Quaternion screenVerticalTwist = Quaternion.AngleAxis(
                KnifeScreenVerticalTwistDegrees,
                camera.transform.up);
            Quaternion restRotation = screenVerticalTwist * gripPointRotation;
            heldVisual.SetPositionAndRotation(gripPointWorld + worldOffset, restRotation);
        }

        void ApplyHeldPickaxeOffset()
        {
            ApplyHeldGripWeaponOffset(
                hands.PickaxeGripAnchor,
                PickaxeVisualFactory.HeldMeshLocalPosition,
                PickaxeVisualFactory.HeldMeshLocalRotation,
                PickaxeVisualFactory.HeldSwingPivotLocal,
                PickaxeScreenOffset);
        }

        void ApplyHeldMachineGunOffset()
        {
            ApplyHeldGripWeaponOffset(
                hands.PickaxeGripAnchor,
                MachineGunVisualFactory.HeldMeshLocalPosition,
                MachineGunVisualFactory.HeldMeshLocalRotation,
                Vector3.zero,
                MachineGunScreenOffset);
        }

        void ApplyHeldShotgunOffset()
        {
            ApplyHeldGripWeaponOffset(
                hands.PickaxeGripAnchor,
                ShotgunVisualFactory.HeldMeshLocalPosition,
                ShotgunVisualFactory.HeldMeshLocalRotation,
                Vector3.zero,
                ShotgunScreenOffset);
        }

        void ApplyHeldRifleOffset()
        {
            ApplyHeldGripWeaponOffset(
                hands.PickaxeGripAnchor,
                RifleVisualFactory.HeldMeshLocalPosition,
                RifleVisualFactory.HeldMeshLocalRotation,
                Vector3.zero,
                RifleScreenOffset);
        }

        void ApplyHeldGripWeaponOffset(
            Transform grip,
            Vector3 meshLocal,
            Quaternion meshLocalRot,
            Vector3 swingPivotLocal,
            Vector3 screenOffset)
        {
            var camera = controller.ViewCamera;

            Vector3 gripPointWorld = grip.TransformPoint(meshLocal);
            Quaternion gripPointRotation = grip.rotation * meshLocalRot;
            float swing = GetPickaxeSwingCurve();
            Vector3 offsetPixels = screenOffset + new Vector3(0f, -swing * 22f, -swing * 28f);
            Vector3 worldOffset = hands.GetScreenWorldOffset(camera, gripPointWorld, offsetPixels);
            Quaternion screenVerticalTwist = Quaternion.AngleAxis(
                PickaxeScreenVerticalTwistDegrees,
                camera.transform.up);
            Vector3 chopAxis = (camera.transform.right - 0.3f * camera.transform.up).normalized;
            Quaternion chopRotation = Quaternion.AngleAxis(swing * 58f, chopAxis);
            Quaternion restRotation = screenVerticalTwist * gripPointRotation;
            Vector3 restPosition = gripPointWorld + worldOffset;
            Vector3 scaledPivot = Vector3.Scale(swingPivotLocal, heldVisual.lossyScale);
            Vector3 basePivotWorld = restPosition + restRotation * scaledPivot;
            Vector3 pivotWorld = basePivotWorld + hands.GetScreenWorldOffset(
                camera,
                basePivotWorld,
                PickaxeSwingPivotPixelOffset);
            Vector3 finalPosition = pivotWorld + chopRotation * (restPosition - pivotWorld);
            Quaternion finalRotation = chopRotation * restRotation;

            heldVisual.SetPositionAndRotation(finalPosition, finalRotation);
        }

        void ApplyHeldLeftHandItemScreenOffset()
        {
            var anchor = GetLeftHandAnchor();
            if (anchor == null)
                return;

            var camera = controller.ViewCamera;
            Vector3 baseWorld = anchor.TransformPoint(HeldPebbleLocalPosition);
            Vector3 worldOffset = hands.GetScreenWorldOffset(camera, baseWorld, LeftHandScreenOffset);
            heldVisual.SetPositionAndRotation(baseWorld + worldOffset, anchor.rotation);
        }

        float GetPickaxeSwingCurve()
        {
            if (!swinging || !holdingPickaxe)
                return 0f;

            return Mathf.Sin(Mathf.Clamp01(swingTimer / SwingDuration) * Mathf.PI);
        }

        float GetKnifeStabCurve()
        {
            if (!swinging || !HoldingThrustWeapon)
                return 0f;

            return Mathf.Sin(Mathf.Clamp01(swingTimer / SwingDuration) * Mathf.PI);
        }

        void Update()
        {
            if (!swinging)
                return;

            swingTimer += Time.deltaTime;
            float t = Mathf.Clamp01(swingTimer / SwingDuration);

            if (hands != null && hands.HasMesh)
            {
                if (holdingPickaxe)
                    hands.ApplyPickaxeForwardSwing(controller.ViewCamera, t);
                else if (!HoldingThrustWeapon)
                {
                    float angle = Mathf.Sin(t * Mathf.PI) * SwingAngle;
                    hands.ApplySwingRotation(angle);
                }
            }
            else if (heldItemAnchor != null)
            {
                float angle = Mathf.Sin(t * Mathf.PI) * SwingAngle;
                heldItemAnchor.localRotation = Quaternion.Euler(angle, 0f, 0f);
            }

            if (t >= 1f)
            {
                swinging = false;
                if (hands != null && hands.HasMesh)
                {
                    if (holdingPickaxe)
                        hands.ResetPickaxeSwing();
                    else if (HoldingThrustWeapon)
                        hands.EndKnifeStab();
                }
                else if (heldItemAnchor != null)
                {
                    heldItemAnchor.localRotation = Quaternion.identity;
                }
            }
        }

        public void TriggerSwing()
        {
            if (IsDrivingCab())
                return;

            swinging = true;
            swingTimer = 0f;
            if (HoldingThrustWeapon)
                hands?.BeginKnifeStab();
        }

        void RefreshGloves()
        {
            if (hands == null || !hands.HasMesh)
                return;

            var gloves = GameContext.Instance?.Inventory?.EquippedGloves;
            if (gloves != null)
                hands.SetGloveColor(gloves.worldColor);
        }

        bool IsDrivingCab()
        {
            var mount = controller != null
                ? controller.GetComponent<PlayerVehicleMount>()
                : GetComponent<PlayerVehicleMount>();
            return mount != null && mount.IsDriving;
        }

        void ApplyDrivingPose()
        {
            HideHeldItemForDriving();
            hands?.SetVisible(false);
            drivingPoseActive = true;
        }

        void EndDrivingPose()
        {
            hands?.SetVisible(true);
            drivingPoseActive = false;
            RefreshHeldItem();
        }

        void HideHeldItemForDriving()
        {
            if (heldVisual != null)
            {
                Destroy(heldVisual.gameObject);
                heldVisual = null;
            }

            if (swinging && hands != null && hands.HasMesh && HoldingThrustWeapon)
            {
                swinging = false;
                hands.EndKnifeStab();
            }

            swinging = false;
            holdingPickaxe = false;
            holdingKnife = false;
            holdingPistol = false;
            holdingSpear = false;
            holdingShotgun = false;
            holdingRifle = false;
            holdingMachineGun = false;
            holdingLeftHandItem = false;
        }

        void RefreshHeldItem()
        {
            if (heldVisual != null)
                Destroy(heldVisual.gameObject);

            if (swinging && hands != null && hands.HasMesh && HoldingThrustWeapon)
            {
                swinging = false;
                hands.EndKnifeStab();
            }

            holdingPickaxe = false;
            holdingKnife = false;
            holdingPistol = false;
            holdingSpear = false;
            holdingShotgun = false;
            holdingRifle = false;
            holdingMachineGun = false;
            holdingLeftHandItem = false;
            heldItemAnchor = hands != null && hands.HasMesh ? hands.RightHandAnchor : heldItemAnchor;

            if (IsDrivingCab())
                return;

            var ctx = GameContext.Instance;
            if (ctx?.Inventory == null || controller?.ViewCamera == null || heldItemAnchor == null)
                return;

            var slot = ctx.Inventory.GetSelectedSlot();
            if (slot == null || slot.IsEmpty)
                return;

            if (InventorySystem.IsBossDrop(slot.item))
                return;

            if (slot.item.itemId == "shiny_pebble")
            {
                heldVisual = PebbleVisualFactory.CreateHeldPebble(GetLeftHandAnchor(), HeldPebbleLocalPosition).transform;
                holdingLeftHandItem = true;
                return;
            }

            if (slot.item.isMonsterDrop)
            {
                heldVisual = MeatVisualFactory.CreateHeldMonsterDrop(
                    slot.item,
                    GetLeftHandAnchor(),
                    HeldPebbleLocalPosition).transform;
                holdingLeftHandItem = true;
                return;
            }

            if (slot.item.itemId == "pickaxe")
            {
                if (hands != null && hands.HasMesh)
                {
                    heldItemAnchor = hands.PickaxeGripAnchor;
                }

                var pickaxe = PickaxeVisualFactory.CreateHeldPickaxe(
                    heldItemAnchor,
                    ctx.PlayerCombat != null ? ctx.PlayerCombat.PickaxeMiningTier : 0);
                if (pickaxe != null)
                {
                    heldVisual = pickaxe.transform;
                    heldVisual.SetParent(controller.ViewCamera.transform, true);
                    holdingPickaxe = true;
                    return;
                }
            }

            if (InventorySystem.IsKnifeItem(slot.item))
            {
                if (hands != null && hands.HasMesh)
                    heldItemAnchor = hands.KnifeGripAnchor;

                var knife = KnifeVisualFactory.CreateHeldKnife(heldItemAnchor, slot.item);
                if (knife != null)
                {
                    heldVisual = knife.transform;
                    heldVisual.SetParent(controller.ViewCamera.transform, true);
                    holdingKnife = true;
                    return;
                }
            }

            if (InventorySystem.IsSpearItem(slot.item))
            {
                if (hands != null && hands.HasMesh)
                    heldItemAnchor = hands.SpearGripAnchor;

                var spear = SpearVisualFactory.CreateHeldSpear(heldItemAnchor, slot.item);
                if (spear != null)
                {
                    heldVisual = spear.transform;
                    heldVisual.SetParent(controller.ViewCamera.transform, true);
                    holdingSpear = true;
                    return;
                }
            }

            if (InventorySystem.IsGrenadeItem(slot.item))
            {
                var grenade = GrenadeVisualFactory.CreateHeldGrenade(heldItemAnchor, slot.item);
                if (grenade != null)
                {
                    heldVisual = grenade.transform;
                    heldVisual.SetParent(heldItemAnchor, false);
                    return;
                }
            }

            if (InventorySystem.IsPistolItem(slot.item))
            {
                if (hands != null && hands.HasMesh)
                    heldItemAnchor = hands.KnifeGripAnchor;

                var pistol = PistolVisualFactory.CreateHeldPistol(heldItemAnchor, slot.item);
                if (pistol != null)
                {
                    heldVisual = pistol.transform;
                    heldVisual.SetParent(controller.ViewCamera.transform, true);
                    holdingPistol = true;
                    return;
                }
            }

            if (InventorySystem.IsShotgunItem(slot.item))
            {
                if (hands != null && hands.HasMesh)
                    heldItemAnchor = hands.PickaxeGripAnchor;

                var shotgun = ShotgunVisualFactory.CreateHeldShotgun(heldItemAnchor, slot.item);
                if (shotgun != null)
                {
                    heldVisual = shotgun.transform;
                    heldVisual.SetParent(controller.ViewCamera.transform, true);
                    holdingShotgun = true;
                    return;
                }
            }

            if (InventorySystem.IsRifleItem(slot.item))
            {
                if (hands != null && hands.HasMesh)
                    heldItemAnchor = hands.PickaxeGripAnchor;

                var rifle = RifleVisualFactory.CreateHeldRifle(heldItemAnchor, slot.item);
                if (rifle != null)
                {
                    heldVisual = rifle.transform;
                    heldVisual.SetParent(controller.ViewCamera.transform, true);
                    holdingRifle = true;
                    return;
                }
            }

            if (InventorySystem.IsMachineGunItem(slot.item))
            {
                if (hands != null && hands.HasMesh)
                    heldItemAnchor = hands.PickaxeGripAnchor;

                var machineGun = MachineGunVisualFactory.CreateHeldMachineGun(heldItemAnchor, slot.item);
                if (machineGun != null)
                {
                    heldVisual = machineGun.transform;
                    heldVisual.SetParent(controller.ViewCamera.transform, true);
                    holdingMachineGun = true;
                    return;
                }
            }

            heldVisual = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                heldItemAnchor.position,
                Vector3.one * 0.12f,
                slot.item.worldColor,
                slot.item.displayName).transform;
            heldVisual.SetParent(heldItemAnchor, false);
            heldVisual.localPosition = HeldItemLocalPosition;
            heldVisual.localRotation = Quaternion.identity;
            Destroy(heldVisual.GetComponent<Collider>());
        }

        Transform GetLeftHandAnchor()
        {
            if (hands != null && hands.HasMesh)
                return hands.LeftHandAnchor;

            if (leftHeldItemAnchor == null && controller?.ViewCamera != null)
            {
                leftHeldItemAnchor = CreateFallbackAnchor(
                    controller.ViewCamera.transform,
                    FallbackLeftAnchorLocalPosition,
                    "LeftHeldItemAnchor");
            }

            return leftHeldItemAnchor != null ? leftHeldItemAnchor : heldItemAnchor;
        }

        static Transform CreateFallbackAnchor(Transform cameraTransform, Vector3 localPosition, string name = "HeldItemAnchor")
        {
            var anchorGo = new GameObject(name);
            var anchor = anchorGo.transform;
            anchor.SetParent(cameraTransform, false);
            anchor.localPosition = localPosition;
            anchor.localRotation = Quaternion.identity;
            return anchor;
        }
    }
}
