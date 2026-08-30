using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class QuarryCatalog
    {
        public const float Quarry2NominalRadiusFeet = 350f;

        public const float VikingCharacterRightOfHallCenterFeet = 7f;
        public const float VikingCharacterSouthOfHallFaceFeet = 9f;
        public const float VikingCharacterShiftLeftFeet = 15f;
        public const float VikingCharacterShiftRightFeet = 50f;
        public const float VikingCharacterShiftNorthFeet = 85f;
        public const float LandQuarry2PlayerSpawnShiftNorthFeet = 90f;
        public const float LandQuarry2ShopWestOfSpawnFeet = 20f;
        public const string JarlLandDisplayName = "Jarl Land";

        public const int PlateauQuarryIndex = 1;
        public const int LandQuarry2Index = 2;
        public const int LandQuarry3Index = 3;
        public const int LandQuarry4Index = 4;
        public const int LandQuarry5Index = 5;

        public readonly struct MapEdgeMarker
        {
            public readonly string Label;
            public readonly Color Color;
            readonly System.Func<GameContext, bool> isVisible;
            readonly System.Func<GameContext, Vector2> getLocalXZ;

            public MapEdgeMarker(
                string label,
                Color color,
                System.Func<GameContext, bool> isVisible,
                System.Func<GameContext, Vector2> getLocalXZ)
            {
                Label = label;
                Color = color;
                this.isVisible = isVisible;
                this.getLocalXZ = getLocalXZ;
            }

            public bool IsVisible(GameContext ctx) => isVisible != null && isVisible(ctx);

            public Vector2 GetLocalXZ(GameContext ctx) => getLocalXZ != null ? getLocalXZ(ctx) : Vector2.zero;
        }

        public static readonly MapEdgeMarker[] EdgeMarkers =
        {
            new MapEdgeMarker(
                "Plateau",
                Color.red,
                ctx => ctx?.CaveProgression != null && ctx.CaveProgression.HasWorldMap,
                _ => Vector2.zero),
            new MapEdgeMarker(
                "Truck",
                Color.blue,
                ctx => ctx?.PlayerTruck != null,
                GetTruckLocal),
            new MapEdgeMarker(
                JarlLandDisplayName,
                new Color(0.2f, 0.85f, 0.3f),
                ctx => ctx?.CaveProgression != null && ctx.CaveProgression.HasLandQuarry2,
                _ => GetLandQuarry2Center()),
            new MapEdgeMarker(
                "Quarry 3",
                new Color(1f, 0.55f, 0.12f),
                ctx => ctx?.CaveProgression != null && ctx.CaveProgression.HasLandQuarry3,
                _ => GetLandQuarryCenter(LandQuarry3Index)),
            new MapEdgeMarker(
                "Quarry 4",
                new Color(0.62f, 0.28f, 0.82f),
                ctx => ctx?.CaveProgression != null && ctx.CaveProgression.HasLandQuarry4,
                _ => GetLandQuarryCenter(LandQuarry4Index)),
            new MapEdgeMarker(
                "Quarry 5",
                Color.white,
                ctx => ctx?.CaveProgression != null && ctx.CaveProgression.HasLandQuarry5,
                _ => GetLandQuarryCenter(LandQuarry5Index)),
        };

        public static Vector2 GetLandQuarry2Center() => new Vector2(0f, WorldScale.Miles(1f));

        public static Vector2 GetLandQuarryCenter(int quarryIndex)
        {
            if (quarryIndex == LandQuarry2Index)
                return GetLandQuarry2Center();

            return Vector2.zero;
        }

        public static bool IsLandQuarry2Local(float localX, float localZ)
        {
            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasLandQuarry2)
                return false;

            var center = GetLandQuarry2Center();
            return LandQuarry2Boundary.ContainsLocal(localX, localZ);
        }

        public static Vector3 ResolveHallFrontSpawnWorld(CavernBounds bounds)
        {
            if (PlayerSpawnPersistence.HasSavedLandSpawn)
            {
                return PlayerSpawnPersistence.LoadSavedLandSpawn();
            }

            return ResolvePlayerSpawnWorld(bounds);
        }

        public static Vector3 ResolvePlayerSpawnWorld(CavernBounds bounds)
        {
            if (bounds == null)
                return Vector3.zero;

            var quarry = FindQuarry2Root(bounds);
            if (quarry == null)
                return Vector3.zero;

            var hall = FindQuarry2Hall(bounds);
            Vector3 spawnLocal = ResolveBesideHallLocal(
                hall != null ? hall.gameObject : null,
                quarry,
                LandQuarry2PlayerSpawnShiftNorthFeet);
            Vector3 worldPoint = quarry.TransformPoint(spawnLocal);
            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                worldPoint,
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        public static Vector3 ResolveVikingCharacterSpawnWorld(CavernBounds bounds)
        {
            if (bounds == null)
                return Vector3.zero;

            var quarry = FindQuarry2Root(bounds);
            if (quarry == null)
                return Vector3.zero;

            var hall = FindQuarry2Hall(bounds);
            Vector3 characterLocal = ResolveBesideHallLocal(
                hall != null ? hall.gameObject : null,
                quarry,
                VikingCharacterShiftNorthFeet);
            Vector3 worldPoint = quarry.TransformPoint(characterLocal);
            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                worldPoint,
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        public static Vector3 ResolveVikingCharacterLocal(GameObject hall, Transform quarryRoot)
        {
            return ResolveBesideHallLocal(hall, quarryRoot, VikingCharacterShiftNorthFeet)
                + new Vector3(WorldScale.Feet(VikingCharacterShiftRightFeet), 0f, 0f);
        }

        public static Vector3 ResolvePlayerSpawnLocal(GameObject hall, Transform quarryRoot)
        {
            return ResolveBesideHallLocal(hall, quarryRoot, LandQuarry2PlayerSpawnShiftNorthFeet);
        }

        public static Vector3 ResolveQuarryShopAnchorLocal(GameObject hall, Transform quarryRoot)
        {
            Vector3 spawnLocal = ResolvePlayerSpawnLocal(hall, quarryRoot);
            return spawnLocal + new Vector3(-WorldScale.Feet(LandQuarry2ShopWestOfSpawnFeet), 0f, 0f);
        }

        static Vector3 ResolveBesideHallLocal(GameObject hall, Transform quarryRoot, float shiftNorthFeet)
        {
            float rightOffset = WorldScale.Feet(VikingCharacterRightOfHallCenterFeet);
            float southOffset = -WorldScale.Feet(VikingCharacterSouthOfHallFaceFeet);

            Vector3 baseLocal;
            if (hall != null && VikingPropVisualFactory.TryGetLocalBounds(hall, quarryRoot, out var hallBounds))
            {
                float centerX = (hallBounds.min.x + hallBounds.max.x) * 0.5f;
                baseLocal = new Vector3(centerX + rightOffset, 0f, hallBounds.min.z + southOffset);
            }
            else
            {
                baseLocal = new Vector3(rightOffset, 0f, southOffset);
            }

            return baseLocal + new Vector3(
                -WorldScale.Feet(VikingCharacterShiftLeftFeet),
                0f,
                WorldScale.Feet(shiftNorthFeet));
        }

        static Transform FindQuarry2Root(CavernBounds bounds)
        {
            if (bounds == null)
                return null;

            var quarry = bounds.transform.Find("CavernContent/JarlLand");
            if (quarry == null)
                quarry = bounds.transform.Find("JarlLand");
            if (quarry == null)
                quarry = bounds.transform.Find("CavernContent/LandQuarry2");
            if (quarry == null)
                quarry = bounds.transform.Find("LandQuarry2");

            return quarry;
        }

        static Transform FindQuarry2Hall(CavernBounds bounds)
        {
            var quarry = FindQuarry2Root(bounds);
            return quarry != null ? quarry.Find(VikingBuildingVisualFactory.HallObjectName) : null;
        }

        public static Vector3 ResolveEdgeSpawnWorld(CavernBounds bounds, float edgeAngleRadians = -Mathf.PI * 0.5f)
        {
            if (bounds == null)
                return Vector3.zero;

            var edgeLocal = LandQuarry2Boundary.GetEdgeLocalPoint(edgeAngleRadians);
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(edgeLocal.x, edgeLocal.y, plainsBaseY);
            var worldPoint = bounds.transform.TransformPoint(new Vector3(edgeLocal.x, groundY, edgeLocal.y));
            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                worldPoint,
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        public static Vector3 ResolveCenterWorld(CavernBounds bounds)
        {
            if (bounds == null)
                return Vector3.zero;

            var center = GetLandQuarry2Center();
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(center.x, center.y, plainsBaseY);
            return bounds.transform.TransformPoint(new Vector3(center.x, groundY, center.y));
        }

        static Vector2 GetTruckLocal(GameContext ctx)
        {
            if (ctx?.PlayerTruck == null || ctx.CavernBounds == null)
                return Vector2.zero;

            Vector3 local = ctx.CavernBounds.transform.InverseTransformPoint(ctx.PlayerTruck.transform.position);
            return new Vector2(local.x, local.z);
        }
    }
}
