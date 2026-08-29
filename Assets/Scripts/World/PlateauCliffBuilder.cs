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

            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(plainsBaseLocalY);
            PlainsWorldBuilder.Build(root, plainsBaseLocalY, bounds);
            PlateauWallBuilder.Build(root, bounds, plainsBaseLocalY, plainsBaseY);
        }
    }
}
