using MonsterMiner.Core;
using MonsterMiner.Interaction;
using MonsterMiner.UI;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class WarrensonBoatNpc : MonoBehaviour, IInteractable, IInteractPromptBounds
    {
        public const string CharacterName = "Warrenson";
        const int RentalCost = 500;
        const string RentalPrompt = "Rent my boat for $500";
        const string ThankYouBody = "The boat is yours to use whenever you like. Safe travels.";

        public static bool IsBoatUsable(GameContext ctx) =>
            ctx?.CaveProgression != null && ctx.CaveProgression.HasBoatRental;

        public string GetPrompt()
        {
            if (IsBoatUsable(GameContext.Instance))
                return $"{CharacterName} [E]";

            return $"{RentalPrompt} [E]";
        }

        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor)
        {
            var ctx = GameContext.Instance;
            if (ctx?.CaveProgression == null || ctx.Wallet == null)
                return;

            if (ctx.CaveProgression.HasBoatRental)
            {
                MinerTurnInPopupDisplay.Show(ThankYouBody, centerBody: true);
                return;
            }

            if (!ctx.Wallet.TrySpend(RentalCost))
            {
                ctx.Hud?.ShowMessage("Not enough $");
                return;
            }

            ctx.CaveProgression.CompleteBoatRental();
            ctx.Hud?.ShowMessage("Boat rental paid. You can set sail now.");
        }

        public bool TryGetPromptScreenRect(Camera camera, out Rect guiRect)
        {
            var collider = GetComponent<Collider>();
            return InteractionPromptBoundsUtility.TryGetColliderScreenRect(camera, collider, out guiRect);
        }
    }
}
