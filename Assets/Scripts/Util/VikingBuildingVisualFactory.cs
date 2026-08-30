using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class VikingBuildingVisualFactory
    {
        const string ResourcePath = "Models/Props/a_base_b";
        public const string HallObjectName = "JarlHall";
        public const string CharacterName = "Jarl Jorgenson";
        const float HallScale = 2f;

        public static GameObject CreateAtLocalPoint(
            Transform parent,
            Vector3 localPosition,
            float floorWorldY,
            Quaternion localRotation)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: A_BaseB mesh not found at Resources/{ResourcePath}.");
                return null;
            }

            var building = Object.Instantiate(prefab, parent, false);
            building.name = HallObjectName;
            building.transform.localScale = Vector3.one * HallScale;
            building.transform.localRotation = localRotation;
            building.transform.localPosition = Vector3.zero;
            KnifeVisualFactory.ApplyUrpMaterials(building);
            StripImportedColliders(building);
            AlignCenterToLocalPoint(building, parent, localPosition);
            FloorAnchor.SnapBottomToFloor(building, floorWorldY);
            AddTightCollision(building);
            return building;
        }

        static void AlignCenterToLocalPoint(GameObject building, Transform parent, Vector3 targetLocal)
        {
            Physics.SyncTransforms();
            if (!TryGetRendererBounds(building, out var bounds))
            {
                building.transform.localPosition = targetLocal;
                return;
            }

            Vector3 centerLocal = parent.InverseTransformPoint(bounds.center);
            building.transform.localPosition += targetLocal - centerLocal;
        }

        static void StripImportedColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
                Object.Destroy(collider);
            }
        }

        static void AddTightCollision(GameObject building)
        {
            var box = building.AddComponent<BoxCollider>();
            if (!TryGetRendererBounds(building, out var bounds))
            {
                box.center = new Vector3(0f, 3f, 0f);
                box.size = new Vector3(8f, 6f, 8f);
                return;
            }

            var root = building.transform;
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
