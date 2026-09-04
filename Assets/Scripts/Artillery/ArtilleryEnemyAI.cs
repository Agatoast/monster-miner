using UnityEngine;

namespace MonsterMiner.Artillery
{
    public static class ArtilleryEnemyAI
    {
        const float LowWindMphMax = 8f;
        const float ModerateWindMphMax = 15f;

        public static bool TryPlanShot(
            ArtilleryField field,
            ArtilleryCatapult catapult,
            ArtilleryHitTarget focusTarget,
            float wind,
            out float angleDegrees,
            out float powerPercent)
        {
            angleDegrees = 45f;
            powerPercent = 60f;
            if (field == null || catapult == null || focusTarget == null)
                return false;

            if (!field.TryGetTargetAimPoint(focusTarget, out Vector2 aimPoint))
                return false;

            ApplyAimJitter(focusTarget, ref aimPoint);

            var start = new Vector2(catapult.GetLaunchLocalPosition().x, catapult.GetLaunchLocalPosition().y);
            if (!ArtilleryTrajectory.TryFindBestShot(
                start,
                wind,
                catapult.Side,
                aimPoint,
                field,
                out float perfectAngle,
                out float perfectPower))
            {
                angleDegrees = Random.Range(35f, 70f);
                powerPercent = Random.Range(50f, 85f);
                return true;
            }

            GetErrorMargins(ArtilleryRockPhysics.WindSpeedToMph(wind), out float angleError, out float powerError);
            angleDegrees = perfectAngle + SampleSignedError(angleError);
            powerPercent = perfectPower + SampleSignedError(powerError);
            angleDegrees = Mathf.Clamp(
                angleDegrees,
                ArtilleryCatapult.MinLaunchAngleDegrees,
                ArtilleryCatapult.MaxLaunchAngleDegrees);
            powerPercent = Mathf.Clamp(powerPercent, 1f, 100f);
            return true;
        }

        static void ApplyAimJitter(ArtilleryHitTarget target, ref Vector2 aimPoint)
        {
            float halfWidth = target.Width * 0.35f;
            float halfHeight = target.Height * 0.25f;
            aimPoint.x += Random.Range(-halfWidth, halfWidth);
            aimPoint.y += Random.Range(-halfHeight, halfHeight);
        }

        static float SampleSignedError(float margin)
        {
            if (margin <= 0f)
                return 0f;

            float magnitude = Random.Range(0f, margin);
            return Random.value < 0.5f ? -magnitude : magnitude;
        }

        static void GetErrorMargins(float windMph, out float angleError, out float powerError)
        {
            if (windMph <= LowWindMphMax)
            {
                angleError = 4f;
                powerError = 5f;
                return;
            }

            if (windMph <= ModerateWindMphMax)
            {
                angleError = 6f;
                powerError = 8f;
                return;
            }

            angleError = 9f;
            powerError = 12f;
        }
    }
}
