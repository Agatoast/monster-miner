using UnityEngine;

namespace MonsterMiner.Artillery
{
    public enum ArtilleryCavalryAdvanceMode
    {
        Advancing,
        PendingInfiltration,
        Siege
    }

    public class ArtilleryCavalryUnit : MonoBehaviour
    {
        ArtilleryHitTarget hitTarget;
        ArtilleryCavalryAdvanceMode advanceMode = ArtilleryCavalryAdvanceMode.Advancing;
        int siegeTargetIndex;

        public ArtilleryHitTarget HitTarget => hitTarget;
        public ArtilleryCavalryAdvanceMode AdvanceMode => advanceMode;
        public int SiegeTargetIndex => siegeTargetIndex;
        public bool IsActive => hitTarget != null && !hitTarget.IsDestroyed;

        public void Bind(ArtilleryHitTarget target)
        {
            hitTarget = target;
        }

        public void SetAdvanceMode(ArtilleryCavalryAdvanceMode mode)
        {
            advanceMode = mode;
            if (mode == ArtilleryCavalryAdvanceMode.Siege)
                siegeTargetIndex = 0;
        }

        public void AdvanceSiegeTargetIndex()
        {
            siegeTargetIndex++;
        }

        public void MoveTo(float centerX, float centerY)
        {
            hitTarget?.SetCenterPosition(centerX, centerY);
        }

        public void DestroyUnit()
        {
            hitTarget?.DestroyTarget();
        }
    }
}
