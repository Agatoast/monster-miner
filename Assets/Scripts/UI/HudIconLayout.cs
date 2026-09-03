using UnityEngine;

namespace MonsterMiner.UI
{
    public static class HudIconLayout
    {
        public const float IconSize = 56f;
        public const float TopMargin = 12f;
        public const float IconSpacing = 10f;

        public static float HeartX => Screen.width * 0.5f - IconSize * 0.5f;
        public static float HeartY => TopMargin;
        public static float ThirstX => HeartX + IconSize + IconSpacing;
        public static float ThirstY => HeartY;
        public static float CompassX => ThirstX + IconSize + IconSpacing;
        public static float CompassY => HeartY;

        public static float MagicCompassCenterX => ThirstX + IconSize * 0.5f;

        public static float MagicCompassCenterY(float arrowInnerRadius) =>
            ThirstY + IconSize + IconSpacing + arrowInnerRadius;
    }
}
