using UnityEngine;

namespace MonsterMiner.World
{
    public static class HuntLocationCatalog
    {
        public const int EggsPerLocation = 3;

        public readonly struct Location
        {
            public readonly string Name;
            public readonly float AngleDegrees;
            public readonly float DistancePastWallFeet;

            public Location(string name, float angleDegrees, float distancePastWallFeet)
            {
                Name = name;
                AngleDegrees = angleDegrees;
                DistancePastWallFeet = distancePastWallFeet;
            }
        }

        public static readonly Location[] All =
        {
            new Location("North Woods", 90f, 220f),
            new Location("East Meadow", 0f, 260f),
            new Location("South Clearing", 270f, 200f),
            new Location("West Scrub", 180f, 240f)
        };

        public static Vector2 GetLocalXZ(CavernBounds bounds, Location location)
        {
            float angle = location.AngleDegrees * Mathf.Deg2Rad;
            float wall = bounds != null
                ? PlateauWallGeometry.GetWallBaseOutwardRadius(angle, bounds.Radius)
                : WorldScale.Feet(WorldScale.PlateauNominalRadiusFeet);
            float distance = wall + WorldScale.Feet(location.DistancePastWallFeet);
            return new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
        }

        public static float GetMapViewRadius(CavernBounds bounds)
        {
            return WorldRegion.GetMapViewRadius(bounds != null ? bounds.Radius : WorldScale.Feet(WorldScale.PlateauNominalRadiusFeet));
        }
    }
}
