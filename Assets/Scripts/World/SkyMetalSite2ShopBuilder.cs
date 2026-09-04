using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class SkyMetalSite2ShopBuilder
    {
        const float StructureExclusionPadFeet = 14f;
        const float ShopFallbackHalfExtentFeet = 20f;
        public const string ShopObjectName = "SkyMetalSite2Shop";

        public static Transform Build(CavernBounds bounds)
        {
            if (bounds == null)
                return null;

            var contentRoot = bounds.transform;
            DestroyExistingChild(contentRoot, ShopObjectName);

            Vector3 shopAnchorLocal = SkyMetalDigSiteCatalog.ResolveSecondSiteShopAnchorContentLocal();
            Vector2 site2 = SkyMetalDigSiteCatalog.GetSecondSiteContentLocalXZ();
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float siteGroundY = PlainsWorldBuilder.SamplePlainsLocalY(site2.x, site2.y, plainsBaseY);
            Vector3 site2Local = new Vector3(site2.x, siteGroundY, site2.y);

            Vector3 toSite = site2Local - shopAnchorLocal;
            toSite.y = 0f;
            Quaternion shopRotation = toSite.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(-toSite.normalized, Vector3.up)
                : Quaternion.identity;

            float floorWorldY = SkyMetalDigSiteCatalog.ResolveSecondSiteShopFloorWorldY(bounds);

            var shop = ShopAreaVisualFactory.Create(
                contentRoot,
                shopAnchorLocal,
                shopRotation,
                floorWorldY,
                ShopAreaShopkeeperType.Normal,
                floorWorldY);

            if (shop.ShopRoot != null)
                shop.ShopRoot.name = ShopObjectName;

            ConfigureShopEconomy(shop);
            AddSpawnExclusion(bounds, shop.ShopRoot, shopAnchorLocal);
            EnableShopRenderers(shop);

            return shop.ShopRoot;
        }

        static void EnableShopRenderers(ShopAreaVisualFactory.ShopAreaBuild shop)
        {
            EnableRenderers(shop.ShopRoot?.gameObject);
            EnableRenderers(shop.Counter);
            EnableRenderers(shop.Board);
            EnableRenderers(shop.SlotCab);
            EnableRenderers(shop.Shopkeeper);
        }

        static void EnableRenderers(GameObject root)
        {
            if (root == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
        }

        static void ConfigureShopEconomy(ShopAreaVisualFactory.ShopAreaBuild shop)
        {
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
                ctx.Shop.RegisterBuyStation(shop.Board.transform);

            if (shop.SlotCab == null)
                return;

            var slot = shop.SlotCab.GetComponent<SlotMachine>();
            if (slot == null)
            {
                slot = shop.SlotCab.AddComponent<SlotMachine>();
                slot.Initialize(shop.SlotCab.transform, SlotMachineVisualFactory.GetVisual(shop.SlotCab));
            }

            if (ctx.SlotMachine == null)
                ctx.SlotMachine = slot;
        }

        static void AddSpawnExclusion(CavernBounds bounds, Transform shopRoot, Vector3 shopAnchorLocal)
        {
            float pad = WorldScale.Feet(StructureExclusionPadFeet);
            if (shopRoot != null
                && VikingPropVisualFactory.TryGetLocalBounds(shopRoot.gameObject, bounds.transform, out var shopBounds))
            {
                bounds.AddSpawnExclusion(
                    shopBounds.min.x - pad,
                    shopBounds.max.x + pad,
                    shopBounds.min.z - pad,
                    shopBounds.max.z + pad);
                return;
            }

            AddContentPointExclusion(
                bounds,
                shopAnchorLocal,
                WorldScale.Feet(ShopFallbackHalfExtentFeet));
        }

        static void AddContentPointExclusion(CavernBounds bounds, Vector3 contentLocal, float halfExtent)
        {
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
