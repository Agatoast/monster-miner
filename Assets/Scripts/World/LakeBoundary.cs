using UnityEngine;

namespace MonsterMiner.World
{
    public static class LakeBoundary
    {
        public static float SampleEdgeDistance(float angleRadians) =>
            LakeCatalog.GetNominalRadiusUnits();

        public static bool ContainsLocal(float localX, float localZ)
        {
            var center = LakeCatalog.GetCenterLocal();
            float dx = localX - center.x;
            float dz = localZ - center.y;
            float distanceSq = dx * dx + dz * dz;
            float radius = LakeCatalog.GetNominalRadiusUnits();
            return distanceSq <= radius * radius;
        }

        public static bool IsBeachLocal(float localX, float localZ)
        {
            if (!IsWithinBeachLength(localX))
                return false;

            float south = LakeCatalog.GetBeachSouthEdgeZ();
            float north = LakeCatalog.GetBeachNorthEdgeZ() + WorldScale.Feet(30f);
            return localZ >= south && localZ <= north;
        }

        static bool IsWithinBeachLength(float contentX)
        {
            var beachCenter = LakeCatalog.GetBeachCenterContentLocal();
            float halfLength = WorldScale.Feet(LakeCatalog.BeachHalfLengthFeet);
            return Mathf.Abs(contentX - beachCenter.x) <= halfLength;
        }
    }
}
