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
        public const bool SpawnAsQuarry2CompleteWithMagicCompass = true;
        public const bool SpawnWithArtilleryTrialWon = true;
        const int MaxInventorySlots = 7;
        const int MachineGunSlotIndex = 2;

        public static void Apply(GameContext ctx)
        {
            if (!Enabled || ctx?.Database == null || ctx.Inventory == null || ctx.PlayerCombat == null)
                return;

            while (ctx.Inventory.SlotCount < MaxInventorySlots)
                ctx.Inventory.ExpandSlots(1);

            ClearNonPickaxeSlots(ctx.Inventory);

            while (ctx.PlayerCombat.PickaxeMiningTier < PickaxeVisualFactory.MaxMiningTier)
                ctx.PlayerCombat.UpgradePickaxe();

            ctx.Inventory.EquipGloves(ctx.Database.glovesGold);
            ctx.Inventory.SetReservedPickaxe(ctx.Database.pickaxeItem);
            AssignSlot(ctx.Inventory, MachineGunSlotIndex, ctx.Database.machinegunItem);

            ctx.PlayerRangedAmmo?.Reload("machinegun");

            if (SpawnAsQuarry2CompleteWithMagicCompass)
                ApplyQuarry2CompleteWithMagicCompass(ctx);

            if (SpawnWithArtilleryTrialWon)
                ApplyArtilleryTrialWon(ctx);

            ctx.Inventory.SelectSlot(InventorySystem.PickaxeSlotIndex);
        }

        static void ApplyArtilleryTrialWon(GameContext ctx)
        {
            var progression = ctx.CaveProgression;
            if (progression == null)
                return;

            progression.MarkSamuraiIntroHeard();
            progression.CompleteQuarry3CompassReturn();
            progression.CompleteArtilleryTrial();
        }

        static void ApplyQuarry2CompleteWithMagicCompass(GameContext ctx)
        {
            ctx.CaveProgression?.CompleteJarlSkullQuest();
        }

        static void ClearNonPickaxeSlots(InventorySystem inventory)
        {
            for (int i = 1; i < inventory.SlotCount; i++)
            {
                var slot = inventory.Slots[i];
                slot.item = null;
                slot.count = 0;
                slot.fromShopPurchase = false;
            }

            inventory.NotifyChanged();
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
