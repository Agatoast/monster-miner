using MonsterMiner.Core;
using UnityEngine;

namespace MonsterMiner.World
{
    public class QuarryVisitTracker : MonoBehaviour
    {
        public int LastVisitedQuarryIndex { get; private set; } = QuarryCatalog.PlateauQuarryIndex;

        void Update()
        {
            if (GameContext.Instance?.IsPlayerDead == true)
                return;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return;

            Vector3 local = bounds.transform.InverseTransformPoint(transform.position);
            if (QuarryCatalog.IsLandQuarry2Local(local.x, local.z))
            {
                LastVisitedQuarryIndex = QuarryCatalog.LandQuarry2Index;
                return;
            }

            if (bounds.IsOnPlateauLocal(local.x, local.z))
                LastVisitedQuarryIndex = QuarryCatalog.PlateauQuarryIndex;
        }
    }
}
