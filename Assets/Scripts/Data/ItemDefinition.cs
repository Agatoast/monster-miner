using UnityEngine;

namespace MonsterMiner.Data
{
    public enum ItemCategory { Ore, Drop, Tool, Weapon, Key, Misc, Gloves }

    [CreateAssetMenu(fileName = "Item", menuName = "Monster Miner/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        public string itemId;
        public string displayName;
        public ItemCategory category = ItemCategory.Misc;
        public int sellValue = 1;
        public int stackLimit = 99;
        public Color worldColor = Color.white;
        public bool isMonsterDrop;
        public bool isEdible;
        public bool isMiningGlove;
        public int miningBonus;
        public int weaponDamage;
        public bool canBeSold = true;
        public string iconResourcePath;
    }
}
