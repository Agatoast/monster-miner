using UnityEngine;

namespace MonsterMiner.Util
{
    public static class MapWaterColorSampler
    {
        static readonly Color32 MapWaterColor = new Color32(140, 200, 230, 255);

        public static Color32 Sample(float localX, float localZ) => MapWaterColor;
    }
}
