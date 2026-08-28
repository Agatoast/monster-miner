using UnityEngine;

namespace MonsterMiner.Util
{
    public static class SpearVisualFactory
    {
        const string PrefabResourcePath = "Models/Tools/spear";
        const float TargetHeldLength = 1.1f;

        static readonly Vector3 HeldLocalEuler = new Vector3(90f, 180f, -90f);
        static Vector3 scaledMeshCenter;

        public static Vector3 ScaledMeshCenter => scaledMeshCenter;
        public static Quaternion HeldMeshLocalRotation => Quaternion.Euler(HeldLocalEuler);

        public static GameObject CreateHeldSpear(Transform parent)
        {
            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: spear prefab not found at Resources/{PrefabResourcePath}.");
                return null;
            }

            var spear = Object.Instantiate(prefab, parent, false);
            spear.name = "HeldSpear";
            spear.transform.localRotation = Quaternion.Euler(HeldLocalEuler);
            spear.transform.localScale = ComputeHeldScale(spear);
            spear.transform.localPosition = Vector3.zero;
            scaledMeshCenter = ComputeScaledMeshCenter(spear);
            ApplyUrpMaterials(spear);
            DisableColliders(spear);
            return spear;
        }

        static Vector3 ComputeHeldScale(GameObject instance)
        {
            var meshFilter = instance.GetComponentInChildren<MeshFilter>();
            var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (mesh == null)
                return Vector3.one * 0.22f;

            Vector3 size = mesh.bounds.size;
            float length = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            if (length < 0.001f)
                return Vector3.one * 0.22f;

            return Vector3.one * (TargetHeldLength / length);
        }

        static Vector3 ComputeScaledMeshCenter(GameObject instance)
        {
            var meshFilter = instance.GetComponentInChildren<MeshFilter>();
            var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (mesh == null)
                return Vector3.zero;

            return Vector3.Scale(mesh.bounds.center, instance.transform.localScale);
        }

        static void ApplyUrpMaterials(GameObject root)
        {
            var template = Resources.Load<Material>("Materials/DefaultSurface");
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (template == null && urpLit == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterial;
                if (source == null)
                    continue;

                var material = template != null ? new Material(template) : new Material(urpLit);
                CopyTexture(source, material, "_MainTex", "_BaseMap", "_BaseMap", "_MainTex");
                CopyTexture(source, material, "_BumpMap", "_BumpMap");
                if (material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap") != null)
                    material.EnableKeyword("_NORMALMAP");

                CopyTexture(source, material, "_MetallicGlossMap", "_MetallicGlossMap");
                if (material.HasProperty("_MetallicGlossMap") && material.GetTexture("_MetallicGlossMap") != null)
                    material.EnableKeyword("_METALLICSPECGLOSSMAP");

                CopyTexture(source, material, "_OcclusionMap", "_OcclusionMap");

                if (material.HasProperty("_Smoothness"))
                    material.SetFloat("_Smoothness", 0.45f);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", Color.white);
                else if (material.HasProperty("_Color"))
                    material.color = Color.white;

                renderer.sharedMaterial = material;
            }
        }

        static void CopyTexture(Material source, Material dest, string sourceName, string destName, params string[] fallbackSourceNames)
        {
            if (source == null || dest == null || !dest.HasProperty(destName))
                return;

            Texture texture = source.HasProperty(sourceName) ? source.GetTexture(sourceName) : null;
            if (texture == null)
            {
                foreach (var name in fallbackSourceNames)
                {
                    if (!source.HasProperty(name))
                        continue;
                    texture = source.GetTexture(name);
                    if (texture != null)
                        break;
                }
            }

            if (texture != null)
                dest.SetTexture(destName, texture);
        }

        static void DisableColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
        }
    }
}
