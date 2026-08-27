using UnityEngine;

namespace MonsterMiner.Util
{
    public static class PrimitiveFactory
    {
        static Material templateMaterial;

        static Material GetTemplateMaterial()
        {
            if (templateMaterial != null)
                return templateMaterial;

            templateMaterial = Resources.Load<Material>("Materials/DefaultSurface");
            if (templateMaterial != null)
                return templateMaterial;

            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
            {
                templateMaterial = new Material(urpLit);
                return templateMaterial;
            }

            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            templateMaterial = temp.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(temp);
            return templateMaterial;
        }

        public static Material CreateColorMaterial(Color color, float smoothness = 0.2f)
        {
            var mat = new Material(GetTemplateMaterial());

            if (mat.HasProperty("_Color"))
                mat.color = color;
            else if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", smoothness);
            else if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);

            return mat;
        }

        public static GameObject CreatePrimitive(PrimitiveType type, Vector3 position, Vector3 scale, Color color, string name = null, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name ?? type.ToString();
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = CreateColorMaterial(color);
            return go;
        }

        public static void EnsureRigidbody(GameObject go, float mass = 1f, bool useGravity = true)
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.useGravity = useGravity;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }
}
