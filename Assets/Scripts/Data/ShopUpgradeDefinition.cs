using UnityEngine;

namespace MonsterMiner.Data
{
    public enum UpgradeType
    {
        PickaxeDamage,
        WeaponDamage,
        MaxHealth,
        InventorySlot,
        MapExpansion,
        WeaponUnlock,
        GloveUpgrade,
        Water
    }

    [CreateAssetMenu(fileName = "ShopUpgrade", menuName = "Monster Miner/Shop Upgrade")]
    public class ShopUpgradeDefinition : ScriptableObject
    {
        public string upgradeId;
        public string displayName;
        public string description;
        public UpgradeType upgradeType;
        public int cost = 5;
        public float value = 1f;
        public int maxPurchases = 5;
        public ItemDefinition unlockItem;
        public string boardVisualResourcePath;
        public float boardVisualScreenYOffsetPixels;
    }
}
