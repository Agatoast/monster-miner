using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Interaction;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class SlotMachine : MonoBehaviour, IInteractable
    {
        Transform body;
        bool spinning;

        public void Initialize(Transform machineBody)
        {
            body = machineBody;
        }

        public string GetPrompt() => spinning ? "Spinning..." : "Spin slot (1 monster drop) [E]";

        public bool CanInteract(GameObject interactor) => !spinning;

        public void Interact(GameObject interactor)
        {
            var ctx = GameContext.Instance;
            if (ctx == null || spinning)
                return;

            var slot = ctx.Inventory.GetSelectedSlot();
            if (slot == null || slot.IsEmpty || slot.item == null || !slot.item.isMonsterDrop)
            {
                ctx.Hud?.ShowMessage("Select a monster drop to gamble");
                return;
            }

            int gambledValue = slot.item.sellValue;
            if (!ctx.Inventory.TryRemoveFromSelected(1))
                return;

            spinning = true;
            ResolveSpin(ctx, gambledValue);
            spinning = false;
        }

        void ResolveSpin(GameContext ctx, int gambledValue)
        {
            float roll = Random.value;
            string result;

            if (roll < 0.001f)
            {
                ctx.Inventory.TryAdd(ctx.Database.legendaryWeaponItem, 1);
                result = "JACKPOT! Legendary Blade!";
                PlayTone(880f);
            }
            else if (roll < 0.1f)
            {
                if (TryUpgradeOwnedKnife(ctx))
                {
                    result = "Minor upgrade: Knife upgraded!";
                    PlayTone(660f);
                }
                else
                {
                    result = "No knife to upgrade";
                    PlayTone(330f);
                }
            }
            else if (roll < 0.5f)
            {
                int payout = Mathf.Max(1, gambledValue / 2);
                ctx.Wallet.Add(payout);
                result = $"Consolation ${payout}";
                PlayTone(440f);
            }
            else
            {
                result = "No prize";
                PlayTone(220f);
            }

            ctx.Hud?.ShowMessage($"Slot: {result}");
        }

        static bool TryUpgradeOwnedKnife(GameContext ctx)
        {
            var db = ctx.Database;
            var current = ctx.Inventory.GetOwnedKnifeItem();
            if (current == null)
                return false;

            var tiers = new[]
            {
                db.knifeItem,
                db.knifeGreenItem,
                db.knifeBlueItem,
                db.knifePurpleItem,
                db.knifeGoldenItem
            };

            for (int i = 0; i < tiers.Length - 1; i++)
            {
                if (current != tiers[i])
                    continue;

                return ctx.Inventory.TryUpgradeKnife(tiers[i + 1]);
            }

            return false;
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
    }
}
