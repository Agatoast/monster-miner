using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class MapWaterColorSampler
    {
        const string WaterMaterialResourcePath = "WaterWorks/SSR_Water";
        const float ShoreFoamWidthFeet = 36f;

        static bool initialized;
        static Color baseColor = new Color(0.19215687f, 0.23921569f, 0.2627451f, 1f);
        static Color edgeColor = new Color(0.25098038f, 0.32941175f, 0.28627446f, 1f);
        static float tiling = 0.2f;
        static float frequency = 15f;
        static float waveSpeed = 2f;

        public static Color32 Sample(float localX, float localZ)
        {
            EnsureInitialized();

            float scale = Mathf.Max(0.05f, tiling) * 0.04f;
            float waveA = Mathf.PerlinNoise(localX * scale + 12.3f, localZ * scale + 4.7f);
            float waveB = Mathf.PerlinNoise(localX * scale * 2.4f + 31.1f, localZ * scale * 2.4f + 18.2f);
            float waveC = Mathf.Sin(localX * frequency * 0.0035f + waveSpeed * 0.15f)
                * Mathf.Cos(localZ * frequency * 0.0035f + waveSpeed * 0.11f) * 0.5f + 0.5f;
            float blend = waveA * 0.52f + waveB * 0.33f + waveC * 0.15f;

            Color color = Color.Lerp(baseColor * 0.82f, baseColor * 1.18f, blend);

            float foam = SampleShoreFoam(localX, localZ);
            if (foam > 0.001f)
                color = Color.Lerp(color, edgeColor, foam * 0.65f);

            return new Color32(
                (byte)Mathf.Clamp(color.r * 255f, 0f, 255f),
                (byte)Mathf.Clamp(color.g * 255f, 0f, 255f),
                (byte)Mathf.Clamp(color.b * 255f, 0f, 255f),
                255);
        }

        static float SampleShoreFoam(float localX, float localZ)
        {
            if (!LakeCatalog.IsOpenWaterLocal(localX, localZ))
                return 0f;

            var shore = LakeCatalog.GetNearestShoreLocal(localX, localZ);
            float distance = Vector2.Distance(new Vector2(localX, localZ), shore);
            return Mathf.Clamp01(1f - distance / WorldScale.Feet(ShoreFoamWidthFeet));
        }

        static void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;
            var material = Resources.Load<Material>(WaterMaterialResourcePath);
            if (material == null)
                return;

            if (material.HasProperty("_Color"))
                baseColor = material.GetColor("_Color");

            if (material.HasProperty("_Edge_Color"))
                edgeColor = material.GetColor("_Edge_Color");
            else if (material.HasProperty("_EdgeColor"))
                edgeColor = material.GetColor("_EdgeColor");

            if (material.HasProperty("_Tiling"))
                tiling = material.GetFloat("_Tiling");

            if (material.HasProperty("_Frequency"))
                frequency = material.GetFloat("_Frequency");

            if (material.HasProperty("_Wave_Speed"))
                waveSpeed = material.GetFloat("_Wave_Speed");
        }
    }
}
