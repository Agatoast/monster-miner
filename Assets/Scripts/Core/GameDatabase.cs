using System.Collections.Generic;
using MonsterMiner.Data;
using UnityEngine;

namespace MonsterMiner.Core
{
    public class GameDatabase : ScriptableObject
    {
        public List<ItemDefinition> items = new();
        public List<MonsterDefinition> monsters = new();
        public List<ShopUpgradeDefinition> shopUpgrades = new();
        public ShopUpgradeDefinition pickaxeUpgrade;
        public ShopUpgradeDefinition inventoryUpgrade;
        public ShopUpgradeDefinition waterPurchase;
        public ShopUpgradeDefinition knifeMapListing;
        public ShopUpgradeDefinition lizardLureListing;
        public ShopUpgradeDefinition phoenixLureListing;
        public ShopUpgradeDefinition caveRatFinderListing;
        public ItemDefinition pickaxeItem;
        public ItemDefinition clubItem;
        public ItemDefinition knifeItem;
        public ItemDefinition knifeGreenItem;
        public ItemDefinition knifeBlueItem;
        public ItemDefinition knifePurpleItem;
        public ItemDefinition knifeGoldenItem;
        public ItemDefinition spearItem;
        public ItemDefinition pistolItem;
        public ItemDefinition rifleItem;
        public ItemDefinition shotgunItem;
        public ItemDefinition machinegunItem;
        public ItemDefinition grenadeItem;
        public ShopUpgradeDefinition spearListing;
        public ShopUpgradeDefinition pistolListing;
        public ShopUpgradeDefinition rifleListing;
        public ShopUpgradeDefinition shotgunListing;
        public ShopUpgradeDefinition machinegunListing;
        public ShopUpgradeDefinition grenadeListing;
        public ItemDefinition lizardLureItem;
        public ItemDefinition phoenixLureItem;
        public ItemDefinition caveRatFinderItem;
        public ItemDefinition legendaryWeaponItem;
        public ItemDefinition glovesGray;
        public ItemDefinition glovesWhite;
        public ItemDefinition glovesGreen;
        public ItemDefinition glovesBlue;
        public ItemDefinition glovesPurple;
        public ItemDefinition glovesGold;

        public static GameDatabase CreateRuntimeDefaults()
        {
            var db = CreateInstance<GameDatabase>();

            db.pickaxeItem = MakeItem("pickaxe", "Pickaxe", ItemCategory.Tool, 0, Color.gray, false);
            db.pickaxeItem.canBeSold = false;
            db.pickaxeItem.iconResourcePath = "Textures/Inventory/Pickaxe";
            db.clubItem = MakeItem("club", "Club", ItemCategory.Weapon, 3, new Color(0.55f, 0.35f, 0.2f), false);
            db.knifeItem = MakeKnife("knife", "Knife", 2, new Color(0.55f, 0.55f, 0.58f));
            db.knifeGreenItem = MakeKnife("knife_green", "Green Knife", 3, new Color(0.25f, 0.85f, 0.35f));
            db.knifeBlueItem = MakeKnife("knife_blue", "Blue Knife", 4, new Color(0.25f, 0.5f, 1f));
            db.knifePurpleItem = MakeKnife("knife_purple", "Purple Knife", 5, new Color(0.65f, 0.25f, 0.95f));
            db.knifeGoldenItem = MakeKnife("knife_golden", "Golden Knife", 6, new Color(1f, 0.82f, 0.15f));
            db.spearItem = MakeWeapon("spear", "Spear", 3, new Color(0.25f, 0.5f, 1f), "Textures/Inventory/Spear");
            db.pistolItem = MakeWeapon("pistol", "Pistol", 4, new Color(0.25f, 0.5f, 1f), "Textures/Inventory/Pistol");
            db.rifleItem = MakeWeapon("rifle", "Rifle", 5, new Color(0.25f, 0.5f, 1f), "Textures/Inventory/Rifle");
            db.shotgunItem = MakeWeapon("shotgun", "Shotgun", 8, new Color(0.25f, 0.5f, 1f), "Textures/Inventory/Shotgun");
            db.machinegunItem = MakeWeapon("machinegun", "Machine Gun", 4, new Color(0.25f, 0.5f, 1f), "Textures/Inventory/Machinegun");
            db.grenadeItem = MakeWeapon("grenade", "Grenade", 12, new Color(0.35f, 0.75f, 0.25f), null, stackLimit: 5);
            db.legendaryWeaponItem = MakeItem("legendary_blade", "Legendary Blade", ItemCategory.Weapon, 50, new Color(1f, 0.85f, 0.2f), false);

            db.glovesGray = MakeGlove("mining_gloves_gray", "Gray Mining Gloves", 0, new Color(0.55f, 0.55f, 0.58f));
            db.glovesWhite = MakeGlove("mining_gloves_white", "White Mining Gloves", 1, new Color(0.92f, 0.92f, 0.92f));
            db.glovesGreen = MakeGlove("mining_gloves_green", "Green Mining Gloves", 2, new Color(0.25f, 0.85f, 0.35f));
            db.glovesBlue = MakeGlove("mining_gloves_blue", "Blue Mining Gloves", 3, new Color(0.25f, 0.5f, 1f));
            db.glovesPurple = MakeGlove("mining_gloves_purple", "Purple Mining Gloves", 4, new Color(0.65f, 0.25f, 0.95f));
            db.glovesGold = MakeGlove("mining_gloves_gold", "Gold Mining Gloves", 5, new Color(1f, 0.82f, 0.15f));

            var monsterMeat = MakeItem("monster_meat", "Monster Meat", ItemCategory.Drop, 4, new Color(0.85f, 0.25f, 0.2f), true);
            monsterMeat.isEdible = true;
            monsterMeat.iconResourcePath = "Textures/MonsterMeat";
            var core = MakeItem("rare_core", "Rare Core", ItemCategory.Drop, 12, new Color(0.3f, 0.9f, 1f), true);
            core.isEdible = true;
            var caveKey = MakeItem("cave_key", "Cave Key", ItemCategory.Key, 25, new Color(1f, 0.75f, 0.1f), true);
            var pebble = MakeItem("shiny_pebble", "Shiny Pebble", ItemCategory.Misc, 1, new Color(1f, 0.85f, 0.25f), false);
            pebble.stackLimit = 1;
            var ore = MakeItem("ore", "Ore", ItemCategory.Ore, 2, new Color(0.5f, 0.45f, 0.4f), false);
            var caveRatFinder = MakeItem("cave_rat_finder", "Cave Rat Egg Finder", ItemCategory.Misc, 2, new Color(0.45f, 0.4f, 0.35f), false);
            var lizardLure = MakeItem("lizard_lure", "Lizard Egg Finder", ItemCategory.Misc, 5, new Color(0.35f, 0.75f, 0.3f), false);
            var phoenixLure = MakeItem("phoenix_lure", "Phoenix Egg Finder", ItemCategory.Misc, 25, new Color(1f, 0.45f, 0.1f), false);

            db.items.AddRange(new[]
            {
                db.pickaxeItem, db.clubItem, db.knifeItem, db.knifeGreenItem, db.knifeBlueItem, db.knifePurpleItem, db.knifeGoldenItem,
                db.spearItem, db.pistolItem, db.rifleItem, db.shotgunItem, db.machinegunItem, db.grenadeItem, db.legendaryWeaponItem,
                db.glovesGray, db.glovesWhite, db.glovesGreen, db.glovesBlue, db.glovesPurple, db.glovesGold,
                monsterMeat, core, caveKey, pebble, ore, caveRatFinder, lizardLure, phoenixLure
            });

            db.monsters.Add(MakeMonster("swarmer", "Weak Swarmer", 12f, 4.5f, 5f, 0.8f, Color.green, monsterMeat, 0.45f, flees: true));
            db.monsters.Add(MakeMonster("brawler", "Average Brawler", 24f, 3.2f, 8f, 1f, Color.yellow, monsterMeat, 0.35f));
            db.monsters.Add(MakeMonster("attacker", "Strong Attacker", 40f, 2.4f, 14f, 1.35f, Color.red, monsterMeat, 0.15f));
            db.monsters.Add(MakeMonster("exploder", "Rare Exploder", 30f, 2.8f, 10f, 1.1f, new Color(1f, 0.4f, 0.1f), core, 0.04f, explodes: true));
            db.monsters.Add(MakeMonster("quest_boss", "Quest Monster", 80f, 2f, 16f, 1.6f, new Color(0.6f, 0.1f, 0.8f), caveKey, 0.01f, boss: true));

            db.pickaxeUpgrade = MakeUpgrade("pickaxe_dmg", "Pickaxe Upgrade", "Upgrade pickaxe mining power and head color.", UpgradeType.PickaxeDamage, 20, 1f, 5);
            db.inventoryUpgrade = MakeUpgrade("inv_slot", "Inventory Upgrade", "Add one inventory slot.", UpgradeType.InventorySlot, 8, 1f, 4);
            db.waterPurchase = MakeUpgrade("water", "Water", "Refill thirst.", UpgradeType.Water, 4, 100f, int.MaxValue);
            db.knifeMapListing = MakeUpgrade("knife_unlock", "Knife", "Buy a knife weapon.", UpgradeType.WeaponUnlock, 9, 1f, 1, db.knifeItem);
            db.spearListing = MakeUpgrade("spear_shop", "Spear", "Buy a spear.", UpgradeType.WeaponUnlock, 15, 1f, 1, db.spearItem);
            db.pistolListing = MakeUpgrade("pistol_shop", "Pistol", "Buy a pistol.", UpgradeType.WeaponUnlock, 25, 1f, 1, db.pistolItem);
            db.rifleListing = MakeUpgrade("rifle_shop", "Rifle", "Buy a rifle.", UpgradeType.WeaponUnlock, 40, 1f, 1, db.rifleItem);
            db.shotgunListing = MakeUpgrade("shotgun_shop", "Shotgun", "Buy a shotgun.", UpgradeType.WeaponUnlock, 55, 1f, 1, db.shotgunItem);
            db.machinegunListing = MakeUpgrade("machinegun_shop", "Machine Gun", "Buy a machine gun.", UpgradeType.WeaponUnlock, 75, 1f, 1, db.machinegunItem);
            db.grenadeListing = MakeUpgrade("grenade_shop", "Grenade", "Buy a grenade.", UpgradeType.WeaponUnlock, 20, 1f, int.MaxValue, db.grenadeItem);
            db.lizardLureItem = lizardLure;
            db.phoenixLureItem = phoenixLure;
            db.caveRatFinderItem = caveRatFinder;
            db.caveRatFinderListing = MakeUpgrade("cave_rat_finder_shop", "Cave Rat Egg Finder", "Buy a cave rat egg finder.", UpgradeType.WeaponUnlock, 3, 1f, int.MaxValue, caveRatFinder);
            db.lizardLureListing = MakeUpgrade("lizard_lure_shop", "Lizard Egg Finder", "Buy a lizard egg finder.", UpgradeType.WeaponUnlock, 10, 1f, int.MaxValue, lizardLure);
            db.phoenixLureListing = MakeUpgrade("phoenix_lure_shop", "Phoenix Egg Finder", "Buy a phoenix egg finder.", UpgradeType.WeaponUnlock, 50, 1f, int.MaxValue, phoenixLure);

            db.shopUpgrades.AddRange(new[] { db.pickaxeUpgrade, db.inventoryUpgrade, db.waterPurchase, db.knifeMapListing, db.caveRatFinderListing, db.lizardLureListing, db.phoenixLureListing });

            return db;

            static ItemDefinition MakeGlove(string id, string name, int miningBonus, Color color)
            {
                var item = MakeItem(id, name, ItemCategory.Gloves, miningBonus * 2, color, false);
                item.isMiningGlove = true;
                item.miningBonus = miningBonus;
                return item;
            }

            static ItemDefinition MakeKnife(string id, string name, int damage, Color color)
            {
                return MakeWeapon(id, name, damage, color, "Textures/Inventory/Knife");
            }

            static ItemDefinition MakeWeapon(string id, string name, int damage, Color color, string iconPath, int sell = 5, int stackLimit = 1)
            {
                var item = MakeItem(id, name, ItemCategory.Weapon, sell, color, false);
                item.weaponDamage = damage;
                item.stackLimit = stackLimit;
                item.iconResourcePath = iconPath;
                return item;
            }

            static ItemDefinition MakeItem(string id, string name, ItemCategory cat, int sell, Color color, bool drop)
            {
                var item = CreateInstance<ItemDefinition>();
                item.itemId = id;
                item.displayName = name;
                item.category = cat;
                item.sellValue = sell;
                item.worldColor = color;
                item.isMonsterDrop = drop;
                return item;
            }

            static MonsterDefinition MakeMonster(
                string id,
                string name,
                float hp,
                float speed,
                float dmg,
                float scale,
                Color color,
                ItemDefinition drop,
                float weight,
                bool explodes = false,
                bool boss = false,
                bool flees = false)
            {
                var m = CreateInstance<MonsterDefinition>();
                m.monsterId = id;
                m.displayName = name;
                m.maxHealth = hp;
                m.moveSpeed = speed;
                m.attackDamage = dmg;
                m.scale = scale;
                m.bodyColor = color;
                m.dropItem = drop;
                m.spawnWeight = weight;
                m.explodesOnDeath = explodes;
                m.isQuestBoss = boss;
                m.fleesFromPlayer = flees;
                return m;
            }

            static ShopUpgradeDefinition MakeUpgrade(string id, string name, string desc, UpgradeType type, int cost, float value, int max, ItemDefinition unlock = null, string boardVisual = null, float boardScreenYOffsetPixels = 0f)
            {
                var u = CreateInstance<ShopUpgradeDefinition>();
                u.upgradeId = id;
                u.displayName = name;
                u.description = desc;
                u.upgradeType = type;
                u.cost = cost;
                u.value = value;
                u.maxPurchases = max;
                u.unlockItem = unlock;
                u.boardVisualResourcePath = boardVisual;
                u.boardVisualScreenYOffsetPixels = boardScreenYOffsetPixels;
                return u;
            }
        }
    }
}
