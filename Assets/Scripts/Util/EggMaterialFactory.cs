using UnityEngine;

namespace MonsterMiner.Util
{
    public static class EggMaterialFactory
    {
        const int TextureSize = 256;
        const float ScalesPerTile = 7f;

        static readonly Color[] EggColors =
        {
            new Color(0.82f, 0.14f, 0.12f),
            new Color(0.16f, 0.38f, 0.86f),
            new Color(0.93f, 0.78f, 0.11f),
            new Color(0.17f, 0.72f, 0.28f)
        };

        public const int GoldColorIndex = 2;

        static Material[] cachedMaterials;

        public static void ApplyRandomDragonScaleMaterial(GameObject root)
        {
            ApplyDragonScaleMaterial(root, Random.Range(0, EggColors.Length));
        }

        public static void ApplyGoldDragonScaleMaterial(GameObject root)
        {
            ApplyDragonScaleMaterial(root, GoldColorIndex);
        }

        public static void ApplyDragonScaleMaterial(GameObject root, int colorIndex)
        {
            if (root == null)
                return;

            var material = GetDragonScaleMaterial(colorIndex);
            if (material == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;
        }

        static Material GetDragonScaleMaterial(int colorIndex)
        {
            EnsureCache();
            if (cachedMaterials == null || colorIndex < 0 || colorIndex >= cachedMaterials.Length)
                return null;

            return cachedMaterials[colorIndex];
        }

        static void EnsureCache()
        {
            if (cachedMaterials != null)
                return;

            cachedMaterials = new Material[EggColors.Length];
            for (int i = 0; i < EggColors.Length; i++)
                cachedMaterials[i] = CreateDragonScaleMaterial(EggColors[i]);
        }

        static Material CreateDragonScaleMaterial(Color baseColor)
        {
            var template = Resources.Load<Material>("Materials/DefaultSurface");
            if (template == null)
            {
                var urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit == null)
                    return PrimitiveFactory.CreateColorMaterial(baseColor, 0.65f);

                template = new Material(urpLit);
            }

            var material = new Material(template);
            var albedo = BakeDragonScaleAlbedo(baseColor);

            if (albedo != null)
            {
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", albedo);
                else if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", albedo);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            else if (material.HasProperty("_Color"))
                material.color = Color.white;

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.68f);
            else if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.68f);

            return material;
        }

        static Texture2D BakeDragonScaleAlbedo(Color baseColor)
        {
            var gapColor = Color.Lerp(baseColor * 0.28f, Color.black, 0.35f);
            var pixels = new Color32[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            {
                float v = y / (float)TextureSize;
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = x / (float)TextureSize;
                    pixels[y * TextureSize + x] = SampleDragonScale(baseColor, gapColor, u, v);
                }
            }

            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        static Color32 SampleDragonScale(Color baseColor, Color gapColor, float u, float v)
        {
            float tu = u * ScalesPerTile;
            float tv = v * ScalesPerTile;

            int row = Mathf.FloorToInt(tv);
            float rowFrac = tv - row;
            tu += (row & 1) == 0 ? 0f : 0.5f;

            float colFrac = tu - Mathf.Floor(tu);

            const float radius = 0.48f;
            float sx = colFrac - 0.5f;
            float sy = rowFrac;

            if (sy < 0f || sx * sx + sy * sy > radius * radius)
                return gapColor;

            float dist = Mathf.Sqrt(sx * sx + sy * sy) / radius;
            float ridge = Mathf.Clamp01(1f - Mathf.Abs(sx) * 2.4f) * Mathf.Clamp01(sy * 1.8f) * 0.22f;
            float shade = Mathf.Lerp(1.08f, 0.7f, dist);
            float rim = Mathf.SmoothStep(0.72f, 1f, dist) * 0.18f;

            Color scale = baseColor * shade;
            scale += Color.white * ridge;
            scale -= Color.black * rim;
            scale.a = 1f;
            return scale;
        }
    }
}
