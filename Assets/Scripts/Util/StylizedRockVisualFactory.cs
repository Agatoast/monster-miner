using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class StylizedRockVisualFactory
    {
        const string ResourceFolder = "Models/Rocks";

        static GameObject[] prefabs;

        public static GameObject CreateOnGround(Transform parent, Vector3 floorContactPoint, int seed)
        {
            var prefab = PickPrefab(seed);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: stylized rock prefabs not found at Resources/{ResourceFolder}.");
                return null;
            }

            var state = Random.state;
            Random.InitState(seed);
            float targetWidth = Random.Range(WorldScale.Feet(2.2f), WorldScale.Feet(7.5f));
            float yaw = Random.Range(0f, 360f);
            Random.state = state;

            var rock = Object.Instantiate(prefab, parent, false);
            rock.name = $"PlainsRock_{seed}";
            rock.transform.SetPositionAndRotation(floorContactPoint, Quaternion.Euler(0f, yaw, 0f));
            KnifeVisualFactory.ApplyUrpMaterials(rock);
            StripImportedColliders(rock);
            ScaleToWidth(rock, targetWidth);
            FloorAnchor.PlaceOnFloor(rock, floorContactPoint);
            return rock;
        }

        static GameObject PickPrefab(int seed)
        {
            if (prefabs == null)
                prefabs = Resources.LoadAll<GameObject>(ResourceFolder);

            if (prefabs == null || prefabs.Length == 0)
                return null;

            var state = Random.state;
            Random.InitState(seed + 7919);
            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            Random.state = state;
            return prefab;
        }

        static void ScaleToWidth(GameObject rock, float targetWidth)
        {
            var renderers = rock.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled)
                    continue;
                bounds.Encapsulate(renderers[i].bounds);
            }

            float currentWidth = Mathf.Max(bounds.size.x, bounds.size.z);
            if (currentWidth < 0.001f)
                return;

            rock.transform.localScale *= targetWidth / currentWidth;
        }

        static void StripImportedColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
                Object.Destroy(collider);
            }
        }
    }
}
