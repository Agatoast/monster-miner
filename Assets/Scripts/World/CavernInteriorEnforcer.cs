using MonsterMiner.Inventory;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class CavernInteriorEnforcer
    {
        const float ShellInset = 0.15f;

        public static float GetHorizontalBoundsRadius(GameObject root)
        {
            if (root == null)
                return 0f;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return 0f;

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                worldBounds.Encapsulate(renderers[i].bounds);

            Vector3 extents = worldBounds.extents;
            return new Vector2(extents.x, extents.z).magnitude;
        }

        public static bool FitsInsideShell(CavernBounds bounds, Vector3 worldPosition, float horizontalRadius)
        {
            if (bounds == null)
                return true;

            Vector3 local = bounds.transform.InverseTransformPoint(worldPosition);
            float angle = Mathf.Atan2(local.z, local.x);
            float maxCenterDistance = PlateauBoundary.SampleBarrierDistance(angle, bounds.Radius) - ShellInset;
            float centerDistance = new Vector2(local.x, local.z).magnitude;
            return centerDistance + horizontalRadius <= maxCenterDistance;
        }

        public static void EnsureInsideShell(GameObject root, CavernBounds bounds)
        {
            if (root == null || bounds == null)
                return;

            float horizontalRadius = GetHorizontalBoundsRadius(root);
            if (horizontalRadius <= 0f)
                return;

            // Skinned meshes / attached props can report huge bounds; don't shove content to the center.
            horizontalRadius = Mathf.Min(horizontalRadius, bounds.Radius * 0.45f);

            Vector3 local = bounds.transform.InverseTransformPoint(root.transform.position);
            Vector2 flat = new Vector2(local.x, local.z);
            float centerDistance = flat.magnitude;
            float angle = Mathf.Atan2(local.z, local.x);
            float plateauEdge = PlateauBoundary.SampleBarrierDistance(angle, bounds.Radius);
            float maxCenterDistance = Mathf.Max(0f, plateauEdge - ShellInset - horizontalRadius);
            if (centerDistance <= maxCenterDistance)
                return;

            Vector2 direction = centerDistance > 0.001f ? flat / centerDistance : Vector2.right;
            Vector2 clamped = direction * maxCenterDistance;
            Vector3 clampedLocal = new Vector3(clamped.x, local.y, clamped.y);
            root.transform.position = bounds.transform.TransformPoint(clampedLocal);
        }

        public static void DisableOutsideRenderers(Transform contentRoot, CavernBounds bounds)
        {
            if (contentRoot == null || bounds == null)
                return;

            foreach (var renderer in contentRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || IsShellRenderer(renderer) || IsPlateauWorldContent(renderer))
                    continue;

                Vector3 local = bounds.transform.InverseTransformPoint(renderer.bounds.center);
                float extent = GetHorizontalBoundsRadius(renderer.gameObject);
                float centerDistance = new Vector2(local.x, local.z).magnitude;
                if (centerDistance + extent > bounds.Radius - ShellInset)
                    renderer.enabled = false;
            }
        }

        static bool IsPlateauWorldContent(Renderer renderer)
        {
            return renderer.GetComponentInParent<MonsterEgg>() != null
                || renderer.GetComponentInParent<WorldPickup>() != null
                || renderer.GetComponentInParent<EggFinderMarker>() != null;
        }

        static bool IsShellRenderer(Renderer renderer)
        {
            var transform = renderer.transform;
            while (transform != null)
            {
                string name = transform.name;
                if (name == "PlainsGround" || name == "PlainsGroundTopCollision"
                    || name == "PlainsGroundSolid" || name == "PlainsGroundCollision"
                    || name == "MinerArea" || name == "MinerNpc"
                    || name == "AngelWings" || name == "EquippedAngelWings" || name == "LowerWorld"
                    || name == "LowerPlainsGround" || name == "LowerTreeCopses" || name == "CliffWalls"
                    || name == "PlateauCliffWalls")
                    return true;
                if (name.StartsWith("PlainsGroundCollider_") || name.StartsWith("Tree_") || name.StartsWith("Copse_")
                    || name.StartsWith("Trunk") || name.StartsWith("Foliage"))
                    return true;
                transform = transform.parent;
            }

            return false;
        }
    }
}
