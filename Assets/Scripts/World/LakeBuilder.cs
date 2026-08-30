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
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float shoreZ = LakeCatalog.GetBeachNorthEdgeZ();
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(center.x, shoreZ, plainsBaseY);

            var root = new GameObject("WarrensonsLake").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(center.x, groundY, center.y);

            float waterLocalY = WorldScale.Feet(0.2f);
            CreateWaterSurface(root, waterLocalY);
            CreateBeach(root, 0f);
            KnarrVisualFactory.CreateAtBeach(root, waterLocalY);
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

        static void CreateBeach(Transform root, float baseLocalY)
        {
            var beachRoot = new GameObject("LakeBeach");
            beachRoot.transform.SetParent(root, false);

            var lakeCenter = LakeCatalog.GetCenterLocal();
            var beachCenter = LakeCatalog.GetBeachCenterContentLocal();
            float halfLength = WorldScale.Feet(LakeCatalog.BeachHalfLengthFeet);
            float southZ = LakeCatalog.GetBeachSouthEdgeZ();
            float northZ = LakeCatalog.GetBeachNorthEdgeZ();
            float depth = northZ - southZ;
            float midZ = (southZ + northZ) * 0.5f;
            int beachSegments = 10;

            for (int i = 0; i < beachSegments; i++)
            {
                float t0 = i / (float)beachSegments;
                float t1 = (i + 1) / (float)beachSegments;
                float contentX0 = beachCenter.x - halfLength + (halfLength * 2f) * t0;
                float contentX1 = beachCenter.x - halfLength + (halfLength * 2f) * t1;
                float midContentX = (contentX0 + contentX1) * 0.5f;
                float segmentWidth = Mathf.Max(0.35f, Mathf.Abs(contentX1 - contentX0) * 1.02f);

                float x = midContentX - lakeCenter.x;
                float z = midZ - lakeCenter.y;

                var segmentGo = new GameObject($"LakeBeachSegment_{i}");
                segmentGo.transform.SetParent(beachRoot.transform, false);
                segmentGo.transform.localPosition = new Vector3(x, baseLocalY, z);
                segmentGo.transform.localRotation = Quaternion.identity;

                var meshGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                meshGo.name = "LakeBeachMesh";
                meshGo.transform.SetParent(segmentGo.transform, false);
                meshGo.transform.localPosition = Vector3.zero;
                meshGo.transform.localRotation = Quaternion.identity;
                meshGo.transform.localScale = new Vector3(segmentWidth, ColliderThickness, depth);
                meshGo.GetComponent<Renderer>().sharedMaterial = CavernSurfaceMaterialFactory.GetSandMaterial();
                Object.Destroy(meshGo.GetComponent<Collider>());

                var colliderGo = new GameObject($"LakeBeachCollider_{i}");
                colliderGo.transform.SetParent(beachRoot.transform, false);
                colliderGo.transform.localPosition = new Vector3(x, baseLocalY, z);
                colliderGo.transform.localRotation = Quaternion.identity;
                var box = colliderGo.AddComponent<BoxCollider>();
                box.size = new Vector3(segmentWidth, ColliderThickness, depth);
            }
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
