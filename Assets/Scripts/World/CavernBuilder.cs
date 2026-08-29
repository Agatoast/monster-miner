using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public class CavernBuilder : MonoBehaviour
    {
        Transform contentRoot;
        Material floorMaterial;

        public CavernBounds Build(Vector3 center)
        {
            contentRoot = new GameObject("CavernContent").transform;
            contentRoot.SetParent(transform, false);
            contentRoot.position = center;

            floorMaterial = CavernSurfaceMaterialFactory.GetFloorMaterial();

            var bounds = contentRoot.gameObject.AddComponent<CavernBounds>();
            bounds.Radius = WorldScale.Feet(WorldScale.PlateauNominalRadiusFeet);
            bounds.Height = 16f;
            bounds.FloorTopLocalY = 0.25f;
            bounds.BowlDepth = 0f;
            ConfigureShopSpawnExclusions(bounds);
            BuildPlateau(bounds);
            BuildLighting();
            BuildShopArea(bounds);
            BuildMinerArea(bounds);
            return bounds;
        }

        void BuildLighting()
        {
            var sunGo = new GameObject("SunLight");
            sunGo.transform.SetParent(contentRoot, false);
            sunGo.transform.localRotation = Quaternion.Euler(52f, -28f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.35f;
            sun.color = new Color(1f, 0.96f, 0.86f);
            sun.shadows = LightShadows.Soft;
        }

        void BuildPlateau(CavernBounds bounds)
        {
            PlainsBiomeVisualFactory.BuildSurroundings(contentRoot, bounds);
            PlateauCliffBuilder.Build(contentRoot, bounds, PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
        }

        static void ConfigureShopSpawnExclusions(CavernBounds bounds)
        {
            const float counterLocalZ = -1.1f;
            float shopAnchorZ = WorldScale.Feet(WorldScale.ShopDistanceFromSpawnFeet);
            bounds.SetSalesmanEggSpawnExclusion(0f, shopAnchorZ, 20f);
            bounds.AddSpawnExclusion(
                -3.3f,
                3.6f,
                shopAnchorZ + counterLocalZ - WorldScale.Feet(3f) - 1.5f,
                shopAnchorZ + 1.2f);

            float houseZ = shopAnchorZ + WorldScale.Feet(30f);
            float houseHalfX = 1.7f + WorldScale.Feet(3f);
            float houseHalfZ = 2.2f + WorldScale.Feet(3f);
            bounds.AddSpawnExclusion(
                -houseHalfX,
                houseHalfX,
                houseZ - houseHalfZ,
                houseZ + houseHalfZ);
        }

        void BuildShopArea(CavernBounds bounds)
        {
            const float counterLocalZ = -1.1f;
            const float counterLocalY = 0.6f;

            float shopAnchorZ = WorldScale.Feet(WorldScale.ShopDistanceFromSpawnFeet);

            var shopRoot = new GameObject("ShopArea");
            shopRoot.transform.SetParent(contentRoot, false);
            shopRoot.transform.localPosition = new Vector3(0f, 0f, shopAnchorZ);

            var counter = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                shopRoot.transform.position,
                new Vector3(3f, 1.2f, 1f),
                new Color(0.45f, 0.28f, 0.15f),
                "ShopCounter",
                shopRoot.transform);
            counter.transform.localPosition = new Vector3(0f, counterLocalY, counterLocalZ);

            LowPolyPeopleVisualFactory.CreateShopkeeper(
                shopRoot.transform,
                Vector3.zero,
                Quaternion.Euler(0f, 180f, 0f),
                bounds.SampleFloorWorldY(0f, shopAnchorZ));

            float houseLocalZ = shopAnchorZ + WorldScale.Feet(30f);
            HandpaintedHouseVisualFactory.CreateOnPlateau(
                contentRoot,
                new Vector3(0f, 0f, houseLocalZ),
                Quaternion.Euler(0f, 180f, 0f),
                bounds.SamplePlateauFloorWorldY(0f, houseLocalZ));

            var board = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                shopRoot.transform.position,
                new Vector3(0.1f, 1.6f, 2.2f),
                new Color(0.35f, 0.25f, 0.12f),
                "ShopBoard",
                shopRoot.transform);
            board.transform.localPosition = new Vector3(-2.2f, 1.8f, counterLocalZ - WorldScale.Feet(3f));

            var slotCab = SlotMachineVisualFactory.CreateShopSlotMachine(
                shopRoot.transform,
                new Vector3(2.5f, 0f, counterLocalZ),
                Quaternion.Euler(0f, 180f, 0f),
                bounds.SampleFloorWorldY(2.5f, shopAnchorZ + counterLocalZ));

            var ctx = GameContext.Instance;
            if (ctx != null)
            {
                ctx.Shop = board.AddComponent<ShopManager>();
                ctx.Shop.Initialize(board.transform);

                if (slotCab != null)
                {
                    ctx.SlotMachine = slotCab.AddComponent<SlotMachine>();
                    ctx.SlotMachine.Initialize(
                        slotCab.transform,
                        SlotMachineVisualFactory.GetVisual(slotCab));
                }
            }
        }

        void BuildMinerArea(CavernBounds bounds)
        {
            const float minerInsetFromEdgeFeet = 10f;
            const float wingsInsetFromEdgeFeet = 5f;
            const float wingsDistanceFromMinerFeet = 20f;

            float shopLocalZ = WorldScale.Feet(WorldScale.ShopDistanceFromSpawnFeet);
            float minerAngle = -Mathf.PI * 0.5f;
            Vector2 minerXZ = PointInFromPlateauEdge(minerAngle, minerInsetFromEdgeFeet, bounds.Radius);
            Vector2 wingsXZ = FindPlateauPointNearEdge(
                minerXZ,
                wingsDistanceFromMinerFeet,
                wingsInsetFromEdgeFeet,
                bounds.Radius);

            var minerRoot = new GameObject("MinerArea");
            minerRoot.transform.SetParent(contentRoot, false);
            minerRoot.transform.localPosition = new Vector3(minerXZ.x, 0f, minerXZ.y);

            var toShop = new Vector3(-minerXZ.x, 0f, shopLocalZ - minerXZ.y);
            if (toShop.sqrMagnitude < 0.001f)
                toShop = Vector3.forward;

            LowPolyPeopleVisualFactory.CreateMinerNpc(
                minerRoot.transform,
                Vector3.zero,
                Quaternion.LookRotation(toShop),
                bounds.SampleFloorWorldY(minerXZ.x, minerXZ.y));

            Vector3 wingsPoint = bounds.TryResolveFloorWorldPoint(wingsXZ.x, wingsXZ.y, out var wingsFloor)
                ? wingsFloor
                : bounds.transform.TransformPoint(new Vector3(
                    wingsXZ.x,
                    bounds.SampleFloorLocalY(wingsXZ.x, wingsXZ.y),
                    wingsXZ.y));

            AngelWingsVisualFactory.CreateOnGround(contentRoot, wingsPoint);

            bounds.AddSpawnExclusion(minerXZ.x - 2f, minerXZ.x + 2f, minerXZ.y - 2f, minerXZ.y + 2f);
            bounds.AddSpawnExclusion(wingsXZ.x - 2f, wingsXZ.x + 2f, wingsXZ.y - 2f, wingsXZ.y + 2f);
        }

        static Vector2 PointInFromPlateauEdge(float angleRadians, float insetFeet, float plateauNominalRadius)
        {
            float edge = PlateauBoundary.SamplePlateauEdgeDistance(angleRadians, plateauNominalRadius);
            float distance = Mathf.Max(1f, edge - WorldScale.Feet(insetFeet));
            return new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians)) * distance;
        }

        static Vector2 FindPlateauPointNearEdge(
            Vector2 from,
            float distanceFromSourceFeet,
            float insetFromEdgeFeet,
            float plateauNominalRadius)
        {
            float targetDistance = WorldScale.Feet(distanceFromSourceFeet);
            float bestError = float.MaxValue;
            Vector2 best = from;
            const int steps = 80;
            float searchWindow = 0.55f;

            for (int i = 0; i <= steps; i++)
            {
                float angle = -Mathf.PI * 0.5f + Mathf.Lerp(-searchWindow, searchWindow, i / (float)steps);
                Vector2 candidate = PointInFromPlateauEdge(angle, insetFromEdgeFeet, plateauNominalRadius);
                if (!PlateauBoundary.IsOnPlateau(candidate.x, candidate.y, plateauNominalRadius))
                    continue;

                float error = Mathf.Abs(Vector2.Distance(from, candidate) - targetDistance);
                if (error >= bestError)
                    continue;

                bestError = error;
                best = candidate;
            }

            return best;
        }

        public void BuildLandQuarry2(CavernBounds bounds)
        {
            if (contentRoot == null || bounds == null)
                return;

            LandQuarry2Builder.Build(contentRoot, bounds);
        }

        public void OpenCave2Passage()
        {
            if (contentRoot == null)
                return;

            const float cave2CenterZ = -24f;
            const float cave2Radius = 10f;

            CreateRoundDisc("Cave2Floor", new Vector3(0f, -0.25f, cave2CenterZ), cave2Radius, 0.5f, floorMaterial);

            var tunnel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tunnel.name = "Cave2TunnelFloor";
            tunnel.transform.SetParent(contentRoot, false);
            tunnel.transform.localPosition = new Vector3(0f, 0.25f, -16.5f);
            tunnel.transform.localScale = new Vector3(6f, 0.5f, 13f);
            tunnel.GetComponent<Renderer>().sharedMaterial = floorMaterial;

            var labelGo = new GameObject("Cave2Marker");
            labelGo.transform.SetParent(contentRoot, false);
            labelGo.transform.localPosition = new Vector3(0f, 2f, cave2CenterZ);
        }

        public void RebuildWalls(CavernBounds bounds)
        {
            if (contentRoot == null)
                return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                var child = contentRoot.GetChild(i);
                if (child.name == "PlainsBiome" || child.name == "PlateauBluff"
                    || child.name == "PlateauCliffs")
                    Destroy(child.gameObject);
            }

            BuildPlateau(bounds);
        }

        void CreateRoundDisc(string name, Vector3 localPos, float radius, float thickness, Material mat)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            disc.transform.SetParent(contentRoot, false);
            disc.transform.localPosition = localPos;
            disc.transform.localScale = new Vector3(radius * 2f, thickness, radius * 2f);
            disc.GetComponent<Renderer>().sharedMaterial = mat;

            // Thin cylinder capsule colliders are invalid (height << radius); use a box instead.
            Destroy(disc.GetComponent<Collider>());
            var box = disc.AddComponent<BoxCollider>();
            box.size = Vector3.one;
            // Align the walkable collider surface with the visible disc top.
            box.center = name == "Floor" || name == "Cave2Floor"
                ? new Vector3(0f, 0.25f, 0f)
                : Vector3.zero;
        }
    }
}
