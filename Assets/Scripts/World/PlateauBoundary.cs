using UnityEngine;

namespace MonsterMiner.World
{
    public static class PlateauBoundary
    {
        const float PlateauNoiseScale = 0.17f;

        public static float SamplePlateauEdgeDistance(float angleRadians, float nominalPlateauRadius)
        {
            float u = Mathf.Cos(angleRadians) * PlateauNoiseScale + 0.11f;
            float v = Mathf.Sin(angleRadians) * PlateauNoiseScale + 0.67f;
            float n = Mathf.PerlinNoise(u, v);
            float n2 = Mathf.PerlinNoise(u * 2.7f + 4.3f, v * 2.7f + 1.2f);
            float n3 = Mathf.PerlinNoise(u * 5.1f + 1.5f, v * 5.1f + 3.8f);
            float blend = n * 0.5f + n2 * 0.35f + n3 * 0.15f;
            float minEdge = nominalPlateauRadius - WorldScale.Feet(WorldScale.PlateauEdgeVariationFeet);
            float maxEdge = nominalPlateauRadius + WorldScale.Feet(WorldScale.PlateauEdgeVariationFeet);
            return Mathf.Lerp(minEdge, maxEdge, blend);
        }

        public static float SampleBarrierDistance(float angleRadians, float nominalPlateauRadius)
        {
            return SamplePlateauEdgeDistance(angleRadians, nominalPlateauRadius)
                - WorldScale.Feet(WorldScale.EdgeBarrierInsetFeet);
        }

        public static bool IsOnPlateau(float localX, float localZ, float nominalPlateauRadius)
        {
            float distance = new Vector2(localX, localZ).magnitude;
            float angle = Mathf.Atan2(localZ, localX);
            return distance <= SamplePlateauEdgeDistance(angle, nominalPlateauRadius);
        }

        public static float GetDistanceToBarrier(float localX, float localZ, float nominalPlateauRadius)
        {
            float distance = new Vector2(localX, localZ).magnitude;
            if (distance < 0.001f)
                return float.MaxValue;

            float angle = Mathf.Atan2(localZ, localX);
            return SampleBarrierDistance(angle, nominalPlateauRadius) - distance;
        }

        public static bool IsNearPlateauEdge(
            float localX,
            float localZ,
            float nominalPlateauRadius,
            float warningDistance)
        {
            if (!IsOnPlateau(localX, localZ, nominalPlateauRadius))
                return false;

            return GetDistanceToBarrier(localX, localZ, nominalPlateauRadius) <= warningDistance;
        }

        public static bool TryClampToBarrier(
            float localX,
            float localZ,
            float nominalPlateauRadius,
            out Vector2 clamped)
        {
            float distance = new Vector2(localX, localZ).magnitude;
            if (distance < 0.001f)
            {
                clamped = Vector2.zero;
                return false;
            }

            float angle = Mathf.Atan2(localZ, localX);
            float maxDistance = SampleBarrierDistance(angle, nominalPlateauRadius);
            if (distance <= maxDistance)
            {
                clamped = new Vector2(localX, localZ);
                return false;
            }

            clamped = new Vector2(localX, localZ).normalized * maxDistance;
            return true;
        }

        public static float MaxExtent(float nominalPlateauRadius)
        {
            return nominalPlateauRadius + WorldScale.Feet(WorldScale.PlateauEdgeVariationFeet) + WorldScale.Feet(6f);
        }
    }
}
