using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class LandQuarry3Builder
    {
        const float TahotoBehindGuideFeet = 48f;
        const float Rock5EastBeyondEdgeFeet = 30f;
        const float Rock5EastShiftFeet = 50f;
        const float StructureExclusionPadFeet = 14f;
        const float ShopExclusionHalfExtentFeet = 20f;
        const float FallbackGuideHalfExtentFeet = 16f;
        const float FallbackPagodaHalfExtentFeet = 38f;

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

            float radius = LandQuarry3Boundary.QuarryRadius;
            var floorMaterial = CavernSurfaceMaterialFactory.GetFloorMaterial();
            QuarryFloorBuilder.CreateBowlFloor(root, radius, 0f, 0f, floorMaterial);
            QuarryFloorBuilder.CreateBowlCollision(root, radius, 0f, 0f);

            float floorWorldY = bounds.transform.TransformPoint(new Vector3(center.x, groundY, center.y)).y;
            var guideRotation = Quaternion.Euler(0f, 180f, 0f);
            Vector3 guideLocalPosition = Vector3.zero;

            var pagoda = PagodaVisualFactory.CreateTahoto20mAtLocalPoint(
                root,
                new Vector3(0f, 0f, WorldScale.Feet(TahotoBehindGuideFeet)),
                floorWorldY,
                guideRotation);

            var guide = LowPolyPeopleVisualFactory.CreateQuarry3QuestNpc(
                root,
                guideLocalPosition,
                guideRotation,
                floorWorldY);

            var shop = BuildPlayerShopArea(root, floorWorldY);

            var rock = NatureRockVisualFactory.CreateInterlockedRock5Pair(
                root,
                new Vector3(
                    LandQuarry3Boundary.QuarryRadius + WorldScale.Feet(Rock5EastBeyondEdgeFeet + Rock5EastShiftFeet),
                    0f,
                    0f),
                floorWorldY,
                scale: 2f);

            ConfigureSpawnExclusions(bounds, root, pagoda, guide, shop.ShopRoot, rock, center);
            GameContext.Instance?.SpawnManager?.CullEggsInSpawnExclusions();
            PlainsWorldBuilder.RebuildGroundExcludingLandFeatures(parent, bounds);
        }

        static ShopAreaVisualFactory.ShopAreaBuild BuildPlayerShopArea(Transform quarryRoot, float floorWorldY)
        {
            Vector3 shopAnchorLocal = QuarryCatalog.ResolveQuarry3ShopAnchorLocal();
            Vector3 spawnLocal = QuarryCatalog.ResolveQuarry3PlayerSpawnLocal();
            Vector3 toPlayer = spawnLocal - shopAnchorLocal;
            toPlayer.y = 0f;
            Quaternion shopRotation = toPlayer.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(-toPlayer.normalized, Vector3.up)
                : Quaternion.Euler(0f, 180f, 0f);

            var shop = ShopAreaVisualFactory.Create(
                quarryRoot,
                shopAnchorLocal,
                shopRotation,
                floorWorldY,
                ShopAreaShopkeeperType.Quarry3Ashigaru,
                floorWorldY);

            if (shop.Board == null)
                return shop;

            var ctx = GameContext.Instance;
            if (ctx != null)
            {
                if (ctx.Shop == null)
                {
                    ctx.Shop = shop.Board.AddComponent<ShopManager>();
                    ctx.Shop.Initialize(shop.Board.transform);
                }
                else
                    ctx.Shop.RegisterBuyStation(shop.Board.transform);

                var buyStation = shop.Board.GetComponent<ShopBuyStation>();
                if (buyStation != null)
                {
                    buyStation.IsJarlLandShop = true;
                    buyStation.IsQuarry3Shop = true;
                }
            }

            if (shop.SlotCab != null)
            {
                var slot = shop.SlotCab.AddComponent<SlotMachine>();
                slot.Initialize(shop.SlotCab.transform, SlotMachineVisualFactory.GetVisual(shop.SlotCab));

                if (ctx != null && ctx.SlotMachine == null)
                    ctx.SlotMachine = slot;
            }

            return shop;
        }

        static void ConfigureSpawnExclusions(
            CavernBounds bounds,
            Transform quarryRoot,
            GameObject pagoda,
            GameObject guide,
            Transform shopRoot,
            GameObject rock,
            Vector2 quarryCenterContent)
        {
            bounds.ClearSpawnExclusionsNear(
                quarryCenterContent.x,
                quarryCenterContent.y,
                LandQuarry3Boundary.QuarryRadius);

            float pad = WorldScale.Feet(StructureExclusionPadFeet);

            if (guide != null && VikingPropVisualFactory.TryGetLocalBounds(guide, quarryRoot, out var guideBounds))
                AddContentBoundsExclusion(bounds, quarryRoot, guideBounds, pad);
            else
                AddContentPointExclusion(bounds, quarryRoot, Vector3.zero, WorldScale.Feet(FallbackGuideHalfExtentFeet));

            if (pagoda != null && VikingPropVisualFactory.TryGetLocalBounds(pagoda, quarryRoot, out var pagodaBounds))
                AddContentBoundsExclusion(bounds, quarryRoot, pagodaBounds, pad);
            else
            {
                AddContentPointExclusion(
                    bounds,
                    quarryRoot,
                    new Vector3(0f, 0f, WorldScale.Feet(TahotoBehindGuideFeet)),
                    WorldScale.Feet(FallbackPagodaHalfExtentFeet));
            }

            if (shopRoot != null && VikingPropVisualFactory.TryGetLocalBounds(shopRoot.gameObject, quarryRoot, out var shopBounds))
                AddContentBoundsExclusion(bounds, quarryRoot, shopBounds, pad);
            else
                AddContentPointExclusion(
                    bounds,
                    quarryRoot,
                    QuarryCatalog.ResolveQuarry3ShopAnchorLocal(),
                    WorldScale.Feet(ShopExclusionHalfExtentFeet));

            if (rock != null && VikingPropVisualFactory.TryGetLocalBounds(rock, quarryRoot, out var rockBounds))
                AddContentBoundsExclusion(bounds, quarryRoot, rockBounds, pad);
        }

        static void AddContentBoundsExclusion(
            CavernBounds bounds,
            Transform quarryRoot,
            Bounds quarryLocalBounds,
            float pad)
        {
            Vector3 min = bounds.transform.InverseTransformPoint(quarryRoot.TransformPoint(quarryLocalBounds.min));
            Vector3 max = bounds.transform.InverseTransformPoint(quarryRoot.TransformPoint(quarryLocalBounds.max));
            bounds.AddSpawnExclusion(min.x - pad, max.x + pad, min.z - pad, max.z + pad);
        }

        static void AddContentPointExclusion(
            CavernBounds bounds,
            Transform quarryRoot,
            Vector3 quarryLocal,
            float halfExtent)
        {
            Vector3 contentLocal = bounds.transform.InverseTransformPoint(quarryRoot.TransformPoint(quarryLocal));
            bounds.AddSpawnExclusion(
                contentLocal.x - halfExtent,
                contentLocal.x + halfExtent,
                contentLocal.z - halfExtent,
                contentLocal.z + halfExtent);
        }

        static void DestroyExistingChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null)
                Object.Destroy(existing.gameObject);
        }
    }
}
