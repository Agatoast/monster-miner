using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class CreatureSurfaceSampler
    {
        public static float SampleWorldY(CavernBounds bounds, float localX, float localZ)
        {
            if (bounds == null)
                return 0f;

            if (LandQuarry3Boundary.ContainsLocal(localX, localZ))
                return SampleQuarryBowlWorldY(bounds, QuarryCatalog.GetLandQuarry3Center());

            if (LandQuarry4Boundary.ContainsLocal(localX, localZ))
                return SampleQuarryBowlWorldY(bounds, QuarryCatalog.GetLandQuarry4Center());

            if (LandQuarry2Boundary.IsSnowGroundLocal(localX, localZ))
            {
                float plainsBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
                float snowLocalY = LandQuarry2Boundary.SampleSnowFloorLocalY(localX, localZ, plainsBase);
                return bounds.transform.TransformPoint(new Vector3(localX, snowLocalY, localZ)).y;
            }

            if (LakeCatalog.IsLakeIslandLocal(localX, localZ)
                && LakeIslandVisualFactory.TrySampleWorldY(localX, localZ, bounds.transform, out float islandWorldY))
            {
                return islandWorldY;
            }

            if (PlateauBoundary.IsOnPlateau(localX, localZ, bounds.Radius))
                return bounds.SamplePlateauFloorWorldY(localX, localZ);

            float angle = Mathf.Atan2(localZ, localX);
            float distance = new Vector2(localX, localZ).magnitude;
            float edgeDistance = PlateauBoundary.SamplePlateauEdgeDistance(angle, bounds.Radius);
            if (distance <= edgeDistance + WorldScale.Feet(0.5f))
                return bounds.SamplePlateauFloorWorldY(localX, localZ);

            float wallBase = PlateauWallGeometry.GetWallBaseOutwardRadius(angle, bounds.Radius);
            float lowerBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            if (distance >= wallBase - WorldScale.Feet(0.5f))
            {
                float plainsLocalY = PlainsWorldBuilder.SamplePlainsLocalY(localX, localZ, lowerBase);
                return bounds.transform.TransformPoint(new Vector3(localX, plainsLocalY, localZ)).y;
            }

            float outwardSpan = Mathf.Max(WorldScale.Feet(1f), wallBase - edgeDistance);
            float descentT = Mathf.Clamp01((distance - edgeDistance) / outwardSpan);
            float edgeX = Mathf.Cos(angle) * edgeDistance;
            float edgeZ = Mathf.Sin(angle) * edgeDistance;
            float topLocalY = bounds.SamplePlateauFloorLocalY(edgeX, edgeZ);
            float bottomLocalY = PlainsWorldBuilder.SamplePlainsLocalY(localX, localZ, lowerBase);
            float localY = Mathf.Lerp(topLocalY, bottomLocalY, descentT);
            return bounds.transform.TransformPoint(new Vector3(localX, localY, localZ)).y;
        }

        static float SampleQuarryBowlWorldY(CavernBounds bounds, Vector2 center)
        {
            float plainsBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(center.x, center.y, plainsBase);
            return bounds.transform.TransformPoint(new Vector3(center.x, groundY, center.y)).y;
        }

        public static bool IsCreatureGroundLocal(CavernBounds bounds, float localX, float localZ)
        {
            if (LakeCatalog.IsLakeIslandLocal(localX, localZ))
                return true;

            if (LakeCatalog.IsOpenWaterLocal(localX, localZ))
                return false;

            if (LakeCatalog.IsLakeLocal(localX, localZ))
                return false;

            return true;
        }
    }
}
