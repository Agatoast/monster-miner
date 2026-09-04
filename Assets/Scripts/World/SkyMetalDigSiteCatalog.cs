using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class SkyMetalDigSiteCatalog
    {
        public const int FirstSiteIndex = 0;
        public const int SecondSiteIndex = 1;
        public const int ThirdSiteIndex = 2;
        public const int FirstSiteRequiredStrikes = 50;
        public const float SecondSiteWestOfFirstMiles = 0.5f;
        public const float SecondSiteSouthOfFirstMiles = 0.5f;
        public const float ThirdSiteEastOfFirstMiles = 1f;

        public static bool SiteHasDigMechanics(int siteIndex) =>
            siteIndex == FirstSiteIndex || siteIndex == ThirdSiteIndex;

        public static int GetRequiredStrikes(int siteIndex) => FirstSiteRequiredStrikes;

        public static string GetMapLabel(int siteIndex)
        {
            return siteIndex switch
            {
                SecondSiteIndex => "Sky X-2",
                ThirdSiteIndex => "Sky X-3",
                _ => "Sky X-1"
            };
        }
        public const float MarkerSizeFeet = 20f;
        public const float ArrivalRadiusFeet = 120f;
        public const float MaxHoleDepthFeet = 8f;
        public const float OrinSouthOfQuarry4CenterFeet = 50f;
        public const float SecondSiteShopSouthOfDigSiteFeet = 50f;
        public const float SecondSitePlayerSpawnNorthOfDigSiteFeet = 30f;

        public static readonly Color DetectorBlue = new Color(0.18f, 0.42f, 0.82f, 1f);

        public static readonly Color MarkerColor = DetectorBlue;

        public static Vector2 GetOrinContentLocalXZ()
        {
            var quarry4 = QuarryCatalog.GetLandQuarry4Center();
            return new Vector2(quarry4.x, quarry4.y - WorldScale.Feet(OrinSouthOfQuarry4CenterFeet));
        }

        public static Vector2 GetFirstSiteContentLocalXZ()
        {
            var orin = GetOrinContentLocalXZ();
            return new Vector2(orin.x + WorldScale.Miles(1f), orin.y - WorldScale.Miles(1f));
        }

        public static Vector3 ResolveSiteWorld(CavernBounds bounds, Vector2 contentLocalXZ)
        {
            if (bounds == null)
                return Vector3.zero;

            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(contentLocalXZ.x, contentLocalXZ.y, plainsBaseY);
            return bounds.transform.TransformPoint(new Vector3(contentLocalXZ.x, groundY, contentLocalXZ.y));
        }

        public static Vector2 GetSecondSiteContentLocalXZ()
        {
            var first = GetFirstSiteContentLocalXZ();
            return new Vector2(
                first.x - WorldScale.Miles(SecondSiteWestOfFirstMiles),
                first.y - WorldScale.Miles(SecondSiteSouthOfFirstMiles));
        }

        public static Vector2 GetSecondSiteShopContentLocalXZ()
        {
            Vector2 site2 = GetSecondSiteContentLocalXZ();
            return new Vector2(
                site2.x,
                site2.y - WorldScale.Feet(SecondSiteShopSouthOfDigSiteFeet));
        }

        public static Vector2 GetSecondSitePlayerSpawnContentLocalXZ()
        {
            Vector2 site2 = GetSecondSiteContentLocalXZ();
            return new Vector2(
                site2.x,
                site2.y + WorldScale.Feet(SecondSitePlayerSpawnNorthOfDigSiteFeet));
        }

        public static Vector3 ResolveSecondSitePlayerSpawnContentLocal()
        {
            Vector2 spawnXz = GetSecondSitePlayerSpawnContentLocalXZ();
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(spawnXz.x, spawnXz.y, plainsBaseY);
            return new Vector3(spawnXz.x, groundY, spawnXz.y);
        }

        public static Vector3 ResolveSecondSiteShopAnchorContentLocal()
        {
            Vector2 shopXz = GetSecondSiteShopContentLocalXZ();
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(shopXz.x, shopXz.y, plainsBaseY);
            return new Vector3(shopXz.x, groundY, shopXz.y);
        }

        public static float ResolveSecondSiteShopFloorWorldY(CavernBounds bounds)
        {
            if (bounds == null)
                return 0f;

            return bounds.transform.TransformPoint(ResolveSecondSiteShopAnchorContentLocal()).y;
        }

        public static Vector2 GetThirdSiteContentLocalXZ()
        {
            var first = GetFirstSiteContentLocalXZ();
            return new Vector2(first.x + WorldScale.Miles(ThirdSiteEastOfFirstMiles), first.y);
        }

        public static Vector2 GetSiteContentLocalXZ(int siteIndex)
        {
            return siteIndex switch
            {
                SecondSiteIndex => GetSecondSiteContentLocalXZ(),
                ThirdSiteIndex => GetThirdSiteContentLocalXZ(),
                _ => GetFirstSiteContentLocalXZ()
            };
        }

        public static Vector3 ResolveFirstSiteWorld(CavernBounds bounds) =>
            ResolveSiteWorld(bounds, GetFirstSiteContentLocalXZ());

        public static Vector3 ResolveSecondSiteWorld(CavernBounds bounds) =>
            ResolveSiteWorld(bounds, GetSecondSiteContentLocalXZ());

        public static Vector3 ResolveThirdSiteWorld(CavernBounds bounds) =>
            ResolveSiteWorld(bounds, GetThirdSiteContentLocalXZ());

        public static Vector3 ResolveSiteWorld(CavernBounds bounds, int siteIndex) =>
            ResolveSiteWorld(bounds, GetSiteContentLocalXZ(siteIndex));
    }
}
