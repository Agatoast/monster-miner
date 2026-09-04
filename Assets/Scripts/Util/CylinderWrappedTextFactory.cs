using UnityEngine;

namespace MonsterMiner.Util
{
    public static class CylinderWrappedTextFactory
    {
        public static void AttachUpsideDown(
            Transform parent,
            string text,
            Bounds localBounds,
            float facingAngleRadians = 0f,
            float heightFraction = 0.55f,
            float surfacePadding = 1.03f)
        {
            if (parent == null || string.IsNullOrEmpty(text))
                return;

            float radius = Mathf.Max(localBounds.extents.x, localBounds.extents.z) * surfacePadding;
            if (radius <= 0.01f)
                return;

            float labelY = Mathf.Lerp(
                localBounds.min.y,
                localBounds.max.y,
                Mathf.Clamp01(heightFraction));

            float characterSize = radius * 0.085f;
            float angularCharWidth = characterSize / radius;

            float totalAngularWidth = 0f;
            for (int i = 0; i < text.Length; i++)
                totalAngularWidth += GetCharacterAngularWidth(text[i], angularCharWidth);

            var labelRoot = new GameObject("CylinderLabel");
            labelRoot.transform.SetParent(parent, false);
            labelRoot.transform.localPosition = Vector3.zero;
            labelRoot.transform.localRotation = Quaternion.identity;
            labelRoot.transform.localScale = Vector3.one;

            float cursorAngle = facingAngleRadians + totalAngularWidth * 0.5f;
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                float charAngularWidth = GetCharacterAngularWidth(character, angularCharWidth);
                cursorAngle -= charAngularWidth * 0.5f;

                if (character != ' ')
                    CreateCharacter(labelRoot.transform, character, radius, labelY, cursorAngle, characterSize);

                cursorAngle -= charAngularWidth * 0.5f;
            }
        }

        public static Bounds GetLocalRendererBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one);

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                worldBounds.Encapsulate(renderers[i].bounds);

            Vector3 localCenter = root.transform.InverseTransformPoint(worldBounds.center);
            Vector3 worldSize = worldBounds.size;
            Vector3 localSize = root.transform.InverseTransformVector(worldSize);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            return new Bounds(localCenter, localSize);
        }

        static float GetCharacterAngularWidth(char character, float angularCharWidth) =>
            character == ' ' ? angularCharWidth * 0.45f : angularCharWidth;

        static void CreateCharacter(
            Transform parent,
            char character,
            float radius,
            float labelY,
            float angleRadians,
            float characterSize)
        {
            float x = Mathf.Sin(angleRadians) * radius;
            float z = Mathf.Cos(angleRadians) * radius;
            var outward = new Vector3(x, 0f, z).normalized;

            var charGo = new GameObject($"Label_{character}");
            charGo.transform.SetParent(parent, false);
            charGo.transform.localPosition = new Vector3(x, labelY, z);
            charGo.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up) * Quaternion.Euler(0f, 0f, 180f);

            var textMesh = charGo.AddComponent<TextMesh>();
            textMesh.text = character.ToString();
            textMesh.fontSize = 96;
            textMesh.characterSize = characterSize;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color(0.92f, 0.94f, 0.98f, 1f);
            textMesh.fontStyle = FontStyle.Bold;

            ApplyUnlitFontMaterial(charGo.GetComponent<MeshRenderer>());
        }

        static void ApplyUnlitFontMaterial(MeshRenderer renderer)
        {
            if (renderer == null)
                return;

            var source = renderer.material;
            var shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Transparent");
            if (shader == null)
                return;

            var material = new Material(shader);
            if (source.mainTexture != null)
            {
                material.mainTexture = source.mainTexture;
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", source.mainTexture);
            }

            material.color = source.color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", source.color);

            renderer.material = material;
        }
    }
}
