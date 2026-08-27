using UnityEngine;

namespace MonsterMiner.Util
{
    public static class MeatVisualFactory
    {
        const float PebbleWorldScaleMultiplier = 2f;
        const float MeatScaleVsPebble = 2f;
        static readonly Color PlaceholderColor = new Color(0.85f, 0.2f, 0.2f);

        public static GameObject CreateWorldMeat(Vector3 worldPoint, string name = "Monster Meat")
        {
            if (!FloorAnchor.TryResolveFloorPoint(worldPoint, 16f, 32f, out var floorPoint))
                floorPoint = worldPoint;

            int seed = Mathf.Abs((floorPoint * 1000f).GetHashCode());
            Vector3 scale = PebbleVisualFactory.GetPebbleScale(seed)
                * PebbleWorldScaleMultiplier
                * MeatScaleVsPebble;

            var go = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                floorPoint,
                scale,
                PlaceholderColor,
                name);

            FloorAnchor.SnapBottomToFloor(go, floorPoint.y, 0.02f);
            return go;
        }
    }
}
