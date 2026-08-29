using MonsterMiner.Core;
using MonsterMiner.Economy;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class QuarryCatalog
    {
        public const float Quarry2RadiusFeet = 160f;

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
                "Quarry 2",
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
            float radius = WorldScale.Feet(Quarry2RadiusFeet);
            return Vector2.Distance(new Vector2(localX, localZ), center) <= radius;
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
