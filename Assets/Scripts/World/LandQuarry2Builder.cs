using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class LandQuarry2Builder
    {
        const float NorthApproachAngle = Mathf.PI * 0.5f;
        const float StairInsetFromEdgeFeet = 8f;
        const float TreeRightFrontOffsetFeet = 10f;

        public static void Build(Transform parent, CavernBounds bounds)
        {
            if (parent == null || bounds == null)
                return;

            DestroyExistingChild(parent, "JarlLand");
            DestroyExistingChild(parent, "WarrensonsLake");
            DestroyExistingChild(parent, "BeachSandSurface");
            DestroyExistingChild(parent, "BoatShoreSandSemicircle");

            var center = QuarryCatalog.GetLandQuarry2Center();
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(center.x, center.y, plainsBaseY);

            var root = new GameObject("JarlLand").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(center.x, groundY, center.y);

            var snowMaterial = CavernSurfaceMaterialFactory.GetSnowMaterial();
            LandQuarry2FloorBuilder.CreateFloor(
                root,
                center,
                plainsBaseY,
                groundY,
                snowMaterial);
            LandQuarry2FloorBuilder.CreateFloorCollision(root, LandQuarry2Boundary.SnowFloorLocalYOffset);
            LandQuarry2FloorBuilder.CreateSnowApron(
                root,
                center,
                plainsBaseY,
                groundY,
                snowMaterial);

            PlainsWorldBuilder.RebuildGroundExcludingLandFeatures(parent, bounds);

            float floorWorldY = bounds.transform.TransformPoint(new Vector3(center.x, groundY, center.y)).y;
            float northEdge = LandQuarry2Boundary.SampleEdgeDistance(NorthApproachAngle);
            Vector3 stairLocal = new Vector3(
                0f,
                0f,
                northEdge - WorldScale.Feet(StairInsetFromEdgeFeet));

            QuarryStairVisualFactory.Create(
                root,
                stairLocal,
                Quaternion.identity,
                floorWorldY);

            var hall = VikingBuildingVisualFactory.CreateAtLocalPoint(
                root,
                Vector3.zero,
                floorWorldY,
                Quaternion.identity);

            float treeOffset = WorldScale.Feet(TreeRightFrontOffsetFeet);
            VikingPropVisualFactory.CreateTreeAtLocalPoint(
                root,
                new Vector3(treeOffset, 0f, treeOffset),
                floorWorldY,
                Quaternion.identity);

            Vector3 characterLocal = QuarryCatalog.ResolveVikingCharacterLocal(hall, root);
            VikingPropVisualFactory.CreateCharacterAtLocalPoint(
                root,
                characterLocal,
                floorWorldY,
                VikingPropVisualFactory.CharacterWorldRotation,
                VikingBuildingVisualFactory.CharacterName);

            BuildPlayerShopArea(root, hall, bounds, floorWorldY);
            ConfigureJarlLandSpawnExclusions(bounds, root, hall);
            LakeBuilder.Build(parent, bounds);
            PlainsWorldBuilder.RebuildGroundExcludingLandFeatures(parent, bounds);
        }

        static void DestroyExistingChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null)
                Object.Destroy(existing.gameObject);
        }

        static void ConfigureJarlLandSpawnExclusions(CavernBounds bounds, Transform quarryRoot, GameObject hall)
        {
            if (bounds == null || quarryRoot == null)
                return;

            float pad = WorldScale.Feet(12f);

            if (hall != null && VikingPropVisualFactory.TryGetLocalBounds(hall, quarryRoot, out var hallBounds))
            {
                Vector3 min = bounds.transform.InverseTransformPoint(quarryRoot.TransformPoint(hallBounds.min));
                Vector3 max = bounds.transform.InverseTransformPoint(quarryRoot.TransformPoint(hallBounds.max));
                bounds.AddSpawnExclusion(min.x - pad, max.x + pad, min.z - pad, max.z + pad);
            }

            AddLocalExclusion(bounds, quarryRoot, QuarryCatalog.ResolveQuarryShopAnchorLocal(hall, quarryRoot), WorldScale.Feet(15f));
            AddLocalExclusion(bounds, quarryRoot, QuarryCatalog.ResolvePlayerSpawnLocal(hall, quarryRoot), WorldScale.Feet(10f));
            AddLocalExclusion(bounds, quarryRoot, QuarryCatalog.ResolveVikingCharacterLocal(hall, quarryRoot), WorldScale.Feet(10f));
        }

        static void AddLocalExclusion(CavernBounds bounds, Transform quarryRoot, Vector3 quarryLocal, float halfExtent)
        {
            Vector3 contentLocal = bounds.transform.InverseTransformPoint(quarryRoot.TransformPoint(quarryLocal));
            bounds.AddSpawnExclusion(
                contentLocal.x - halfExtent,
                contentLocal.x + halfExtent,
                contentLocal.z - halfExtent,
                contentLocal.z + halfExtent);
        }

        static void BuildPlayerShopArea(Transform quarryRoot, GameObject hall, CavernBounds bounds, float floorWorldY)
        {
            Vector3 shopAnchorLocal = QuarryCatalog.ResolveQuarryShopAnchorLocal(hall, quarryRoot);
            Vector3 spawnLocal = QuarryCatalog.ResolvePlayerSpawnLocal(hall, quarryRoot);
            Vector3 toPlayer = spawnLocal - shopAnchorLocal;
            toPlayer.y = 0f;
            Quaternion shopRotation = toPlayer.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(-toPlayer.normalized, Vector3.up)
                : Quaternion.Euler(0f, 90f, 0f);

            var shop = ShopAreaVisualFactory.Create(
                quarryRoot,
                shopAnchorLocal,
                shopRotation,
                floorWorldY,
                ShopAreaShopkeeperType.StrongMan,
                floorWorldY);

            if (shop.Board == null)
                return;

            var ctx = GameContext.Instance;
            if (ctx == null)
                return;

            if (ctx.Shop == null)
            {
                ctx.Shop = shop.Board.AddComponent<ShopManager>();
                ctx.Shop.Initialize(shop.Board.transform);
            }
            else
            {
                ctx.Shop.RegisterBuyStation(shop.Board.transform);
                var buyStation = shop.Board.GetComponent<ShopBuyStation>();
                if (buyStation != null)
                    buyStation.IsJarlLandShop = true;
            }

            if (shop.SlotCab == null)
                return;

            var slot = shop.SlotCab.AddComponent<SlotMachine>();
            slot.Initialize(shop.SlotCab.transform, SlotMachineVisualFactory.GetVisual(shop.SlotCab));
        }
    }
}
