using System.Collections.Generic;
using MonsterMiner.Core;
using MonsterMiner.Player;
using MonsterMiner.Data;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class ShopManager : MonoBehaviour
    {
        const string MapIdFirstCave = "cave_1";
        const int GloveUpgradeBaseCost = 8;
        const int KnifeUpgradeBaseCost = 12;

        readonly Dictionary<string, int> purchaseCounts = new();
        readonly List<ShopMenuEntry> menuEntries = new();

        ShopBuyStation buyStation;
        string currentMapId = MapIdFirstCave;

        public bool IsMenuOpen { get; private set; }

        public void Initialize(Transform boardTransform)
        {
            buyStation = boardTransform.GetComponent<ShopBuyStation>();
            if (buyStation == null)
                buyStation = boardTransform.gameObject.AddComponent<ShopBuyStation>();
            buyStation.Initialize(this);
        }

        void Start()
        {
            var camera = GameContext.Instance?.Player?.ViewCamera;
            if (camera != null)
                buyStation?.ConfigureHitbox(camera);
        }

        void Update()
        {
            UI.ShopBuyMenuDisplay.HandleInput(this);

            if (GameContext.Instance?.IsPlayerDead == true)
            {
                if (IsMenuOpen)
                    CloseMenu();
                return;
            }

            if (!IsMenuOpen || buyStation == null)
                return;

            var player = GameContext.Instance?.Player;
            var interactor = player != null ? player.GetComponent<Interactor>() : null;
            if (interactor == null || !interactor.IsInInteractionRange(buyStation))
                CloseMenu();
        }

        public void OpenMenu()
        {
            IsMenuOpen = true;
            RebuildMenuEntries();
        }

        public void CloseMenu()
        {
            IsMenuOpen = false;
        }

        public void SetCurrentMapId(string mapId)
        {
            currentMapId = mapId;
            if (IsMenuOpen)
                RebuildMenuEntries();
        }

        public IReadOnlyList<ShopMenuEntry> GetMenuEntries()
        {
            if (menuEntries.Count == 0)
                RebuildMenuEntries();
            return menuEntries;
        }

        public bool TryPurchase(int index)
        {
            RebuildMenuEntries();
            if (index < 0 || index >= menuEntries.Count)
                return false;

            var entry = menuEntries[index];
            if (!entry.canPurchase)
                return false;

            return TryPurchaseById(entry.id);
        }

        void RebuildMenuEntries()
        {
            menuEntries.Clear();
            var ctx = GameContext.Instance;
            if (ctx?.Database == null)
                return;

            AddPickaxeEntry(ctx);
            AddGloveEntry(ctx);
            AddKnifeUpgradeEntry(ctx);
            AddInventoryEntry(ctx);
            AddWaterEntry(ctx);

            foreach (var mapItem in GetMapShopItems(ctx))
                menuEntries.Add(mapItem);

            AddLureEntries(ctx);
        }

        void AddLureEntries(GameContext ctx)
        {
            AddFixedPriceShopEntry(ctx, ctx.Database.caveRatFinderListing);
            AddFixedPriceShopEntry(ctx, ctx.Database.lizardLureListing);
            AddFixedPriceShopEntry(ctx, ctx.Database.rabbitFinderListing);
            AddFixedPriceShopEntry(ctx, ctx.Database.caveLizardFinderListing);
            AddFixedPriceShopEntry(ctx, ctx.Database.salamanderLureListing);
            AddFixedPriceShopEntry(ctx, ctx.Database.phoenixLureListing);
            AddWeaponShopEntry(ctx, ctx.Database.spearListing);
            AddWeaponShopEntry(ctx, ctx.Database.pistolListing);
            AddWeaponShopEntry(ctx, ctx.Database.rifleListing);
            AddWeaponShopEntry(ctx, ctx.Database.shotgunListing);
            AddWeaponShopEntry(ctx, ctx.Database.machinegunListing);
            AddFixedPriceShopEntry(ctx, ctx.Database.grenadeListing);
        }

        void AddWeaponShopEntry(GameContext ctx, ShopUpgradeDefinition listing)
        {
            if (listing == null)
                return;

            bool alreadyOwned = listing.unlockItem != null && PlayerOwnsItem(ctx, listing.unlockItem);
            bool alreadyBought = GetPurchaseCount(listing.upgradeId) >= listing.maxPurchases;
            bool canBuy = CanPurchaseWeaponListing(ctx, listing, alreadyOwned, alreadyBought);
            if (canBuy && listing.unlockItem != null && !ctx.Inventory.CanAdd(listing.unlockItem, 1))
                canBuy = false;

            menuEntries.Add(new ShopMenuEntry(
                listing.upgradeId,
                listing.displayName,
                listing.cost,
                canBuy,
                canBuy ? null : alreadyOwned ? "OWNED" : "SOLD"));
        }

        static bool CanPurchaseWeaponListing(GameContext ctx, ShopUpgradeDefinition listing, bool alreadyOwned, bool alreadyBought)
        {
            if (alreadyOwned)
                return false;

            if (IsRepurchasableBaseSpearListing(listing))
                return true;

            return !alreadyBought;
        }

        static bool IsRepurchasableBaseSpearListing(ShopUpgradeDefinition listing) =>
            listing?.upgradeId == "spear_shop";

        static bool CanRepurchaseWeaponWithoutInventory(GameContext ctx, ShopUpgradeDefinition upgrade)
        {
            if (!IsRepurchasableBaseSpearListing(upgrade) || upgrade.unlockItem == null)
                return false;

            return !PlayerOwnsItem(ctx, upgrade.unlockItem);
        }

        void AddFixedPriceShopEntry(GameContext ctx, ShopUpgradeDefinition listing)
        {
            if (listing == null)
                return;

            int count = GetPurchaseCount(listing.upgradeId);
            bool canBuy = count < listing.maxPurchases;
            if (canBuy && listing.unlockItem != null && !ctx.Inventory.CanAdd(listing.unlockItem, 1))
                canBuy = false;

            menuEntries.Add(new ShopMenuEntry(
                listing.upgradeId,
                listing.displayName,
                listing.cost,
                canBuy,
                canBuy ? null : count >= listing.maxPurchases ? "MAX" : "FULL"));
        }

        void AddPickaxeEntry(GameContext ctx)
        {
            var upgrade = ctx.Database.pickaxeUpgrade;
            if (upgrade == null)
                return;

            bool canBuy = ctx.PlayerCombat.PickaxeMiningTier < PickaxeVisualFactory.MaxMiningTier;
            int cost = GetEscalatingCost(upgrade.cost, ctx.PlayerCombat.PickaxeMiningTier);
            menuEntries.Add(new ShopMenuEntry(
                upgrade.upgradeId,
                "Pickaxe Upgrade",
                cost,
                canBuy,
                canBuy ? null : "MAX"));
        }

        void AddKnifeUpgradeEntry(GameContext ctx)
        {
            if (!TryGetNextKnifeUpgrade(ctx, out var nextKnife, out int cost))
            {
                if (ctx.Inventory.GetOwnedKnifeItem() == ctx.Database.knifeGoldenItem)
                    menuEntries.Add(new ShopMenuEntry("knife_upgrade", "Knife Upgrade", 0, false, "MAX"));
                return;
            }

            menuEntries.Add(new ShopMenuEntry(
                BuildKnifeEntryId(nextKnife),
                "Knife Upgrade",
                cost,
                true));
        }

        void AddGloveEntry(GameContext ctx)
        {
            if (!TryGetNextGloveUpgrade(ctx, out var nextGlove, out int cost))
            {
                menuEntries.Add(new ShopMenuEntry("glove_upgrade", "Mining Glove Upgrade", 0, false, "MAX"));
                return;
            }

            menuEntries.Add(new ShopMenuEntry(
                BuildGloveEntryId(nextGlove),
                "Mining Glove Upgrade",
                cost,
                true));
        }

        void AddInventoryEntry(GameContext ctx)
        {
            var upgrade = ctx.Database.inventoryUpgrade;
            if (upgrade == null)
                return;

            int count = GetPurchaseCount(upgrade.upgradeId);
            bool canBuy = count < upgrade.maxPurchases;
            int cost = GetEscalatingCost(upgrade.cost, count);
            menuEntries.Add(new ShopMenuEntry(
                upgrade.upgradeId,
                "Inventory Upgrade",
                cost,
                canBuy,
                canBuy ? null : "MAX"));
        }

        void AddWaterEntry(GameContext ctx)
        {
            var upgrade = ctx.Database.waterPurchase;
            if (upgrade == null)
                return;

            int count = GetPurchaseCount(upgrade.upgradeId);
            int cost = GetEscalatingCost(upgrade.cost, count);
            menuEntries.Add(new ShopMenuEntry(
                upgrade.upgradeId,
                "Water",
                cost,
                true));
        }

        IEnumerable<ShopMenuEntry> GetMapShopItems(GameContext ctx)
        {
            if (currentMapId != MapIdFirstCave)
                yield break;

            var knife = ctx.Database.knifeMapListing;
            if (knife == null)
                yield break;

            bool alreadyOwned = PlayerOwnsItem(ctx, knife.unlockItem);
            bool alreadyBought = GetPurchaseCount(knife.upgradeId) >= knife.maxPurchases;
            bool canBuy = !alreadyOwned && !alreadyBought;
            int cost = GetEscalatingCost(knife.cost, GetPurchaseCount(knife.upgradeId));

            yield return new ShopMenuEntry(
                knife.upgradeId,
                knife.displayName,
                cost,
                canBuy,
                canBuy ? null : alreadyOwned ? "OWNED" : "SOLD");
        }

        bool TryPurchaseById(string entryId)
        {
            var ctx = GameContext.Instance;
            if (ctx == null)
                return false;

            if (entryId.StartsWith("glove_"))
                return TryPurchaseGloveUpgrade(ctx, entryId);

            if (entryId.StartsWith("knife_knife"))
                return TryPurchaseKnifeUpgrade(ctx, entryId);

            ShopUpgradeDefinition upgrade = ResolveUpgrade(ctx, entryId);
            if (upgrade == null)
                return false;

            if (upgrade.upgradeType == UpgradeType.PickaxeDamage &&
                ctx.PlayerCombat.PickaxeMiningTier >= PickaxeVisualFactory.MaxMiningTier)
                return false;

            int count = GetPurchaseCount(upgrade.upgradeId);
            if (upgrade.upgradeType != UpgradeType.PickaxeDamage
                && count >= upgrade.maxPurchases
                && !CanRepurchaseWeaponWithoutInventory(ctx, upgrade))
                return false;

            if (upgrade.upgradeType == UpgradeType.WeaponUnlock &&
                upgrade.unlockItem != null &&
                !IsRepeatableShopItem(upgrade) &&
                PlayerOwnsItem(ctx, upgrade.unlockItem))
                return false;

            if (PurchaseAddsToInventory(upgrade) && !ctx.Inventory.CanAddShopPurchase(upgrade.unlockItem))
            {
                ctx.Hud?.ShowMessage("Inventory Full");
                return false;
            }

            int cost = GetPurchaseCost(upgrade, count, ctx);

            if (!ctx.Wallet.TrySpend(cost))
            {
                ctx.Hud?.ShowMessage("Not enough $");
                return false;
            }

            ApplyUpgrade(ctx, upgrade);
            purchaseCounts[upgrade.upgradeId] = count + 1;
            ctx.Shopkeeper?.ThankCustomer();
            ctx.Hud?.ShowMessage($"Purchased {GetPurchaseLabel(upgrade)}");
            RebuildMenuEntries();
            return true;
        }

        bool TryPurchaseKnifeUpgrade(GameContext ctx, string entryId)
        {
            if (!TryGetNextKnifeUpgrade(ctx, out var nextKnife, out int cost))
                return false;

            if (BuildKnifeEntryId(nextKnife) != entryId)
                RebuildMenuEntries();

            if (!ctx.Wallet.TrySpend(cost))
            {
                ctx.Hud?.ShowMessage("Not enough $");
                return false;
            }

            if (!ctx.Inventory.TryUpgradeKnife(nextKnife))
            {
                ctx.Wallet.Add(cost);
                ctx.Hud?.ShowMessage("Knife not found");
                return false;
            }

            ctx.Shopkeeper?.ThankCustomer();
            ctx.Hud?.ShowMessage($"Purchased {nextKnife.displayName}");
            RebuildMenuEntries();
            return true;
        }

        bool TryPurchaseGloveUpgrade(GameContext ctx, string entryId)
        {
            if (!TryGetNextGloveUpgrade(ctx, out var nextGlove, out int cost))
                return false;

            if (BuildGloveEntryId(nextGlove) != entryId)
                RebuildMenuEntries();

            if (!ctx.Wallet.TrySpend(cost))
            {
                ctx.Hud?.ShowMessage("Not enough $");
                return false;
            }

            ctx.Inventory.EquipGloves(nextGlove);
            ctx.Shopkeeper?.ThankCustomer();
            ctx.Hud?.ShowMessage($"Purchased {nextGlove.displayName}");
            RebuildMenuEntries();
            return true;
        }

        static ShopUpgradeDefinition ResolveUpgrade(GameContext ctx, string entryId)
        {
            if (ctx.Database.pickaxeUpgrade?.upgradeId == entryId)
                return ctx.Database.pickaxeUpgrade;
            if (ctx.Database.inventoryUpgrade?.upgradeId == entryId)
                return ctx.Database.inventoryUpgrade;
            if (ctx.Database.waterPurchase?.upgradeId == entryId)
                return ctx.Database.waterPurchase;
            if (ctx.Database.knifeMapListing?.upgradeId == entryId)
                return ctx.Database.knifeMapListing;
            if (ctx.Database.caveRatFinderListing?.upgradeId == entryId)
                return ctx.Database.caveRatFinderListing;
            if (ctx.Database.lizardLureListing?.upgradeId == entryId)
                return ctx.Database.lizardLureListing;
            if (ctx.Database.rabbitFinderListing?.upgradeId == entryId)
                return ctx.Database.rabbitFinderListing;
            if (ctx.Database.caveLizardFinderListing?.upgradeId == entryId)
                return ctx.Database.caveLizardFinderListing;
            if (ctx.Database.salamanderLureListing?.upgradeId == entryId)
                return ctx.Database.salamanderLureListing;
            if (ctx.Database.phoenixLureListing?.upgradeId == entryId)
                return ctx.Database.phoenixLureListing;
            if (ctx.Database.spearListing?.upgradeId == entryId)
                return ctx.Database.spearListing;
            if (ctx.Database.pistolListing?.upgradeId == entryId)
                return ctx.Database.pistolListing;
            if (ctx.Database.rifleListing?.upgradeId == entryId)
                return ctx.Database.rifleListing;
            if (ctx.Database.shotgunListing?.upgradeId == entryId)
                return ctx.Database.shotgunListing;
            if (ctx.Database.machinegunListing?.upgradeId == entryId)
                return ctx.Database.machinegunListing;
            if (ctx.Database.grenadeListing?.upgradeId == entryId)
                return ctx.Database.grenadeListing;
            return null;
        }

        static int GetPurchaseCost(ShopUpgradeDefinition upgrade, int purchaseCount, GameContext ctx)
        {
            if (upgrade.upgradeType == UpgradeType.PickaxeDamage)
                return GetEscalatingCost(upgrade.cost, ctx.PlayerCombat.PickaxeMiningTier);

            if (IsFixedPriceListing(upgrade))
                return upgrade.cost;

            return GetEscalatingCost(upgrade.cost, purchaseCount);
        }

        static bool IsFixedPriceListing(ShopUpgradeDefinition upgrade)
        {
            return upgrade.upgradeId == "gremlin_finder_shop"
                || upgrade.upgradeId == "lizard_lure_shop"
                || upgrade.upgradeId == "rabbit_finder_shop"
                || upgrade.upgradeId == "cave_lizard_finder_shop"
                || upgrade.upgradeId == "salamander_finder_shop"
                || upgrade.upgradeId == "phoenix_lure_shop"
                || upgrade.upgradeId == "grenade_shop";
        }

        static bool IsRepeatableShopItem(ShopUpgradeDefinition upgrade) => IsFixedPriceListing(upgrade);

        static bool PurchaseAddsToInventory(ShopUpgradeDefinition upgrade)
        {
            return upgrade != null
                && upgrade.upgradeType == UpgradeType.WeaponUnlock
                && upgrade.unlockItem != null;
        }

        static string GetPurchaseLabel(ShopUpgradeDefinition upgrade)
        {
            return upgrade.upgradeType switch
            {
                UpgradeType.PickaxeDamage => "Pickaxe Upgrade",
                UpgradeType.InventorySlot => "Inventory Upgrade",
                UpgradeType.Water => "Water",
                _ => upgrade.displayName
            };
        }

        static bool TryGetNextKnifeUpgrade(GameContext ctx, out ItemDefinition nextKnife, out int cost)
        {
            nextKnife = null;
            cost = 0;
            var db = ctx.Database;
            var current = ctx.Inventory.GetOwnedKnifeItem();
            if (current == null)
                return false;

            var tiers = new[]
            {
                db.knifeItem,
                db.knifeGreenItem,
                db.knifeBlueItem,
                db.knifePurpleItem,
                db.knifeGoldenItem
            };

            for (int i = 0; i < tiers.Length - 1; i++)
            {
                if (current != tiers[i])
                    continue;

                nextKnife = tiers[i + 1];
                cost = GetEscalatingCost(KnifeUpgradeBaseCost, i);
                return true;
            }

            return false;
        }

        static bool TryGetNextGloveUpgrade(GameContext ctx, out ItemDefinition nextGlove, out int cost)
        {
            nextGlove = null;
            cost = 0;
            var db = ctx.Database;
            var current = ctx.Inventory.EquippedGloves ?? db.glovesGray;

            var tiers = new[]
            {
                db.glovesGray,
                db.glovesWhite,
                db.glovesGreen,
                db.glovesBlue,
                db.glovesPurple,
                db.glovesGold
            };

            for (int i = 0; i < tiers.Length - 1; i++)
            {
                if (current != tiers[i])
                    continue;

                nextGlove = tiers[i + 1];
                cost = GetEscalatingCost(GloveUpgradeBaseCost, i);
                return true;
            }

            return false;
        }

        static int GetEscalatingCost(int baseCost, int purchaseCount)
        {
            return baseCost * (int)Mathf.Pow(3, purchaseCount);
        }

        static string BuildGloveEntryId(ItemDefinition glove) => $"glove_{glove.itemId}";

        static string BuildKnifeEntryId(ItemDefinition knife) => $"knife_{knife.itemId}";

        static bool PlayerOwnsItem(GameContext ctx, ItemDefinition item)
        {
            if (item == null)
                return false;

            foreach (var slot in ctx.Inventory.Slots)
            {
                if (!slot.IsEmpty && slot.item == item)
                    return true;
            }

            return false;
        }

        int GetPurchaseCount(string upgradeId)
        {
            return purchaseCounts.TryGetValue(upgradeId, out int count) ? count : 0;
        }

        static void ApplyUpgrade(GameContext ctx, ShopUpgradeDefinition upgrade)
        {
            switch (upgrade.upgradeType)
            {
                case UpgradeType.PickaxeDamage:
                    ctx.PlayerCombat.UpgradePickaxe();
                    ctx.Inventory.NotifyChanged();
                    break;
                case UpgradeType.InventorySlot:
                    ctx.Inventory.ExpandSlots((int)upgrade.value);
                    break;
                case UpgradeType.Water:
                    ctx.PlayerThirst?.Drink(upgrade.value);
                    break;
                case UpgradeType.WeaponUnlock:
                    if (upgrade.unlockItem != null)
                        ctx.Inventory.TryAdd(upgrade.unlockItem, 1, fromShopPurchase: true);
                    break;
                case UpgradeType.GloveUpgrade:
                    if (upgrade.unlockItem != null)
                        ctx.Inventory.EquipGloves(upgrade.unlockItem);
                    break;
            }
        }
    }
}
