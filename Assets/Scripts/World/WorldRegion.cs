using UnityEngine;

namespace MonsterMiner.World
{
    public static class WorldRegion
    {
        public const float LandRadiusMiles = 10f;

        const float LandInnerMarginFeet = 20f;

        public static bool IsQuarryLocal(CavernBounds bounds, float localX, float localZ)
        {
            if (bounds == null)
                return false;

            if (bounds.IsOnPlateauLocal(localX, localZ))
                return true;

            if (bounds.IsInCave2ZoneLocal(localX, localZ))
                return true;

            return QuarryCatalog.IsLandQuarry2Local(localX, localZ)
                || QuarryCatalog.IsLandQuarry3Local(localX, localZ)
                || QuarryCatalog.IsLandQuarry4Local(localX, localZ);
        }

        public static bool IsLandLocal(CavernBounds bounds, float localX, float localZ)
        {
            if (bounds == null || !IsLandLocalRegion(bounds, localX, localZ))
                return false;

            return bounds.TryResolveFloorWorldPoint(localX, localZ, out _);
        }

        public static bool IsLandLocalRegion(CavernBounds bounds, float localX, float localZ)
        {
            if (bounds == null || IsQuarryLocal(bounds, localX, localZ))
                return false;

            if (LakeCatalog.IsLakeLocal(localX, localZ) || LakeCatalog.IsBeachLocal(localX, localZ))
                return false;

            if (LandQuarry2Boundary.IsSnowGroundLocal(localX, localZ))
                return false;

            float angle = Mathf.Atan2(localZ, localX);
            float distance = new Vector2(localX, localZ).magnitude;
            return distance >= GetLandInnerRadius(angle, bounds.Radius)
                && distance <= GetLandOuterRadius(bounds.Radius);
        }

        public static float GetLandInnerRadius(float angleRadians, float quarryNominalRadius)
        {
            return PlateauWallGeometry.GetWallBaseOutwardRadius(angleRadians, quarryNominalRadius)
                + WorldScale.Feet(LandInnerMarginFeet);
        }

        public static float GetLandOuterRadius(float quarryNominalRadius)
        {
            return WorldScale.Miles(LandRadiusMiles);
        }

        public static float GetMapViewRadius(float quarryNominalRadius)
        {
            return GetLandOuterRadius(quarryNominalRadius);
        }
    }
}
