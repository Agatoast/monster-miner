using UnityEngine;

namespace MonsterMiner.Util
{
    public static class PagodaVisualFactory
    {
        const string Tahoto20mResourcePath = "Models/Architecture/Pagoda_Tahoto_20m";
        public const string Tahoto20mObjectName = "Pagoda_Tahoto_20m";

        public static GameObject CreateTahoto20mAtLocalPoint(
            Transform parent,
            Vector3 localPosition,
            float floorWorldY,
            Quaternion localRotation)
        {
            var prefab = Resources.Load<GameObject>(Tahoto20mResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"Monster Miner: pagoda prefab not found at Resources/{Tahoto20mResourcePath}. "
                    + "Copy Pagoda Architecture/Prefabs/Pagoda_Tahoto_20m into Assets/Resources/Models/Architecture/.");
                return null;
            }

            var pagoda = Object.Instantiate(prefab, parent, false);
            pagoda.name = Tahoto20mObjectName;
            pagoda.transform.localRotation = localRotation;
            pagoda.transform.localPosition = localPosition;
            KnifeVisualFactory.ApplyUrpMaterials(pagoda);
            StripImportedColliders(pagoda);
            StripRigidbodies(pagoda);
            AlignBaseCenterToLocalPoint(pagoda, parent, localPosition);
            FloorAnchor.SnapBottomToFloor(pagoda, floorWorldY);
            return pagoda;
        }

        static void AlignBaseCenterToLocalPoint(GameObject pagoda, Transform parent, Vector3 targetLocal)
        {
            Physics.SyncTransforms();
            if (!TryGetRendererBounds(pagoda, out var bounds))
            {
                pagoda.transform.localPosition = targetLocal;
                return;
            }

            Vector3 baseCenterLocal = parent.InverseTransformPoint(
                new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            pagoda.transform.localPosition += targetLocal - baseCenterLocal;
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

        static void StripImportedColliders(GameObject root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null)
                    continue;

                if (Application.isPlaying)
                    Object.DestroyImmediate(collider);
                else
                    Object.Destroy(collider);
            }

            Physics.SyncTransforms();
        }

        static void StripRigidbodies(GameObject root)
        {
            var rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                var body = rigidbodies[i];
                if (body == null)
                    continue;

                if (Application.isPlaying)
                    Object.DestroyImmediate(body);
                else
                    Object.Destroy(body);
            }
        }
    }
}
