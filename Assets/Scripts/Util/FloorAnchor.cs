using MonsterMiner.Core;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class FloorAnchor
    {
        public static void SnapBottomToFloor(GameObject go, float floorY, float restOffset = -1f)
        {
            if (go == null)
                return;

            if (restOffset < 0f)
                restOffset = WorldScale.SpawnDropHeight;

            Physics.SyncTransforms();
            float bottomY = GetBottomY(go);
            go.transform.position += Vector3.up * (floorY + restOffset - bottomY);
            Physics.SyncTransforms();

            bottomY = GetBottomY(go);
            if (bottomY < floorY - 0.001f)
                go.transform.position += Vector3.up * (floorY + restOffset - bottomY);
        }

        public static void PlaceOnFloor(GameObject go, Vector3 worldPoint, CavernBounds bounds = null)
        {
            if (go == null)
                return;

            bounds ??= GameContext.Instance?.CavernBounds;
            float floorY = ResolveFloorSurfaceY(worldPoint, bounds);

            go.transform.position = new Vector3(worldPoint.x, floorY + 4f, worldPoint.z);
            Physics.SyncTransforms();
            SnapBottomToFloor(go, floorY);
        }

        public static float ResolveFloorSurfaceY(Vector3 worldPoint, CavernBounds bounds = null)
        {
            bounds ??= GameContext.Instance?.CavernBounds;
            float floorY = worldPoint.y;

            if (bounds != null)
            {
                var local = bounds.transform.InverseTransformPoint(worldPoint);
                floorY = bounds.SampleFloorWorldY(local.x, local.z);
            }

            float raycastY = float.MinValue;
            float rayHeight = bounds != null ? bounds.Height + 24f : 32f;
            var highProbe = new Vector3(worldPoint.x, floorY + rayHeight, worldPoint.z);
            if (TryResolveFloorPoint(highProbe, 0f, rayHeight + 16f, out var highHit))
                raycastY = highHit.y;

            var localProbe = new Vector3(worldPoint.x, floorY + 8f, worldPoint.z);
            if (TryResolveFloorPoint(localProbe, 0f, 24f, out var localHit))
                raycastY = Mathf.Max(raycastY, localHit.y);

            if (raycastY > float.MinValue)
                floorY = Mathf.Max(floorY, raycastY);

            return floorY;
        }

        public static bool TryResolveFloorPoint(Vector3 worldPoint, float rayHeight, float maxDistance, out Vector3 floorPoint)
        {
            return FloorColliderUtility.TryResolveFloorPoint(worldPoint, rayHeight, maxDistance, out floorPoint);
        }

        public static float GetVisualBottomY(GameObject go)
        {
            Physics.SyncTransforms();

            float bottomY = float.MaxValue;
            bool found = false;

            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                bottomY = Mathf.Min(bottomY, renderer.bounds.min.y);
                found = true;
            }

            return found ? bottomY : go.transform.position.y;
        }

        public static float GetBottomY(GameObject go)
        {
            Physics.SyncTransforms();

            float bottomY = float.MaxValue;
            bool found = false;

            foreach (var collider in go.GetComponentsInChildren<Collider>())
            {
                if (collider == null || !collider.enabled)
                    continue;

                bottomY = Mathf.Min(bottomY, collider.bounds.min.y);
                found = true;
            }

            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                bottomY = Mathf.Min(bottomY, renderer.bounds.min.y);
                found = true;
            }

            return found ? bottomY : go.transform.position.y;
        }
    }
}
