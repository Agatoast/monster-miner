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

            if (QuarryCatalog.IsLandQuarry2Local(localX, localZ))
            {
                float plainsBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
                float snowLocalY = LandQuarry2Boundary.SampleSnowFloorLocalY(localX, localZ, plainsBase);
                return bounds.transform.TransformPoint(new Vector3(localX, snowLocalY, localZ)).y;
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
    }
}
