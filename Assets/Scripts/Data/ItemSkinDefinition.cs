using UnityEngine;

namespace MonsterMiner.Data
{
    [CreateAssetMenu(fileName = "ItemSkin", menuName = "Monster Miner/Item Skin")]
    public class ItemSkinDefinition : ScriptableObject
    {
        public string skinId;
        public string targetItemId;
        public SkinTargetType targetType = SkinTargetType.Item;
        public string requiredMapId;
        public string displayName;
        public Color previewColor = Color.white;
        public string iconResourcePath;
        public string visualPrefabResourcePath;
    }
}
