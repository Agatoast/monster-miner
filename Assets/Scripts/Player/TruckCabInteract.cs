using MonsterMiner.Core;
using MonsterMiner.Interaction;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class TruckCabInteract : MonoBehaviour, IInteractable
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
            if (mount != null && mount.IsDriving && mount.CurrentTruck == truck)
                return truck.CanDismount ? "Get out front [E]" : "Get out front (slow down) [E]";

            return "Get in front [E]";
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

            if (mount.IsDriving)
                return mount.CurrentTruck == truck;

            if (mount.IsInCargo)
                return false;

            return !truck.HasDriver;
        }

        public void Interact(GameObject interactor)
        {
            if (truck == null)
                return;

            var mount = interactor.GetComponent<PlayerVehicleMount>();
            if (mount == null)
                return;

            if (mount.IsDriving && mount.CurrentTruck == truck)
            {
                mount.TryDismount();
                return;
            }

            if (!truck.HasDriver)
                mount.TryMountDriver(truck);
        }
    }
}
