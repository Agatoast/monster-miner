using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public class SkyMetalDigSiteManager : MonoBehaviour
    {
        SkyMetalDigSite firstSite;
        SkyMetalDigSite secondSite;
        SkyMetalDigSite thirdSite;
        Transform secondSiteShopRoot;

        public static SkyMetalDigSiteManager Instance { get; private set; }

        public static void EnsureForDetector()
        {
            if (GameContext.Instance?.CaveProgression == null
                || !GameContext.Instance.CaveProgression.HasSkyMetalDetector)
                return;

            if (Instance == null)
            {
                var go = new GameObject("SkyMetalDigSiteManager");
                go.transform.SetParent(GameContext.Instance.transform, false);
                Instance = go.AddComponent<SkyMetalDigSiteManager>();
            }

            Instance.EnsureFirstSiteSpawned();
            Instance.EnsureSecondSiteSpawnedInternal();
            Instance.EnsureThirdSiteSpawnedInternal();
        }

        public static void EnsureSecondSiteSpawned()
        {
            EnsureForDetector();
            Instance?.EnsureSecondSiteSpawnedInternal();
        }

        public static void EnsureThirdSiteSpawned()
        {
            EnsureForDetector();
            Instance?.EnsureThirdSiteSpawnedInternal();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void EnsureFirstSiteSpawned()
        {
            if (firstSite != null)
                return;

            var progression = GameContext.Instance?.CaveProgression;
            if (progression != null && progression.HasCompletedFirstSkyMetalDig)
                return;

            SpawnSite(SkyMetalDigSiteCatalog.FirstSiteIndex, ref firstSite);
        }

        void EnsureSecondSiteSpawnedInternal()
        {
            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasCompletedFirstSkyMetalDig)
                return;

            if (secondSite == null && !progression.HasCompletedSecondSkyMetalDig)
                SpawnSite(SkyMetalDigSiteCatalog.SecondSiteIndex, ref secondSite);

            EnsureSecondSiteShopSpawned();
        }

        void EnsureSecondSiteShopSpawned()
        {
            if (secondSiteShopRoot != null)
                return;

            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasCompletedFirstSkyMetalDig)
                return;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return;

            secondSiteShopRoot = SkyMetalSite2ShopBuilder.Build(bounds);
        }

        void EnsureThirdSiteSpawnedInternal()
        {
            if (thirdSite != null)
                return;

            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasCompletedSecondSkyMetalDig)
                return;

            if (progression.HasCompletedThirdSkyMetalDig)
                return;

            SpawnSite(SkyMetalDigSiteCatalog.ThirdSiteIndex, ref thirdSite);
        }

        void SpawnSite(int siteIndex, ref SkyMetalDigSite siteField)
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return;

            Vector3 worldPosition = SkyMetalDigSiteCatalog.ResolveSiteWorld(bounds, siteIndex);
            siteField = SkyMetalDigSite.Create(siteIndex, worldPosition);
            siteField.transform.SetParent(transform, true);
            siteField.TryRevealMarkerForNearbyPlayer();
        }

        public static bool TryRegisterPickaxeStrike(RaycastHit hit)
        {
            EnsureForDetector();
            EnsureActiveDigSiteSpawned();
            if (Instance == null)
                return false;

            var site = hit.collider.GetComponentInParent<SkyMetalDigSite>();
            if (site != null)
                return site.TryRegisterStrike(hit.point);

            if (!FloorColliderUtility.IsWalkSurfaceCollider(hit.collider))
                return false;

            var activeSite = GetActiveDigSite();
            if (activeSite != null && activeSite.TryRegisterStrike(hit.point))
                return true;

            if (Instance.firstSite != null && Instance.firstSite.TryRegisterStrike(hit.point))
                return true;

            if (Instance.secondSite != null && Instance.secondSite.TryRegisterStrike(hit.point))
                return true;

            return Instance.thirdSite != null && Instance.thirdSite.TryRegisterStrike(hit.point);
        }

        static void EnsureActiveDigSiteSpawned()
        {
            if (Instance == null)
                return;

            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasSkyMetalDetector)
                return;

            if (!progression.HasCompletedFirstSkyMetalDig)
                Instance.EnsureFirstSiteSpawned();
            else if (!progression.HasCompletedSecondSkyMetalDig)
                Instance.EnsureSecondSiteSpawnedInternal();
            else if (!progression.HasCompletedThirdSkyMetalDig)
                Instance.EnsureThirdSiteSpawnedInternal();
        }

        static SkyMetalDigSite GetActiveDigSite()
        {
            if (Instance == null)
                return null;

            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasSkyMetalDetector)
                return null;

            if (!progression.HasCompletedFirstSkyMetalDig)
                return Instance.firstSite;

            if (!progression.HasCompletedSecondSkyMetalDig)
                return Instance.secondSite;

            if (!progression.HasCompletedThirdSkyMetalDig)
                return Instance.thirdSite;

            return null;
        }

        public static Vector3? GetActiveCompassTargetWorld()
        {
            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasSkyMetalDetector)
                return null;

            var bounds = GameContext.Instance?.CavernBounds;

            if (!progression.HasCompletedFirstSkyMetalDig)
                return ResolveCompassTarget(bounds, SkyMetalDigSiteCatalog.FirstSiteIndex, Instance?.firstSite);

            if (!progression.HasCompletedSecondSkyMetalDig)
            {
                EnsureSecondSiteSpawned();
                return ResolveCompassTarget(bounds, SkyMetalDigSiteCatalog.SecondSiteIndex, Instance?.secondSite);
            }

            if (progression.HasCompletedThirdSkyMetalDig)
                return null;

            EnsureThirdSiteSpawned();
            return ResolveCompassTarget(bounds, SkyMetalDigSiteCatalog.ThirdSiteIndex, Instance?.thirdSite);
        }

        static Vector3? ResolveCompassTarget(CavernBounds bounds, int siteIndex, SkyMetalDigSite spawnedSite)
        {
            if (spawnedSite != null)
                return spawnedSite.WorldCenter;

            if (bounds == null)
                return null;

            return SkyMetalDigSiteCatalog.ResolveSiteWorld(bounds, siteIndex);
        }
    }
}
