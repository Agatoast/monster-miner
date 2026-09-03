using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Interaction;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class BoatDeckInteract : MonoBehaviour, IInteractable
    {
        DriveableBoat boat;

        public void Initialize(DriveableBoat owner)
        {
            boat = owner;
        }

        public string GetPrompt()
        {
            if (boat == null)
                return string.Empty;

            if (!WarrensonBoatNpc.IsBoatUsable(GameContext.Instance))
                return "Rent Warrenson's boat first";

            var mount = GameContext.Instance?.Player?.GetComponent<PlayerVehicleMount>();
            if (mount != null && mount.IsDrivingBoat && mount.CurrentBoat == boat)
            {
                if (!boat.IsNearDismountShore)
                    return string.Empty;

                return "Stop Sailing [E]";
            }

            return "Set Sail [E]";
        }

        public bool CanInteract(GameObject interactor)
        {
            if (boat == null || interactor == null)
                return false;

            if (!WarrensonBoatNpc.IsBoatUsable(GameContext.Instance))
                return false;

            var mount = interactor.GetComponent<PlayerVehicleMount>();
            if (mount == null)
                return false;

            if (interactor.GetComponent<PlayerWingsFlight>()?.IsFlying == true)
                return false;

            if (mount.IsDrivingBoat)
                return mount.CurrentBoat == boat && boat.IsNearDismountShore;

            if (mount.IsMounted)
                return false;

            return !boat.HasDriver;
        }

        public void Interact(GameObject interactor)
        {
            if (boat == null)
                return;

            var mount = interactor.GetComponent<PlayerVehicleMount>();
            if (mount == null)
                return;

            if (mount.IsDrivingBoat && mount.CurrentBoat == boat)
            {
                mount.TryDismount();
                return;
            }

            mount.TryMountBoatDriver(boat);
        }
    }
}
