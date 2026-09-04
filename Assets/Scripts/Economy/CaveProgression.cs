using System.Collections;
using MonsterMiner.Core;
using MonsterMiner.Player;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class CaveProgression : MonoBehaviour
    {
        const float BlastDuration = 10f;
        const string Cave2MapId = "cave_2";

        bool blastInProgress;

        public bool IsCave2Unlocked { get; private set; }
        public bool IsBlastInProgress => blastInProgress;
        public bool HasMinerWingsPermission { get; private set; }
        public bool MinerWingsConsumed { get; private set; }
        public bool HasWorldMap { get; private set; }
        public bool HasLandedOnLand { get; private set; }
        public bool HasLandQuarry2 { get; private set; }
        public bool HasLandQuarry3 { get; private set; }
        public bool HasLandQuarry4 { get; private set; }
        public bool HasLandQuarry5 { get; private set; }
        public bool JarlSkullQuestComplete { get; private set; }
        public bool Quarry3CompassReturned { get; private set; }
        public bool HasHeardSamuraiIntro { get; private set; }
        public bool ArtilleryTrialWon { get; private set; }
        public bool ArtilleryTrialLost { get; private set; }
        public bool Quest5Complete { get; private set; }

        public bool HasHeardJarlIntro { get; private set; }
        public bool HasMagicCompass => JarlSkullQuestComplete && !Quarry3CompassReturned;
        public bool HasSkyMetalDetector { get; private set; }
        public bool HasCompletedFirstSkyMetalDig { get; private set; }
        public bool HasCompletedSecondSkyMetalDig { get; private set; }
        public bool HasCompletedThirdSkyMetalDig { get; private set; }
        public bool HasLegendarySkyMetalMachineGun { get; private set; }
        public bool HasBoatRental { get; private set; }

        bool discoveredSkyMetalSite1;
        bool discoveredSkyMetalSite2;
        bool discoveredSkyMetalSite3;
        public bool CanEquipMinerWings =>
            HasMinerWingsPermission && !MinerWingsConsumed;

        public static bool HasPentachickHeartInInventory()
        {
            var ctx = GameContext.Instance;
            var heart = ctx?.Database?.pentachickHeartItem;
            return heart != null && ctx.Inventory != null && ctx.Inventory.ContainsItem(heart);
        }

        public void GrantMinerWingsPermission() => HasMinerWingsPermission = true;

        public void ConsumeMinerWings() => MinerWingsConsumed = true;

        public void CompleteMinerHeartTurnIn()
        {
            GrantMinerWingsPermission();
            GrantWorldMap();
        }

        public void GrantWorldMap()
        {
            if (HasWorldMap)
                return;

            HasWorldMap = true;
            GameContext.Instance?.SpawnManager?.SpawnHuntGroundEggs();
        }

        public void NotifyLandedOnLand()
        {
            if (HasLandedOnLand)
                return;

            HasLandedOnLand = true;
            UnlockLandQuarry2();
        }

        public void UnlockLandQuarry2()
        {
            HasLandQuarry2 = true;
            var builder = FindFirstObjectByType<CavernBuilder>();
            builder?.BuildLandQuarry2(GameContext.Instance?.CavernBounds);
            GameContext.Instance?.SpawnManager?.EnsureJarlLandEggsSpawned();
        }

        public void CompleteJarlSkullQuest()
        {
            if (JarlSkullQuestComplete)
                return;

            JarlSkullQuestComplete = true;
            UnlockLandQuarry3();
        }

        public void UnlockLandQuarry3()
        {
            if (HasLandQuarry3)
                return;

            HasLandQuarry3 = true;
            var builder = FindFirstObjectByType<CavernBuilder>();
            builder?.BuildLandQuarry3(GameContext.Instance?.CavernBounds);
            GameContext.Instance?.SpawnManager?.EnsureLandQuarry3EggsSpawned();
        }

        public void UnlockLandQuarry4()
        {
            if (HasLandQuarry4)
                return;

            HasLandQuarry4 = true;
            var builder = FindFirstObjectByType<CavernBuilder>();
            builder?.BuildLandQuarry4(GameContext.Instance?.CavernBounds);
            GameContext.Instance?.SpawnManager?.EnsureLandQuarry4EggsSpawned();
        }

        public void CompleteQuarry3CompassReturn()
        {
            Quarry3CompassReturned = true;
        }

        public void GrantSkyMetalDetector()
        {
            if (HasSkyMetalDetector)
                return;

            HasSkyMetalDetector = true;
            SkyMetalDigSiteManager.EnsureForDetector();
        }

        public void CompleteFirstSkyMetalDig()
        {
            if (HasCompletedFirstSkyMetalDig)
                return;

            HasCompletedFirstSkyMetalDig = true;
            SkyMetalDigSiteManager.EnsureSecondSiteSpawned();
        }

        public void CompleteSecondSkyMetalDig()
        {
            if (HasCompletedSecondSkyMetalDig)
                return;

            HasCompletedSecondSkyMetalDig = true;
            SkyMetalDigSiteManager.EnsureThirdSiteSpawned();
        }

        public void CompleteThirdSkyMetalDig()
        {
            HasCompletedThirdSkyMetalDig = true;
        }

        public void CompleteSkyMetalMachineGunTurnIn()
        {
            HasLegendarySkyMetalMachineGun = true;
        }

        public bool HasDiscoveredSkyMetalSite(int siteIndex)
        {
            return siteIndex switch
            {
                SkyMetalDigSiteCatalog.FirstSiteIndex => discoveredSkyMetalSite1,
                SkyMetalDigSiteCatalog.SecondSiteIndex => discoveredSkyMetalSite2,
                SkyMetalDigSiteCatalog.ThirdSiteIndex => discoveredSkyMetalSite3,
                _ => false
            };
        }

        public void DiscoverSkyMetalSite(int siteIndex)
        {
            switch (siteIndex)
            {
                case SkyMetalDigSiteCatalog.FirstSiteIndex:
                    discoveredSkyMetalSite1 = true;
                    break;
                case SkyMetalDigSiteCatalog.SecondSiteIndex:
                    discoveredSkyMetalSite2 = true;
                    break;
                case SkyMetalDigSiteCatalog.ThirdSiteIndex:
                    discoveredSkyMetalSite3 = true;
                    break;
            }
        }

        public void MarkSamuraiIntroHeard()
        {
            HasHeardSamuraiIntro = true;
        }

        public void MarkJarlIntroHeard()
        {
            HasHeardJarlIntro = true;
        }

        public void CompleteArtilleryTrial()
        {
            ArtilleryTrialWon = true;
            ArtilleryTrialLost = false;
            UnlockLandQuarry4();
        }

        public void MarkArtilleryTrialLost()
        {
            if (ArtilleryTrialWon)
                return;

            ArtilleryTrialLost = true;
        }

        public void ClearArtilleryTrialLost()
        {
            ArtilleryTrialLost = false;
        }

        public void CompleteBoatRental()
        {
            HasBoatRental = true;
        }

        public void SyncUnlockedLandmarks()
        {
            if (JarlSkullQuestComplete)
                UnlockLandQuarry3();

            if (ArtilleryTrialWon)
                UnlockLandQuarry4();
        }

        public void CompleteQuest5()
        {
            Quest5Complete = true;
            HasLandQuarry5 = true;
        }

        public void BeginBlastSequence()
        {
            if (blastInProgress || IsCave2Unlocked)
                return;

            StartCoroutine(RunBlastSequence());
        }

        IEnumerator RunBlastSequence()
        {
            blastInProgress = true;

            var ctx = GameContext.Instance;
            ctx?.Hud?.ShowMessage("The miner blasts a path to the next cave!");

            var shake = ctx?.Player?.GetComponent<PlayerCameraShake>();
            if (shake == null && ctx?.Player != null)
                shake = ctx.Player.gameObject.AddComponent<PlayerCameraShake>();
            shake?.BeginViolentShake(BlastDuration);

            float elapsed = 0f;
            while (elapsed < BlastDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            var builder = FindFirstObjectByType<CavernBuilder>();
            builder?.OpenCave2Passage();

            IsCave2Unlocked = true;
            blastInProgress = false;

            if (ctx?.CavernBounds != null)
                ctx.CavernBounds.SetCave2Unlocked(true);

            if (ctx?.Shop != null)
                ctx.Shop.SetCurrentMapId(Cave2MapId);

            ctx?.Hud?.ShowMessage("Cave 2 is now open!");
        }
    }
}
