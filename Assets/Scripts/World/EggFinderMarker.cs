using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public class EggFinderMarker : MonoBehaviour
    {
        const float HoverAboveEgg = 1.6f;
        const float LabelAboveArrow = 1.35f;
        const float PulseSpeed = 3.2f;
        const float PulseAmount = 0.08f;

        MonsterEgg target;
        Transform labelTransform;
        Vector3 baseScale = Vector3.one;
        float pulse;

        public static EggFinderMarker Create(MonsterEgg egg, Color color, string creatureLabel)
        {
            if (egg == null)
                return null;

            var root = new GameObject("EggFinderMarker");
            var marker = root.AddComponent<EggFinderMarker>();
            marker.target = egg;
            marker.BuildVisual(color);
            marker.BuildLabel(creatureLabel);
            marker.SnapToTarget();
            return marker;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            SnapToTarget();
            FaceLabelToCamera();

            pulse += Time.deltaTime * PulseSpeed;
            float scale = 1f + Mathf.Sin(pulse) * PulseAmount;
            transform.localScale = baseScale * scale;
        }

        void SnapToTarget()
        {
            if (target == null)
                return;

            var bounds = GetTargetBounds();
            transform.position = new Vector3(bounds.center.x, bounds.max.y + HoverAboveEgg, bounds.center.z);
            transform.rotation = Quaternion.identity;
        }

        void FaceLabelToCamera()
        {
            if (labelTransform == null)
                return;

            var camera = Camera.main;
            if (camera == null)
                return;

            labelTransform.rotation = Quaternion.LookRotation(
                labelTransform.position - camera.transform.position,
                Vector3.up);
        }

        Bounds GetTargetBounds()
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(target.transform.position, Vector3.one);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        void BuildLabel(string creatureLabel)
        {
            if (string.IsNullOrEmpty(creatureLabel))
                return;

            var labelGo = new GameObject("CreatureLabel");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, LabelAboveArrow, 0f);
            labelTransform = labelGo.transform;

            var textMesh = labelGo.AddComponent<TextMesh>();
            textMesh.text = creatureLabel;
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.045f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            textMesh.fontStyle = FontStyle.Bold;

            var meshRenderer = labelGo.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                meshRenderer.sortingOrder = 100;
        }

        void BuildVisual(Color color)
        {
            var material = CreateLightedMaterial(color);

            var shaft = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cylinder,
                transform.position,
                new Vector3(0.14f, 0.42f, 0.14f),
                color,
                "Shaft",
                transform);
            shaft.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            ApplyMarkerMaterial(shaft, material);

            var left = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                transform.position,
                new Vector3(0.12f, 0.38f, 0.12f),
                color,
                "HeadLeft",
                transform);
            left.transform.localPosition = new Vector3(-0.12f, 0.16f, 0f);
            left.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
            ApplyMarkerMaterial(left, material);

            var right = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                transform.position,
                new Vector3(0.12f, 0.38f, 0.12f),
                color,
                "HeadRight",
                transform);
            right.transform.localPosition = new Vector3(0.12f, 0.16f, 0f);
            right.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
            ApplyMarkerMaterial(right, material);

            var light = gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5f;
            light.intensity = 2.1f;
            light.color = color;
            light.shadows = LightShadows.None;
        }

        static void ApplyMarkerMaterial(GameObject part, Material material)
        {
            var collider = part.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        static Material CreateLightedMaterial(Color color)
        {
            var material = PrimitiveFactory.CreateColorMaterial(color, 0.85f);
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 2.4f);
            return material;
        }
    }
}
