using MonsterMiner.Data;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class ItemIconUtility
    {
        public static Texture2D GetIcon(ItemDefinition item)
        {
            if (item == null || string.IsNullOrEmpty(item.iconResourcePath))
                return null;

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
            var source = Resources.Load<Texture2D>(resourcePath);
            if (source == null)
                return null;

            var readable = CreateReadableCopy(source);
            if (readable == null)
                return source;

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
            return readable;
        }

        public static bool TryDrawIcon(Rect rect, ItemDefinition item)
        {
            var icon = GetIcon(item);
            if (icon == null)
                return false;

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
