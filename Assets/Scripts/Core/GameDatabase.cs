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
        public ShopUpgradeDefinition rabbitFinderListing;
        public ShopUpgradeDefinition caveLizardFinderListing;
        public ShopUpgradeDefinition salamanderLureListing;
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
        public ItemDefinition rabbitFinderItem;
        public ItemDefinition caveLizardFinderItem;
        public ItemDefinition salamanderLureItem;
        public ItemDefinition phoenixLureItem;
        public ItemDefinition pentachickHeartItem;
        public ItemDefinition caveRatFinderItem;
        public ItemDefinition legendaryWeaponItem;
        public ItemDefinition legendarySpearItem;
        public ItemDefinition legendaryPistolItem;
        public ItemDefinition legendaryRifleItem;
        public ItemDefinition legendaryShotgunItem;
        public ItemDefinition legendaryMachinegunItem;
        public ItemDefinition slotTestTokenItem;
        public ItemDefinition glovesGray;
        public ItemDefinition glovesWhite;
        public ItemDefinition glovesGreen;
        public ItemDefinition glovesBlue;
        public ItemDefinition glovesPurple;
        public ItemDefinition glovesGold;

        public ItemDefinition[] GetNonBossMonsterDropItems()
        {
            var seen = new HashSet<string>();
            var results = new List<ItemDefinition>();
            foreach (var monster in monsters)
            {
                if (monster == null || monster.isQuestBoss || monster.dropItem == null)
                    continue;

                if (seen.Add(monster.dropItem.itemId))
                    results.Add(monster.dropItem);
            }

            return results.ToArray();
        }

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
            db.pistolItem = MakeWeapon("pistol", "Pistol", 25, new Color(0.25f, 0.5f, 1f), "Textures/Inventory/Pistol");
            db.rifleItem = MakeWeapon("rifle", "Rifle", 200, new Color(0.25f, 0.5f, 1f), "Textures/Inventory/Rifle");
            db.shotgunItem = MakeWeapon("shotgun", "Shotgun", 30, new Color(0.25f, 0.5f, 1f), "Textures/Inventory/Shotgun");
            db.machinegunItem = MakeWeapon("machinegun", "Machine Gun", 40, new Color(0.25f, 0.5f, 1f), "Textures/Inventory/Machinegun");
            db.grenadeItem = MakeWeapon("grenade", "Grenade", 12, new Color(0.35f, 0.75f, 0.25f), "Textures/Inventory/Grenade");
            db.legendaryWeaponItem = MakeLegendaryKnife("legendary_blade", "Legendary Blade", 6, new Color(1f, 0.85f, 0.2f));
            db.legendarySpearItem = MakeLegendaryWeapon("spear", "Spear", 3, "Textures/Inventory/Spear");
            db.legendaryPistolItem = MakeLegendaryWeapon("pistol", "Pistol", 25, "Textures/Inventory/Pistol");
            db.legendaryRifleItem = MakeLegendaryWeapon("rifle", "Rifle", 200, "Textures/Inventory/Rifle");
            db.legendaryShotgunItem = MakeLegendaryWeapon("shotgun", "Shotgun", 30, "Textures/Inventory/Shotgun");
            db.legendaryMachinegunItem = MakeLegendaryWeapon("machinegun", "Machine Gun", 40, "Textures/Inventory/Machinegun");

            db.glovesGray = MakeGlove("mining_gloves_gray", "Gray Mining Gloves", 0, new Color(0.55f, 0.55f, 0.58f));
            db.glovesWhite = MakeGlove("mining_gloves_white", "White Mining Gloves", 1, new Color(0.92f, 0.92f, 0.92f));
            db.glovesGreen = MakeGlove("mining_gloves_green", "Green Mining Gloves", 2, new Color(0.25f, 0.85f, 0.35f));
            db.glovesBlue = MakeGlove("mining_gloves_blue", "Blue Mining Gloves", 3, new Color(0.25f, 0.5f, 1f));
            db.glovesPurple = MakeGlove("mining_gloves_purple", "Purple Mining Gloves", 4, new Color(0.65f, 0.25f, 0.95f));
            db.glovesGold = MakeGlove("mining_gloves_gold", "Gold Mining Gloves", 5, new Color(1f, 0.82f, 0.15f));

            var rabbitMeat = MakeMeat("rabbit_meat", 3, "Textures/Creatures/Meat/rabbit");
            var iguanaMeat = MakeMeat("iguana_meat", 3, "Textures/Creatures/Meat/iguana");
            var caveLizardMeat = MakeMeat("cave_lizard_meat", 5, "Textures/Creatures/Meat/cave_lizard");
            var gremlinMeat = MakeMeat("gremlin_meat", 7, "Textures/Creatures/Meat/gremlin");
            var salamanderMeat = MakeMeat("salamander_meat", 9, "Textures/Creatures/Meat/salamander");
            var core = MakeItem("rare_core", "Rare Core", ItemCategory.Drop, 12, new Color(0.3f, 0.9f, 1f), true);
            core.isEdible = true;
            var caveKey = MakeItem("cave_key", "Cave Key", ItemCategory.Key, 25, new Color(1f, 0.75f, 0.1f), false);
            caveKey.isBossDrop = true;
            var pebble = MakeItem("shiny_pebble", "Shiny Pebble", ItemCategory.Misc, 1, new Color(1f, 0.85f, 0.25f), false);
            var ore = MakeItem("ore", "Ore", ItemCategory.Ore, 2, new Color(0.5f, 0.45f, 0.4f), false);
            var gremlinFinder = MakeFinder("gremlin_finder", "Gremlin Finder", 2, new Color(0.72f, 0.38f, 0.58f), "gremlin", EggFinderRarity.Common, 3, 5);
            var lizardLure = MakeFinder("lizard_lure", "Iguana Finder", 0, new Color(0.35f, 0.75f, 0.3f), "iguana", EggFinderRarity.Common, 3, 5);
            var rabbitFinder = MakeFinder("rabbit_finder", "Rabbit Finder", 0, new Color(0.82f, 0.78f, 0.72f), "rabbit", EggFinderRarity.Common, 3, 5);
            var caveLizardFinder = MakeFinder("cave_lizard_finder", "Cave Lizard Finder", 1, new Color(0.62f, 0.48f, 0.28f), "cave_lizard", EggFinderRarity.Common, 3, 5);
            var salamanderFinder = MakeFinder("salamander_finder", "Salamander Finder", 5, new Color(0.82f, 0.34f, 0.18f), "salamander", EggFinderRarity.Uncommon, 1, 35);
            var phoenixLure = MakeFinder("phoenix_lure", "Pentachick Finder", 50, new Color(1f, 0.45f, 0.1f), "pentachick", EggFinderRarity.Rare, 1, 2);
            var pentachickHeart = MakeItem("pentachick_heart", "Pentachick Heart", ItemCategory.Key, 50, new Color(1f, 0.35f, 0.12f), false);
            pentachickHeart.isBossDrop = true;
            var slotTestToken = MakeItem("slot_test_token", "Slot Test Token", ItemCategory.Misc, 0, new Color(1f, 0.82f, 0.15f), false);
            slotTestToken.canBeSold = false;
            slotTestToken.isSlotTestToken = true;
            db.slotTestTokenItem = slotTestToken;

            db.items.AddRange(new[]
            {
                db.pickaxeItem, db.clubItem, db.knifeItem, db.knifeGreenItem, db.knifeBlueItem, db.knifePurpleItem, db.knifeGoldenItem,
                db.spearItem, db.pistolItem, db.rifleItem, db.shotgunItem, db.machinegunItem, db.grenadeItem,
                db.legendaryWeaponItem, db.legendarySpearItem, db.legendaryPistolItem, db.legendaryRifleItem,
                db.legendaryShotgunItem, db.legendaryMachinegunItem,
                db.glovesGray, db.glovesWhite, db.glovesGreen, db.glovesBlue, db.glovesPurple, db.glovesGold,
                rabbitMeat, iguanaMeat, caveLizardMeat, gremlinMeat, salamanderMeat,
                core, caveKey, pebble, ore, gremlinFinder, lizardLure, rabbitFinder, caveLizardFinder, salamanderFinder, phoenixLure, pentachickHeart,
                slotTestToken
            });

            db.monsters.Add(MakeMonster("iguana", "Iguana", 9f, 12f, 0f, 1f, new Color(0.42f, 0.68f, 0.34f), iguanaMeat, 0f, flees: true, fleeOverEdge: true, prefabPath: "Models/Creatures/iguana"));
            db.monsters.Add(MakeMonster("rabbit", "Rabbit", 8f, 13f, 0f, 0.85f, new Color(0.82f, 0.78f, 0.72f), rabbitMeat, 0f, flees: true, fleeOverEdge: true, prefabPath: "Models/Creatures/rabbit"));
            db.monsters.Add(MakeMonster("cave_lizard", "Cave Lizard", 10f, 11f, 5f, 2f, new Color(0.58f, 0.42f, 0.22f), caveLizardMeat, 0f, prefabPath: "Models/Creatures/cave_lizard"));
            db.monsters.Add(MakeMonster("gremlin", "Gremlin", 11f, 12f, 7f, 1.8f, new Color(0.68f, 0.32f, 0.52f), gremlinMeat, 0f, prefabPath: "Models/Creatures/gremlin"));
            db.monsters.Add(MakeMonster("salamander", "Salamander", 22f, 11f, 9f, 2f, new Color(0.78f, 0.36f, 0.16f), salamanderMeat, 0f, prefabPath: "Models/Creatures/salamander"));
            db.monsters.Add(MakeMonster("pentachick", "Pentachick", 50f, 12f, 15f, 1f, new Color(1f, 0.45f, 0.1f), pentachickHeart, 0f, boss: true, prefabPath: "Models/Creatures/pentachick"));
            db.monsters.Add(MakeMonster("exploder", "Rare Exploder", 30f, 11f, 10f, 1.1f, new Color(1f, 0.4f, 0.1f), core, 0f, explodes: true));
            db.monsters.Add(MakeMonster("quest_boss", "Quest Monster", 80f, 11f, 16f, 1.6f, new Color(0.6f, 0.1f, 0.8f), caveKey, 0f, boss: true));

            db.pickaxeUpgrade = MakeUpgrade("pickaxe_dmg", "Pickaxe Upgrade", "Upgrade pickaxe mining power and head color.", UpgradeType.PickaxeDamage, 20, 1f, 5);
            db.inventoryUpgrade = MakeUpgrade("inv_slot", "Inventory Upgrade", "Add one inventory slot.", UpgradeType.InventorySlot, 8, 1f, 7);
            db.waterPurchase = MakeUpgrade("water", "Water", "Refill thirst.", UpgradeType.Water, 4, 100f, int.MaxValue);
            db.knifeMapListing = MakeUpgrade("knife_unlock", "Knife", "Buy a knife weapon.", UpgradeType.WeaponUnlock, 9, 1f, 1, db.knifeItem);
            db.spearListing = MakeUpgrade("spear_shop", "Spear", "Buy a spear.", UpgradeType.WeaponUnlock, 15, 1f, 1, db.spearItem);
            db.pistolListing = MakeUpgrade("pistol_shop", "Pistol", "Buy a pistol.", UpgradeType.WeaponUnlock, 50, 1f, 1, db.pistolItem);
            db.rifleListing = MakeUpgrade("rifle_shop", "Rifle", "Buy a rifle.", UpgradeType.WeaponUnlock, 4000, 1f, 1, db.rifleItem);
            db.shotgunListing = MakeUpgrade("shotgun_shop", "Shotgun", "Buy a shotgun.", UpgradeType.WeaponUnlock, 8000, 1f, 1, db.shotgunItem);
            db.machinegunListing = MakeUpgrade("machinegun_shop", "Machine Gun", "Buy a machine gun.", UpgradeType.WeaponUnlock, 20000, 1f, 1, db.machinegunItem);
            db.grenadeListing = MakeUpgrade("grenade_shop", "Grenade", "Buy a grenade.", UpgradeType.WeaponUnlock, 20, 1f, int.MaxValue, db.grenadeItem);
            db.lizardLureItem = lizardLure;
            db.rabbitFinderItem = rabbitFinder;
            db.caveLizardFinderItem = caveLizardFinder;
            db.salamanderLureItem = salamanderFinder;
            db.phoenixLureItem = phoenixLure;
            db.pentachickHeartItem = pentachickHeart;
            db.caveRatFinderItem = gremlinFinder;
            db.caveRatFinderListing = MakeUpgrade("gremlin_finder_shop", "Gremlin Finder", "Buy a gremlin finder. Locates gremlin eggs.", UpgradeType.WeaponUnlock, 2, 1f, int.MaxValue, gremlinFinder);
            db.lizardLureListing = MakeUpgrade("lizard_lure_shop", "Iguana Finder", "Buy an iguana finder. Locates iguana eggs.", UpgradeType.WeaponUnlock, 0, 1f, int.MaxValue, lizardLure);
            db.rabbitFinderListing = MakeUpgrade("rabbit_finder_shop", "Rabbit Finder", "Buy a rabbit finder. Locates rabbit eggs.", UpgradeType.WeaponUnlock, 0, 1f, int.MaxValue, rabbitFinder);
            db.caveLizardFinderListing = MakeUpgrade("cave_lizard_finder_shop", "Cave Lizard Finder", "Buy a cave lizard finder. Locates cave lizard eggs.", UpgradeType.WeaponUnlock, 1, 1f, int.MaxValue, caveLizardFinder);
            db.salamanderLureListing = MakeUpgrade("salamander_finder_shop", "Salamander Finder", "Buy a salamander finder. Locates salamander eggs.", UpgradeType.WeaponUnlock, 5, 1f, int.MaxValue, salamanderFinder);
            db.phoenixLureListing = MakeUpgrade("phoenix_lure_shop", "Pentachick Finder", "Buy a pentachick finder. Locates pentachick eggs.", UpgradeType.WeaponUnlock, 50, 1f, int.MaxValue, phoenixLure);

            db.shopUpgrades.AddRange(new[] { db.pickaxeUpgrade, db.inventoryUpgrade, db.waterPurchase, db.knifeMapListing, db.caveRatFinderListing, db.lizardLureListing, db.rabbitFinderListing, db.caveLizardFinderListing, db.salamanderLureListing, db.phoenixLureListing });

            return db;

            static ItemDefinition MakeLegendaryWeapon(string baseId, string name, int baseDamage, string iconPath)
            {
                var item = MakeWeapon(
                    $"legendary_{baseId}",
                    $"Legendary {name}",
                    baseDamage,
                    new Color(1f, 0.85f, 0.2f),
                    iconPath,
                    Mathf.Max(10, baseDamage));
                item.isLegendary = true;
                return item;
            }

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

            static ItemDefinition MakeLegendaryKnife(string id, string name, int baseDamage, Color goldColor)
            {
                var item = MakeKnife(id, name, baseDamage, goldColor);
                item.isLegendary = true;
                item.sellValue = 50;
                return item;
            }

            static ItemDefinition MakeWeapon(string id, string name, int damage, Color color, string iconPath, int sell = 5)
            {
                var item = MakeItem(id, name, ItemCategory.Weapon, sell, color, false);
                item.weaponDamage = damage;
                item.iconResourcePath = iconPath;
                return item;
            }

            static ItemDefinition MakeMeat(string id, int sell, string iconPath)
            {
                var meat = MakeItem(id, "Monster Meat", ItemCategory.Drop, sell, new Color(0.85f, 0.25f, 0.2f), true);
                meat.isEdible = true;
                meat.iconResourcePath = iconPath;
                return meat;
            }

            static ItemDefinition MakeFinder(
                string id,
                string name,
                int sell,
                Color color,
                string targetCreatureId,
                EggFinderRarity rarity,
                int locateMin,
                int locateMax)
            {
                var item = MakeItem(id, name, ItemCategory.Misc, sell, color, false);
                item.stackLimit = 99;
                item.isEggFinder = true;
                item.finderTargetCreatureId = targetCreatureId;
                item.finderRarity = rarity;
                item.finderLocateMin = locateMin;
                item.finderLocateMax = locateMax;
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
                item.stackLimit = 1;
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
                bool flees = false,
                bool fleeOverEdge = false,
                string prefabPath = null)
            {
                var m = CreateInstance<MonsterDefinition>();
                m.monsterId = id;
                m.displayName = name;
                m.maxHealth = hp;
                m.moveSpeedMph = speed;
                m.attackDamage = dmg;
                m.scale = scale;
                m.bodyColor = color;
                m.dropItem = drop;
                m.spawnWeight = weight;
                m.explodesOnDeath = explodes;
                m.isQuestBoss = boss;
                m.fleesFromPlayer = flees;
                m.fleesOverPlateauEdge = fleeOverEdge;
                m.visualPrefabResourcePath = prefabPath;
                if (boss && drop != null)
                    drop.isBossDrop = true;
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
