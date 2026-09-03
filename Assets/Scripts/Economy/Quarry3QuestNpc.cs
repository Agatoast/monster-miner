using MonsterMiner.Core;
using MonsterMiner.Interaction;
using MonsterMiner.Inventory;
using MonsterMiner.UI;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class Quarry3QuestNpc : MonoBehaviour, IInteractable, IInteractPromptBounds
    {
        const string GuideName = "Quarry Guide";
        const string CompassPrompt = "My compass";
        const string TurnInPopupBody =
            "My compass! Thank you, stranger — I feared it was lost forever.";
        const string ThankYouBody =
            "Thank you again for returning my compass.";

        public string GetDialogueBody()
        {
            var ctx = GameContext.Instance;
            if (ctx?.CaveProgression != null && ctx.CaveProgression.Quarry3CompassReturned)
                return ThankYouBody;

            if (HasMagicCompass(ctx))
                return CompassPrompt;

            return "I seem to have misplaced my compass.";
        }

        public string GetPrompt() => $"{GuideName} [E]";

        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor)
        {
            var ctx = GameContext.Instance;
            if (ctx?.CaveProgression == null || ctx.Inventory == null)
                return;

            if (ctx.CaveProgression.Quarry3CompassReturned)
            {
                MinerTurnInPopupDisplay.Show(ThankYouBody);
                return;
            }

            if (!HasMagicCompass(ctx))
            {
                MinerTurnInPopupDisplay.Show(GetDialogueBody());
                return;
            }

            var compass = ctx.Database?.magicCompassItem;
            if (compass == null || !ctx.Inventory.TryRemove(compass, 1))
            {
                ctx.Hud?.ShowMessage("You do not have the Magic Compass.");
                return;
            }

            ctx.CaveProgression.CompleteQuarry3CompassReturn();
            MinerTurnInPopupDisplay.Show(TurnInPopupBody);
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

        static bool HasMagicCompass(GameContext ctx) =>
            ctx?.Inventory != null && ctx.Inventory.HasMagicCompass();
    }
}
