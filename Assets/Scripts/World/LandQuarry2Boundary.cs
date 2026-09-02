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

        public static bool IsSnowGroundLocal(float localX, float localZ)
        {
            if (ContainsLocal(localX, localZ))
                return true;

            if (LakeCatalog.IsBeachLocal(localX, localZ))
                return false;

            return IsLakeApproachLandLocal(localX, localZ);
        }

        public static bool IsLakeApproachLandLocal(float localX, float localZ)
        {
            if (ContainsLocal(localX, localZ))
                return false;

            if (LakeBoundary.IsBeachLocal(localX, localZ))
                return true;

            if (LakeBoundary.ContainsLocal(localX, localZ))
                return false;

            var lakeCenter = LakeCatalog.GetCenterLocal();
            float lakeRadius = LakeCatalog.GetNominalRadiusUnits();
            if (Mathf.Abs(localX - lakeCenter.x) > lakeRadius)
                return false;

            float beachSouth = LakeCatalog.GetBeachSouthEdgeZ();
            if (localZ >= beachSouth - WorldScale.Feet(0.5f))
                return true;

            float lakeSouthZ = SampleLakeSouthShoreLocalZ(localX);
            if (localZ > lakeSouthZ + WorldScale.Feet(1f))
                return false;

            float jarlNorthZ = SampleJarlNorthLocalZAtX(localX);
            return jarlNorthZ > float.NegativeInfinity
                && localZ >= jarlNorthZ - WorldScale.Feet(0.5f);
        }

        public static float SampleLakeSouthShoreLocalZ(float localX)
        {
            var center = LakeCatalog.GetCenterLocal();
            float dx = localX - center.x;
            float radius = LakeCatalog.GetNominalRadiusUnits();
            if (Mathf.Abs(dx) >= radius)
                return center.y;

            return center.y - Mathf.Sqrt(radius * radius - dx * dx);
        }

        public static float SampleJarlNorthLocalZAtX(float localX)
        {
            float halfCell = WorldScale.Feet(18f);
            float bestZ = float.NegativeInfinity;
            int steps = 360 / SampleStepDegrees;
            for (int i = 0; i < steps; i++)
            {
                float angle = i * SampleStepDegrees * Mathf.Deg2Rad;
                var edge = GetEdgeLocalPoint(angle);
                if (Mathf.Abs(edge.x - localX) <= halfCell)
                    bestZ = Mathf.Max(bestZ, edge.y);
            }

            return bestZ;
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
