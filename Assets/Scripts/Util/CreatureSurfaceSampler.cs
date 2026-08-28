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

            if (PlateauBoundary.IsOnPlateau(localX, localZ, bounds.Radius))
                return bounds.SampleFloorWorldY(localX, localZ);

            float angle = Mathf.Atan2(localZ, localX);
            float distance = new Vector2(localX, localZ).magnitude;
            float edgeDistance = PlateauBoundary.SamplePlateauEdgeDistance(angle, bounds.Radius);
            if (distance <= edgeDistance + WorldScale.Feet(0.5f))
                return bounds.SampleFloorWorldY(localX, localZ);

            float wallBase = PlateauWallGeometry.GetWallBaseOutwardRadius(angle, bounds.Radius);
            float lowerBase = LowerWorldBuilder.GetLowerGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            if (distance >= wallBase - WorldScale.Feet(0.5f))
            {
                float lowerLocalY = LowerWorldBuilder.SampleLowerPlainsLocalY(localX, localZ, lowerBase);
                return bounds.transform.TransformPoint(new Vector3(localX, lowerLocalY, localZ)).y;
            }

            float outwardSpan = Mathf.Max(WorldScale.Feet(1f), wallBase - edgeDistance);
            float descentT = Mathf.Clamp01((distance - edgeDistance) / outwardSpan);
            float edgeX = Mathf.Cos(angle) * edgeDistance;
            float edgeZ = Mathf.Sin(angle) * edgeDistance;
            float topLocalY = bounds.SampleFloorLocalY(edgeX, edgeZ);
            float bottomLocalY = LowerWorldBuilder.SampleLowerPlainsLocalY(localX, localZ, lowerBase);
            float localY = Mathf.Lerp(topLocalY, bottomLocalY, descentT);
            return bounds.transform.TransformPoint(new Vector3(localX, localY, localZ)).y;
        }
    }
}
