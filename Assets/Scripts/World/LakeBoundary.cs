using UnityEngine;

namespace MonsterMiner.World
{
    public static class LakeBoundary
    {
        public const int SampleCount = 72;
        const float NoiseScaleA = 0.18f;
        const float NoiseScaleB = 0.52f;
        const float NoiseScaleC = 1.35f;
        const float NoiseAmplitudeA = 0.11f;
        const float NoiseAmplitudeB = 0.06f;
        const float NoiseAmplitudeC = 0.03f;
        const float NoiseSeedX = 17.31f;
        const float NoiseSeedY = 83.62f;

        public static float SampleEdgeDistance(float angleRadians)
        {
            float baseRadius = LakeCatalog.GetNominalRadiusUnits();
            float nx = Mathf.Cos(angleRadians);
            float ny = Mathf.Sin(angleRadians);

            float n1 = Mathf.PerlinNoise(nx * NoiseScaleA + NoiseSeedX, ny * NoiseScaleA + NoiseSeedY);
            float n2 = Mathf.PerlinNoise(nx * NoiseScaleB + NoiseSeedX * 1.7f, ny * NoiseScaleB + NoiseSeedY * 1.3f);
            float n3 = Mathf.PerlinNoise(nx * NoiseScaleC + NoiseSeedX * 2.9f, ny * NoiseScaleC + NoiseSeedY * 2.1f);
            float distortion = (n1 - 0.5f) * NoiseAmplitudeA
                + (n2 - 0.5f) * NoiseAmplitudeB
                + (n3 - 0.5f) * NoiseAmplitudeC;

            return baseRadius * (1f + distortion);
        }

        public static bool ContainsLocal(float localX, float localZ)
        {
            var center = LakeCatalog.GetCenterLocal();
            float dx = localX - center.x;
            float dz = localZ - center.y;
            float distance = new Vector2(dx, dz).magnitude;
            if (distance < 0.001f)
                return true;

            float angle = Mathf.Atan2(dz, dx);
            return distance <= SampleEdgeDistance(angle);
        }

        public static bool IsBeachLocal(float localX, float localZ)
        {
            if (!IsWithinBeachLength(localX))
                return false;

            float south = LakeCatalog.GetBeachSouthEdgeZ();
            float north = LakeCatalog.GetBeachNorthEdgeZ();
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
