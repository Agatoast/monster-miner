using UnityEngine;

namespace MonsterMiner.Util
{
    public static class PebbleVisualFactory
    {
        const float WorldScaleMultiplier = 2f;

        // Hand cubes are ~15 cu in; pebbles are ~16 cu in.
        static readonly Vector3 HandDimensions = new Vector3(0.08f, 0.12f, 0.08f);
        const float HandVolumeCubicInches = 15f;
        const float PebbleVolumeCubicInches = 16f;

        static Material pebbleMaterial;

        public static Vector3 GetPebbleScale(int seed)
        {
            float handVolume = HandDimensions.x * HandDimensions.y * HandDimensions.z;
            float pebbleVolume = handVolume * (PebbleVolumeCubicInches / HandVolumeCubicInches);
            float linear = Mathf.Pow(pebbleVolume, 1f / 3f);

            var state = Random.state;
            Random.InitState(seed);
            var scale = new Vector3(
                linear * Random.Range(0.82f, 1.12f),
                linear * Random.Range(0.72f, 1.02f),
                linear * Random.Range(0.82f, 1.12f));
            Random.state = state;
            return scale;
        }

        public static GameObject CreateWorldPebble(Vector3 floorContactPoint, string name = "Shiny Pebble")
        {
            int seed = Mathf.Abs((floorContactPoint * 1000f).GetHashCode());
            var scale = GetPebbleScale(seed) * WorldScaleMultiplier;
            var pebble = CreatePebble(name, floorContactPoint, scale, seed, includeCollider: true);
            FloorAnchor.PlaceOnFloor(pebble, floorContactPoint);
            return pebble;
        }

        public static GameObject CreateHeldPebble(Transform parent, Vector3 localPosition, int seed = 0)
        {
            var pebble = CreatePebble("HeldPebble", parent.position, GetPebbleScale(seed), seed, includeCollider: false);
            pebble.transform.SetParent(parent, false);
            pebble.transform.localPosition = localPosition;
            return pebble;
        }

        static GameObject CreatePebble(string name, Vector3 position, Vector3 scale, int seed, bool includeCollider)
        {
            var state = Random.state;
            Random.InitState(seed == 0 ? 9137 : seed);

            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.transform.rotation = Random.rotation;

            Random.state = state;

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = GetPebbleMesh(seed);

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetPebbleMaterial();

            if (includeCollider)
            {
                var collider = go.AddComponent<SphereCollider>();
                collider.radius = 0.5f;
            }

            return go;
        }

        static Mesh GetPebbleMesh(int seed)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var source = temp.GetComponent<MeshFilter>().sharedMesh;
            var mesh = Object.Instantiate(source);
            Object.Destroy(temp);

            var vertices = mesh.vertices;
            var state = Random.state;
            Random.InitState(seed == 0 ? 9137 : seed);

            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                float bump = Random.Range(0.78f, 1.18f);
                v.x *= bump * Random.Range(0.9f, 1.15f);
                v.y *= bump * Random.Range(0.75f, 1.05f);
                v.z *= bump * Random.Range(0.9f, 1.15f);
                vertices[i] = v.normalized * 0.5f;
            }

            Random.state = state;
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Material GetPebbleMaterial()
        {
            if (pebbleMaterial != null)
                return pebbleMaterial;

            pebbleMaterial = PrimitiveFactory.CreateColorMaterial(new Color(1f, 0.85f, 0.25f), 0.82f);
            var texture = Resources.Load<Texture2D>("Textures/ShinyPebble");

            if (texture != null && pebbleMaterial.HasProperty("_BaseMap"))
                pebbleMaterial.SetTexture("_BaseMap", texture);

            if (pebbleMaterial.HasProperty("_BaseColor"))
                pebbleMaterial.SetColor("_BaseColor", Color.white);

            if (pebbleMaterial.HasProperty("_Metallic"))
                pebbleMaterial.SetFloat("_Metallic", 0.9f);

            if (pebbleMaterial.HasProperty("_Smoothness"))
                pebbleMaterial.SetFloat("_Smoothness", 0.82f);

            return pebbleMaterial;
        }
    }
}
