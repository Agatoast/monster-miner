using UnityEngine;

namespace MonsterMiner.World
{
    public class PlainsRockObstacle : MonoBehaviour
    {
        public void ConfigureCollider()
        {
            EnsureCollider();
        }

        public void EnsureCollider()
        {
            if (GetComponent<Collider>() != null)
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
    }
}
