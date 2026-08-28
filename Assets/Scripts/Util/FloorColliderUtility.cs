using UnityEngine;

namespace MonsterMiner.Util
{
    public static class FloorColliderUtility
    {
        public static bool IsFloorCollider(Collider collider)
        {
            if (collider == null)
                return false;

            var transform = collider.transform;
            while (transform != null)
            {
                string name = transform.name;
                if (name == "Floor" || name == "Cave2Floor" || name == "Cave2TunnelFloor"
                    || name == "FloorCollision" || name == "PlainsGroundTopCollision"
                    || name == "PlainsGroundCollision" || name == "FloorCenterCap"
                    || name == "LowerPlainsGround" || name == "LowerPlainsGroundCollision"
                    || name == "PlateauCliffWalls")
                    return true;
                if (name.StartsWith("FloorCollider_") || name.StartsWith("PlainsGroundCollider_"))
                    return true;
                transform = transform.parent;
            }

            return false;
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

            int best = -1;
            for (int i = 0; i < hits.Length; i++)
            {
                if (!IsFloorCollider(hits[i].collider))
                    continue;

                if (best < 0 || hits[i].point.y > hits[best].point.y)
                    best = i;
            }

            if (best < 0)
            {
                floorPoint = worldPoint;
                return false;
            }

            floorPoint = hits[best].point;
            return true;
        }
    }
}
