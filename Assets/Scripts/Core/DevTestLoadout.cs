using MonsterMiner.Combat;
using MonsterMiner.Data;
using MonsterMiner.Inventory;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.Core
{
    public static class DevTestLoadout
    {
        public const bool Enabled = true;
        const int MaxInventorySlots = 7;
        const int MachineGunSlotIndex = 2;

        public static void Apply(GameContext ctx)
        {
            if (!Enabled || ctx?.Database == null || ctx.Inventory == null || ctx.PlayerCombat == null)
                return;

            while (ctx.Inventory.SlotCount < MaxInventorySlots)
                ctx.Inventory.ExpandSlots(1);

            while (ctx.PlayerCombat.PickaxeMiningTier < PickaxeVisualFactory.MaxMiningTier)
                ctx.PlayerCombat.UpgradePickaxe();

            ctx.Inventory.EquipGloves(ctx.Database.glovesGold);
            ctx.Inventory.SetReservedPickaxe(ctx.Database.pickaxeItem);

            AssignSlot(ctx.Inventory, 1, ctx.Database.knifeGoldenItem);
            AssignSlot(ctx.Inventory, MachineGunSlotIndex, ctx.Database.machinegunItem);
            AssignSlot(ctx.Inventory, 3, ctx.Database.grenadeItem);
            AssignSlot(ctx.Inventory, 4, ctx.Database.rifleItem);
            AssignSlot(ctx.Inventory, 5, ctx.Database.shotgunItem);
            AssignSlot(ctx.Inventory, 6, ctx.Database.slotTestTokenItem);

            ctx.PlayerRangedAmmo?.Reload("machinegun");
            ctx.PlayerRangedAmmo?.Reload("rifle");
            ctx.PlayerRangedAmmo?.Reload("shotgun");

            ctx.Inventory.SelectSlot(InventorySystem.PickaxeSlotIndex);
        }

        static void AssignSlot(InventorySystem inventory, int index, ItemDefinition item)
        {
            if (inventory == null || item == null || index == InventorySystem.PickaxeSlotIndex)
                return;

            while (inventory.SlotCount <= index)
                inventory.ExpandSlots(1);

            inventory.AssignSlot(index, item, 1);
        }
    }
}
