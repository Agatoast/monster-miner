using System;
using UnityEngine;

namespace MonsterMiner.Artillery
{
    public class ArtilleryCatapultAnimator : MonoBehaviour
    {
        const float FirstRowFrameDuration = 0.05f;
        const float SecondRowFrameDuration = 0.1f;
        const int ReleaseFrameIndex = 8;

        MeshRenderer targetRenderer;
        Material material;
        Texture2D[] frames;
        int frameIndex;
        float frameTimer;
        int frameCount;
        int sheetColumns;
        bool playing;
        Action onReleaseFrame;
        Action onComplete;

        public bool IsPlaying => playing;
        public int NormalizedFrameWidth { get; private set; }
        public int NormalizedFrameHeight { get; private set; }

        public void Configure(MeshRenderer renderer, Texture2D sheetTexture, int columns, int rows, bool flipHorizontal = false)
        {
            targetRenderer = renderer;
            sheetColumns = Mathf.Max(1, columns);
            int sheetRows = Mathf.Max(1, rows);
            frameCount = sheetColumns * sheetRows;

            int sourceWidth = sheetTexture.width;
            int sourceHeight = sheetTexture.height;
            int cellWidth = sourceWidth / sheetColumns;
            int cellHeight = sourceHeight / sheetRows;
            var sourcePixels = sheetTexture.GetPixels();

            var crops = new CroppedFrame[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                int column = i % sheetColumns;
                int row = i / sheetColumns;
                var crop = ExtractMarkedFrame(
                    sourcePixels,
                    sourceWidth,
                    cellWidth,
                    cellHeight,
                    column,
                    row,
                    sheetRows);
                crops[i] = flipHorizontal ? FlipCroppedFrame(crop) : crop;
            }

            frames = BuildAlignedFrames(crops, out int canvasWidth, out int canvasHeight);
            NormalizedFrameWidth = canvasWidth;
            NormalizedFrameHeight = canvasHeight;

            material = targetRenderer != null ? targetRenderer.material : null;
            frameIndex = 0;
            frameTimer = 0f;
            playing = false;
            ApplyFrame();
        }

        readonly struct CroppedFrame
        {
            public readonly Color[] Pixels;
            public readonly int Width;
            public readonly int Height;
            public readonly int GreenX;
            public readonly int GreenY;

            public CroppedFrame(Color[] pixels, int width, int height, int greenX, int greenY)
            {
                Pixels = pixels;
                Width = width;
                Height = height;
                GreenX = greenX;
                GreenY = greenY;
            }
        }

        static CroppedFrame ExtractMarkedFrame(
            Color[] sourcePixels,
            int sourceWidth,
            int cellWidth,
            int cellHeight,
            int column,
            int row,
            int sheetRows)
        {
            int cellX0 = column * cellWidth;
            int cellY0 = (sheetRows - 1 - row) * cellHeight;
            int markerBandHeight = Mathf.Max(8, cellHeight / 5);
            var columnCounts = BuildColumnCounts(sourcePixels, sourceWidth, cellX0, cellY0, cellWidth, cellHeight);
            var bottomBandCounts = BuildColumnCounts(
                sourcePixels,
                sourceWidth,
                cellX0,
                cellY0,
                cellWidth,
                markerBandHeight);

            FindMarkersInBand(
                sourcePixels,
                sourceWidth,
                cellX0,
                cellY0,
                cellWidth,
                markerBandHeight,
                out int greenXInCell,
                out int greenYInCell,
                out int cutColumnX,
                out bool foundGreen,
                out bool foundCutColumn);

            // Red dot marks the vertical cut for this sprite: keep columns [0, cutColumnX].
            // The cut goes one pixel right of the red dot so the next sheet column is never included.
            int cropWidth = foundCutColumn
                ? cutColumnX + 1
                : FindContentRightEdge(columnCounts, cellWidth) + 1;
            cropWidth = Mathf.Clamp(cropWidth, 1, cellWidth);

            if (!foundGreen)
            {
                greenXInCell = FindFallbackAnchorX(bottomBandCounts, cellWidth, markerBandHeight);
                greenYInCell = 0;
            }

            int cropHeight = cellHeight;
            var pixels = new Color[cropWidth * cropHeight];

            for (int y = 0; y < cropHeight; y++)
            {
                for (int x = 0; x < cropWidth; x++)
                {
                    int srcX = cellX0 + x;
                    int srcY = cellY0 + y;
                    var color = sourcePixels[srcY * sourceWidth + srcX];
                    if (IsMarkerPixel(color))
                        color = Color.clear;
                    pixels[y * cropWidth + x] = color;
                }
            }

            greenXInCell = Mathf.Clamp(greenXInCell, 0, cropWidth - 1);
            return new CroppedFrame(pixels, cropWidth, cropHeight, greenXInCell, greenYInCell);
        }

        static CroppedFrame FlipCroppedFrame(CroppedFrame frame)
        {
            var flippedPixels = new Color[frame.Pixels.Length];
            for (int y = 0; y < frame.Height; y++)
            {
                int rowStart = y * frame.Width;
                for (int x = 0; x < frame.Width; x++)
                    flippedPixels[rowStart + frame.Width - 1 - x] = frame.Pixels[rowStart + x];
            }

            int flippedGreenX = frame.Width - 1 - frame.GreenX;
            return new CroppedFrame(flippedPixels, frame.Width, frame.Height, flippedGreenX, frame.GreenY);
        }

        static int[] BuildColumnCounts(
            Color[] sourcePixels,
            int sourceWidth,
            int cellX0,
            int cellY0,
            int cellWidth,
            int cellHeight)
        {
            var counts = new int[cellWidth];
            for (int y = 0; y < cellHeight; y++)
            {
                for (int x = 0; x < cellWidth; x++)
                {
                    if (IsOpaquePixel(sourcePixels[(cellY0 + y) * sourceWidth + cellX0 + x]))
                        counts[x]++;
                }
            }

            return counts;
        }

        static void FindMarkersInBand(
            Color[] sourcePixels,
            int sourceWidth,
            int cellX0,
            int cellY0,
            int cellWidth,
            int markerBandHeight,
            out int greenXInCell,
            out int greenYInCell,
            out int cutColumnX,
            out bool foundGreen,
            out bool foundCutColumn)
        {
            greenXInCell = cellWidth / 2;
            greenYInCell = 0;
            cutColumnX = -1;
            foundGreen = false;
            foundCutColumn = false;

            for (int y = 0; y < markerBandHeight; y++)
            {
                for (int x = 0; x < cellWidth; x++)
                {
                    var color = sourcePixels[(cellY0 + y) * sourceWidth + cellX0 + x];
                    if (IsGreenMarkerPixel(color))
                    {
                        foundGreen = true;
                        greenXInCell = x;
                        greenYInCell = y;
                    }
                    else if (IsRedMarkerPixel(color))
                    {
                        if (foundGreen && x <= greenXInCell)
                            continue;

                        // Red dot marks the vertical cut column on the right edge of this sprite.
                        if (!foundCutColumn || x > cutColumnX)
                        {
                            foundCutColumn = true;
                            cutColumnX = x;
                        }
                    }
                }
            }
        }

        static int FindContentRightEdge(int[] columnCounts, int cellWidth)
        {
            const int gapMin = 8;
            const int gapThreshold = 2;
            const int bodyThreshold = 4;
            const int rightMargin = 8;

            int x = cellWidth - 1;
            while (x >= 0)
            {
                if (columnCounts[x] < bodyThreshold)
                {
                    x--;
                    continue;
                }

                int clusterEnd = x;
                while (x >= 0 && columnCounts[x] >= bodyThreshold)
                    x--;

                int clusterStart = x + 1;
                int gap = 0;
                int gapX = x;
                while (gapX >= 0 && gap < gapMin)
                {
                    if (columnCounts[gapX] <= gapThreshold)
                    {
                        gap++;
                        gapX--;
                    }
                    else
                    {
                        break;
                    }
                }

                if (gap >= gapMin && clusterStart >= cellWidth - rightMargin)
                {
                    x = gapX;
                    continue;
                }

                return clusterEnd;
            }

            return cellWidth - 1;
        }

        static int FindFallbackAnchorX(int[] columnCounts, int cellWidth, int markerBandHeight)
        {
            int bandThreshold = Mathf.Max(3, markerBandHeight / 2);
            for (int x = 0; x < cellWidth; x++)
            {
                if (columnCounts[x] >= bandThreshold)
                    return x;
            }

            for (int x = 0; x < cellWidth; x++)
            {
                if (columnCounts[x] > 0)
                    return x;
            }

            return cellWidth / 4;
        }

        static bool IsOpaquePixel(Color color)
        {
            if (color.a <= 0.08f)
                return false;

            return (color.r + color.g + color.b) > 0.1f;
        }

        static Texture2D[] BuildAlignedFrames(CroppedFrame[] crops, out int canvasWidth, out int canvasHeight)
        {
            int anchorX = crops[0].GreenX;
            int anchorY = crops[0].GreenY;

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            for (int i = 0; i < crops.Length; i++)
            {
                int offsetX = anchorX - crops[i].GreenX;
                int offsetY = anchorY - crops[i].GreenY;
                minX = Mathf.Min(minX, offsetX);
                minY = Mathf.Min(minY, offsetY);
                maxX = Mathf.Max(maxX, offsetX + crops[i].Width);
                maxY = Mathf.Max(maxY, offsetY + crops[i].Height);
            }

            canvasWidth = Mathf.Max(1, maxX - minX);
            canvasHeight = Mathf.Max(1, maxY - minY);

            var frames = new Texture2D[crops.Length];
            for (int i = 0; i < crops.Length; i++)
            {
                var frameTexture = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, false);
                frameTexture.filterMode = FilterMode.Bilinear;
                frameTexture.wrapMode = TextureWrapMode.Clamp;

                var canvas = new Color[canvasWidth * canvasHeight];
                for (int p = 0; p < canvas.Length; p++)
                    canvas[p] = Color.clear;

                int offsetX = anchorX - crops[i].GreenX - minX;
                int offsetY = anchorY - crops[i].GreenY - minY;

                for (int y = 0; y < crops[i].Height; y++)
                {
                    for (int x = 0; x < crops[i].Width; x++)
                    {
                        var color = crops[i].Pixels[y * crops[i].Width + x];
                        if (color.a <= 0f)
                            continue;

                        int destX = offsetX + x;
                        int destY = offsetY + y;
                        if (destX < 0 || destX >= canvasWidth || destY < 0 || destY >= canvasHeight)
                            continue;

                        canvas[destY * canvasWidth + destX] = color;
                    }
                }

                frameTexture.SetPixels(canvas);
                frameTexture.Apply();
                frames[i] = frameTexture;
            }

            return frames;
        }

        static bool IsGreenMarkerPixel(Color color)
        {
            return color.g > 0.55f && color.g > color.r + 0.35f && color.g > color.b + 0.35f;
        }

        static bool IsRedMarkerPixel(Color color)
        {
            return color.r > 0.55f && color.r > color.g + 0.35f && color.r > color.b + 0.35f;
        }

        static bool IsMarkerPixel(Color color)
        {
            return IsGreenMarkerPixel(color) || IsRedMarkerPixel(color);
        }

        float GetFrameDuration(int index)
        {
            int row = index / Mathf.Max(1, sheetColumns);
            return row == 0 ? FirstRowFrameDuration : SecondRowFrameDuration;
        }

        public void ResetToIdle()
        {
            playing = false;
            onReleaseFrame = null;
            onComplete = null;
            frameIndex = 0;
            frameTimer = 0f;
            ApplyFrame();
        }

        public void PlayOnce(Action releaseCallback, Action completeCallback = null)
        {
            if (frames == null || frameCount <= 1)
            {
                releaseCallback?.Invoke();
                completeCallback?.Invoke();
                return;
            }

            onReleaseFrame = releaseCallback;
            onComplete = completeCallback;
            frameIndex = 0;
            frameTimer = 0f;
            playing = true;
            ApplyFrame();
        }

        void Update()
        {
            if (!playing || frames == null || frameCount <= 1)
                return;

            frameTimer += Time.deltaTime;
            float frameDuration = GetFrameDuration(frameIndex);
            if (frameTimer < frameDuration)
                return;

            frameTimer -= frameDuration;

            if (frameIndex >= frameCount - 1)
            {
                playing = false;
                onReleaseFrame = null;
                onComplete?.Invoke();
                onComplete = null;
                return;
            }

            frameIndex++;
            if (frameIndex == ReleaseFrameIndex)
                onReleaseFrame?.Invoke();

            ApplyFrame();
        }

        void ApplyFrame()
        {
            if (frames == null || frameIndex < 0 || frameIndex >= frames.Length)
                return;

            if (material == null && targetRenderer != null)
                material = targetRenderer.material;

            if (material == null)
                return;

            var frame = frames[frameIndex];
            material.mainTexture = frame;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", frame);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", frame);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetTextureScale("_MainTex", Vector2.one);
            material.SetTextureOffset("_MainTex", Vector2.zero);
        }
    }
}
