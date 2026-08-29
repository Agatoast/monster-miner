using MonsterMiner.Data;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class MachineGunVisualFactory
    {
        const string PrefabResourcePath = "Models/Tools/m249";
        const float TargetHeldLength = 0.95f;

        static readonly Vector3 HeldLocalPosition = new Vector3(0.02f, -0.03f, 0.04f);
        static readonly Quaternion HeldLocalRotation =
            Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(0f, 0f, -90f) * Quaternion.Euler(0f, 0f, 180f) * Quaternion.Euler(-15f, 0f, 0f);

        public static Vector3 HeldMeshLocalPosition => HeldLocalPosition;
        public static Quaternion HeldMeshLocalRotation => HeldLocalRotation;

        public static GameObject CreateHeldMachineGun(Transform parent, ItemDefinition item = null)
        {
            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: M249 prefab not found at Resources/{PrefabResourcePath}.");
                return null;
            }

            var machineGun = Object.Instantiate(prefab, parent, false);
            machineGun.name = "HeldMachineGun";
            machineGun.transform.localRotation = HeldLocalRotation;
            machineGun.transform.localScale = ComputeHeldScale(machineGun);
            machineGun.transform.localPosition = HeldLocalPosition;
            ApplyUrpMaterials(machineGun);
            KnifeVisualFactory.ApplyLegendaryGoldMaterialsIfNeeded(machineGun, item);
            DisableColliders(machineGun);
            return machineGun;
        }

        static Vector3 ComputeHeldScale(GameObject instance)
        {
            Bounds bounds = ComputeCombinedBounds(instance);
            if (bounds.size.sqrMagnitude < 0.000001f)
                return Vector3.one * 0.35f;

            Vector3 size = bounds.size;
            float length = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            if (length < 0.001f)
                return Vector3.one * 0.35f;

            return Vector3.one * (TargetHeldLength / length);
        }

        static Bounds ComputeCombinedBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 localCenter = instance.transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = bounds.size;
            float maxScale = Mathf.Max(
                instance.transform.lossyScale.x,
                instance.transform.lossyScale.y,
                instance.transform.lossyScale.z);
            if (maxScale > 0.0001f)
                localSize /= maxScale;

            return new Bounds(localCenter, localSize);
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
