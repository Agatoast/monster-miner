using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.Artillery
{
    public class ArtilleryCherryTree : MonoBehaviour
    {
        public static ArtilleryCherryTree Create(Transform parent, Vector3 floorPoint, int seed)
        {
            var go = new GameObject("CherryTree");
            go.transform.SetParent(parent, false);
            go.transform.position = floorPoint;

            var tree = go.AddComponent<ArtilleryCherryTree>();
            var rng = new System.Random(seed);
            float scale = 0.78f + (float)rng.NextDouble() * 0.35f;
            float trunkHeight = 0.38f * scale;
            float trunkRadius = 0.018f * scale;

            var trunk = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cylinder,
                floorPoint,
                new Vector3(trunkRadius * 2f, trunkHeight * 0.5f, trunkRadius * 2f),
                new Color(0.18f, 0.11f, 0.08f),
                "Trunk",
                go.transform);
            trunk.transform.localPosition = new Vector3(0f, trunkHeight * 0.5f, 0f);
            Object.Destroy(trunk.GetComponent<Collider>());

            int puffs = 3 + rng.Next(3);
            for (int i = 0; i < puffs; i++)
            {
                float px = ((float)rng.NextDouble() - 0.5f) * 0.16f * scale;
                float py = trunkHeight + (0.08f + (float)rng.NextDouble() * 0.14f) * scale;
                float pz = ((float)rng.NextDouble() - 0.5f) * 0.10f * scale;
                float size = (0.11f + (float)rng.NextDouble() * 0.08f) * scale;
                var pink = Color.Lerp(
                    new Color(0.93f, 0.42f, 0.62f),
                    new Color(0.98f, 0.62f, 0.74f),
                    (float)rng.NextDouble());

                var puff = PrimitiveFactory.CreatePrimitive(
                    PrimitiveType.Sphere,
                    floorPoint,
                    new Vector3(size, size * 0.85f, size),
                    pink,
                    "Blossom",
                    go.transform);
                puff.transform.localPosition = new Vector3(px, py, pz);
                Object.Destroy(puff.GetComponent<Collider>());
            }

            var hit = go.AddComponent<CapsuleCollider>();
            hit.center = new Vector3(0f, trunkHeight * 0.7f, 0f);
            hit.radius = 0.09f * scale;
            hit.height = trunkHeight + 0.22f * scale;
            return tree;
        }

        public void DestroyTree()
        {
            Destroy(gameObject);
        }
    }
}
