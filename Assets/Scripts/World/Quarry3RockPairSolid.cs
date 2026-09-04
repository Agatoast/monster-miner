using UnityEngine;

namespace MonsterMiner.World
{
    [DisallowMultipleComponent]
    public sealed class Quarry3RockPairSolid : MonoBehaviour
    {
        void Start()
        {
            if (name != "NatureRock5Pair")
                return;

            NatureRockPairCollisionBuilder.BuildPairCollision(gameObject);
        }
    }
}
