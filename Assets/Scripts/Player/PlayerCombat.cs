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

        int pickaxeMiningTier;
        float attackRange = 2.4f;
        float attackCooldown = 0.45f;
        float nextAttackTime;

        PlayerController controller;
        PlayerHands hands;

        public int PickaxeMiningTier => pickaxeMiningTier;

        public void Initialize(PlayerController playerController, PlayerHands playerHands)
        {
            controller = playerController;
            hands = playerHands;
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

            if (GameContext.Instance?.Shop?.IsMenuOpen == true)
                return;

            if (SellConfirmationDisplay.IsActive)
                return;

            bool weaponHold = IsWeaponSelected() && Input.GetMouseButton(0);
            bool pickaxeHold = IsPickaxeSelected() && Input.GetMouseButton(0);

            if (!weaponHold && !pickaxeHold)
                return;

            if (Time.time < nextAttackTime)
                return;

            nextAttackTime = Time.time + attackCooldown;

            if (weaponHold || pickaxeHold)
                hands?.TriggerSwing();

            PerformAttack();
        }

        void PerformAttack()
        {
            if (controller?.ViewCamera == null)
                return;

            var cam = controller.ViewCamera.transform;
            var origin = cam.position;
            var direction = cam.forward;

            if (!Physics.SphereCast(origin, 0.25f, direction, out var hit, attackRange))
                return;

            var egg = hit.collider.GetComponentInParent<MonsterEgg>();
            if (egg != null)
            {
                if (IsPickaxeSelected())
                    egg.TakeDamage(GetPickaxeDamage(), fromPickaxe: true);
                else if (IsWeaponSelected())
                    egg.TakeDamage(GetSelectedWeaponDamage(), fromPickaxe: false);
                return;
            }

            var monster = hit.collider.GetComponentInParent<Monster>();
            if (monster != null)
            {
                if (IsPickaxeSelected())
                    monster.TakeDamage(GetPickaxeDamage(), hit.point, direction);
                else if (IsWeaponSelected())
                    monster.TakeDamage(GetSelectedWeaponDamage(), hit.point, direction);
            }
        }

        bool IsPickaxeSelected()
        {
            var ctx = GameContext.Instance;
            var selected = ctx?.Inventory?.GetSelectedSlot();
            return selected != null && !selected.IsEmpty && selected.item.itemId == "pickaxe";
        }

        bool IsWeaponSelected()
        {
            var selected = GameContext.Instance?.Inventory?.GetSelectedSlot();
            return InventorySystem.IsWeaponItem(selected?.item);
        }

        float GetPickaxeDamage() => PickaxeDamage;

        float GetSelectedWeaponDamage()
        {
            var selected = GameContext.Instance?.Inventory?.GetSelectedSlot();
            if (selected == null || selected.IsEmpty || !InventorySystem.IsWeaponItem(selected.item))
                return 1f;

            return Mathf.Max(1f, selected.item.weaponDamage);
        }
    }
}
