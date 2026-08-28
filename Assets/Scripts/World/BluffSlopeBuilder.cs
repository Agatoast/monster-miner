using UnityEngine;

namespace MonsterMiner.World
{
    public static class BluffSlopeBuilder
    {
        public static float GetBluffBaseOutwardRadius(float angle, float quarryNominalRadius)
        {
            return PlateauWallGeometry.GetWallBaseOutwardRadius(angle, quarryNominalRadius);
        }

        public static float SampleOutwardOffset(float descentT)
        {
            return PlateauWallGeometry.SampleOutwardOffset(descentT);
        }
    }
}
