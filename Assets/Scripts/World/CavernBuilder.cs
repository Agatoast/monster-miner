using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public class CavernBuilder : MonoBehaviour
    {
        const int WallColliderSegments = 32;
        const float WallColliderThickness = 0.5f;

        Transform contentRoot;
        Material shellMaterial;
        Material wallMaterial;
        Material floorMaterial;

        public CavernBounds Build(Vector3 center)
        {
            contentRoot = new GameObject("CavernContent").transform;
            contentRoot.SetParent(transform, false);
            contentRoot.position = center;

            shellMaterial = CavernSurfaceMaterialFactory.GetShellMaterial();
            floorMaterial = CavernSurfaceMaterialFactory.GetFloorMaterial();
            wallMaterial = CavernSurfaceMaterialFactory.GetWallMaterial();

            var bounds = contentRoot.gameObject.AddComponent<CavernBounds>();
            bounds.Radius = 12f;
            bounds.Height = 16f;
            bounds.FloorTopLocalY = 0.25f;
            bounds.WallThickness = WallColliderThickness;
            BuildShell(bounds);
            BuildLighting(bounds);
            BuildShopArea(bounds);
            BuildMinerArea(bounds);
            CavernInteriorEnforcer.DisableOutsideRenderers(contentRoot, bounds);
            return bounds;
        }

        void BuildLighting(CavernBounds bounds)
        {
            var sunGo = new GameObject("SunLight");
            sunGo.transform.SetParent(contentRoot, false);
            sunGo.transform.localRotation = Quaternion.Euler(48f, -35f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.05f;
            sun.color = new Color(0.95f, 0.9f, 0.82f);
            sun.shadows = LightShadows.Soft;

            var fillGo = new GameObject("CavernFillLight");
            fillGo.transform.SetParent(contentRoot, false);
            fillGo.transform.localPosition = new Vector3(0f, bounds.Height - 0.75f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = bounds.Radius * 3.5f;
            fill.intensity = 1.6f;
            fill.color = new Color(0.92f, 0.86f, 0.78f);
            fill.shadows = LightShadows.None;
        }

        void BuildShell(CavernBounds bounds)
        {
            float radius = bounds.Radius;
            float height = bounds.Height;

            CreateRoundDisc("Floor", new Vector3(0f, -0.25f, 0f), radius, 0.5f, floorMaterial);
            CreateRoundDisc("Ceiling", new Vector3(0f, height + 0.25f, 0f), radius, 0.5f, shellMaterial);
            CreateInvertedCylinderWall("WallCylinder", radius, height, wallMaterial);
            CreateWallCollision(radius, height);
        }

        void BuildShopArea(CavernBounds bounds)
        {
            const float shopkeeperInsetFromWall = 0.85f;
            const float counterLocalZ = -1.1f;
            const float counterLocalY = 0.6f;

            float shopAnchorZ = bounds.WalkableRadius - shopkeeperInsetFromWall;

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
                bounds.FloorTopWorldY);

            var board = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                shopRoot.transform.position,
                new Vector3(0.1f, 1.6f, 2.2f),
                new Color(0.35f, 0.25f, 0.12f),
                "ShopBoard",
                shopRoot.transform);
            board.transform.localPosition = new Vector3(-2.2f, 1.8f, counterLocalZ);

            var slotCab = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                shopRoot.transform.position,
                new Vector3(1.2f, 2f, 1f),
                new Color(0.8f, 0.15f, 0.15f),
                "SlotMachineBody",
                shopRoot.transform);
            slotCab.transform.localPosition = new Vector3(2.5f, 1f, counterLocalZ);

            bounds.AddSpawnExclusion(
                -3.3f,
                3.6f,
                shopAnchorZ + counterLocalZ - 1.5f,
                shopAnchorZ + 1.2f);

            var ctx = GameContext.Instance;
            if (ctx != null)
            {
                ctx.Shop = board.AddComponent<ShopManager>();
                ctx.Shop.Initialize(board.transform);
                ctx.SlotMachine = slotCab.AddComponent<SlotMachine>();
                ctx.SlotMachine.Initialize(slotCab.transform);
            }
        }

        void BuildMinerArea(CavernBounds bounds)
        {
            const float minerInsetFromWall = 2.1f;
            float minerAnchorZ = -(bounds.WalkableRadius - minerInsetFromWall);

            var minerRoot = new GameObject("MinerArea");
            minerRoot.transform.SetParent(contentRoot, false);
            minerRoot.transform.localPosition = new Vector3(0f, 0f, minerAnchorZ);

            LowPolyPeopleVisualFactory.CreateMinerNpc(
                minerRoot.transform,
                new Vector3(0f, 0f, 0.35f),
                Quaternion.identity,
                bounds.FloorTopWorldY);

            bounds.AddSpawnExclusion(
                -1.8f,
                1.8f,
                minerAnchorZ - 0.4f,
                minerAnchorZ + 1.4f);
        }

        public void OpenCave2Passage()
        {
            if (contentRoot == null)
                return;

            OpenWallSegmentNearNegativeZ();

            const float cave2CenterZ = -24f;
            const float cave2Radius = 10f;

            CreateRoundDisc("Cave2Floor", new Vector3(0f, -0.25f, cave2CenterZ), cave2Radius, 0.5f, floorMaterial);

            var tunnel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tunnel.name = "Cave2TunnelFloor";
            tunnel.transform.SetParent(contentRoot, false);
            tunnel.transform.localPosition = new Vector3(0f, 0.25f, -16.5f);
            tunnel.transform.localScale = new Vector3(6f, 0.5f, 13f);
            tunnel.GetComponent<Renderer>().sharedMaterial = floorMaterial;

            var tunnelLightGo = new GameObject("Cave2Light");
            tunnelLightGo.transform.SetParent(contentRoot, false);
            tunnelLightGo.transform.localPosition = new Vector3(0f, 6f, cave2CenterZ);
            var tunnelLight = tunnelLightGo.AddComponent<Light>();
            tunnelLight.type = LightType.Point;
            tunnelLight.range = cave2Radius * 2.5f;
            tunnelLight.intensity = 3.5f;
            tunnelLight.color = new Color(1f, 0.92f, 0.82f);

            var labelGo = new GameObject("Cave2Marker");
            labelGo.transform.SetParent(contentRoot, false);
            labelGo.transform.localPosition = new Vector3(0f, 2f, cave2CenterZ);
        }

        void OpenWallSegmentNearNegativeZ()
        {
            var wallRoot = contentRoot.Find("WallCollision");
            if (wallRoot == null)
                return;

            for (int i = wallRoot.childCount - 1; i >= 0; i--)
            {
                var segment = wallRoot.GetChild(i);
                float angleDeg = i / (float)WallColliderSegments * 360f;
                if (Mathf.Abs(Mathf.DeltaAngle(angleDeg, 270f)) <= 42f)
                    Destroy(segment.gameObject);
            }
        }

        public void RebuildWalls(CavernBounds bounds)
        {
            if (contentRoot == null)
                return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                var child = contentRoot.GetChild(i);
                if (child.name == "Floor" || child.name == "Ceiling" || child.name == "WallCylinder"
                    || child.name == "WallCollision" || child.name.StartsWith("Wall"))
                    Destroy(child.gameObject);
            }

            BuildShell(bounds);
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
            box.center = name == "Floor"
                ? new Vector3(0f, 0.25f, 0f)
                : Vector3.zero;
        }

        void CreateInvertedCylinderWall(string name, float radius, float height, Material mat)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wall.name = name;
            wall.transform.SetParent(contentRoot, false);
            wall.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            // Negative X scale flips winding so the curved wall faces inward.
            wall.transform.localScale = new Vector3(-radius * 2f, height * 0.5f, radius * 2f);
            wall.GetComponent<Renderer>().sharedMaterial = mat;
            Destroy(wall.GetComponent<Collider>());
        }

        void CreateWallCollision(float radius, float height)
        {
            var root = new GameObject("WallCollision");
            root.transform.SetParent(contentRoot, false);

            float wallCenterRadius = radius + WallColliderThickness * 0.5f;
            float segmentWidth = 2f * Mathf.PI * radius / WallColliderSegments * 1.05f;
            for (int i = 0; i < WallColliderSegments; i++)
            {
                float angle = i / (float)WallColliderSegments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * wallCenterRadius;
                float z = Mathf.Sin(angle) * wallCenterRadius;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                var segment = new GameObject($"WallCollider_{i}");
                segment.transform.SetParent(root.transform, false);
                segment.transform.localPosition = new Vector3(x, height * 0.5f, z);
                segment.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);

                var box = segment.AddComponent<BoxCollider>();
                box.size = new Vector3(segmentWidth, height, WallColliderThickness);
            }
        }
    }
}
