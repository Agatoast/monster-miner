using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.UI;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class PlayerInput : MonoBehaviour
    {
        Interactor interactor;
        PlayerEggCarrier eggCarrier;
        PlayerCreatureCarrier creatureCarrier;

        public void Initialize(Interactor playerInteractor, PlayerEggCarrier carrier, PlayerCreatureCarrier creature)
        {
            interactor = playerInteractor;
            eggCarrier = carrier;
            creatureCarrier = creature;
        }

        void Update()
        {
            if (PlayerController.IsGameplayBlocked())
                return;

            if (WorldMapDisplay.IsActive)
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

                if (creatureCarrier != null && creatureCarrier.IsCarrying)
                {
                    creatureCarrier.ThrowCarriedCreature();
                    return;
                }

                var vehicleMount = GetComponent<PlayerVehicleMount>();
                if (vehicleMount != null && vehicleMount.IsMounted)
                {
                    vehicleMount.TryDismount();
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
