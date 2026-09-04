using UnityEngine;

namespace MonsterMiner.Artillery
{
    public static class ArtilleryTrajectory
    {
        const float MotionScale = 0.5f;
        const float MaxSimTime = 12f;
        const float SimDt = 0.025f;

        public struct SimResult
        {
            public Vector2 LandingPoint;
            public float DistanceToAim;
            public bool LeftBounds;
        }

        public static SimResult Simulate(Vector2 start, Vector2 velocity, float wind, Vector2 aimPoint, ArtilleryField field)
        {
            field.GetScreenSize(out float width, out float height);
            var position = start;
            var result = new SimResult
            {
                LandingPoint = start,
                DistanceToAim = float.MaxValue,
                LeftBounds = false
            };

            float elapsed = 0f;
            while (elapsed < MaxSimTime)
            {
                float motionDt = SimDt * MotionScale;
                elapsed += SimDt;

                velocity.y -= ArtilleryRockPhysics.Gravity * motionDt;
                velocity += ArtilleryRockPhysics.ComputeDragAccelerationWorld(velocity, wind) * motionDt;

                var previous = position;
                position.x += velocity.x * motionDt;
                position.y += velocity.y * motionDt;

                float bestDist = DistanceToPoint(position, aimPoint);
                bestDist = Mathf.Min(bestDist, DistanceToPoint(previous, aimPoint));
                if (bestDist < result.DistanceToAim)
                {
                    result.DistanceToAim = bestDist;
                    result.LandingPoint = position;
                }

                if (position.y <= 0f)
                {
                    result.LandingPoint = position;
                    result.DistanceToAim = DistanceToPoint(position, aimPoint);
                    break;
                }

                if (position.x < 0f || position.x > width || position.y > height + 2f)
                {
                    result.LandingPoint = position;
                    result.DistanceToAim = DistanceToPoint(position, aimPoint);
                    result.LeftBounds = true;
                    break;
                }
            }

            return result;
        }

        public static bool TryFindBestShot(
            Vector2 start,
            float wind,
            ArtillerySide shooterSide,
            Vector2 aimPoint,
            ArtilleryField field,
            out float angleDegrees,
            out float powerPercent)
        {
            if (!SearchBestShot(
                    start,
                    wind,
                    shooterSide,
                    aimPoint,
                    field,
                    ArtilleryCatapult.MinLaunchAngleDegrees,
                    ArtilleryCatapult.MaxLaunchAngleDegrees,
                    2f,
                    5f,
                    out angleDegrees,
                    out powerPercent))
            {
                return false;
            }

            float refinedAngle = angleDegrees;
            float refinedPower = powerPercent;
            if (SearchBestShot(
                    start,
                    wind,
                    shooterSide,
                    aimPoint,
                    field,
                    angleDegrees - 4f,
                    angleDegrees + 4f,
                    1f,
                    1f,
                    out refinedAngle,
                    out refinedPower))
            {
                angleDegrees = refinedAngle;
                powerPercent = refinedPower;
            }

            return true;
        }

        static bool SearchBestShot(
            Vector2 start,
            float wind,
            ArtillerySide shooterSide,
            Vector2 aimPoint,
            ArtilleryField field,
            float minAngle,
            float maxAngle,
            float angleStep,
            float powerStep,
            out float angleDegrees,
            out float powerPercent)
        {
            angleDegrees = 45f;
            powerPercent = 60f;
            float bestScore = float.MaxValue;
            bool found = false;

            for (float angle = minAngle; angle <= maxAngle; angle += angleStep)
            {
                if (angle < ArtilleryCatapult.MinLaunchAngleDegrees || angle > ArtilleryCatapult.MaxLaunchAngleDegrees)
                    continue;

                var direction = BuildLaunchDirection(shooterSide, angle);
                for (float power = 5f; power <= 100f; power += powerStep)
                {
                    float speed = ArtilleryRockPhysics.LaunchSpeed(power);
                    var sim = Simulate(start, direction * speed, wind, aimPoint, field);
                    if (sim.LeftBounds || sim.DistanceToAim >= bestScore)
                        continue;

                    bestScore = sim.DistanceToAim;
                    angleDegrees = angle;
                    powerPercent = power;
                    found = true;
                }
            }

            return found;
        }

        static Vector2 BuildLaunchDirection(ArtillerySide shooterSide, float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            float horizontal = Mathf.Cos(radians);
            float vertical = Mathf.Sin(radians);
            return shooterSide == ArtillerySide.Left
                ? new Vector2(horizontal, vertical)
                : new Vector2(-horizontal, vertical);
        }

        static float DistanceToPoint(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b);
        }
    }
}
