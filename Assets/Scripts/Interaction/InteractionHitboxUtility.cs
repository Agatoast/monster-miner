using UnityEngine;

namespace MonsterMiner.Interaction
{
    public static class InteractionHitboxUtility
    {
        public static void RaiseColliderByScreenPixels(Collider collider, Camera camera, float screenPixelRaise)
        {
            if (collider == null || camera == null || Mathf.Approximately(screenPixelRaise, 0f))
                return;

            var transform = collider.transform;
            Vector3 worldAnchor = collider.bounds.center;
            Vector3 screenPoint = camera.WorldToScreenPoint(worldAnchor);
            screenPoint.y += screenPixelRaise;

            Vector3 raisedWorld = camera.ScreenToWorldPoint(screenPoint);
            Vector3 worldDelta = raisedWorld - worldAnchor;
            Vector3 localDelta = transform.InverseTransformVector(worldDelta);

            if (collider is BoxCollider box)
                box.center += localDelta;
            else
                transform.position += worldDelta;
        }

        public static void OffsetTransformByScreenPixels(Transform transform, Camera camera, Vector3 pixelOffset)
        {
            if (transform == null || camera == null)
                return;

            transform.position += ScreenPixelsToWorldOffset(camera, transform.position, pixelOffset);
        }

        public static Vector3 ScreenPixelsToWorldOffset(Camera camera, Vector3 worldReference, Vector3 pixelOffset)
        {
            float depth = Vector3.Dot(worldReference - camera.transform.position, camera.transform.forward);
            if (depth <= 0.001f)
                return Vector3.zero;

            float worldPerPixel = 2f * depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) / camera.pixelHeight;
            // X/Y = screen plane; +Z = out of screen toward the player.
            return camera.transform.right * (pixelOffset.x * worldPerPixel)
                 + camera.transform.up * (pixelOffset.y * worldPerPixel)
                 - camera.transform.forward * (pixelOffset.z * worldPerPixel);
        }
    }
}
