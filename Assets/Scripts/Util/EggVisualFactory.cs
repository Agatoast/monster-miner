using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class EggVisualFactory
    {
        const string EggResourceFolder = "Models/Eggs";
        const float WorldScale = 13.75f;
        const float WorldInteractionRadius = 0.55f;

        static readonly Vector3 HeldLocalPosition = new Vector3(0f, -0.03f, 0.08f);
        static readonly Vector3 HeldLocalEuler = new Vector3(0f, 180f, 0f);

        static GameObject[] prefabCache;
        static float cachedHorizontalRadius;

        public static Vector3 HeldEggLocalPosition => HeldLocalPosition;
        public static Vector3 HeldEggLocalEuler => HeldLocalEuler;
        public static float WorldEggScale => WorldScale;

        public static float GetWorldHorizontalRadius()
        {
            if (cachedHorizontalRadius > 0f)
                return cachedHorizontalRadius;

            var prefabs = LoadPrefabs();
            if (prefabs.Length == 0)
            {
                cachedHorizontalRadius = 21.25f * 0.5f;
                return cachedHorizontalRadius;
            }

            var temp = Object.Instantiate(prefabs[0]);
            temp.transform.localScale = Vector3.one * WorldScale;
            cachedHorizontalRadius = CavernInteriorEnforcer.GetHorizontalBoundsRadius(temp);
            Object.Destroy(temp);
            return Mathf.Max(0.5f, cachedHorizontalRadius);
        }

        public static GameObject CreateWorldEgg(Vector3 position)
        {
            var prefabs = LoadPrefabs();
            if (prefabs.Length == 0)
            {
                Debug.LogWarning($"Monster Miner: no egg prefabs found at Resources/{EggResourceFolder}.");
                return CreateFallbackEgg(position);
            }

            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            var egg = Object.Instantiate(prefab);
            egg.name = "MonsterEgg";
            egg.transform.SetPositionAndRotation(position, Quaternion.identity);
            egg.transform.localScale = Vector3.one * WorldScale;
            EggMaterialFactory.ApplyRandomDragonScaleMaterial(egg);
            EnsureCollider(egg);
            DisableRigidbodies(egg);
            return egg;
        }

        static GameObject[] LoadPrefabs()
        {
            if (prefabCache != null)
                return prefabCache;

            prefabCache = Resources.LoadAll<GameObject>(EggResourceFolder);
            if (prefabCache.Length == 0)
                prefabCache = null;
            return prefabCache ?? System.Array.Empty<GameObject>();
        }

        static GameObject CreateFallbackEgg(Vector3 position)
        {
            var egg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            egg.name = "MonsterEgg";
            egg.transform.position = position;
            egg.transform.localScale = Vector3.one * 21.25f;
            var renderer = egg.GetComponent<Renderer>();
            if (renderer != null)
                EggMaterialFactory.ApplyRandomDragonScaleMaterial(egg);
            return egg;
        }

        static void EnsureCollider(GameObject root)
        {
            if (root.GetComponentInChildren<Collider>() != null)
                return;

            var sphere = root.AddComponent<SphereCollider>();
            sphere.radius = WorldInteractionRadius / WorldScale;
        }

        static void DisableRigidbodies(GameObject root)
        {
            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
                Object.Destroy(rb);
        }
    }
}
