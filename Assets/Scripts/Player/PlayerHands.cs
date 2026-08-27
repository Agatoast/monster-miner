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
        static readonly Vector3 PickaxeSwingPivotPixelOffset = new Vector3(0f, 20f, 0f);
        const float PickaxeScreenVerticalTwistDegrees = 90f;
        const float KnifeScreenVerticalTwistDegrees = 90f;
        static readonly Vector3 FallbackAnchorLocalPosition = new Vector3(0.18f, -0.15f, 0.35f);
        static readonly Vector3 FallbackLeftAnchorLocalPosition = new Vector3(-0.18f, -0.15f, 0.35f);
        static readonly Vector3 HeldItemLocalPosition = new Vector3(0f, -0.03f, 0.06f);
        static readonly Vector3 HeldPebbleLocalPosition = new Vector3(0f, -0.04f, 0.08f);
        static readonly Vector3 PebbleScreenOffset = new Vector3(0f, 120f, -100f);

        VrHandsVisualFactory hands;
        Transform heldItemAnchor;
        Transform leftHeldItemAnchor;
        Transform heldVisual;
        PlayerController controller;
        float swingTimer;
        bool swinging;
        bool holdingPickaxe;
        bool holdingKnife;
        bool holdingPebble;

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
            if (heldVisual == null || hands == null || !hands.HasMesh || controller?.ViewCamera == null)
                return;

            if (holdingPebble)
            {
                ApplyHeldPebbleScreenOffset();
                return;
            }

            if (holdingKnife)
            {
                if (swinging)
                    hands.ApplyKnifeStab(controller.ViewCamera, GetKnifeStabCurve());

                ApplyHeldKnifeOffset();
                return;
            }

            if (!holdingPickaxe)
                return;

            ApplyHeldPickaxeOffset();
        }

        void ApplyHeldKnifeOffset()
        {
            var camera = controller.ViewCamera;
            Transform grip = hands.KnifeGripAnchor;
            Vector3 meshLocal = KnifeVisualFactory.HeldMeshLocalPosition;
            Quaternion meshLocalRot = KnifeVisualFactory.HeldMeshLocalRotation;

            Vector3 gripPointWorld = grip.TransformPoint(meshLocal);
            Quaternion gripPointRotation = grip.rotation * meshLocalRot;
            Vector3 worldOffset = hands.GetScreenWorldOffset(camera, gripPointWorld, KnifeScreenOffset);
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

        void ApplyHeldPebbleScreenOffset()
        {
            var anchor = GetLeftHandAnchor();
            if (anchor == null)
                return;

            var camera = controller.ViewCamera;
            Vector3 baseWorld = anchor.TransformPoint(HeldPebbleLocalPosition);
            Vector3 worldOffset = hands.GetScreenWorldOffset(camera, baseWorld, PebbleScreenOffset);
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
            if (!swinging || !holdingKnife)
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
                else if (!holdingKnife)
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
                    else if (holdingKnife)
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
            swinging = true;
            swingTimer = 0f;
            if (holdingKnife)
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

        void RefreshHeldItem()
        {
            if (heldVisual != null)
                Destroy(heldVisual.gameObject);

            if (swinging && hands != null && hands.HasMesh && holdingKnife)
            {
                swinging = false;
                hands.EndKnifeStab();
            }

            holdingPickaxe = false;
            holdingKnife = false;
            holdingPebble = false;
            heldItemAnchor = hands != null && hands.HasMesh ? hands.RightHandAnchor : heldItemAnchor;

            var ctx = GameContext.Instance;
            if (ctx?.Inventory == null || controller?.ViewCamera == null || heldItemAnchor == null)
                return;

            var slot = ctx.Inventory.GetSelectedSlot();
            if (slot == null || slot.IsEmpty)
                return;

            if (slot.item.itemId == "shiny_pebble")
            {
                heldVisual = PebbleVisualFactory.CreateHeldPebble(GetLeftHandAnchor(), HeldPebbleLocalPosition).transform;
                holdingPebble = true;
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
