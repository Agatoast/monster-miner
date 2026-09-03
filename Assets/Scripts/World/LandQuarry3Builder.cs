using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class LandQuarry3Builder
    {
        const float QuarryRadiusFeet = 160f;

        public static void Build(Transform parent, CavernBounds bounds)
        {
            if (parent == null || bounds == null)
                return;

            DestroyExistingChild(parent, "LandQuarry3");

            var center = QuarryCatalog.GetLandQuarry3Center();
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(center.x, center.y, plainsBaseY);

            var root = new GameObject("LandQuarry3").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(center.x, groundY, center.y);

            float radius = WorldScale.Feet(QuarryRadiusFeet);
            var floorMaterial = CavernSurfaceMaterialFactory.GetFloorMaterial();
            QuarryFloorBuilder.CreateBowlFloor(root, radius, 0f, 0f, floorMaterial);
            QuarryFloorBuilder.CreateBowlCollision(root, radius, 0f, 0f);

            float floorWorldY = bounds.transform.TransformPoint(new Vector3(center.x, groundY, center.y)).y;
            LowPolyPeopleVisualFactory.CreateQuarry3QuestNpc(
                root,
                Vector3.zero,
                Quaternion.Euler(0f, 180f, 0f),
                floorWorldY);

            bounds.AddSpawnExclusion(
                center.x - WorldScale.Feet(12f),
                center.x + WorldScale.Feet(12f),
                center.y - WorldScale.Feet(12f),
                center.y + WorldScale.Feet(12f));
        }

        static void DestroyExistingChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null)
                Object.Destroy(existing.gameObject);
        }
    }
}
