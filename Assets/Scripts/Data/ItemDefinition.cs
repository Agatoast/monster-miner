using UnityEngine;

namespace MonsterMiner.Data
{
    public enum ItemCategory { Ore, Drop, Tool, Weapon, Key, Misc, Gloves }

    public enum EggFinderRarity { Common, Uncommon, Rare }

    [CreateAssetMenu(fileName = "Item", menuName = "Monster Miner/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        public string itemId;
        public string displayName;
        public ItemCategory category = ItemCategory.Misc;
        public int sellValue = 1;
        public int stackLimit = 1;
        public Color worldColor = Color.white;
        public bool isMonsterDrop;
        public bool isEdible;
        public bool isMiningGlove;
        public int miningBonus;
        public int weaponDamage;
        public bool isLegendary;
        public bool canBeSold = true;
        public string iconResourcePath;
        public bool isEggFinder;
        public string finderTargetCreatureId;
        public EggFinderRarity finderRarity;
        public int finderLocateMin = 1;
        public int finderLocateMax = 1;
        public bool isSlotTestToken;
        public bool isBossDrop;
    }
}
