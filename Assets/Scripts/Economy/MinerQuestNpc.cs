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
            + "Bring me the Heart of the Pentachick.";

        const string GiveFinderPrompt = "Give Miner the Heart of the Pentachick";
        const string WingsPermissionPrompt =
            "Thank you, take the wings and feel free to use my motorcycle down below.";
        const string WingsSpentPrompt = "Those wings will not lift you again.";

        public string GetDialogueBody()
        {
            var ctx = GameContext.Instance;
            var progression = ctx?.CaveProgression;
            if (progression != null && progression.MinerWingsConsumed)
                return WingsSpentPrompt;

            if (progression != null && progression.HasMinerWingsPermission)
                return WingsPermissionPrompt;

            if (HasPentachickHeart(ctx))
                return GiveFinderPrompt;

            return QuestPrompt;
        }

        public bool ShouldShowInteractPrompt()
        {
            var ctx = GameContext.Instance;
            if (ctx?.CaveProgression != null
                && (ctx.CaveProgression.HasMinerWingsPermission || ctx.CaveProgression.IsCave2Unlocked))
                return true;

            return HasPentachickHeart(ctx);
        }

        public bool ShouldHighlightPhoenixHeart()
        {
            var ctx = GameContext.Instance;
            if (ctx?.CaveProgression != null
                && (ctx.CaveProgression.HasMinerWingsPermission || ctx.CaveProgression.IsCave2Unlocked))
                return false;

            return !HasPentachickHeart(ctx);
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

            if (ctx.CaveProgression.HasMinerWingsPermission)
            {
                ctx.Hud?.ShowMessage(
                    ctx.CaveProgression.MinerWingsConsumed
                        ? "Those wings will not lift you again."
                        : "Take the wings and glide down below.");
                return;
            }

            if (!HasPentachickHeart(ctx))
                return;

            var heart = ctx.Database?.pentachickHeartItem;
            if (heart == null || !ctx.Inventory.TryRemove(heart, 1))
            {
                ctx.Hud?.ShowMessage("You need the Heart of the Pentachick.");
                return;
            }

            ctx.CaveProgression.GrantMinerWingsPermission();
            ctx.Hud?.ShowMessage(WingsPermissionPrompt);
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
