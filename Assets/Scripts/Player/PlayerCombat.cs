using MonsterMiner.Combat;
using MonsterMiner.Core;
using MonsterMiner.Inventory;
using MonsterMiner.UI;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class PlayerCombat : MonoBehaviour
    {
        const float PickaxeDamage = 1f;
        const float BaseEggHealth = MonsterEgg.BasePickaxeHits;
        const float MeleeAttackRange = 2.4f;
        const float RangedAimRadius = 0.05f;

        int pickaxeMiningTier;
        float attackCooldown = 0.45f;
        float nextAttackTime;
        float nextRangedAttackTime;

        PlayerController controller;
        PlayerHands hands;
        RangedWeaponAmmo rangedAmmo;

        public int PickaxeMiningTier => pickaxeMiningTier;

        public void Initialize(PlayerController playerController, PlayerHands playerHands)
        {
            controller = playerController;
            hands = playerHands;
            rangedAmmo = GameContext.Instance?.PlayerRangedAmmo ?? GetComponent<RangedWeaponAmmo>();
        }

        public void UpgradePickaxe()
        {
            pickaxeMiningTier = Mathf.Min(pickaxeMiningTier + 1, Util.PickaxeVisualFactory.MaxMiningTier);
        }

        public int GetPickaxeMiningBonus() => pickaxeMiningTier;

        void Update()
        {
            if (PlayerController.IsGameplayBlocked())
                return;

            if (GetComponent<PlayerVehicleMount>()?.IsDriving == true)
                return;

            if (GameContext.Instance?.Shop?.IsMenuOpen == true)
                return;

            if (SellConfirmationDisplay.IsActive || WorldMapDisplay.IsActive)
                return;

            if (GrenadeThrowController.IsGrenadeEquipped)
                return;

            TryReloadRangedWeapon();

            if (TryPerformRangedAttack())
                return;

            bool weaponHold = IsMeleeWeaponSelected() && Input.GetMouseButton(0);
            bool pickaxeHold = IsPickaxeSelected() && Input.GetMouseButton(0);

            if (!weaponHold && !pickaxeHold)
                return;

            if (Time.time < nextAttackTime)
                return;

            nextAttackTime = Time.time + attackCooldown;

            if (weaponHold || pickaxeHold)
                hands?.TriggerSwing();

            PerformMeleeAttack();
        }

        void TryReloadRangedWeapon()
        {
            if (!Input.GetKeyDown(KeyCode.R))
                return;

            string weaponId = GetSelectedRangedWeaponId();
            if (string.IsNullOrEmpty(weaponId) || rangedAmmo == null)
                return;

            rangedAmmo.Reload(weaponId);
        }

        bool TryPerformRangedAttack()
        {
            string weaponId = GetSelectedRangedWeaponId();
            if (string.IsNullOrEmpty(weaponId) || !RangedWeaponStats.TryGetConfig(weaponId, out var config))
                return false;

            bool isMachineGun = InventorySystem.ResolveBaseWeaponId(weaponId) == "machinegun";
            bool wantsFire = isMachineGun
                ? Input.GetMouseButton(0)
                : Input.GetMouseButtonDown(0);

            if (!wantsFire)
                return false;

            if (Time.time < nextRangedAttackTime)
                return true;

            if (rangedAmmo == null || !rangedAmmo.TryConsume(weaponId, config.RoundsPerTrigger, out int consumed))
                return true;

            nextRangedAttackTime = Time.time + RangedWeaponStats.FireIntervalSeconds;
            PerformRangedAttack(weaponId, config, consumed);
            return true;
        }

        void PerformRangedAttack(string weaponId, RangedWeaponConfig config, int shotsFired)
        {
            if (controller?.ViewCamera == null)
                return;

            float damage = config.DamagePerShot;
            var selected = GameContext.Instance?.Inventory?.GetSelectedSlot();
            if (selected != null && !selected.IsEmpty && selected.item != null && selected.item.isLegendary)
                damage += 1f;

            if (config.HitsAllInView)
            {
                PerformShotgunAttack(damage);
                ApplyWeaponRecoil(config.RecoilDegrees);
                return;
            }

            for (int i = 0; i < shotsFired; i++)
            {
                PerformSingleShotAttack(damage);
                ApplyWeaponRecoil(config.RecoilDegrees);
            }
        }

        void ApplyWeaponRecoil(float degrees)
        {
            controller?.ApplyViewRecoil(degrees);
            hands?.ApplyWeaponKick(degrees * 0.35f);
        }

        void PerformSingleShotAttack(float damage)
        {
            var camera = controller.ViewCamera;
            var origin = camera.transform.position;
            var direction = camera.transform.forward;
            float maxDistance = Mathf.Max(camera.farClipPlane, 1000f);

            if (!Physics.SphereCast(
                    origin,
                    RangedAimRadius,
                    direction,
                    out var hit,
                    maxDistance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
                return;

            var monster = hit.collider.GetComponentInParent<Monster>();
            if (monster != null)
                monster.TakeDamage(damage, hit.point, direction);
        }

        void PerformShotgunAttack(float damage)
        {
            var camera = controller.ViewCamera;
            var origin = camera.transform.position;
            var forward = camera.transform.forward;
            float maxDistance = Mathf.Max(camera.farClipPlane, 1000f);
            float maxAngle = camera.fieldOfView * 0.5f + 2f;

            foreach (var monster in FindObjectsByType<Monster>(FindObjectsSortMode.None))
            {
                if (monster == null)
                    continue;

                Vector3 toMonster = monster.transform.position - origin;
                float distance = toMonster.magnitude;
                if (distance <= 0.01f || distance > maxDistance)
                    continue;

                Vector3 direction = toMonster / distance;
                if (Vector3.Dot(forward, direction) <= 0f)
                    continue;

                if (Vector3.Angle(forward, direction) > maxAngle)
                    continue;

                monster.TakeDamage(damage, monster.transform.position, direction);
            }
        }

        void PerformMeleeAttack()
        {
            var cam = controller.ViewCamera.transform;
            var origin = cam.position;
            var direction = cam.forward;

            if (!Physics.SphereCast(origin, 0.25f, direction, out var hit, MeleeAttackRange))
                return;

            var egg = hit.collider.GetComponentInParent<MonsterEgg>();
            if (egg != null)
            {
                if (IsPickaxeSelected())
                    egg.TakeDamage(GetPickaxeEggDamage(), fromPickaxe: true);
                return;
            }

            var monster = hit.collider.GetComponentInParent<Monster>();
            if (monster != null)
            {
                if (IsPickaxeSelected())
                    monster.TakeDamage(GetPickaxeDamage(), hit.point, direction);
                else if (IsMeleeWeaponSelected())
                    monster.TakeDamage(GetSelectedWeaponDamage(), hit.point, direction);
            }
        }

        bool IsPickaxeSelected()
        {
            var ctx = GameContext.Instance;
            var selected = ctx?.Inventory?.GetSelectedSlot();
            return selected != null && !selected.IsEmpty && selected.item.itemId == "pickaxe";
        }

        bool IsMeleeWeaponSelected()
        {
            var selected = GameContext.Instance?.Inventory?.GetSelectedSlot();
            return InventorySystem.IsWeaponItem(selected?.item)
                && !InventorySystem.IsRangedWeaponItem(selected.item);
        }

        string GetSelectedRangedWeaponId()
        {
            var selected = GameContext.Instance?.Inventory?.GetSelectedSlot();
            return InventorySystem.IsRangedWeaponItem(selected?.item) ? selected.item.itemId : null;
        }

        float GetPickaxeDamage() => PickaxeDamage;

        float GetPickaxeEggDamage()
        {
            int hitsNeeded = GameContext.Instance?.Inventory?.GetEggHitsNeeded() ?? MonsterEgg.BasePickaxeHits;
            return BaseEggHealth / hitsNeeded;
        }

        float GetSelectedWeaponDamage()
        {
            var selected = GameContext.Instance?.Inventory?.GetSelectedSlot();
            if (selected == null || selected.IsEmpty || !InventorySystem.IsWeaponItem(selected.item))
                return 1f;

            return Mathf.Max(1f, InventorySystem.GetWeaponDamage(selected.item));
        }
    }
}
