using MonsterMiner.Core;
using MonsterMiner.Interaction;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class TruckBedInteract : MonoBehaviour, IInteractable
    {
        DriveableTruck truck;

        public void Initialize(DriveableTruck owner)
        {
            truck = owner;
        }

        public string GetPrompt()
        {
            if (truck == null)
                return string.Empty;

            var mount = GameContext.Instance?.Player?.GetComponent<PlayerVehicleMount>();
            if (mount != null && mount.IsInCargo && mount.CurrentTruck == truck)
                return "Get out back [E]";

            return "Get in back [E]";
        }

        public bool CanInteract(GameObject interactor)
        {
            if (truck == null || interactor == null)
                return false;

            var mount = interactor.GetComponent<PlayerVehicleMount>();
            if (mount == null)
                return false;

            if (interactor.GetComponent<PlayerWingsFlight>()?.IsFlying == true)
                return false;

            if (mount.IsInCargo)
                return mount.CurrentTruck == truck;

            if (mount.IsDriving)
                return false;

            return !truck.HasCargoOccupant;
        }

        public void Interact(GameObject interactor)
        {
            if (truck == null)
                return;

            var mount = interactor.GetComponent<PlayerVehicleMount>();
            if (mount == null)
                return;

            if (mount.IsInCargo && mount.CurrentTruck == truck)
            {
                mount.TryDismount();
                return;
            }

            if (!truck.HasCargoOccupant)
                mount.TryMountCargo(truck);
        }
    }
}
