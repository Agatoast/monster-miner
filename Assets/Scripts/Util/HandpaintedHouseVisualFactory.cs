using UnityEngine;

namespace MonsterMiner.Util
{
    public static class HandpaintedHouseVisualFactory
    {
        const string ResourcePath = "Models/Props/handpainted_house";

        public static GameObject CreateOnPlateau(
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            float floorWorldY)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: handpainted house prefab not found at Resources/{ResourcePath}.");
                return null;
            }

            var house = Object.Instantiate(prefab, parent, false);
            house.name = "HandpaintedHouse";
            house.transform.localRotation = localRotation;
            house.transform.localPosition = localPosition;
            KnifeVisualFactory.ApplyUrpMaterials(house);
            StripImportedColliders(house);
            FloorAnchor.SnapBottomToFloor(house, floorWorldY);
            AddTightCollision(house);
            return house;
        }

        static void StripImportedColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
                Object.Destroy(collider);
            }
        }

        static void AddTightCollision(GameObject house)
        {
            var box = house.AddComponent<BoxCollider>();
            var renderers = house.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                box.center = new Vector3(0f, 2.5f, 0f);
                box.size = new Vector3(3.2f, 5f, 4.3f);
                return;
            }

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled)
                    continue;
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            var root = house.transform;
            Vector3 localCenter = root.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = root.InverseTransformVector(worldBounds.size);
            box.center = localCenter;
            box.size = new Vector3(
                Mathf.Abs(localSize.x),
                Mathf.Abs(localSize.y),
                Mathf.Abs(localSize.z));
        }
    }
}
