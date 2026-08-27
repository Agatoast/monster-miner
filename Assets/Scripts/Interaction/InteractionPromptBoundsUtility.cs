using UnityEngine;

namespace MonsterMiner.Interaction
{
    public static class InteractionPromptBoundsUtility
    {
        public static bool TryGetColliderScreenRect(Camera camera, Collider collider, out Rect guiRect)
        {
            guiRect = default;
            if (camera == null || collider == null)
                return false;

            return TryGetBoundsScreenRect(camera, collider.bounds, out guiRect);
        }

        public static bool TryGetBoundsScreenRect(Camera camera, Bounds bounds, out Rect guiRect)
        {
            guiRect = default;
            if (camera == null)
                return false;

            var center = bounds.center;
            var extents = bounds.extents;
            var corners = new[]
            {
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y, extents.z),
                center + new Vector3(-extents.x, extents.y, -extents.z),
                center + new Vector3(-extents.x, extents.y, extents.z),
                center + new Vector3(extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, -extents.y, extents.z),
                center + new Vector3(extents.x, extents.y, -extents.z),
                center + new Vector3(extents.x, extents.y, extents.z)
            };

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            bool anyVisible = false;

            foreach (var corner in corners)
            {
                Vector3 screen = camera.WorldToScreenPoint(corner);
                if (screen.z <= 0f)
                    continue;

                anyVisible = true;
                float guiX = screen.x;
                float guiY = Screen.height - screen.y;
                minX = Mathf.Min(minX, guiX);
                minY = Mathf.Min(minY, guiY);
                maxX = Mathf.Max(maxX, guiX);
                maxY = Mathf.Max(maxY, guiY);
            }

            if (!anyVisible)
                return false;

            guiRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return guiRect.width > 1f && guiRect.height > 1f;
        }
    }
}
