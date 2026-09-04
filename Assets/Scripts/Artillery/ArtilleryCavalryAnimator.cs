using UnityEngine;

namespace MonsterMiner.Artillery
{
    public class ArtilleryCavalryAnimator : MonoBehaviour
    {
        const float FrameDuration = 0.05f;

        Material material;
        int frameIndex;
        float frameTimer;
        int frameCount;
        int sheetColumns;
        int sheetRows;

        public void Configure(Material sharedMaterial, int columns, int rows)
        {
            material = sharedMaterial;
            sheetColumns = Mathf.Max(1, columns);
            sheetRows = Mathf.Max(1, rows);
            frameCount = sheetColumns * sheetRows;
            ApplyFrame();
        }

        void Update()
        {
            if (material == null || frameCount <= 1)
                return;

            frameTimer += Time.deltaTime;
            if (frameTimer < FrameDuration)
                return;

            frameTimer = 0f;
            frameIndex = (frameIndex + 1) % frameCount;
            ApplyFrame();
        }

        void ApplyFrame()
        {
            if (material == null)
                return;

            int column = frameIndex % sheetColumns;
            int row = frameIndex / sheetColumns;
            var scale = new Vector2(1f / sheetColumns, 1f / sheetRows);
            var offset = new Vector2(
                column / (float)sheetColumns,
                (sheetRows - 1 - row) / (float)sheetRows);

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTextureScale("_BaseMap", scale);
                material.SetTextureOffset("_BaseMap", offset);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTextureScale("_MainTex", scale);
                material.SetTextureOffset("_MainTex", offset);
            }
        }
    }
}
