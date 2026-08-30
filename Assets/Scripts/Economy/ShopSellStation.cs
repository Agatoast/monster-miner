using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Interaction;
using MonsterMiner.Inventory;
using MonsterMiner.Player;
using MonsterMiner.UI;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class ShopSellStation : MonoBehaviour, IInteractable
    {
        public void ConfigureHitbox(Camera camera) { }

        void Update()
        {
            if (!SellConfirmationDisplay.IsForStation(this))
                return;

            SellConfirmationDisplay.HandleInput();

            var ctx = GameContext.Instance;
            var slot = ctx?.Inventory?.GetSelectedSlot();
            if (!CanSellSelectedSlot(slot) || !slot.fromShopPurchase)
            {
                SellConfirmationDisplay.Cancel();
                return;
            }

            var interactor = ctx.Player != null ? ctx.Player.GetComponent<Interactor>() : null;
            if (interactor == null || !interactor.IsInInteractionRange(this))
                SellConfirmationDisplay.Cancel();
        }

        public string GetPrompt()
        {
            var ctx = GameContext.Instance;
            var slot = ctx?.Inventory?.GetSelectedSlot();
            if (slot == null || slot.IsEmpty)
                return "Sell to shopkeeper [E]";
            if (!slot.item.canBeSold)
            {
                if (slot.item.category == ItemCategory.Tool)
                    return "Pickaxe upgrades at the shop";
                return "This item cannot be sold";
            }

            if (SellConfirmationDisplay.IsForStation(this))
                return $"Sell {slot.item.displayName} for ${GetSellValue(slot)}? [E] Confirm";

            if (slot.fromShopPurchase)
                return $"Sell {slot.item.displayName} for ${GetSellValue(slot)} [E]";

            return $"Sell {slot.item.displayName} to shopkeeper for ${GetSellValue(slot)} [E]";
        }

        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor)
        {
            var ctx = GameContext.Instance;
            if (ctx == null)
                return;

            var slot = ctx.Inventory.GetSelectedSlot();
            if (!CanSellSelectedSlot(slot))
            {
                if (slot == null || slot.IsEmpty)
                    ctx.Hud?.ShowMessage("Nothing selected to sell");
                else if (!slot.item.canBeSold)
                {
                    if (slot.item.category == ItemCategory.Tool)
                        ctx.Hud?.ShowMessage("Pickaxe cannot be sold — buy upgrades at the shop");
                    else
                        ctx.Hud?.ShowMessage("This item cannot be sold");
                }

                SellConfirmationDisplay.Cancel();
                return;
            }

            if (SellConfirmationDisplay.IsForStation(this))
            {
                CompleteSell(ctx);
                SellConfirmationDisplay.Cancel();
                return;
            }

            if (slot.fromShopPurchase)
            {
                int value = GetSellValue(slot);
                SellConfirmationDisplay.Show(this, slot.item.displayName, value, () => CompleteSell(ctx));
                return;
            }

            CompleteSell(ctx);
        }

        static void CompleteSell(GameContext ctx)
        {
            var slot = ctx.Inventory.GetSelectedSlot();
            if (!CanSellSelectedSlot(slot))
                return;

            int value = GetSellValue(slot);
            bool isKey = slot.item.itemId == "cave_key";
            bool isPebble = slot.item.itemId == "shiny_pebble";
            ctx.Wallet.Add(value);
            ctx.Inventory.TryRemoveFromSelected(1);

            if (isPebble)
                ctx.SpawnManager?.NotifyPebbleSold();

            ctx.Shopkeeper?.ThankCustomer(fromSale: true);

            if (isKey)
                ctx.Hud?.ShowMessage("Cave Key sold — next cave TBD");
            else
                ctx.Hud?.ShowMessage($"Sold for ${value}");
        }

        static bool CanSellSelectedSlot(InventorySlot slot)
        {
            return slot != null && !slot.IsEmpty && slot.item.canBeSold;
        }

        static int GetSellValue(InventorySlot slot) => InventorySystem.GetShopSellBackValue(slot);
    }
}
