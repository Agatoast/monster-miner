using MonsterMiner.Data;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class KnifeVisualFactory
    {
        const string PrefabResourcePath = "Models/Tools/knife_polymer";

        static readonly Vector3 HeldLocalPosition = new Vector3(0.01f, -0.04f, 0f);
        static readonly Vector3 HeldLocalEuler = new Vector3(90f, 180f, -90f);
        static readonly Vector3 HeldLocalScale = Vector3.one * 0.9f;
        static readonly Vector3 SwingPivotLocal = new Vector3(0f, -0.08f, 0f);

        static readonly Vector3 BoardLocalEuler = new Vector3(0f, 90f, 90f);
        const float BoardLocalScale = 1.35f;

        public static Vector3 HeldMeshLocalPosition => HeldLocalPosition;
        public static Quaternion HeldMeshLocalRotation => Quaternion.Euler(HeldLocalEuler);
        public static Vector3 HeldSwingPivotLocal => SwingPivotLocal;

        public static GameObject CreateHeldKnife(Transform parent)
        {
            return CreateVisual(parent, HeldLocalPosition, Quaternion.Euler(HeldLocalEuler), HeldLocalScale, "HeldKnife", null);
        }

        public static GameObject CreateHeldKnife(Transform parent, ItemDefinition knifeItem)
        {
            return CreateVisual(
                parent,
                HeldLocalPosition,
                Quaternion.Euler(HeldLocalEuler),
                HeldLocalScale,
                knifeItem != null ? $"HeldKnife_{knifeItem.itemId}" : "HeldKnife",
                knifeItem?.worldColor);
        }

        public static GameObject CreateBoardKnife(Transform parent)
        {
            return CreateBoardVisual(parent, PrefabResourcePath);
        }

        public static GameObject CreateBoardVisual(Transform parent, string resourcePath)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
                return null;

            var visual = Object.Instantiate(prefab, parent, false);
            visual.name = "BoardVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(BoardLocalEuler);
            visual.transform.localScale = Vector3.one * BoardLocalScale;
            ApplyUrpMaterials(visual);
            DisableColliders(visual);
            return visual;
        }

        static GameObject CreateVisual(Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, string objectName, Color? bladeTint)
        {
            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: knife prefab not found at Resources/{PrefabResourcePath}.");
                return null;
            }

            var knife = Object.Instantiate(prefab, parent, false);
            knife.name = objectName;
            knife.transform.localPosition = localPosition;
            knife.transform.localRotation = localRotation;
            knife.transform.localScale = localScale;
            ApplyUrpMaterials(knife, bladeTint);
            DisableColliders(knife);
            return knife;
        }

        static GameObject CreateVisual(Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, string objectName)
        {
            return CreateVisual(parent, localPosition, localRotation, localScale, objectName, null);
        }

        public static void ApplyUrpMaterials(GameObject root)
        {
            ApplyUrpMaterials(root, null);
        }

        public static void ApplyUrpMaterials(GameObject root, Color? bladeTint)
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
                var albedo = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
                if (albedo == null && source.HasProperty("_BaseMap"))
                    albedo = source.GetTexture("_BaseMap");

                if (albedo != null)
                {
                    if (material.HasProperty("_BaseMap"))
                        material.SetTexture("_BaseMap", albedo);
                    else if (material.HasProperty("_MainTex"))
                        material.SetTexture("_MainTex", albedo);
                }

                Color tint = bladeTint ?? (source.HasProperty("_Color") ? source.color : Color.white);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", tint);
                else if (material.HasProperty("_Color"))
                    material.color = tint;

                renderer.sharedMaterial = material;
            }
        }

        static void DisableColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
        }
    }
}
