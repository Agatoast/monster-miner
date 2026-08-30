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
