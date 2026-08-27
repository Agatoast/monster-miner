using UnityEngine;

namespace MonsterMiner.Util
{
    public static class FloorAnchor
    {
        public static void SnapBottomToFloor(GameObject go, float floorY, float restOffset = 0.01f)
        {
            if (go == null)
                return;

            float bottomY = GetBottomY(go);
            go.transform.position += Vector3.up * (floorY + restOffset - bottomY);
        }

        public static bool TryResolveFloorPoint(Vector3 worldPoint, float rayHeight, float maxDistance, out Vector3 floorPoint)
        {
            var origin = worldPoint + Vector3.up * rayHeight;
            var hits = Physics.RaycastAll(origin, Vector3.down, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                floorPoint = worldPoint;
                return false;
            }

            int lowest = 0;
            for (int i = 1; i < hits.Length; i++)
            {
                if (hits[i].point.y < hits[lowest].point.y)
                    lowest = i;
            }

            floorPoint = hits[lowest].point;
            return true;
        }

        public static float GetBottomY(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                return bounds.min.y;
            }

            var collider = go.GetComponentInChildren<Collider>();
            if (collider != null)
                return collider.bounds.min.y;

            return go.transform.position.y;
        }
    }
}
