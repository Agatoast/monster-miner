using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Interaction;
using MonsterMiner.Player;
using UnityEngine;

namespace MonsterMiner.World
{
    public class AngelWingsPickup : MonoBehaviour, IInteractable
    {
        public string GetPrompt()
        {
            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasMinerWingsPermission)
                return "The miner's wings rest here.";

            if (progression.MinerWingsConsumed)
                return "The wings are spent.";

            return "Equip the miner's wings [E]";
        }

        public bool CanInteract(GameObject interactor)
        {
            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasMinerWingsPermission || progression.MinerWingsConsumed)
                return false;

            var flight = interactor.GetComponent<PlayerWingsFlight>();
            return flight != null && !flight.IsFlying;
        }

        public void Interact(GameObject interactor)
        {
            var flight = interactor.GetComponent<PlayerWingsFlight>();
            if (flight == null)
                return;

            flight.EquipFromWorld(gameObject);
        }
    }
}
