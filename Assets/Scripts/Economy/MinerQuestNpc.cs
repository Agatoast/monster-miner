using MonsterMiner.Core;
using MonsterMiner.Interaction;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class MinerQuestNpc : MonoBehaviour, IInteractable, IInteractPromptBounds
    {
        const string QuestPrompt =
            "I need your help! I have been mining here for years and I am so close to breaking into the motherlode.\n\n"
            + "I think there is another cave on the other side of this wall but I need something to blast through.\n\n"
            + "Bring me the Heart of the Phoenix.";

        const string GiveFinderPrompt = "Give Miner the Phoenix Egg Finder";
        const string CaveOpenPrompt = "The blast opened the way to Cave 2.";

        public string GetDialogueBody()
        {
            var ctx = GameContext.Instance;
            if (ctx?.CaveProgression != null && ctx.CaveProgression.IsCave2Unlocked)
                return CaveOpenPrompt;

            if (HasPhoenixLure(ctx))
                return GiveFinderPrompt;

            return QuestPrompt;
        }

        public bool ShouldShowInteractPrompt()
        {
            var ctx = GameContext.Instance;
            if (ctx?.CaveProgression != null && ctx.CaveProgression.IsCave2Unlocked)
                return true;

            return HasPhoenixLure(ctx);
        }

        public bool ShouldHighlightPhoenixHeart()
        {
            var ctx = GameContext.Instance;
            if (ctx?.CaveProgression != null && ctx.CaveProgression.IsCave2Unlocked)
                return false;

            return !HasPhoenixLure(ctx);
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

            if (ctx.CaveProgression.IsCave2Unlocked)
            {
                ctx.Hud?.ShowMessage("Cave 2 lies through the blasted wall.");
                return;
            }

            if (ctx.CaveProgression.IsBlastInProgress)
                return;

            if (!HasPhoenixLure(ctx))
                return;

            var lure = ctx.Database?.phoenixLureItem;
            if (lure == null || !ctx.Inventory.TryRemove(lure, 1))
            {
                ctx.Hud?.ShowMessage("You need a Phoenix Egg Finder.");
                return;
            }

            ctx.CaveProgression.BeginBlastSequence();
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

        static bool HasPhoenixLure(GameContext ctx)
        {
            var lure = ctx?.Database?.phoenixLureItem;
            if (lure == null || ctx.Inventory == null)
                return false;

            int total = 0;
            foreach (var slot in ctx.Inventory.Slots)
            {
                if (!slot.IsEmpty && slot.item == lure)
                    total += slot.count;
            }

            return total > 0;
        }
    }
}
