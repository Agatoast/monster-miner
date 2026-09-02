using MonsterMiner.Core;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class LakeBuilder
    {
        const float ColliderThickness = 0.36f;

        public static void Build(Transform parent, CavernBounds bounds)
        {
            if (parent == null || bounds == null)
                return;

            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasLandQuarry2)
                return;

            var center = LakeCatalog.GetCenterLocal();
            var beachCenter = LakeCatalog.GetBeachCenterContentLocal();
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float shoreZ = LakeCatalog.GetBeachNorthEdgeZ();
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(beachCenter.x, shoreZ, plainsBaseY);

            var root = new GameObject("WarrensonsLake").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(center.x, groundY, center.y);

            float waterLocalY = LakeCatalog.WaterSurfaceLocalYOffset;
            CreateWaterSurface(root, waterLocalY);
            LakeIslandVisualFactory.Create(root, parent, waterLocalY, bounds);
            KnarrVisualFactory.CreateAtBeach(parent);
            ConfigureSpawnExclusions(bounds, root);
            EnsureRenderersEnabled(root.gameObject);
        }

        static void EnsureRenderersEnabled(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
        }

        static void CreateWaterSurface(Transform root, float waterLocalY)
        {
            float diameter = LakeCatalog.GetNominalRadiusUnits() * 2f;
            WaterWorksLakeVisualFactory.CreateLakeSurface(root, waterLocalY, diameter);
        }

        static void ConfigureSpawnExclusions(CavernBounds bounds, Transform lakeRoot)
        {
            if (bounds == null || lakeRoot == null)
                return;

            var center = LakeCatalog.GetCenterLocal();
            float pad = WorldScale.Feet(20f);
            float radius = LakeCatalog.GetNominalRadiusUnits();
            float jarlNorthEdge = QuarryCatalog.GetLandQuarry2Center().y + LandQuarry2Boundary.MaxEdgeDistance;
            bounds.AddSpawnExclusion(
                center.x - radius - pad,
                center.x + radius + pad,
                jarlNorthEdge - WorldScale.Feet(5f),
                center.y + radius + pad);
        }
    }
}
