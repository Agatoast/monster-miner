using UnityEngine;

namespace MonsterMiner.World
{
    public class PlainsRockObstacle : MonoBehaviour
    {
        public void ConfigureCollider()
        {
            EnsureColliderWithBoxFallback();
        }

        public void ConfigureMeshColliders()
        {
            foreach (var meshFilter in GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh == null)
                    continue;

                var meshObject = meshFilter.gameObject;
                var meshCollider = meshObject.GetComponent<MeshCollider>();
                if (meshCollider == null)
                    meshCollider = meshObject.AddComponent<MeshCollider>();

                // Prefab/import mesh colliders keep their editor-assigned mesh when the
                // source mesh is not readable at runtime.
                if (meshCollider.sharedMesh == null)
                    meshCollider.sharedMesh = meshFilter.sharedMesh;

                meshCollider.convex = false;
                meshCollider.providesContacts = true;
                meshCollider.enabled = true;
            }
        }

        public void EnsureCollider()
        {
            EnsureSolidMeshColliders();
        }

        public void EnsureSolidMeshColliders()
        {
            Physics.SyncTransforms();
            ConfigureMeshColliders();

            foreach (var meshFilter in GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh == null)
                    continue;

                var meshObject = meshFilter.gameObject;
                var meshCollider = meshObject.GetComponent<MeshCollider>();
                if (meshCollider == null)
                    meshCollider = meshObject.AddComponent<MeshCollider>();

                var mesh = meshFilter.sharedMesh;
                meshCollider.sharedMesh = null;
                Physics.SyncTransforms();
                meshCollider.sharedMesh = mesh;
                meshCollider.convex = false;
                meshCollider.providesContacts = true;
                meshCollider.enabled = true;
                meshCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation
                    | MeshColliderCookingOptions.EnableMeshCleaning
                    | MeshColliderCookingOptions.WeldColocatedVertices;
            }

            StripBoxColliders();
            EnsureKinematicRigidbody();
            Physics.SyncTransforms();
        }

        void EnsureKinematicRigidbody()
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;
        }

        public void EnsureColliderWithBoxFallback()
        {
            EnsureSolidMeshColliders();

            if (HasValidMeshColliders())
                return;

            if (GetComponent<BoxCollider>() != null)
                return;

            Physics.SyncTransforms();
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled)
                    continue;

                bounds.Encapsulate(renderers[i].bounds);
            }

            var box = gameObject.AddComponent<BoxCollider>();
            box.center = transform.InverseTransformPoint(bounds.center);
            Vector3 lossy = transform.lossyScale;
            box.size = new Vector3(
                bounds.size.x / Mathf.Max(0.001f, lossy.x),
                bounds.size.y / Mathf.Max(0.001f, lossy.y),
                bounds.size.z / Mathf.Max(0.001f, lossy.z));
        }

        static bool HasValidMeshColliders(GameObject root)
        {
            foreach (var meshCollider in root.GetComponentsInChildren<MeshCollider>(true))
            {
                if (meshCollider != null
                    && meshCollider.enabled
                    && !meshCollider.isTrigger
                    && meshCollider.sharedMesh != null)
                    return true;
            }

            return false;
        }

        bool HasValidMeshColliders() => HasValidMeshColliders(gameObject);

        public static bool IsSolidRockCollider(Collider collider)
        {
            if (collider == null || collider.isTrigger)
                return false;

            Transform current = collider.transform;
            while (current != null)
            {
                if (current.name.StartsWith("NatureRock5")
                    || current.name == "SolidRockCollision"
                    || current.name == "NatureRock5PairCollision"
                    || current.name.StartsWith("PlainsRock_"))
                    return true;

                current = current.parent;
            }

            return false;
        }

        void StripBoxColliders()
        {
            foreach (var box in GetComponents<BoxCollider>())
                Destroy(box);
        }
    }
}
