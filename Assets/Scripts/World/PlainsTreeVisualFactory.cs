using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class PlainsTreeVisualFactory
    {
        public static void CreateTreeCopse(
            Transform parent,
            Vector2 localCenter,
            int seed,
            System.Func<float, float, float> sampleGroundY,
            CavernBounds bounds = null)
        {
            var copse = new GameObject($"Copse_{seed}").transform;
            copse.SetParent(parent, false);
            copse.localPosition = new Vector3(localCenter.x, 0f, localCenter.y);

            int treeCount = Random.Range(4, 8);
            for (int i = 0; i < treeCount; i++)
            {
                float offsetAngle = Random.Range(0f, Mathf.PI * 2f);
                float offsetDistance = Random.Range(0.4f, 3.2f);
                var offset = new Vector3(
                    Mathf.Cos(offsetAngle) * offsetDistance,
                    0f,
                    Mathf.Sin(offsetAngle) * offsetDistance);

                float treeX = localCenter.x + offset.x;
                float treeZ = localCenter.y + offset.z;
                if (bounds != null && !bounds.AllowsEggStyleSpawn(treeX, treeZ))
                    continue;

                CreateTree(copse, offset, seed * 17 + i, localCenter, sampleGroundY);
            }
        }

        static void CreateTree(
            Transform parent,
            Vector3 localOffset,
            int seed,
            Vector2 copseCenter,
            System.Func<float, float, float> sampleGroundY)
        {
            float treeX = copseCenter.x + localOffset.x;
            float treeZ = copseCenter.y + localOffset.z;
            float groundY = sampleGroundY(treeX, treeZ);

            var tree = new GameObject($"Tree_{seed}").transform;
            tree.SetParent(parent, false);
            tree.localPosition = new Vector3(localOffset.x, groundY, localOffset.z);

            float trunkHeight = Random.Range(1.8f, 3.1f);
            float trunkRadius = Random.Range(0.14f, 0.24f);
            float foliageScale = Random.Range(1.4f, 2.4f);

            var trunk = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cylinder,
                tree.position,
                new Vector3(trunkRadius * 2f, trunkHeight * 0.5f, trunkRadius * 2f),
                new Color(0.38f, 0.24f, 0.12f),
                "Trunk",
                tree);
            trunk.transform.localPosition = new Vector3(0f, trunkHeight * 0.5f, 0f);
            Object.Destroy(trunk.GetComponent<Collider>());

            var foliage = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                tree.position,
                new Vector3(foliageScale, foliageScale * 1.15f, foliageScale),
                new Color(0.22f, 0.48f, 0.18f),
                "Foliage",
                tree);
            foliage.transform.localPosition = new Vector3(0f, trunkHeight + foliageScale * 0.35f, 0f);
            foliage.transform.localRotation = Quaternion.Euler(0f, seed * 37f % 360f, 0f);
            Object.Destroy(foliage.GetComponent<Collider>());

            var foliageAccent = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                tree.position,
                new Vector3(foliageScale * 0.75f, foliageScale * 0.55f, foliageScale * 0.75f),
                new Color(0.28f, 0.58f, 0.22f),
                "FoliageAccent",
                tree);
            foliageAccent.transform.localPosition = foliage.transform.localPosition + new Vector3(0f, foliageScale * 0.22f, 0f);
            foliageAccent.transform.localRotation = Quaternion.Euler(0f, (seed * 53f + 40f) % 360f, 0f);
            Object.Destroy(foliageAccent.GetComponent<Collider>());
        }

        public static void CreateVistaTreeCopse(
            Transform parent,
            Vector2 localCenter,
            int seed,
            System.Func<float, float, float> sampleGroundY)
        {
            var randomState = Random.state;
            Random.InitState(seed * 7919 + 104729);

            var copse = new GameObject($"VistaCopse_{seed}").transform;
            copse.SetParent(parent, false);
            copse.localPosition = new Vector3(localCenter.x, 0f, localCenter.y);

            var canopyMaterial = CavernSurfaceMaterialFactory.GetUnlitVistaCanopyMaterial();
            int blobCount = Random.Range(3, 6);
            for (int i = 0; i < blobCount; i++)
            {
                float offsetAngle = Random.Range(0f, Mathf.PI * 2f);
                float offsetDistance = Random.Range(WorldScale.Feet(6f), WorldScale.Feet(28f));
                float blobLocalX = Mathf.Cos(offsetAngle) * offsetDistance;
                float blobLocalZ = Mathf.Sin(offsetAngle) * offsetDistance;
                float groundY = sampleGroundY(localCenter.x + blobLocalX, localCenter.y + blobLocalZ);

                float width = Random.Range(WorldScale.Feet(45f), WorldScale.Feet(90f));
                float depth = width * Random.Range(0.68f, 1.08f);
                float height = Random.Range(WorldScale.Feet(16f), WorldScale.Feet(34f));

                var canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                canopy.name = $"VistaCanopy_{seed}_{i}";
                canopy.transform.SetParent(copse, false);
                canopy.transform.localPosition = new Vector3(blobLocalX, groundY + height * 0.42f, blobLocalZ);
                canopy.transform.localRotation = Quaternion.Euler(
                    Random.Range(-4f, 4f),
                    Random.Range(0f, 360f),
                    Random.Range(-3f, 3f));
                canopy.transform.localScale = new Vector3(width, height, depth);
                canopy.GetComponent<Renderer>().sharedMaterial = canopyMaterial;
                Object.Destroy(canopy.GetComponent<Collider>());
            }

            Random.state = randomState;
        }
    }
}
