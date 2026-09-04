using UnityEngine;

namespace MonsterMiner.World
{
    /// <summary>
    /// Ensures Quarry 3 Rock5 collision is present after the pair finishes moving in the hierarchy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Quarry3RockSolid : MonoBehaviour
    {
        void Start()
        {
            if (!name.StartsWith("NatureRock5"))
                return;

            NatureRockCollisionBuilder.BuildSolidCollision(gameObject);
        }
    }
}
