using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.World
{
    /// <summary>
    /// Bakes solid mesh colliders for the Quarry 3 Rock5 pair only (NatureRock5_N / NatureRock5_S).
    /// </summary>
    public static class NatureRockCollisionBuilder
    {
        public static void BuildSolidCollision(GameObject rockRoot)
        {
            if (rockRoot == null)
                return;

            foreach (var rb in rockRoot.GetComponentsInChildren<Rigidbody>(true))
                Object.Destroy(rb);

            foreach (var child in rockRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child != rockRoot.transform && child.name == "SolidRockCollision")
                    Object.Destroy(child.gameObject);
            }

            Physics.SyncTransforms();

            int bakedCount = 0;
            foreach (var meshFilter in rockRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh == null)
                    continue;

                var meshObject = meshFilter.gameObject;
                foreach (var collider in meshObject.GetComponents<Collider>())
                    Object.Destroy(collider);

                Mesh collisionMesh = CopyMesh(meshFilter.sharedMesh);
                if (collisionMesh == null || collisionMesh.vertexCount <= 0)
                    continue;

                var meshCollider = meshObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = collisionMesh;
                meshCollider.convex = false;

                bakedCount++;
            }

            if (rockRoot.GetComponent<PlainsRockObstacle>() == null)
                rockRoot.AddComponent<PlainsRockObstacle>();

            Physics.SyncTransforms();

            if (bakedCount == 0)
            {
                Debug.LogWarning(
                    $"Monster Miner: no solid collision baked for {rockRoot.name}. "
                    + "Check that the Rock5 mesh is import-readable.");
            }
        }

        static Mesh CopyMesh(Mesh source)
        {
            if (source == null)
                return null;

            var copy = Object.Instantiate(source);
            copy.name = source.name + "_SolidCollision";
            if (copy.vertexCount > 65000)
                copy.indexFormat = IndexFormat.UInt32;
            return copy;
        }
    }
}
