using UnityEngine;

namespace MonsterMiner.Artillery
{
    public enum ArtillerySide
    {
        Left,
        Right
    }

    public class ArtilleryCatapult : MonoBehaviour
    {
        const float LaunchHeightFraction = 0.72f;
        const float LaunchForwardFraction = 0.18f;
        public const float MinLaunchAngleDegrees = 20f;
        public const float MaxLaunchAngleDegrees = 89f;

        ArtillerySide side;
        ArtilleryCatapultAnimator animator;
        Collider clickCollider;
        float unitWidth;
        float unitHeight;

        public ArtillerySide Side => side;
        public ArtilleryCatapultAnimator Animator => animator;
        public Collider ClickCollider => clickCollider;

        public void Configure(
            ArtillerySide catapultSide,
            ArtilleryCatapultAnimator catapultAnimator,
            Collider collider,
            float width,
            float height)
        {
            side = catapultSide;
            animator = catapultAnimator;
            clickCollider = collider;
            unitWidth = width;
            unitHeight = height;
        }

        public Vector3 GetLaunchWorldPosition()
        {
            return transform.parent != null
                ? transform.parent.TransformPoint(GetLaunchLocalPosition())
                : transform.TransformPoint(GetLaunchLocalOffset());
        }

        public Vector3 GetLaunchLocalPosition()
        {
            return GetLaunchLocalOffset();
        }

        Vector3 GetLaunchLocalOffset()
        {
            var local = transform.localPosition;
            float forward = unitWidth * LaunchForwardFraction;
            float height = unitHeight * LaunchHeightFraction;
            float launchX = side == ArtillerySide.Left
                ? local.x + forward
                : local.x - forward;
            return new Vector3(launchX, local.y + height, local.z);
        }

        public Vector2 GetLaunchDirection2D(float angleDegrees)
        {
            angleDegrees = Mathf.Clamp(
                angleDegrees,
                MinLaunchAngleDegrees,
                MaxLaunchAngleDegrees);
            float radians = angleDegrees * Mathf.Deg2Rad;
            float horizontal = Mathf.Cos(radians);
            float vertical = Mathf.Sin(radians);
            return side == ArtillerySide.Left
                ? new Vector2(horizontal, vertical)
                : new Vector2(-horizontal, vertical);
        }

        public Vector3 GetLaunchDirection(float angleDegrees)
        {
            var flat = GetLaunchDirection2D(angleDegrees);
            return new Vector3(flat.x, flat.y, 0f);
        }
    }
}
