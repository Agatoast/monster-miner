using MonsterMiner.Combat;
using MonsterMiner.Core;
using MonsterMiner.Inventory;
using MonsterMiner.UI;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    [DefaultExecutionOrder(-50)]
    public class GrenadeThrowController : MonoBehaviour
    {
        const float MouseDistanceSensitivityFeet = 5f;
        const float ThrowCooldownSeconds = 0.75f;
        const float DefaultThrowDistanceFeet = 30f;

        static GrenadeThrowController instance;

        PlayerController controller;
        InventorySystem inventory;
        GrenadeAimIndicator aimIndicator;
        float throwDistanceFeet = DefaultThrowDistanceFeet;
        float nextThrowTime;

        public static bool IsGrenadeEquipped => instance != null && instance.HasGrenadeEquipped();
        public static bool UsesMouseYForRange => IsGrenadeEquipped;

        void Awake()
        {
            instance = this;
            controller = GetComponent<PlayerController>();

            var indicatorGo = new GameObject("GrenadeAimIndicator");
            indicatorGo.transform.SetParent(transform, false);
            aimIndicator = indicatorGo.AddComponent<GrenadeAimIndicator>();
        }

        void OnDestroy()
        {
            if (inventory != null)
                inventory.OnSelectedChanged -= OnSelectedChanged;

            if (instance == this)
                instance = null;
        }

        void Start()
        {
            inventory = GameContext.Instance?.Inventory;
            if (inventory != null)
                inventory.OnSelectedChanged += OnSelectedChanged;
        }

        void OnSelectedChanged(int _)
        {
            if (HasGrenadeEquipped())
                throwDistanceFeet = DefaultThrowDistanceFeet;
        }

        void LateUpdate()
        {
            if (PlayerController.IsGameplayBlocked() || GameContext.Instance?.Shop?.IsMenuOpen == true || SellConfirmationDisplay.IsActive)
            {
                aimIndicator.SetVisible(false);
                return;
            }

            if (!HasGrenadeEquipped())
            {
                aimIndicator.SetVisible(false);
                return;
            }

            AdjustThrowDistance();
            aimIndicator.SetVisible(true);
            aimIndicator.UpdateAim(GetAimGroundPoint(GetThrowDistance()));

            if (Input.GetMouseButtonDown(0) && Time.time >= nextThrowTime)
                TryThrow();
        }

        void AdjustThrowDistance()
        {
            float mouseY = Input.GetAxisRaw("Mouse Y");
            if (Mathf.Abs(mouseY) < 0.001f)
                return;

            throwDistanceFeet += mouseY * MouseDistanceSensitivityFeet;
            throwDistanceFeet = Mathf.Clamp(
                throwDistanceFeet,
                WorldScale.GrenadeMinThrowFeet,
                WorldScale.GrenadeMaxThrowFeet);
        }

        float GetThrowDistance() => WorldScale.Feet(throwDistanceFeet);

        void TryThrow()
        {
            var ctx = GameContext.Instance;
            var slot = ctx?.Inventory?.GetSelectedSlot();
            if (slot == null || slot.IsEmpty || !InventorySystem.IsGrenadeItem(slot.item))
                return;

            Vector3 target = GetAimGroundPoint(GetThrowDistance());
            Vector3 start = controller != null && controller.ViewCamera != null
                ? controller.ViewCamera.transform.position + controller.ViewCamera.transform.forward * 0.35f
                : transform.position + Vector3.up * 1.2f;

            float damage = Mathf.Max(1f, slot.item.weaponDamage);
            if (!ctx.Inventory.TryRemoveFromSelected(1))
                return;

            GrenadeProjectile.Launch(start, target, WorldScale.GrenadeBlastRadius, damage);
            nextThrowTime = Time.time + ThrowCooldownSeconds;
        }

        bool HasGrenadeEquipped()
        {
            var slot = GameContext.Instance?.Inventory?.GetSelectedSlot();
            return slot != null && !slot.IsEmpty && InventorySystem.IsGrenadeItem(slot.item);
        }

        Vector3 GetAimGroundPoint(float distance)
        {
            Vector3 origin = GetThrowOrigin();
            Vector3 probe = origin + GetFlatForward() * distance;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds != null)
            {
                var local = bounds.transform.InverseTransformPoint(probe);
                if (bounds.TryResolveFloorWorldPoint(local.x, local.z, out var floorPoint))
                    return floorPoint + Vector3.up * 0.03f;
            }

            var rayStart = probe + Vector3.up * 24f;
            if (FloorAnchor.TryResolveFloorPoint(rayStart, 0f, 48f, out var hit))
                return hit + Vector3.up * 0.03f;

            return probe;
        }

        Vector3 GetThrowOrigin()
        {
            Vector3 origin = transform.position;
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return origin;

            var local = bounds.transform.InverseTransformPoint(origin);
            origin.y = bounds.SampleFloorWorldY(local.x, local.z);
            return origin;
        }

        Vector3 GetFlatForward()
        {
            if (controller?.ViewCamera == null)
            {
                Vector3 forward = transform.forward;
                forward.y = 0f;
                return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            }

            Vector3 cameraForward = controller.ViewCamera.transform.forward;
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude < 0.001f)
                cameraForward = transform.forward;

            return cameraForward.normalized;
        }
    }
}
