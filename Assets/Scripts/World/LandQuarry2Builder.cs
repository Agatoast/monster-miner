using UnityEngine;

namespace MonsterMiner.World
{
    public static class LandQuarry2Builder
    {
        public static void Build(Transform parent, CavernBounds bounds)
        {
            if (parent == null || bounds == null)
                return;

            var center = QuarryCatalog.GetLandQuarry2Center();
            float radius = WorldScale.Feet(QuarryCatalog.Quarry2RadiusFeet);
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(center.x, center.y, plainsBaseY);

            var root = new GameObject("LandQuarry2").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(center.x, groundY, center.y);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Quarry2Floor";
            floor.transform.SetParent(root, false);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(radius * 2f, 0.35f, radius * 2f);
            floor.GetComponent<Renderer>().sharedMaterial = CavernSurfaceMaterialFactory.GetFloorMaterial();

            Object.Destroy(floor.GetComponent<Collider>());
            var box = floor.AddComponent<BoxCollider>();
            box.size = Vector3.one;
            box.center = new Vector3(0f, 0.25f, 0f);

            var marker = new GameObject("Quarry2Marker");
            marker.transform.SetParent(root, false);
            marker.transform.localPosition = new Vector3(0f, 2f, 0f);
        }
    }
}
