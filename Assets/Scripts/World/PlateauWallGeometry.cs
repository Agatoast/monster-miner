using UnityEngine;

namespace MonsterMiner.World
{
    public static class PlateauWallGeometry
    {
        public static float CliffHeightUnits => WorldScale.Feet(WorldScale.PlateauCliffHeightFeet);

        public static float SampleOutwardOffsetFeet(float depthFeet)
        {
            depthFeet = Mathf.Clamp(depthFeet, 0f, WorldScale.PlateauCliffHeightFeet);
            if (depthFeet <= 0.001f)
                return 0f;

            float degreesPerFoot = WorldScale.PlateauWallTaperDegreesPerHundredFeet / 100f;
            float angleRad = depthFeet * degreesPerFoot * Mathf.Deg2Rad;
            float k = degreesPerFoot * Mathf.Deg2Rad;
            return -Mathf.Log(Mathf.Cos(angleRad)) / k;
        }

        public static float SampleOutwardOffset(float descentT)
        {
            descentT = Mathf.Clamp01(descentT);
            float depthFeet = descentT * WorldScale.PlateauCliffHeightFeet;
            return WorldScale.Feet(SampleOutwardOffsetFeet(depthFeet));
        }

        public static float MaxOutwardOffset => SampleOutwardOffset(1f);

        public static float GetWallBaseOutwardRadius(float angle, float quarryNominalRadius)
        {
            float edge = PlateauBoundary.SamplePlateauEdgeDistance(angle, quarryNominalRadius);
            return edge + MaxOutwardOffset;
        }

        public static float SampleWallAngleDegrees(float descentT)
        {
            descentT = Mathf.Clamp01(descentT);
            return descentT * WorldScale.PlateauCliffHeightFeet
                / 100f * WorldScale.PlateauWallTaperDegreesPerHundredFeet;
        }
    }
}
