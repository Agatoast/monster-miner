using MonsterMiner.Core;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class LakeCatalog
    {
        public const float NominalDiameterFeet = 5280f;
        public const float BeachHalfLengthFeet = 50f;
        public const float BeachLengthFeet = BeachHalfLengthFeet * 2f;
        public const float BeachGapFeet = 100f;

        public const float JarlLakeConnectionAngle = Mathf.PI * 0.5f;
        const float LakeSouthShoreAngle = -Mathf.PI * 0.5f;

        public static Vector2 GetJarlNorthShoreContentLocal() =>
            LandQuarry2Boundary.GetEdgeLocalPoint(JarlLakeConnectionAngle);

        public static float GetBeachSouthEdgeZ() => GetJarlNorthShoreContentLocal().y;

        public static float GetBeachNorthEdgeZ() =>
            GetBeachSouthEdgeZ() + WorldScale.Feet(BeachGapFeet);

        public static Vector2 GetBeachCenterContentLocal()
        {
            var jarlNorth = GetJarlNorthShoreContentLocal();
            float centerX = jarlNorth.x;

            if (PlayerSpawnPersistence.HasSavedLandSpawn)
            {
                var bounds = GameContext.Instance?.CavernBounds;
                if (bounds != null)
                {
                    Vector3 saved = PlayerSpawnPersistence.LoadSavedLandSpawn();
                    Vector3 local = bounds.transform.InverseTransformPoint(saved);
                    centerX = local.x;
                }
            }

            float centerZ = GetBeachSouthEdgeZ() + WorldScale.Feet(BeachGapFeet * 0.5f);
            return new Vector2(centerX, centerZ);
        }

        public static float GetBeachShoreAngle(float contentX)
        {
            var lakeCenter = GetCenterLocal();
            var beachCenter = GetBeachCenterContentLocal();
            float dx = contentX - lakeCenter.x;
            float dz = beachCenter.y - lakeCenter.y;
            if (Mathf.Abs(dx) < 0.01f)
                return dz >= 0f ? Mathf.PI * 0.5f : LakeSouthShoreAngle;

            return Mathf.Atan2(dz, dx);
        }

        public static Vector2 GetCenterLocal()
        {
            var jarlNorth = GetJarlNorthShoreContentLocal();
            float lakeRadius = GetNominalRadiusUnits();
            float beachNorth = GetBeachNorthEdgeZ();
            return new Vector2(jarlNorth.x, beachNorth + lakeRadius);
        }

        public static float GetNominalRadiusUnits() => WorldScale.Feet(NominalDiameterFeet * 0.5f);

        public static bool IsLakeLocal(float localX, float localZ)
        {
            if (QuarryCatalog.IsLandQuarry2Local(localX, localZ))
                return false;

            if (IsBeachLocal(localX, localZ))
                return false;

            return LakeBoundary.ContainsLocal(localX, localZ);
        }

        public static bool IsBeachLocal(float localX, float localZ)
        {
            if (QuarryCatalog.IsLandQuarry2Local(localX, localZ))
                return false;

            return LakeBoundary.IsBeachLocal(localX, localZ);
        }

        public static bool IsOpenWaterLocal(float localX, float localZ)
        {
            if (QuarryCatalog.IsLandQuarry2Local(localX, localZ))
                return false;

            if (IsBeachLocal(localX, localZ))
                return false;

            return LakeBoundary.ContainsLocal(localX, localZ);
        }

        public static Vector2 GetNearestShoreLocal(float localX, float localZ)
        {
            var center = GetCenterLocal();
            float dx = localX - center.x;
            float dz = localZ - center.y;
            float angle = Mathf.Atan2(dz, dx);
            float edge = LakeBoundary.SampleEdgeDistance(angle);
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * edge;
        }
    }
}
