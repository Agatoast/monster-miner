using UnityEngine;

namespace MonsterMiner.Data
{
    public static class ItemSkinColorRules
    {
        public static readonly Color LegendaryWeaponGold = new Color(1f, 0.85f, 0.2f);

        const float MaxChannelDelta = 0.1f;

        public static Color SanitizeSkinPreviewColor(Color color)
        {
            return LooksLikeLegendaryWeaponGold(color)
                ? new Color(0.82f, 0.62f, 0.14f)
                : color;
        }

        public static bool LooksLikeLegendaryWeaponGold(Color color)
        {
            return Mathf.Abs(color.r - LegendaryWeaponGold.r) <= MaxChannelDelta
                && Mathf.Abs(color.g - LegendaryWeaponGold.g) <= MaxChannelDelta
                && Mathf.Abs(color.b - LegendaryWeaponGold.b) <= MaxChannelDelta;
        }
    }
}
