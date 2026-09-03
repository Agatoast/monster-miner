using MonsterMiner.Core;
using MonsterMiner.Interaction;
using MonsterMiner.Inventory;
using MonsterMiner.UI;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class JarlQuestNpc : MonoBehaviour, IInteractable, IInteractPromptBounds
    {
        const string DialogueBody =
            "Greetings Stranger and well met. Forgive me but I am a little too busy at the moment to help you find a Dragon Stone. My assistant can help you with any of your supply needs.\n\n"
            + "On second thought, maybe you can help me.\n\n"
            + "I need the skull of Jörmungandr. The Jörmungandr is a giant flying serpent that is only found on the island in the middle of the lake. If you bring me one, I can give you something to help with your quest.\n\n"
            + "Warrenson has a boat he will rent you for $500 and you can use that to get to the island. Good fortune to you.";

        const string TurnInPrompt = "Give Jarl the skull of Jörmungandr";
        const string TurnInPopupBody =
            "\"This is going to look great over my mantle...\"\n\n"
            + "The Jarl realizes he is speaking out loud and says,\n\n\"Oh, well done Stranger. Here. Take this Magic Compass. It points a person to the place they most need to be. Good fortune to you.\"";
        const string ThankYouBody = "Well met, Stranger.";

        public string GetDialogueBody()
        {
            var ctx = GameContext.Instance;
            if (ctx?.CaveProgression != null && ctx.CaveProgression.JarlSkullQuestComplete)
                return ThankYouBody;

            if (HasJormungandrSkull(ctx))
                return TurnInPrompt;

            return DialogueBody;
        }

        public string GetPrompt() => $"{VikingBuildingVisualFactory.CharacterName} [E]";

        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor)
        {
            var ctx = GameContext.Instance;
            if (ctx?.CaveProgression == null || ctx.Inventory == null)
                return;

            if (ctx.CaveProgression.JarlSkullQuestComplete)
            {
                MinerTurnInPopupDisplay.Show(ThankYouBody, centerBody: true);
                return;
            }

            if (HasJormungandrSkull(ctx))
            {
                var skull = ctx.Database?.jormungandrSkullItem;
                var compass = ctx.Database?.magicCompassItem;
                if (skull == null || compass == null || !ctx.Inventory.TryRemove(skull, 1))
                {
                    ctx.Hud?.ShowMessage("You need the skull of Jörmungandr.");
                    return;
                }

                if (!ctx.Inventory.TryAdd(compass, 1))
                {
                    ctx.Inventory.TryAdd(skull, 1);
                    ctx.Hud?.ShowMessage("Make room in your inventory for the Magic Compass.");
                    return;
                }

                ctx.CaveProgression.CompleteJarlSkullQuest();
                MinerTurnInPopupDisplay.Show(TurnInPopupBody, centerBody: true);
                return;
            }

            MinerTurnInPopupDisplay.Show(DialogueBody, centerBody: true);
        }

        public bool TryGetPromptScreenRect(Camera camera, out Rect guiRect)
        {
            var collider = GetComponent<Collider>();
            return InteractionPromptBoundsUtility.TryGetColliderScreenRect(camera, collider, out guiRect);
        }

        public bool TryGetDialogueAnchorScreenPoint(Camera camera, out Vector2 guiPoint)
        {
            guiPoint = default;
            if (camera == null)
                return false;

            Vector3 shoulderWorld = transform.position
                + transform.right * 0.32f
                + Vector3.up * 1.42f;
            Vector3 screenPoint = camera.WorldToScreenPoint(shoulderWorld);
            if (screenPoint.z <= 0f)
                return false;

            guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            return true;
        }

        static bool HasJormungandrSkull(GameContext ctx)
        {
            var skull = ctx?.Database?.jormungandrSkullItem;
            return skull != null && ctx.Inventory != null && ctx.Inventory.ContainsItem(skull);
        }
    }
}
