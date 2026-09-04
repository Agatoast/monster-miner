using UnityEngine;

namespace MonsterMiner.Artillery
{
    public class ArtilleryBuildingPadMarker : MonoBehaviour
    {
        public Vector3 Size { get; private set; }

        public void Configure(Vector3 size)
        {
            Size = size;
        }
    }
}
