using UnityEngine;

namespace MonsterMiner.Artillery
{
    public class ArtillerySoldierSquad : MonoBehaviour
    {
        const float FrameDuration = 0.18f;

        Material material;
        int frameIndex;
        float frameTimer;
        int animatedFrameCount;
        int sheetFrameCount;

        public void Configure(Material sharedMaterial, int animatedFrames, int totalSheetFrames = 4)
        {
            material = sharedMaterial;
            animatedFrameCount = Mathf.Max(1, animatedFrames);
            sheetFrameCount = Mathf.Max(1, totalSheetFrames);
            ApplyFrame();
        }

        void Update()
        {
            if (material == null || animatedFrameCount <= 1)
                return;

            frameTimer += Time.deltaTime;
            if (frameTimer < FrameDuration)
                return;

            frameTimer = 0f;
            frameIndex = (frameIndex + 1) % animatedFrameCount;
            ApplyFrame();
        }

        void ApplyFrame()
        {
            if (material == null)
                return;

            float frameWidth = 1f / sheetFrameCount;
            var scale = new Vector2(frameWidth, 1f);
            var offset = new Vector2(frameIndex * frameWidth, 0f);

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
