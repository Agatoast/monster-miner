using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.World
{
    /// <summary>
    /// Builds one combined solid mesh collider for the Quarry 3 NatureRock5 pair.
    /// </summary>
    public static class NatureRockPairCollisionBuilder
    {
        const string PairCollisionObjectName = "NatureRock5PairCollision";

        public static void BuildPairCollision(GameObject pairRoot)
        {
            if (pairRoot == null)
                return;

            foreach (var rb in pairRoot.GetComponentsInChildren<Rigidbody>(true))
                Object.Destroy(rb);

            foreach (var collider in pairRoot.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);

            foreach (var child in pairRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child != pairRoot.transform && child.name == PairCollisionObjectName)
                    Object.Destroy(child.gameObject);
            }

            Physics.SyncTransforms();

            Mesh combinedMesh = TryCombineMeshes(pairRoot.transform);
            if (combinedMesh == null || combinedMesh.vertexCount <= 0)
            {
                Debug.LogWarning(
                    $"Monster Miner: failed to combine collision mesh for {pairRoot.name}; using bounds fallback.");
                BuildBoundsFallback(pairRoot);
                return;
            }

            var collisionGo = new GameObject(PairCollisionObjectName);
            collisionGo.transform.SetParent(pairRoot.transform, false);
            collisionGo.transform.localPosition = Vector3.zero;
            collisionGo.transform.localRotation = Quaternion.identity;
            collisionGo.transform.localScale = Vector3.one;

            var meshCollider = collisionGo.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = combinedMesh;
            meshCollider.convex = false;

            if (pairRoot.GetComponent<PlainsRockObstacle>() == null)
                pairRoot.AddComponent<PlainsRockObstacle>();

            Physics.SyncTransforms();
        }

        static Mesh TryCombineMeshes(Transform pairRoot)
        {
            var meshFilters = pairRoot.GetComponentsInChildren<MeshFilter>(true);
            if (meshFilters.Length == 0)
                return null;

            var combines = new CombineInstance[meshFilters.Length];
            Matrix4x4 pairWorldToLocal = pairRoot.worldToLocalMatrix;
            int count = 0;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                var meshFilter = meshFilters[i];
                if (meshFilter.sharedMesh == null)
                    continue;

                Mesh readable = CopyMesh(meshFilter.sharedMesh);
                if (readable == null || readable.vertexCount <= 0)
                    continue;

                combines[count++] = new CombineInstance
                {
                    mesh = readable,
                    transform = pairWorldToLocal * meshFilter.transform.localToWorldMatrix,
                };
            }

            if (count == 0)
                return null;

            if (count != combines.Length)
            {
                var trimmed = new CombineInstance[count];
                for (int i = 0; i < count; i++)
                    trimmed[i] = combines[i];
                combines = trimmed;
            }

            var combined = new Mesh
            {
                name = "NatureRock5Pair_Collision",
                indexFormat = IndexFormat.UInt32,
            };
            combined.CombineMeshes(combines, mergeSubMeshes: true, useMatrices: true);
            combined.RecalculateBounds();
            return combined.vertexCount > 0 ? combined : null;
        }

        static void BuildBoundsFallback(GameObject pairRoot)
        {
            if (!TryGetRendererBounds(pairRoot, out Bounds bounds))
                return;

            var collisionGo = new GameObject(PairCollisionObjectName);
            collisionGo.transform.SetParent(pairRoot.transform, false);
            collisionGo.transform.localPosition = Vector3.zero;
            collisionGo.transform.localRotation = Quaternion.identity;
            collisionGo.transform.localScale = Vector3.one;

            var box = collisionGo.AddComponent<BoxCollider>();
            box.center = pairRoot.transform.InverseTransformPoint(bounds.center);
            Vector3 lossy = pairRoot.transform.lossyScale;
            box.size = new Vector3(
                bounds.size.x / Mathf.Max(0.001f, lossy.x),
                bounds.size.y / Mathf.Max(0.001f, lossy.y),
                bounds.size.z / Mathf.Max(0.001f, lossy.z));

            if (pairRoot.GetComponent<PlainsRockObstacle>() == null)
                pairRoot.AddComponent<PlainsRockObstacle>();

            Physics.SyncTransforms();
        }

        static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
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

        static Mesh CopyMesh(Mesh source)
        {
            if (source == null)
                return null;

            if (source.isReadable)
            {
                var readable = new Mesh
                {
                    name = source.name + "_Collision",
                    indexFormat = source.vertexCount > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                };
                readable.vertices = source.vertices;
                readable.triangles = source.triangles;
                readable.RecalculateBounds();
                return readable.vertexCount > 0 ? readable : null;
            }

            var instantiated = Object.Instantiate(source);
            instantiated.name = source.name + "_Collision";
            if (instantiated.vertexCount > 65000)
                instantiated.indexFormat = IndexFormat.UInt32;
            return instantiated.vertexCount > 0 ? instantiated : null;
        }
    }
}
