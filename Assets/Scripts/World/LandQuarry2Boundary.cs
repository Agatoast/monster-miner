using UnityEngine;

namespace MonsterMiner.World
{
    public static class LandQuarry2Boundary
    {
        public const int SampleStepDegrees = 10;
        public const float MinEdgeFeet = 278f;
        public const float MaxEdgeFeet = 423f;

        const int Quarry2BoundarySeed = 4283;

        static float[] edgeRadii;
        static bool radiiInitialized;

        public static float MaxEdgeDistance => WorldScale.Feet(MaxEdgeFeet);
        public const float SnowFloorLocalYOffsetFeet = 0.35f;
        public static float SnowFloorLocalYOffset => WorldScale.Feet(SnowFloorLocalYOffsetFeet);

        public static float SampleSnowFloorLocalY(float localX, float localZ, float plainsBaseLocalY)
        {
            return PlainsWorldBuilder.SamplePlainsLocalY(localX, localZ, plainsBaseLocalY) + SnowFloorLocalYOffset;
        }

        public static float SampleEdgeDistance(float angleRadians)
        {
            EnsureRadii();

            float turns = angleRadians / (Mathf.PI * 2f);
            turns -= Mathf.Floor(turns);
            float sampleIndex = turns * edgeRadii.Length;
            int i0 = Mathf.FloorToInt(sampleIndex) % edgeRadii.Length;
            int i1 = (i0 + 1) % edgeRadii.Length;
            int iPrev = (i0 - 1 + edgeRadii.Length) % edgeRadii.Length;
            int iNext = (i0 + 2) % edgeRadii.Length;
            float t = sampleIndex - Mathf.Floor(sampleIndex);
            return CatmullRom(edgeRadii[iPrev], edgeRadii[i0], edgeRadii[i1], edgeRadii[iNext], t);
        }

        public static bool ContainsLocal(float localX, float localZ)
        {
            var center = QuarryCatalog.GetLandQuarry2Center();
            float dx = localX - center.x;
            float dz = localZ - center.y;
            float distance = new Vector2(dx, dz).magnitude;
            if (distance < 0.001f)
                return true;

            float angle = Mathf.Atan2(dz, dx);
            return distance <= SampleEdgeDistance(angle);
        }

        public static Vector2 GetEdgeLocalPoint(float angleRadians)
        {
            var center = QuarryCatalog.GetLandQuarry2Center();
            float radius = SampleEdgeDistance(angleRadians);
            return center + new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians)) * radius;
        }

        public static Vector2 GetCenterLocal() => QuarryCatalog.GetLandQuarry2Center();

        static void EnsureRadii()
        {
            if (radiiInitialized)
                return;

            int count = 360 / SampleStepDegrees;
            edgeRadii = new float[count];
            var rng = new System.Random(Quarry2BoundarySeed);
            for (int i = 0; i < count; i++)
            {
                float feet = MinEdgeFeet + (float)rng.NextDouble() * (MaxEdgeFeet - MinEdgeFeet);
                edgeRadii[i] = WorldScale.Feet(feet);
            }

            radiiInitialized = true;
        }

        static float CatmullRom(float p0, float p1, float p2, float p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                2f * p1
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
