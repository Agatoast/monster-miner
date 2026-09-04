using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.Artillery
{
    public class ArtilleryRockImpactEffect : MonoBehaviour
    {
        const string SheetResourcePath = "Textures/Artillery/rock_impact";
        const int SheetColumns = 4;
        const int SheetRows = 3;
        const int FrameCount = 11;
        const float FrameDuration = 0.08f;
        const float BackgroundThreshold = 0.93f;
        const float FringeThreshold = 0.965f;

        static Material sharedMaterial;
        static float frameAspect = 186f / 256f;

        MeshRenderer targetRenderer;
        Material instanceMaterial;
        int frameIndex;
        float frameTimer;
        bool playing;

        public static float FrameAspect => frameAspect;

        public static void PlayAt(
            Transform parent,
            float centerX,
            float impactBottomY,
            float targetWidth,
            float depth)
        {
            EnsureAssets();

            float width = Mathf.Clamp(targetWidth * 1.15f, 0.55f, 4f);
            float height = width * frameAspect;

            var root = new GameObject("RockImpact");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(centerX, impactBottomY + height * 0.5f, depth - 0.02f);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "RockImpactSprite";
            quad.transform.SetParent(root.transform, false);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(width, height, 1f);
            Object.Destroy(quad.GetComponent<Collider>());

            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = sharedMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var effect = root.AddComponent<ArtilleryRockImpactEffect>();
            effect.Begin(renderer);
        }

        static void EnsureAssets()
        {
            if (sharedMaterial != null)
                return;

            var source = Resources.Load<Texture2D>(SheetResourcePath);
            if (source == null)
            {
                Debug.LogWarning($"Monster Miner: rock impact sheet not found at Resources/{SheetResourcePath}.");
                sharedMaterial = BuildFallbackMaterial();
                return;
            }

            var texture = BuildProcessedSheet(source);
            frameAspect = (source.height / (float)SheetRows) / (source.width / (float)SheetColumns);
            sharedMaterial = BuildTransparentMaterial(texture);
        }

        static Texture2D BuildProcessedSheet(Texture2D source)
        {
            int width = source.width;
            int height = source.height;
            var pixels = source.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = ProcessImpactPixel(pixels[i]);

            RemoveIsolatedFringePixels(pixels, width, height);

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static Color ProcessImpactPixel(Color color)
        {
            if (color.a <= 0.01f)
                return Color.clear;

            float maxChannel = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float minChannel = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            float lum = (color.r + color.g + color.b) / 3f;

            if (maxChannel >= BackgroundThreshold && minChannel >= BackgroundThreshold - 0.02f)
                return Color.clear;

            if (lum >= FringeThreshold)
            {
                float alpha = Mathf.Clamp01((FringeThreshold - lum) / 0.035f);
                if (alpha <= 0.01f)
                    return Color.clear;

                color.a = alpha;
                return color;
            }

            color.a = 1f;
            return color;
        }

        static void RemoveIsolatedFringePixels(Color[] pixels, int width, int height)
        {
            var scratch = new Color[pixels.Length];
            System.Array.Copy(pixels, scratch, pixels.Length);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    var color = scratch[index];
                    if (color.a <= 0.01f)
                        continue;

                    float lum = (color.r + color.g + color.b) / 3f;
                    if (lum < 0.88f)
                        continue;

                    if (HasOpaqueNeighbor(scratch, width, height, x, y, minLuminance: 0.72f))
                        continue;

                    pixels[index] = Color.clear;
                }
            }
        }

        static bool HasOpaqueNeighbor(Color[] pixels, int width, int height, int x, int y, float minLuminance)
        {
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0)
                        continue;

                    int nx = x + ox;
                    int ny = y + oy;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;

                    var neighbor = pixels[ny * width + nx];
                    if (neighbor.a <= 0.01f)
                        continue;

                    float lum = (neighbor.r + neighbor.g + neighbor.b) / 3f;
                    if (lum <= minLuminance)
                        return true;
                }
            }

            return false;
        }

        static Material BuildTransparentMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            var material = new Material(shader);
            material.renderQueue = (int)RenderQueue.Transparent;
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend"))
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            return material;
        }

        static Material BuildFallbackMaterial()
        {
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = i == 27 ? new Color(0.4f, 0.4f, 0.4f, 1f) : Color.clear;
            texture.SetPixels(pixels);
            texture.Apply();
            frameAspect = 1f;
            return BuildTransparentMaterial(texture);
        }

        void Begin(MeshRenderer renderer)
        {
            targetRenderer = renderer;
            instanceMaterial = targetRenderer.material;
            frameIndex = 0;
            frameTimer = 0f;
            playing = true;
            ApplyFrame();
        }

        void Update()
        {
            if (!playing || instanceMaterial == null)
                return;

            frameTimer += Time.deltaTime;
            if (frameTimer < FrameDuration)
                return;

            frameTimer = 0f;
            frameIndex++;
            if (frameIndex >= FrameCount)
            {
                playing = false;
                Destroy(gameObject);
                return;
            }

            ApplyFrame();
        }

        void ApplyFrame()
        {
            int column = frameIndex % SheetColumns;
            int row = frameIndex / SheetColumns;
            var scale = new Vector2(1f / SheetColumns, 1f / SheetRows);
            var offset = new Vector2(
                column / (float)SheetColumns,
                (SheetRows - 1 - row) / (float)SheetRows);

            if (instanceMaterial.HasProperty("_BaseMap"))
            {
                instanceMaterial.SetTextureScale("_BaseMap", scale);
                instanceMaterial.SetTextureOffset("_BaseMap", offset);
            }

            if (instanceMaterial.HasProperty("_MainTex"))
            {
                instanceMaterial.SetTextureScale("_MainTex", scale);
                instanceMaterial.SetTextureOffset("_MainTex", offset);
            }
        }

        void OnDestroy()
        {
            if (instanceMaterial != null)
                Destroy(instanceMaterial);
        }
    }
}
