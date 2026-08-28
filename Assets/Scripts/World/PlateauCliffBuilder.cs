using UnityEngine;

namespace MonsterMiner.World
{
    public static class PlateauCliffBuilder
    {
        public static void Build(
            Transform parent,
            CavernBounds bounds,
            float plainsBaseLocalY)
        {
            var root = new GameObject("PlateauBluff").transform;
            root.SetParent(parent, false);

            float lowerBaseY = LowerWorldBuilder.GetLowerGroundBaseY(plainsBaseLocalY);
            LowerWorldBuilder.Build(root, plainsBaseLocalY, bounds.Radius);
            PlateauWallBuilder.Build(root, bounds, plainsBaseLocalY, lowerBaseY);
        }
    }
}
