using System.Collections.Generic;
using MonsterMiner.Data;
using MonsterMiner.Inventory;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class ItemIconUtility
    {
        static readonly Dictionary<string, Texture2D> processedIconCache = new Dictionary<string, Texture2D>();

        public static Texture2D GetIcon(ItemDefinition item)
        {
            if (item == null)
                return null;

            if (InventorySystem.IsMonsterMeat(item))
            {
                var meatIcon = MeatVisualFactory.GetDropTileIcon(item);
                if (meatIcon != null)
                    return meatIcon;
            }

            if (string.IsNullOrEmpty(item.iconResourcePath))
                return null;

            if (InventorySystem.IsGrenadeItem(item))
            {
                return LoadIconWithTransparentBackground(
                    item.iconResourcePath,
                    IconBackgroundKeyMode.Black);
            }

            return Resources.Load<Texture2D>(item.iconResourcePath);
        }

        public enum IconBackgroundKeyMode
        {
            Black,
            White,
            BlackAndWhite
        }

        public static Texture2D LoadIconWithTransparentBackground(
            string resourcePath,
            IconBackgroundKeyMode keyMode = IconBackgroundKeyMode.Black,
            float darkThreshold = 0.12f,
            float whiteThreshold = 0.95f)
        {
            string cacheKey = $"{resourcePath}|{(int)keyMode}|{darkThreshold:F3}|{whiteThreshold:F3}";
            if (processedIconCache.TryGetValue(cacheKey, out var cached) && cached != null)
                return cached;

            var source = Resources.Load<Texture2D>(resourcePath);
            if (source == null)
                return null;

            var readable = CreateReadableCopy(source);
            if (readable == null)
            {
                processedIconCache[cacheKey] = source;
                return source;
            }

            var pixels = readable.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                float luminance = pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f;
                bool isDark = luminance <= darkThreshold;
                bool isWhite = pixel.r >= whiteThreshold && pixel.g >= whiteThreshold && pixel.b >= whiteThreshold;
                bool shouldKey = keyMode switch
                {
                    IconBackgroundKeyMode.Black => isDark,
                    IconBackgroundKeyMode.White => isWhite,
                    IconBackgroundKeyMode.BlackAndWhite => isDark || isWhite,
                    _ => isDark
                };

                if (shouldKey)
                    pixels[i] = new Color(0f, 0f, 0f, 0f);
            }

            readable.SetPixels(pixels);
            readable.Apply(false, false);
            processedIconCache[cacheKey] = readable;
            return readable;
        }

        public static Texture2D LoadMiningGloveIcon(
            string resourcePath = "Textures/Inventory/Glove",
            float darkThreshold = 0.12f)
        {
            string cacheKey = $"{resourcePath}|mining_glove_opaque|{darkThreshold:F3}";
            if (processedIconCache.TryGetValue(cacheKey, out var cached) && cached != null)
                return cached;

            var source = Resources.Load<Texture2D>(resourcePath);
            if (source == null)
                return null;

            var readable = CreateReadableCopy(source);
            if (readable == null)
            {
                processedIconCache[cacheKey] = source;
                return source;
            }

            int width = readable.width;
            int height = readable.height;
            var pixels = readable.GetPixels();
            var gloveMask = BuildDilatedGloveMask(pixels, width, height, darkThreshold, dilateRadius: 2);
            Color fillColor = ComputeAverageGloveColor(pixels, gloveMask, darkThreshold);

            for (int i = 0; i < pixels.Length; i++)
            {
                if (!gloveMask[i])
                {
                    pixels[i] = new Color(0f, 0f, 0f, 0f);
                    continue;
                }

                Color pixel = pixels[i];
                if (pixel.a < 0.999f || GetLuminance(pixel) <= darkThreshold)
                    pixels[i] = new Color(fillColor.r, fillColor.g, fillColor.b, 1f);
                else
                    pixels[i] = new Color(pixel.r, pixel.g, pixel.b, 1f);
            }

            readable.SetPixels(pixels);
            readable.Apply(false, false);
            processedIconCache[cacheKey] = readable;
            return readable;
        }

        static bool[] BuildDilatedGloveMask(Color[] pixels, int width, int height, float darkThreshold, int dilateRadius)
        {
            int length = pixels.Length;
            var seedMask = new bool[length];
            for (int i = 0; i < length; i++)
            {
                Color pixel = pixels[i];
                seedMask[i] = pixel.a > 0.01f && GetLuminance(pixel) > darkThreshold;
            }

            var dilatedMask = new bool[length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (!HasGloveSeedNear(pixels, seedMask, width, height, x, y, darkThreshold, dilateRadius))
                        continue;

                    dilatedMask[index] = true;
                }
            }

            return dilatedMask;
        }

        static bool HasGloveSeedNear(
            Color[] pixels,
            bool[] seedMask,
            int width,
            int height,
            int centerX,
            int centerY,
            float darkThreshold,
            int radius)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (x < 0 || y < 0 || x >= width || y >= height)
                        continue;

                    int index = y * width + x;
                    if (seedMask[index])
                        return true;

                    Color pixel = pixels[index];
                    if (pixel.a > 0.01f && GetLuminance(pixel) > darkThreshold * 0.65f)
                        return true;
                }
            }

            return false;
        }

        static Color ComputeAverageGloveColor(Color[] pixels, bool[] gloveMask, float darkThreshold)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (!gloveMask[i])
                    continue;

                Color pixel = pixels[i];
                if (pixel.a <= 0.01f || GetLuminance(pixel) <= darkThreshold)
                    continue;

                sum += new Vector3(pixel.r, pixel.g, pixel.b);
                count++;
            }

            if (count <= 0)
                return new Color(0.55f, 0.36f, 0.24f, 1f);

            sum /= count;
            return new Color(sum.x, sum.y, sum.z, 1f);
        }

        static float GetLuminance(Color color)
        {
            return color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
        }

        public static bool TryDrawIcon(Rect rect, ItemDefinition item)
        {
            var icon = GetIcon(item);
            if (icon == null)
                return false;

            if (InventorySystem.IsGrenadeItem(item))
            {
                GUI.color = Color.white;
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
            return true;
        }

        static Texture2D CreateReadableCopy(Texture2D source)
        {
            if (source == null)
                return null;

            var renderTexture = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTexture);
            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;

            var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
            readable.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            return readable;
        }
    }
}
