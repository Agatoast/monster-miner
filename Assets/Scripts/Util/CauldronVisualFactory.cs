using UnityEngine;

namespace MonsterMiner.Util
{
    public static class CauldronVisualFactory
    {
        const string PrefabResourcePath = "Models/Props/Cauldron_01";

        public static GameObject CreateCenteredOnDigSite(Transform parent, Vector3 worldCenter)
        {
            if (parent == null)
                return null;

            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: cauldron prefab not found at Resources/{PrefabResourcePath}.");
                return null;
            }

            var cauldron = Object.Instantiate(prefab, parent, false);
            cauldron.name = "Cauldron_01";
            cauldron.transform.localPosition = Vector3.zero;
            cauldron.transform.localScale = Vector3.one * 10f;

            KnifeVisualFactory.ApplyUrpMaterials(cauldron);
            CenterMeshOnParentOrigin(cauldron);

            float floorY = FloorAnchor.ResolveFloorSurfaceY(worldCenter);
            FloorAnchor.SnapBottomToFloor(cauldron, floorY, restOffset: 0f);
            EnsureSolidCollider(cauldron);

            Bounds localBounds = CylinderWrappedTextFactory.GetLocalRendererBounds(cauldron);
            CylinderWrappedTextFactory.AttachUpsideDown(
                cauldron.transform,
                "Escape Pod 17",
                localBounds,
                facingAngleRadians: 0f);

            return cauldron;
        }

        static void CenterMeshOnParentOrigin(GameObject go)
        {
            Physics.SyncTransforms();
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 localCenter = go.transform.InverseTransformPoint(bounds.center);
            var offset = go.transform.localPosition;
            offset.x -= localCenter.x;
            offset.z -= localCenter.z;
            go.transform.localPosition = offset;
        }

        static void EnsureSolidCollider(GameObject root)
        {
            Physics.SyncTransforms();

            bool hasMeshCollider = false;
            foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh == null || IsLabelMesh(meshFilter.transform))
                    continue;

                var meshObject = meshFilter.gameObject;
                var meshCollider = meshObject.GetComponent<MeshCollider>();
                if (meshCollider == null)
                    meshCollider = meshObject.AddComponent<MeshCollider>();

                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;
                meshCollider.isTrigger = false;
                meshCollider.providesContacts = true;
                meshCollider.enabled = true;
                hasMeshCollider = true;
            }

            if (hasMeshCollider)
            {
                foreach (var box in root.GetComponentsInChildren<BoxCollider>(true))
                    DestroyCollider(box);
            }
            else
            {
                EnsureBoxColliderFromRenderers(root);
            }

            var rb = root.GetComponent<Rigidbody>();
            if (rb == null)
                rb = root.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;
            Physics.SyncTransforms();
        }

        static void EnsureBoxColliderFromRenderers(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled || IsLabelMesh(renderers[i].transform))
                    continue;

                bounds.Encapsulate(renderers[i].bounds);
            }

            foreach (var box in root.GetComponents<BoxCollider>())
                DestroyCollider(box);

            var boxCollider = root.AddComponent<BoxCollider>();
            boxCollider.center = root.transform.InverseTransformPoint(bounds.center);
            Vector3 lossy = root.transform.lossyScale;
            boxCollider.size = new Vector3(
                bounds.size.x / Mathf.Max(0.001f, lossy.x),
                bounds.size.y / Mathf.Max(0.001f, lossy.y),
                bounds.size.z / Mathf.Max(0.001f, lossy.z));
            boxCollider.isTrigger = false;
        }

        static bool IsLabelMesh(Transform transform)
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name == "CylinderLabel")
                    return true;

                current = current.parent;
            }

            return false;
        }

        static void DestroyCollider(Collider collider)
        {
            if (collider == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }
    }
}
