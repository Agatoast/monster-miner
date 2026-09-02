using UnityEngine;

namespace MonsterMiner.Player
{
    public interface ICargoVehicle
    {
        Transform CargoBed { get; }
        Transform HostTransform { get; }
        Vector3 CargoEntryLocalPosition { get; }
        Vector3 ClampCargoLocalPosition(Vector3 localPosition);
        bool HasCargoOccupant { get; }
        void SetCargoOccupant(PlayerVehicleMount mount);
        void ClearCargoOccupant(PlayerVehicleMount mount);
    }
}
