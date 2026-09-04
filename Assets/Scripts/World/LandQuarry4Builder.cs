using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class LandQuarry4Builder
    {
        const float ShopEastOfOrinFeet = 30f;
        const float BlacksmithWestOfOrinFeet = 30f;
        const float OrinCampSouthOffsetFeet = 50f;
        const float StructureExclusionPadFeet = 14f;
        const float OrinFallbackHalfExtentFeet = 18f;
        const float ShopFallbackHalfExtentFeet = 20f;
        const float BlacksmithFallbackHalfExtentFeet = 55f;

        static Vector3 ResolveOrinCampLocalOffset() =>
            new Vector3(0f, 0f, -WorldScale.Feet(OrinCampSouthOffsetFeet));

        static Vector3 ResolveOrinLocalPosition() => ResolveOrinCampLocalOffset();

        public static void Build(Transform parent, CavernBounds bounds)
        {
            if (parent == null || bounds == null)
                return;

            DestroyExistingChild(parent, "LandQuarry4");

            var center = QuarryCatalog.GetLandQuarry4Center();
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(center.x, center.y, plainsBaseY);

            var root = new GameObject("LandQuarry4").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(center.x, groundY, center.y);

            float radius = LandQuarry4Boundary.QuarryRadius;
            var floorMaterial = CavernSurfaceMaterialFactory.GetFloorMaterial();
            QuarryFloorBuilder.CreateBowlFloor(root, radius, 0f, 0f, floorMaterial);
            QuarryFloorBuilder.CreateBowlCollision(root, radius, 0f, 0f);

            bounds.ClearSpawnExclusionsNear(center.x, center.y, radius);

            float floorWorldY = bounds.transform.TransformPoint(new Vector3(center.x, groundY, center.y)).y;
            Vector3 orinLocalPosition = ResolveOrinLocalPosition();
            var orin = OrinVisualFactory.CreateAtLocalPoint(
                root,
                orinLocalPosition,
                floorWorldY,
                Quaternion.Euler(0f, 180f, 0f));

            var blacksmith = BuildBlacksmithArea(root, floorWorldY);
            var shop = BuildPlayerShopArea(root, floorWorldY);

            ConfigureSpawnExclusions(bounds, root, orin, blacksmith, shop.ShopRoot);

            PlainsWorldBuilder.RebuildGroundExcludingLandFeatures(parent, bounds);
            GameContext.Instance?.SpawnManager?.CullEggsInSpawnExclusions();
        }

        static GameObject BuildBlacksmithArea(Transform quarryRoot, float floorWorldY)
        {
            DestroyExistingChild(quarryRoot, BlacksmithVisualFactory.BlacksmithObjectName);

            Vector3 blacksmithAnchorLocal = new Vector3(-WorldScale.Feet(BlacksmithWestOfOrinFeet), 0f, 0f);
            Vector3 toOrin = Vector3.zero - blacksmithAnchorLocal;
            toOrin.y = 0f;
            Quaternion blacksmithRotation = toOrin.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(toOrin.normalized, Vector3.up)
                : Quaternion.LookRotation(Vector3.right, Vector3.up);

            return BlacksmithVisualFactory.CreateAtLocalPoint(
                quarryRoot,
                blacksmithAnchorLocal,
                floorWorldY,
                blacksmithRotation);
        }

        static ShopAreaVisualFactory.ShopAreaBuild BuildPlayerShopArea(Transform quarryRoot, float floorWorldY)
        {
            DestroyExistingChild(quarryRoot, "ShopArea");

            Vector3 orinLocalPosition = ResolveOrinLocalPosition();
            Vector3 shopAnchorLocal = orinLocalPosition + new Vector3(WorldScale.Feet(ShopEastOfOrinFeet), 0f, 0f);
            Vector3 toOrin = orinLocalPosition - shopAnchorLocal;
            toOrin.y = 0f;
            // Counter/board sit on local -Z; negate so that side faces Orin (same as Q2/Q3 shops).
            Quaternion shopRotation = toOrin.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(-toOrin.normalized, Vector3.up)
                : Quaternion.LookRotation(Vector3.left, Vector3.up);

            var shop = ShopAreaVisualFactory.Create(
                quarryRoot,
                shopAnchorLocal,
                shopRotation,
                floorWorldY,
                ShopAreaShopkeeperType.Quarry4StrongWoman,
                floorWorldY);

            if (shop.Board != null)
            {
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
                        buyStation.IsQuarry4Shop = true;
                }
            }

            if (shop.SlotCab != null)
            {
                var slot = shop.SlotCab.AddComponent<SlotMachine>();
                slot.Initialize(shop.SlotCab.transform, SlotMachineVisualFactory.GetVisual(shop.SlotCab));

                var ctx = GameContext.Instance;
                if (ctx != null && ctx.SlotMachine == null)
                    ctx.SlotMachine = slot;
            }

            return shop;
        }

        static void ConfigureSpawnExclusions(
            CavernBounds bounds,
            Transform quarryRoot,
            GameObject orin,
            GameObject blacksmith,
            Transform shopRoot)
        {
            float pad = WorldScale.Feet(StructureExclusionPadFeet);

            if (orin != null && VikingPropVisualFactory.TryGetLocalBounds(orin, quarryRoot, out var orinBounds))
                AddContentBoundsExclusion(bounds, quarryRoot, orinBounds, pad);
            else
                AddContentPointExclusion(
                    bounds,
                    quarryRoot,
                    ResolveOrinLocalPosition(),
                    WorldScale.Feet(OrinFallbackHalfExtentFeet));

            if (shopRoot != null && VikingPropVisualFactory.TryGetLocalBounds(shopRoot.gameObject, quarryRoot, out var shopBounds))
                AddContentBoundsExclusion(bounds, quarryRoot, shopBounds, pad);
            else
            {
                Vector3 shopAnchorLocal = ResolveOrinLocalPosition()
                    + new Vector3(WorldScale.Feet(ShopEastOfOrinFeet), 0f, 0f);
                AddContentPointExclusion(
                    bounds,
                    quarryRoot,
                    shopAnchorLocal,
                    WorldScale.Feet(ShopFallbackHalfExtentFeet));
            }

            if (blacksmith != null && VikingPropVisualFactory.TryGetLocalBounds(blacksmith, quarryRoot, out var blacksmithBounds))
                AddContentBoundsExclusion(bounds, quarryRoot, blacksmithBounds, pad);
            else
            {
                AddContentPointExclusion(
                    bounds,
                    quarryRoot,
                    new Vector3(-WorldScale.Feet(BlacksmithWestOfOrinFeet), 0f, 0f),
                    WorldScale.Feet(BlacksmithFallbackHalfExtentFeet));
            }
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
