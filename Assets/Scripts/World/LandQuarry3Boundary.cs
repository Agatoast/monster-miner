using UnityEngine;

namespace MonsterMiner.World
{
    public static class LandQuarry3Boundary
    {
        public const float QuarryRadiusFeet = 160f;

        public static float QuarryRadius => WorldScale.Feet(QuarryRadiusFeet);

        public static bool ContainsLocal(float localX, float localZ)
        {
            var center = QuarryCatalog.GetLandQuarry3Center();
            float dx = localX - center.x;
            float dz = localZ - center.y;
            return new Vector2(dx, dz).sqrMagnitude <= QuarryRadius * QuarryRadius;
        }

        public static float SampleEdgeDistance(float angleRadians) => QuarryRadius;
    }
}
