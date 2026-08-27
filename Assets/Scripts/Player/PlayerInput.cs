using MonsterMiner.Core;
using MonsterMiner.Economy;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class PlayerInput : MonoBehaviour
    {
        Interactor interactor;
        PlayerEggCarrier eggCarrier;

        public void Initialize(Interactor playerInteractor, PlayerEggCarrier carrier)
        {
            interactor = playerInteractor;
            eggCarrier = carrier;
        }

        void Update()
        {
            if (PlayerController.IsGameplayBlocked())
                return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (GameContext.Instance?.Shop != null && GameContext.Instance.Shop.IsMenuOpen)
                    return;

                if (eggCarrier != null && eggCarrier.IsCarryingEgg)
                {
                    eggCarrier.DropEgg();
                    return;
                }

                interactor?.TryInteract();
            }

            if (Input.GetKeyDown(KeyCode.F))
                TryEatSelected();
        }

        void TryEatSelected()
        {
            var ctx = GameContext.Instance;
            if (ctx?.Inventory == null || ctx.PlayerHealth == null)
                return;

            if (!ctx.Inventory.CanEatSelected())
                return;

            if (ctx.PlayerHealth.CurrentHealth >= ctx.PlayerHealth.MaxHealth)
            {
                ctx.Hud?.ShowMessage("Already at full health.");
                return;
            }

            int healAmount = ctx.Inventory.GetSelectedEatHealAmount();
            if (healAmount <= 0 || !ctx.Inventory.TryRemoveFromSelected(1))
                return;

            ctx.PlayerHealth.Heal(healAmount);
            ctx.Hud?.ShowMessage($"Ate drop for +{healAmount} HP.");
        }
    }
}
