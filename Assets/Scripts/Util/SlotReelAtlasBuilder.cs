using System.Collections.Generic;
using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Inventory;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class SlotReelAtlasBuilder
    {
        const int GridColumns = 12;
        const int GridRows = 12;
        const int CellSize = 128;
        const string BaseTextureResourcePath = "Textures/SlotMachine/slots_base";

        struct ReelSymbol
        {
            public Texture2D Icon;
            public Color Tint;
            public Color FallbackColor;
            public string Label;
        }

        static readonly char[] SpecialSymbols = { '@', '#', '$', '%', '&', '*', '(', ')' };

        public static Texture2D Build(GameDatabase database)
        {
            var symbols = CollectSymbols(database);
            if (symbols.Count == 0)
                symbols.Add(new ReelSymbol { FallbackColor = Color.white, Label = "?" });

            int width = GridColumns * CellSize;
            int height = GridRows * CellSize;
            var atlas = CreateBaseAtlas(width, height);
            var pixels = atlas.GetPixels();

            int cellCount = GridColumns * GridRows;
            for (int i = 0; i < cellCount; i++)
            {
                var symbol = symbols[i % symbols.Count];
                StampSymbol(pixels, width, i % GridColumns, i / GridColumns, symbol);
            }

            atlas.SetPixels(pixels);
            atlas.Apply(false, false);
            atlas.filterMode = FilterMode.Bilinear;
            atlas.wrapMode = TextureWrapMode.Repeat;
            atlas.anisoLevel = 1;
            return atlas;
        }

        static Texture2D CreateBaseAtlas(int width, int height)
        {
            var source = Resources.Load<Texture2D>(BaseTextureResourcePath);
            if (source != null)
            {
                var readable = CreateReadableCopy(source);
                if (readable != null)
                {
                    var atlas = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    var pixels = new Color[width * height];
                    for (int y = 0; y < height; y++)
                    {
                        float v = height <= 1 ? 0f : y / (float)(height - 1);
                        int srcY = Mathf.Clamp(Mathf.RoundToInt(v * (readable.height - 1)), 0, readable.height - 1);
                        for (int x = 0; x < width; x++)
                        {
                            float u = width <= 1 ? 0f : x / (float)(width - 1);
                            int srcX = Mathf.Clamp(Mathf.RoundToInt(u * (readable.width - 1)), 0, readable.width - 1);
                            pixels[y * width + x] = readable.GetPixel(srcX, srcY);
                        }
                    }

                    atlas.SetPixels(pixels);
                    atlas.Apply(false, false);
                    Object.Destroy(readable);
                    return atlas;
                }
            }

            var fallback = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var fallbackPixels = new Color[width * height];
            var background = new Color(0.14f, 0.11f, 0.18f, 1f);
            for (int i = 0; i < fallbackPixels.Length; i++)
                fallbackPixels[i] = background;
            fallback.SetPixels(fallbackPixels);
            fallback.Apply(false, false);
            return fallback;
        }

        static List<ReelSymbol> CollectSymbols(GameDatabase database)
        {
            var symbols = new List<ReelSymbol>();
            if (database == null)
                return symbols;

            foreach (var item in database.items)
            {
                if (item == null || ShouldExcludeFromReels(item))
                    continue;

                if (!HasReelIcon(item))
                    continue;

                if (item.category == ItemCategory.Weapon
                    || item.category == ItemCategory.Tool
                    || item.category == ItemCategory.Gloves
                    || item.category == ItemCategory.Ore
                    || item.category == ItemCategory.Misc
                    || item.isMonsterDrop)
                {
                    symbols.Add(CreateItemSymbol(item));
                }
            }

            foreach (char symbol in SpecialSymbols)
                symbols.Add(CreateTextSymbol(symbol));

            return symbols;
        }

        static bool ShouldExcludeFromReels(ItemDefinition item)
        {
            return item.itemId == "pentachick_heart"
                || item.itemId == "cave_key"
                || item.isBossDrop
                || item.isSlotTestToken;
        }

        static bool HasReelIcon(ItemDefinition item)
        {
            return ItemIconUtility.GetIcon(item) != null;
        }

        static ReelSymbol CreateItemSymbol(ItemDefinition item)
        {
            return new ReelSymbol
            {
                Icon = ItemIconUtility.GetIcon(item),
                Tint = Color.white,
                FallbackColor = item.worldColor,
                Label = item.displayName
            };
        }

        static ReelSymbol CreateTextSymbol(char symbol)
        {
            return new ReelSymbol
            {
                Icon = BuildGlyphTexture(symbol),
                Tint = Color.white,
                FallbackColor = new Color(0.12f, 0.1f, 0.16f),
                Label = symbol.ToString()
            };
        }

        static void StampSymbol(Color[] pixels, int atlasWidth, int column, int row, ReelSymbol symbol)
        {
            int startX = column * CellSize;
            int startY = row * CellSize;

            if (symbol.Icon != null)
            {
                BlitIconIntoCell(pixels, atlasWidth, startX, startY, symbol.Icon, symbol.Tint);
                return;
            }

            FillEntireCell(pixels, atlasWidth, startX, startY, symbol.FallbackColor);
        }

        static void FillEntireCell(Color[] pixels, int atlasWidth, int startX, int startY, Color color)
        {
            for (int y = 0; y < CellSize; y++)
            {
                for (int x = 0; x < CellSize; x++)
                {
                    int px = startX + x;
                    int py = startY + y;
                    pixels[py * atlasWidth + px] = color;
                }
            }
        }

        static void BlitIconIntoCell(
            Color[] pixels,
            int atlasWidth,
            int startX,
            int startY,
            Texture2D icon,
            Color tint)
        {
            var readable = CreateReadableCopy(icon);
            if (readable == null)
                return;

            const int padding = 6;
            int targetSize = CellSize - padding * 2;
            for (int y = 0; y < targetSize; y++)
            {
                float v = targetSize <= 1 ? 0f : y / (float)(targetSize - 1);
                int srcY = Mathf.Clamp(Mathf.RoundToInt(v * (readable.height - 1)), 0, readable.height - 1);
                for (int x = 0; x < targetSize; x++)
                {
                    float u = targetSize <= 1 ? 0f : x / (float)(targetSize - 1);
                    int srcX = Mathf.Clamp(Mathf.RoundToInt(u * (readable.width - 1)), 0, readable.width - 1);
                    Color sample = readable.GetPixel(srcX, srcY) * tint;
                    int px = startX + padding + x;
                    int py = startY + padding + y;
                    Color background = pixels[py * atlasWidth + px];
                    pixels[py * atlasWidth + px] = sample.a <= 0.01f
                        ? background
                        : Color.Lerp(background, sample, sample.a);
                }
            }
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

        static Texture2D BuildGlyphTexture(char symbol)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            DrawGlyph(pixels, size, symbol, new Color(1f, 0.88f, 0.2f, 1f));
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        static void DrawGlyph(Color[] pixels, int size, char symbol, Color color)
        {
            bool[,] pattern = GetGlyphPattern(symbol);
            if (pattern == null)
                return;

            int glyphWidth = pattern.GetLength(0);
            int glyphHeight = pattern.GetLength(1);
            int offsetX = (size - glyphWidth) / 2;
            int offsetY = (size - glyphHeight) / 2;

            for (int y = 0; y < glyphHeight; y++)
            {
                for (int x = 0; x < glyphWidth; x++)
                {
                    if (!pattern[x, glyphHeight - 1 - y])
                        continue;

                    int px = offsetX + x;
                    int py = offsetY + y;
                    if (px < 0 || py < 0 || px >= size || py >= size)
                        continue;

                    pixels[py * size + px] = color;
                }
            }
        }

        static bool[,] GetGlyphPattern(char symbol)
        {
            switch (symbol)
            {
                case '@': return GlyphAt();
                case '#': return GlyphHash();
                case '$': return GlyphDollar();
                case '%': return GlyphPercent();
                case '&': return GlyphAmp();
                case '*': return GlyphStar();
                case '(': return GlyphParen(true);
                case ')': return GlyphParen(false);
                default: return GlyphStar();
            }
        }

        static bool[,] GlyphAt()
        {
            return Parse(
                "..####..",
                ".#....#.",
                "#.#..#.#",
                "#..##..#",
                "#......#",
                "#..##..#",
                "#.#..#.#",
                ".#....#.",
                "..####..");
        }

        static bool[,] GlyphHash()
        {
            return Parse(
                "..#..#..",
                "..#..#..",
                "########",
                "..#..#..",
                "########",
                "..#..#..",
                "..#..#..");
        }

        static bool[,] GlyphDollar()
        {
            return Parse(
                "...##...",
                "..#..#..",
                ".#....#.",
                ".####...",
                "...##...",
                "....##..",
                ".#....#.",
                "..#..#..",
                "...##...");
        }

        static bool[,] GlyphPercent()
        {
            return Parse(
                "#....#..",
                "#...#...",
                "....#...",
                "...#....",
                "..#.....",
                ".#......",
                "#....#..",
                ".....#..",
                "....#...");
        }

        static bool[,] GlyphAmp()
        {
            return Parse(
                ".####...",
                "#....#..",
                "#....#..",
                ".####...",
                "#..#....",
                "#...#...",
                "#....#..",
                ".###.#..",
                ".....#..");
        }

        static bool[,] GlyphStar()
        {
            return Parse(
                "...#....",
                "...#....",
                "########",
                ".#...#..",
                "..#.#...",
                ".#...#..",
                "#.....#.",
                "........",
                "........");
        }

        static bool[,] GlyphParen(bool open)
        {
            if (open)
            {
                return Parse(
                    "...##...",
                    "..#.....",
                    ".#......",
                    ".#......",
                    ".#......",
                    ".#......",
                    "..#.....",
                    "...##...");
            }

            return Parse(
                "...##...",
                ".....#..",
                "......#.",
                "......#.",
                "......#.",
                "......#.",
                ".....#..",
                "...##...");
        }

        static bool[,] Parse(params string[] rows)
        {
            int height = rows.Length;
            int width = rows[0].Length;
            var pattern = new bool[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    pattern[x, y] = rows[y][x] == '#';
            }

            return pattern;
        }
    }
}
