using System.Collections.Generic;
using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Player;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Inventory
{
    public class ItemSkinCollection : MonoBehaviour
    {
        readonly Dictionary<string, HashSet<string>> unlockedSkinsByTarget = new();
        readonly Dictionary<string, string> equippedSkinByTarget = new();
        readonly List<ItemSkinDefinition> catalog = new();

        public IReadOnlyList<ItemSkinDefinition> Catalog => catalog;

        public void Initialize(GameDatabase database)
        {
            catalog.Clear();
            unlockedSkinsByTarget.Clear();
            equippedSkinByTarget.Clear();
            RegisterDefaultItemSkins(database);
            RegisterDefaultTruckSkins();
        }

        public bool IsUnlocked(ItemSkinDefinition skin)
        {
            if (skin == null || string.IsNullOrEmpty(skin.targetItemId) || string.IsNullOrEmpty(skin.skinId))
                return false;

            return unlockedSkinsByTarget.TryGetValue(skin.targetItemId, out var unlocked)
                && unlocked.Contains(skin.skinId);
        }

        public bool IsAvailable(ItemSkinDefinition skin)
        {
            if (skin == null)
                return false;

            if (string.IsNullOrEmpty(skin.requiredMapId))
                return true;

            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null)
                return false;

            if (skin.requiredMapId == MapSpawnCatalog.Cave2MapId)
                return progression.IsCave2Unlocked;

            return true;
        }

        public bool TryUnlock(ItemSkinDefinition skin)
        {
            if (skin == null || string.IsNullOrEmpty(skin.targetItemId) || string.IsNullOrEmpty(skin.skinId))
                return false;

            if (!unlockedSkinsByTarget.TryGetValue(skin.targetItemId, out var unlocked))
            {
                unlocked = new HashSet<string>();
                unlockedSkinsByTarget.Add(skin.targetItemId, unlocked);
            }

            return unlocked.Add(skin.skinId);
        }

        public string GetEquippedSkinId(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
                return null;

            return equippedSkinByTarget.TryGetValue(targetId, out var skinId) ? skinId : null;
        }

        public string GetEquippedTruckSkinId() => GetEquippedSkinId(PlayerTruckIds.DefaultTruckId);

        public bool TrySetEquippedSkin(string targetId, string skinId)
        {
            if (string.IsNullOrEmpty(targetId) || string.IsNullOrEmpty(skinId))
                return false;

            var skin = FindSkin(targetId, skinId);
            if (skin == null || !IsUnlocked(skin) || !IsAvailable(skin))
                return false;

            equippedSkinByTarget[targetId] = skinId;
            return true;
        }

        public bool TrySetEquippedTruckSkin(string skinId) =>
            TrySetEquippedSkin(PlayerTruckIds.DefaultTruckId, skinId);

        public bool TryGrantRandomSkin(out ItemSkinDefinition grantedSkin)
        {
            grantedSkin = null;
            var locked = new List<ItemSkinDefinition>();
            foreach (var skin in catalog)
            {
                if (skin != null && IsAvailable(skin) && !IsUnlocked(skin))
                    locked.Add(skin);
            }

            if (locked.Count == 0)
                return false;

            grantedSkin = locked[Random.Range(0, locked.Count)];
            TryUnlock(grantedSkin);
            return true;
        }

        public ItemSkinDefinition FindSkin(string targetId, string skinId)
        {
            foreach (var skin in catalog)
            {
                if (skin != null && skin.targetItemId == targetId && skin.skinId == skinId)
                    return skin;
            }

            return null;
        }

        public IReadOnlyList<ItemSkinDefinition> GetUnlockedSkinsForTarget(string targetId)
        {
            var results = new List<ItemSkinDefinition>();
            if (string.IsNullOrEmpty(targetId))
                return results;

            foreach (var skin in catalog)
            {
                if (skin != null
                    && skin.targetItemId == targetId
                    && IsUnlocked(skin)
                    && IsAvailable(skin))
                {
                    results.Add(skin);
                }
            }

            return results;
        }

        public IReadOnlyList<ItemSkinDefinition> GetUnlockedTruckSkins() =>
            GetUnlockedSkinsForTarget(PlayerTruckIds.DefaultTruckId);

        public string ResolveTargetDisplayName(ItemSkinDefinition skin, GameDatabase database)
        {
            if (skin == null)
                return "item";

            if (skin.targetType == SkinTargetType.Vehicle
                && skin.targetItemId == PlayerTruckIds.DefaultTruckId)
            {
                return PlayerTruckIds.DisplayName;
            }

            if (database?.items == null || string.IsNullOrEmpty(skin.targetItemId))
                return skin.targetItemId ?? "item";

            foreach (var item in database.items)
            {
                if (item != null && item.itemId == skin.targetItemId)
                    return item.displayName;
            }

            return skin.targetItemId;
        }

        void RegisterDefaultItemSkins(GameDatabase database)
        {
            if (database == null)
                return;

            RegisterItemSkinPack(database.knifeItem, new[]
            {
                ("skin_emerald", "Emerald", new Color(0.2f, 0.85f, 0.45f)),
                ("skin_ruby", "Ruby", new Color(0.85f, 0.15f, 0.2f)),
                ("skin_azure", "Azure", new Color(0.2f, 0.55f, 0.95f)),
                ("skin_shadow", "Shadow", new Color(0.15f, 0.15f, 0.18f)),
                ("skin_neon", "Neon", new Color(0.95f, 0.2f, 0.95f))
            });

            RegisterItemSkinPack(database.pickaxeItem, new[]
            {
                ("skin_copper", "Copper", new Color(0.78f, 0.42f, 0.22f)),
                ("skin_silver", "Silver", new Color(0.78f, 0.8f, 0.86f)),
                ("skin_brass", "Brass", new Color(0.82f, 0.62f, 0.14f)),
                ("skin_obsidian", "Obsidian", new Color(0.12f, 0.1f, 0.14f))
            });

            RegisterItemSkinPack(database.spearItem, new[]
            {
                ("skin_frost", "Frost", new Color(0.65f, 0.85f, 1f)),
                ("skin_blood", "Blood", new Color(0.75f, 0.1f, 0.12f))
            });

            RegisterItemSkinPack(database.pistolItem, new[]
            {
                ("skin_chrome", "Chrome", new Color(0.82f, 0.84f, 0.88f)),
                ("skin_carbon", "Carbon", new Color(0.12f, 0.12f, 0.14f))
            });

            RegisterItemSkinPack(database.rifleItem, new[]
            {
                ("skin_desert", "Desert", new Color(0.72f, 0.62f, 0.38f)),
                ("skin_arctic", "Arctic", new Color(0.82f, 0.9f, 0.96f))
            });

            RegisterItemSkinPack(database.shotgunItem, new[]
            {
                ("skin_rust", "Rust", new Color(0.62f, 0.28f, 0.14f)),
                ("skin_olive", "Olive", new Color(0.34f, 0.42f, 0.18f))
            });

            RegisterItemSkinPack(database.machinegunItem, new[]
            {
                ("skin_steel", "Steel", new Color(0.55f, 0.58f, 0.62f)),
                ("skin_crimson", "Crimson", new Color(0.72f, 0.08f, 0.12f))
            });
        }

        void RegisterDefaultTruckSkins()
        {
            RegisterVehicleSkinPack(PlayerTruckIds.DefaultTruckId, MapSpawnCatalog.Cave2MapId, new[]
            {
                ("truck_desert_hauler", "Desert Hauler", new Color(0.72f, 0.58f, 0.32f)),
                ("truck_midnight_rig", "Midnight Rig", new Color(0.12f, 0.14f, 0.2f)),
                ("truck_flame_wrap", "Flame Wrap", new Color(0.92f, 0.34f, 0.08f)),
                ("truck_moss_runner", "Moss Runner", new Color(0.28f, 0.46f, 0.22f)),
                ("truck_chrome_dream", "Chrome Dream", new Color(0.78f, 0.82f, 0.88f))
            });
        }

        void RegisterItemSkinPack(ItemDefinition item, (string id, string name, Color color)[] skins)
        {
            if (item == null || skins == null)
                return;

            for (int i = 0; i < skins.Length; i++)
            {
                var entry = skins[i];
                var skin = ScriptableObject.CreateInstance<ItemSkinDefinition>();
                skin.skinId = entry.id;
                skin.targetItemId = item.itemId;
                skin.targetType = SkinTargetType.Item;
                skin.displayName = entry.name;
                skin.previewColor = ItemSkinColorRules.SanitizeSkinPreviewColor(entry.color);
                if (!string.IsNullOrEmpty(item.iconResourcePath))
                    skin.iconResourcePath = item.iconResourcePath;
                catalog.Add(skin);
            }
        }

        void RegisterVehicleSkinPack(
            string vehicleId,
            string requiredMapId,
            (string id, string name, Color color)[] skins)
        {
            if (string.IsNullOrEmpty(vehicleId) || skins == null)
                return;

            for (int i = 0; i < skins.Length; i++)
            {
                var entry = skins[i];
                var skin = ScriptableObject.CreateInstance<ItemSkinDefinition>();
                skin.skinId = entry.id;
                skin.targetItemId = vehicleId;
                skin.targetType = SkinTargetType.Vehicle;
                skin.requiredMapId = requiredMapId;
                skin.displayName = entry.name;
                skin.previewColor = ItemSkinColorRules.SanitizeSkinPreviewColor(entry.color);
                catalog.Add(skin);
            }
        }
    }
}
