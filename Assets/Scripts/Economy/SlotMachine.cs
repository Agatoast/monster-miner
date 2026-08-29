using System.Collections;
using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Interaction;
using MonsterMiner.Inventory;
using MonsterMiner.Util;
using ithappy.Casino;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class SlotMachine : MonoBehaviour, IInteractable
    {
        const int WinPresetCount = 3;
        const int LosePresetStartIndex = 3;
        const int LosePresetCount = 5;
        const int TestTokenSpinValue = 10;

        const float NothingChance = 0.50f;
        const float HalfValueChance = 0.40f;
        const float LegendaryChance = 0.001f;

        Transform body;
        PresetUVSlotMachine visual;
        SlotResultBanner resultBanner;
        bool spinning;

        public void Initialize(Transform machineBody, PresetUVSlotMachine visualSlot)
        {
            body = machineBody;
            visual = visualSlot;

            if (body != null)
            {
                resultBanner = body.GetComponent<SlotResultBanner>();
                if (resultBanner == null)
                    resultBanner = body.gameObject.AddComponent<SlotResultBanner>();

                resultBanner.Initialize(body.gameObject);
            }
        }

        public string GetPrompt()
        {
            if (spinning)
                return "Spinning...";

            var slot = GameContext.Instance?.Inventory?.GetSelectedSlot();
            if (slot != null && !slot.IsEmpty && InventorySystem.IsSlotTestToken(slot.item))
                return "Spin slot (test token) [E]";

            if (slot != null && !slot.IsEmpty && slot.item != null && slot.item.isMonsterDrop)
                return "Spin slot (1 monster drop) [E]";

            return "Spin slot (monster drop or test token) [E]";
        }

        public bool CanInteract(GameObject interactor) => !spinning;

        public void Interact(GameObject interactor)
        {
            var ctx = GameContext.Instance;
            if (ctx == null || spinning)
                return;

            var slot = ctx.Inventory.GetSelectedSlot();
            if (slot == null || slot.IsEmpty || slot.item == null)
            {
                ctx.Hud?.ShowMessage("Select a monster drop or the Slot Test Token");
                return;
            }

            bool isTestToken = InventorySystem.IsSlotTestToken(slot.item);
            if (!isTestToken && !slot.item.isMonsterDrop)
            {
                ctx.Hud?.ShowMessage("Select a monster drop or the Slot Test Token");
                return;
            }

            int gambledValue = isTestToken ? TestTokenSpinValue : slot.item.sellValue;
            if (!isTestToken && !ctx.Inventory.TryRemoveFromSelected(1))
                return;

            StartCoroutine(SpinRoutine(ctx, gambledValue));
        }

        IEnumerator SpinRoutine(GameContext ctx, int gambledValue)
        {
            spinning = true;
            resultBanner?.Hide();

            SpinOutcome outcome = RollOutcome(ctx, gambledValue);

            if (visual != null)
            {
                visual.SpinPresetByIndex(outcome.VisualPresetIndex);
                while (visual.IsSpinning)
                    yield return null;
            }

            resultBanner?.Show(outcome.BannerText);
            outcome.Apply(ctx);
            ctx.Hud?.ShowMessage($"Slot: {outcome.Message}");
            PlayTone(outcome.TonePitch);
            spinning = false;
        }

        static SpinOutcome CreateLegendaryOutcome()
        {
            return new SpinOutcome(
                useWinVisual: true,
                bannerText: "Legendary Weapon!",
                message: "JACKPOT! Legendary Blade!",
                tonePitch: 880f,
                apply: c => c.Inventory.TryAdd(c.Database.legendaryWeaponItem, 1));
        }

        static SpinOutcome CreateLoseOutcome()
        {
            return new SpinOutcome(
                useWinVisual: false,
                bannerText: "You Lose!",
                message: "No prize",
                tonePitch: 220f,
                apply: _ => { });
        }

        static SpinOutcome CreateHalfValueOutcome(int gambledValue)
        {
            int payout = Mathf.Max(1, gambledValue / 2);
            return new SpinOutcome(
                useWinVisual: false,
                bannerText: "Half Your Cash Back",
                message: $"Half back ${payout}",
                tonePitch: 440f,
                apply: c => c.Wallet.Add(payout));
        }

        static SpinOutcome CreateSkinOutcome(GameContext ctx)
        {
            string skinMessage = "Skin unlocked!";
            if (ctx.ItemSkins != null && ctx.ItemSkins.TryGrantRandomSkin(out var skin) && skin != null)
                skinMessage = $"Unlocked {skin.displayName} skin for {ctx.ItemSkins.ResolveTargetDisplayName(skin, ctx.Database)}.";
            else
                skinMessage = "Skin prize rolled, but no new skins are available.";

            return new SpinOutcome(
                useWinVisual: true,
                bannerText: "Skin!",
                message: skinMessage,
                tonePitch: 660f,
                apply: _ => { });
        }

        static SpinOutcome RollOutcome(GameContext ctx, int gambledValue)
        {
            float roll = Random.value;

            if (roll < LegendaryChance)
                return CreateLegendaryOutcome();

            if (roll < LegendaryChance + NothingChance)
                return CreateLoseOutcome();

            if (roll < LegendaryChance + NothingChance + HalfValueChance)
                return CreateHalfValueOutcome(gambledValue);

            return CreateSkinOutcome(ctx);
        }

        void PlayTone(float pitch)
        {
            if (body == null)
                return;

            var audio = body.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.pitch = pitch / 440f;
        }

        readonly struct SpinOutcome
        {
            public readonly bool UseWinVisual;
            public readonly string BannerText;
            public readonly string Message;
            public readonly float TonePitch;
            public readonly int VisualPresetIndex;
            readonly System.Action<GameContext> apply;

            public SpinOutcome(
                bool useWinVisual,
                string bannerText,
                string message,
                float tonePitch,
                System.Action<GameContext> apply)
            {
                UseWinVisual = useWinVisual;
                BannerText = bannerText;
                Message = message;
                TonePitch = tonePitch;
                VisualPresetIndex = useWinVisual
                    ? Random.Range(0, WinPresetCount)
                    : Random.Range(LosePresetStartIndex, LosePresetStartIndex + LosePresetCount);
                this.apply = apply;
            }

            public void Apply(GameContext ctx) => apply?.Invoke(ctx);
        }
    }
}
