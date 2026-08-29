using MonsterMiner.Core;
using MonsterMiner.Interaction;
using MonsterMiner.UI;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class MinerQuestNpc : MonoBehaviour, IInteractable, IInteractPromptBounds
    {
        const string QuestPrompt =
            "Adventurer, the Shop Keeper told me you are looking for a Dragon Stone. I might be able to help, but first you have to help me.\n\n"
            + "My wife is dying and the apothecary needs the Heart of a Pentachick to craft the medicine. I had a Pentachick Finder, but the bird was too much for me. I need you to bring me the heart to save my wife. The Shop Keeper sells Finders to help you see what is in the eggs so you can find the right one.";

        const string GiveFinderPrompt = "Give Miner the Heart of the Pentachick";
        const string WingsPermissionPrompt =
            "Thank you, take the wings and feel free to use my truck down below.";
        const string TurnInPopupBody =
            "Thank you for bringing me the Heart of the Pentachick. Now I can save my wife.\n\n"
            + "It looks like the Dragon Stone you are looking for isn't here and you are going to need a way off of this rock. Take my wings. Once you have them on, jump over the edge and you will glide down. When you get down, feel free to use my truck to get around.\n\n"
            + "Before you go, take this Map to help you find your way.";
        const string WingsSpentPrompt = "Those wings will not lift you again.";

        public string GetDialogueBody()
        {
            var ctx = GameContext.Instance;
            var progression = ctx?.CaveProgression;
            if (HasPentachickHeart(ctx))
                return GiveFinderPrompt;

            if (progression != null && progression.MinerWingsConsumed)
                return WingsSpentPrompt;

            if (progression != null && progression.HasMinerWingsPermission)
                return WingsPermissionPrompt;

            return QuestPrompt;
        }

        public bool ShouldShowInteractPrompt()
        {
            var ctx = GameContext.Instance;
            if (HasPentachickHeart(ctx))
                return true;

            if (ctx?.CaveProgression != null
                && (ctx.CaveProgression.HasMinerWingsPermission || ctx.CaveProgression.IsCave2Unlocked))
                return true;

            return false;
        }

        public bool ShouldHighlightPhoenixHeart()
        {
            var ctx = GameContext.Instance;
            if (HasPentachickHeart(ctx))
                return true;

            if (ctx?.CaveProgression != null
                && (ctx.CaveProgression.HasMinerWingsPermission || ctx.CaveProgression.IsCave2Unlocked))
                return false;

            return true;
        }

        public string GetPrompt()
        {
            string body = GetDialogueBody();
            return ShouldShowInteractPrompt() ? body + " [E]" : body;
        }

        public bool CanInteract(GameObject interactor)
        {
            var progression = GameContext.Instance?.CaveProgression;
            return progression == null || !progression.IsBlastInProgress;
        }

        public void Interact(GameObject interactor)
        {
            var ctx = GameContext.Instance;
            if (ctx?.CaveProgression == null)
                return;

            if (ctx.CaveProgression.IsBlastInProgress)
                return;

            if (HasPentachickHeart(ctx))
            {
                var heart = ctx.Database?.pentachickHeartItem;
                if (heart == null || !ctx.Inventory.TryRemove(heart, 1))
                {
                    ctx.Hud?.ShowMessage("You need the Heart of the Pentachick.");
                    return;
                }

                ctx.CaveProgression.GrantMinerWingsPermission();
                ctx.CaveProgression.GrantWorldMap();
                MinerTurnInPopupDisplay.Show(TurnInPopupBody);
                return;
            }

            if (ctx.CaveProgression.HasMinerWingsPermission)
            {
                ctx.Hud?.ShowMessage(
                    ctx.CaveProgression.MinerWingsConsumed
                        ? "Those wings will not lift you again."
                        : "Take the wings and glide down below.");
            }
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

        public bool TryGetPromptScreenRect(Camera camera, out Rect guiRect)
        {
            var collider = GetComponent<Collider>();
            return InteractionPromptBoundsUtility.TryGetColliderScreenRect(camera, collider, out guiRect);
        }

        static bool HasPentachickHeart(GameContext ctx)
        {
            var heart = ctx?.Database?.pentachickHeartItem;
            if (heart == null || ctx.Inventory == null)
                return false;

            return ctx.Inventory.ContainsItem(heart);
        }
    }
}
