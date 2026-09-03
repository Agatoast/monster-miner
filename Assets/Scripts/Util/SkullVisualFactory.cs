using System;
using MonsterMiner.World;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonsterMiner.Util
{
    public static class SkullVisualFactory
    {
        const string PrefabEditorPath = "Assets/Stylized Skull/Prefabs/Skull.prefab";
        const string PrefabResourcePath = "Models/Props/skull";
        public const float WorldDropScaleMultiplier = 5f;

        public static GameObject CreateWorldDrop(Vector3 floorContactPoint, string displayName)
        {
            GameObject prefab = LoadPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("Monster Miner: Stylized Skull prefab missing.");
                return null;
            }

            if (!FloorAnchor.TryResolveFloorPoint(floorContactPoint, 16f, 32f, out var floorPoint))
                floorPoint = floorContactPoint;

            int seed = Mathf.Abs((floorPoint * 1000f).GetHashCode());
            var state = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed);
            float yaw = UnityEngine.Random.Range(0f, 360f);
            UnityEngine.Random.state = state;

            var skull = UnityEngine.Object.Instantiate(prefab);
            skull.name = displayName ?? "Jörmungandr Skull";
            skull.transform.SetPositionAndRotation(floorPoint, Quaternion.Euler(0f, yaw, 0f));
            PrepareForGroundDrop(skull);
            skull.transform.localScale = Vector3.one * WorldDropScaleMultiplier;
            Physics.SyncTransforms();
            EnsureBoxCollider(skull);
            FloorAnchor.PlaceOnFloor(skull, floorPoint);
            return skull;
        }

        static void PrepareForGroundDrop(GameObject root)
        {
            KnifeVisualFactory.ApplyUrpMaterials(root);

            var lodGroup = root.GetComponent<LODGroup>();
            if (lodGroup != null)
                lodGroup.enabled = false;

            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                bool isLod0 = renderer.gameObject.name.IndexOf("LOD0", StringComparison.OrdinalIgnoreCase) >= 0;
                renderer.gameObject.SetActive(isLod0);
                renderer.enabled = isLod0;
            }
        }

        static void EnsureBoxCollider(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                    UnityEngine.Object.DestroyImmediate(collider);
            }

            Bounds localBounds = ComputeEnabledRendererBoundsLocal(root);
            var box = root.AddComponent<BoxCollider>();
            box.center = localBounds.center;
            box.size = Vector3.Max(localBounds.size, Vector3.one * 0.05f);
            box.isTrigger = false;
        }

        static Bounds ComputeEnabledRendererBoundsLocal(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds? world = null;
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                if (world == null)
                    world = renderer.bounds;
                else
                {
                    var bounds = world.Value;
                    bounds.Encapsulate(renderer.bounds);
                    world = bounds;
                }
            }

            if (world == null)
                return new Bounds(Vector3.zero, Vector3.one * 0.3f);

            Vector3 localCenter = root.transform.InverseTransformPoint(world.Value.center);
            Vector3 lossy = root.transform.lossyScale;
            Vector3 localSize = new Vector3(
                lossy.x > 0.0001f ? world.Value.size.x / lossy.x : world.Value.size.x,
                lossy.y > 0.0001f ? world.Value.size.y / lossy.y : world.Value.size.y,
                lossy.z > 0.0001f ? world.Value.size.z / lossy.z : world.Value.size.z);
            return new Bounds(localCenter, localSize);
        }

        static GameObject LoadPrefab()
        {
            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab != null)
                return prefab;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabEditorPath);
#else
            return null;
#endif
        }
    }
}
