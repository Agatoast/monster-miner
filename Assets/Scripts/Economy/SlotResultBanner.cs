using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.Economy
{
    public class SlotResultBanner : MonoBehaviour
    {
        const float SurfaceOffset = 0.012f;
        const int BannerTexWidth = 512;
        const int BannerTexHeight = 96;
        const int MaxBakeFontSize = 72;
        const int MinBakeFontSize = 18;

        static readonly Color GoldColor = new Color(0.92f, 0.74f, 0.18f, 1f);
        static readonly Color TextColor = Color.black;

        static Font bakeFont;

        GameObject overlayObject;
        MeshRenderer overlayRenderer;
        Material overlayMaterial;
        Texture2D bannerTexture;

        Vector3 worldCenter;
        Vector3 planeUp;
        Vector3 planeNormal;
        float worldWidth;
        float worldHeight;

        public void Initialize(GameObject slotMachineRoot)
        {
            if (slotMachineRoot == null)
                return;

            CleanupOverlay();

            if (!TryGetReelPlane(slotMachineRoot, out worldCenter, out planeUp, out planeNormal, out worldWidth, out worldHeight))
                return;

            overlayMaterial = CreateOverlayMaterial();
            overlayObject = CreateOverlayObject(slotMachineRoot.transform);
            overlayRenderer = overlayObject.GetComponent<MeshRenderer>();
            overlayRenderer.sharedMaterial = overlayMaterial;
            PositionOverlay();
            Hide();
        }

        public void Show(string message)
        {
            if (overlayObject == null || overlayMaterial == null)
                return;

            bannerTexture = BakeBannerTexture(message ?? string.Empty, bannerTexture);
            overlayMaterial.mainTexture = bannerTexture;
            if (overlayMaterial.HasProperty("_BaseMap"))
                overlayMaterial.SetTexture("_BaseMap", bannerTexture);

            overlayObject.SetActive(true);
        }

        public void Hide()
        {
            if (overlayObject != null)
                overlayObject.SetActive(false);
        }

        void OnDestroy()
        {
            CleanupOverlay();
            if (bannerTexture != null)
                Destroy(bannerTexture);
        }

        void CleanupOverlay()
        {
            if (overlayObject != null)
                Destroy(overlayObject);

            overlayObject = null;
            overlayRenderer = null;

            if (overlayMaterial != null)
                Destroy(overlayMaterial);

            overlayMaterial = null;
        }

        GameObject CreateOverlayObject(Transform parent)
        {
            var overlay = new GameObject("SlotResultBannerOverlay");
            overlay.transform.SetParent(parent, true);

            var meshFilter = overlay.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateUnitQuadMesh();

            var renderer = overlay.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return overlay;
        }

        void PositionOverlay()
        {
            if (overlayObject == null)
                return;

            overlayObject.transform.position = worldCenter + planeNormal * SurfaceOffset;
            overlayObject.transform.rotation = Quaternion.LookRotation(planeNormal, planeUp);

            Transform parent = overlayObject.transform.parent;
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            overlayObject.transform.localScale = new Vector3(
                worldWidth / Mathf.Max(0.0001f, parentScale.x),
                worldHeight / Mathf.Max(0.0001f, parentScale.y),
                1f / Mathf.Max(0.0001f, parentScale.z));
        }

        static Mesh CreateUnitQuadMesh()
        {
            var mesh = new Mesh { name = "SlotResultBannerQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static bool TryGetReelPlane(
            GameObject slotMachineRoot,
            out Vector3 center,
            out Vector3 up,
            out Vector3 normal,
            out float width,
            out float height)
        {
            center = default;
            up = Vector3.up;
            normal = Vector3.forward;
            width = 0f;
            height = 0f;

            if (slotMachineRoot == null)
                return false;

            var spinRenderers = CollectSpinRenderers(slotMachineRoot);
            if (spinRenderers.Count == 0)
                return false;

            Transform face = slotMachineRoot.transform;
            normal = face.forward.sqrMagnitude > 0.0001f ? face.forward.normalized : Vector3.forward;
            up = face.up.sqrMagnitude > 0.0001f ? face.up.normalized : Vector3.up;
            Vector3 right = face.right.sqrMagnitude > 0.0001f ? face.right.normalized : Vector3.right;

            Bounds reelBounds = spinRenderers[0].bounds;
            for (int i = 1; i < spinRenderers.Count; i++)
                reelBounds.Encapsulate(spinRenderers[i].bounds);

            width = MeasureBoundsExtent(reelBounds, right);
            float fullHeight = MeasureBoundsExtent(reelBounds, up);
            height = fullHeight / 3f;
            center = reelBounds.center;

            return width > 0.001f && height > 0.001f;
        }

        static float MeasureBoundsExtent(Bounds bounds, Vector3 axis)
        {
            Vector3 extent = bounds.extents;
            Vector3[] corners =
            {
                bounds.center + new Vector3( extent.x,  extent.y,  extent.z),
                bounds.center + new Vector3( extent.x,  extent.y, -extent.z),
                bounds.center + new Vector3( extent.x, -extent.y,  extent.z),
                bounds.center + new Vector3( extent.x, -extent.y, -extent.z),
                bounds.center + new Vector3(-extent.x,  extent.y,  extent.z),
                bounds.center + new Vector3(-extent.x,  extent.y, -extent.z),
                bounds.center + new Vector3(-extent.x, -extent.y,  extent.z),
                bounds.center + new Vector3(-extent.x, -extent.y, -extent.z),
            };

            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < corners.Length; i++)
            {
                float projection = Vector3.Dot(corners[i], axis);
                min = Mathf.Min(min, projection);
                max = Mathf.Max(max, projection);
            }

            return max - min;
        }

        static List<Renderer> CollectSpinRenderers(GameObject slotMachineRoot)
        {
            var spinRenderers = new List<Renderer>();
            foreach (var renderer in slotMachineRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && renderer.name.Contains("Spin"))
                    spinRenderers.Add(renderer);
            }

            return spinRenderers;
        }

        static Texture2D BakeBannerTexture(string message, Texture2D existingTexture)
        {
            var texture = existingTexture;
            if (texture == null || texture.width != BannerTexWidth || texture.height != BannerTexHeight)
            {
                if (texture != null)
                    Destroy(texture);

                texture = new Texture2D(BannerTexWidth, BannerTexHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            FillGold(texture);

            if (!string.IsNullOrEmpty(message))
            {
                int fontSize = ChooseFontSize(message);
                int baselineY = ComputeBaselineY(message, fontSize);
                int startX = ComputeStartX(message, fontSize);
                DrawText(message, texture, fontSize, startX, baselineY);
            }

            FinalizeBannerTexture(texture);
            FlipTextureHorizontally(texture);
            texture.Apply(false, false);
            return texture;
        }

        static Font GetBakeFont()
        {
            if (bakeFont != null)
                return bakeFont;

            bakeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            return bakeFont;
        }

        static int ChooseFontSize(string message)
        {
            int maxWidth = Mathf.RoundToInt(BannerTexWidth * 0.88f);
            int maxHeight = Mathf.RoundToInt(BannerTexHeight * 0.72f);

            for (int fontSize = MaxBakeFontSize; fontSize >= MinBakeFontSize; fontSize--)
            {
                if (MeasureTextWidth(message, fontSize) <= maxWidth
                    && MeasureTextHeight(message, fontSize) <= maxHeight)
                    return fontSize;
            }

            return MinBakeFontSize;
        }

        static int MeasureTextWidth(string message, int fontSize)
        {
            var font = GetBakeFont();
            if (font == null)
                return 0;

            font.RequestCharactersInTexture(message, fontSize, FontStyle.Bold);
            int width = 0;
            foreach (char character in message)
            {
                if (font.GetCharacterInfo(character, out var info, fontSize, FontStyle.Bold))
                    width += info.advance;
            }

            return width;
        }

        static int MeasureTextHeight(string message, int fontSize)
        {
            var font = GetBakeFont();
            if (font == null)
                return 0;

            font.RequestCharactersInTexture(message, fontSize, FontStyle.Bold);
            int minY = 0;
            int maxY = 0;
            bool hasGlyph = false;

            foreach (char character in message)
            {
                if (!font.GetCharacterInfo(character, out var info, fontSize, FontStyle.Bold))
                    continue;

                if (!hasGlyph)
                {
                    minY = info.minY;
                    maxY = info.maxY;
                    hasGlyph = true;
                    continue;
                }

                minY = Mathf.Min(minY, info.minY);
                maxY = Mathf.Max(maxY, info.maxY);
            }

            return hasGlyph ? maxY - minY : fontSize;
        }

        static int ComputeBaselineY(string message, int fontSize)
        {
            var font = GetBakeFont();
            if (font == null)
                return BannerTexHeight / 2;

            font.RequestCharactersInTexture(message, fontSize, FontStyle.Bold);
            int minY = 0;
            int maxY = 0;
            bool hasGlyph = false;

            foreach (char character in message)
            {
                if (!font.GetCharacterInfo(character, out var info, fontSize, FontStyle.Bold))
                    continue;

                if (!hasGlyph)
                {
                    minY = info.minY;
                    maxY = info.maxY;
                    hasGlyph = true;
                    continue;
                }

                minY = Mathf.Min(minY, info.minY);
                maxY = Mathf.Max(maxY, info.maxY);
            }

            int textHeight = hasGlyph ? maxY - minY : fontSize;
            return (BannerTexHeight - textHeight) / 2 - minY;
        }

        static int ComputeStartX(string message, int fontSize)
        {
            int textWidth = MeasureTextWidth(message, fontSize);
            return (BannerTexWidth - textWidth) / 2;
        }

        static void DrawText(string message, Texture2D target, int fontSize, int startX, int baselineY)
        {
            var font = GetBakeFont();
            if (font == null || font.material == null || font.material.mainTexture == null)
                return;

            var fontTexture = font.material.mainTexture as Texture2D;
            if (fontTexture == null)
                return;

            Texture2D readableFontTexture = CreateReadableCopy(fontTexture);
            if (readableFontTexture == null)
                return;

            font.RequestCharactersInTexture(message, fontSize, FontStyle.Bold);
            int penX = startX;
            foreach (char character in message)
            {
                if (font.GetCharacterInfo(character, out var info, fontSize, FontStyle.Bold))
                {
                    DrawGlyph(target, readableFontTexture, info, penX, baselineY, TextColor);
                    penX += info.advance;
                }
            }

            Destroy(readableFontTexture);
        }

        static void DrawGlyph(
            Texture2D target,
            Texture2D fontTexture,
            CharacterInfo glyph,
            int penX,
            int penY,
            Color ink)
        {
            int width = glyph.glyphWidth;
            int height = glyph.glyphHeight;
            if (width <= 0 || height <= 0)
                return;

            int destX = penX + glyph.minX;
            int destY = penY + glyph.minY;

            for (int y = 0; y < height; y++)
            {
                float v = height <= 1 ? 0.5f : (float)y / (height - 1);
                for (int x = 0; x < width; x++)
                {
                    float u = width <= 1 ? 0.5f : (float)x / (width - 1);
                    if (glyph.flipped)
                        u = 1f - u;

                    Vector2 uvBottom = Vector2.Lerp(glyph.uvBottomLeft, glyph.uvBottomRight, u);
                    Vector2 uvTop = Vector2.Lerp(glyph.uvTopLeft, glyph.uvTopRight, u);
                    Vector2 uv = Vector2.Lerp(uvBottom, uvTop, v);

                    int srcX = Mathf.Clamp(Mathf.RoundToInt(uv.x * fontTexture.width), 0, fontTexture.width - 1);
                    int srcY = Mathf.Clamp(Mathf.RoundToInt(uv.y * fontTexture.height), 0, fontTexture.height - 1);

                    Color sample = fontTexture.GetPixel(srcX, srcY);
                    if (sample.a < 0.05f)
                        continue;

                    int pixelX = destX + x;
                    int pixelY = destY + y;
                    if (pixelX < 0 || pixelX >= target.width || pixelY < 0 || pixelY >= target.height)
                        continue;

                    target.SetPixel(pixelX, pixelY, ink);
                }
            }
        }

        static void FlipTextureHorizontally(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;
            var pixels = texture.GetPixels();
            var flipped = new Color[pixels.Length];

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                    flipped[row + x] = pixels[row + (width - 1 - x)];
            }

            texture.SetPixels(flipped);
        }

        static Texture2D CreateReadableCopy(Texture2D source)
        {
            if (source == null)
                return null;

            var renderTarget = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTarget);
            var previous = RenderTexture.active;
            RenderTexture.active = renderTarget;

            var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
            readable.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTarget);
            return readable;
        }

        static void FillGold(Texture2D texture)
        {
            var pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = GoldColor;

            texture.SetPixels(pixels);
        }

        static void FinalizeBannerTexture(Texture2D texture)
        {
            var pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color sample = pixels[i];
                float luminance = sample.r * 0.299f + sample.g * 0.587f + sample.b * 0.114f;
                if (luminance < 0.42f && sample.a > 0.2f)
                    pixels[i] = new Color(0f, 0f, 0f, 1f);
                else
                    pixels[i] = GoldColor;
            }

            texture.SetPixels(pixels);
        }

        static Material CreateOverlayMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);

            material.color = Color.white;
            material.renderQueue = 3101;
            material.SetInt("_Cull", (int)CullMode.Off);

            if (material.HasProperty("_ZWrite"))
                material.SetInt("_ZWrite", 1);

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 0f);

            return material;
        }
    }
}
