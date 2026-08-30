using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class QuarryStairVisualFactory
    {
        const string ResourcePath = "Models/Props/fi_vil_stair01_long_C";

        public static GameObject Create(Transform parent, Vector3 localPosition, Quaternion localRotation, float floorWorldY)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: quarry stair mesh not found at Resources/{ResourcePath}.");
                return null;
            }

            var stairs = Object.Instantiate(prefab, parent, false);
            stairs.name = "Quarry2Stairs";
            stairs.transform.localPosition = localPosition;
            stairs.transform.localRotation = localRotation;
            KnifeVisualFactory.ApplyUrpMaterials(stairs);
            StripImportedColliders(stairs);
            FloorAnchor.SnapBottomToFloor(stairs, floorWorldY);
            AddWalkCollider(stairs);
            return stairs;
        }

        public static Vector3 GetFootLocalPoint(GameObject stairs, Transform parent)
        {
            if (stairs == null || parent == null)
                return Vector3.zero;

            if (!TryGetRendererBounds(stairs, out var bounds))
                return parent.InverseTransformPoint(stairs.transform.position);

            Vector3 footWorld = new Vector3(bounds.center.x, bounds.min.y, bounds.min.z);
            return parent.InverseTransformPoint(footWorld);
        }

        static void StripImportedColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
        }

        static void AddWalkCollider(GameObject stairs)
        {
            if (!TryGetRendererBounds(stairs, out var bounds))
                return;

            var box = stairs.AddComponent<BoxCollider>();
            var root = stairs.transform;
            Vector3 localCenter = root.InverseTransformPoint(bounds.center);
            Vector3 localSize = root.InverseTransformVector(bounds.size);
            box.center = localCenter;
            box.size = new Vector3(
                Mathf.Abs(localSize.x),
                Mathf.Abs(localSize.y),
                Mathf.Abs(localSize.z));
        }

        static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return false;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled)
                    continue;

                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }
    }
}
