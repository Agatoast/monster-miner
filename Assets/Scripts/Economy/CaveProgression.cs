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
            ctx?.Hud?.ShowMessage("The miner blasts the wall!");

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
