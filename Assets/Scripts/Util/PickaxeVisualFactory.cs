using UnityEngine;

namespace MonsterMiner.Util
{
    public static class PickaxeVisualFactory
    {
        const string PrefabResourcePath = "Models/Tools/basic_pickaxe";
        const string BaseColorTexturePath = "Textures/Tools/T_BasicPickaxe_BC";
        const string NormalTexturePath = "Textures/Tools/T_BasicPickaxe_N";
        const string MaskTexturePath = "Textures/Tools/T_BasicPickaxe_Mask";

        static readonly Vector3 HeldLocalPosition = new Vector3(0.01f, -0.04f, 0f);
        static readonly Vector3 HeldLocalEuler = new Vector3(75f, 190f, -145f);
        static readonly Vector3 HeldLocalScale = Vector3.one * 0.66528f;
        static readonly Vector3 SwingPivotLocal = new Vector3(0f, -0.245f, 0f);

        public const int MaxMiningTier = 5;

        static readonly Color[] HeadColors =
        {
            new Color(0.55f, 0.55f, 0.58f),
            new Color(0.92f, 0.92f, 0.92f),
            new Color(0.25f, 0.85f, 0.35f),
            new Color(0.25f, 0.5f, 1f),
            new Color(0.65f, 0.25f, 0.95f),
            new Color(1f, 0.82f, 0.15f)
        };

        static Material[] tierMaterials;
        static Texture2D[] tierAlbedoCache;

        static readonly Vector3 NpcHandLocalPosition = new Vector3(0f, 0.04f, 0.03f);
        static readonly Vector3 NpcHandLocalEuler = new Vector3(10f, 95f, -105f);
        static readonly Vector3 NpcHandLocalScale = Vector3.one * 0.52f;

        public static Vector3 HeldMeshLocalPosition => HeldLocalPosition;
        public static Quaternion HeldMeshLocalRotation => Quaternion.Euler(HeldLocalEuler);
        public static Vector3 HeldSwingPivotLocal => SwingPivotLocal;

        public static Color GetHeadColor(int tier) => HeadColors[Mathf.Clamp(tier, 0, MaxMiningTier)];

        public static Color GetTierBackgroundColor(int tier)
        {
            Color head = GetHeadColor(tier);
            return new Color(head.r, head.g, head.b, 0.92f);
        }

        public static GameObject CreateHeldPickaxe(Transform parent, int tier = 0)
        {
            return CreatePickaxe(parent, tier, HeldLocalPosition, HeldLocalEuler, HeldLocalScale, "HeldPickaxe");
        }

        public static GameObject CreateNpcHeldPickaxe(Transform parent, int tier = 0)
        {
            return CreatePickaxe(parent, tier, NpcHandLocalPosition, NpcHandLocalEuler, NpcHandLocalScale, "NpcPickaxe");
        }

        static GameObject CreatePickaxe(
            Transform parent,
            int tier,
            Vector3 localPosition,
            Vector3 localEuler,
            Vector3 localScale,
            string baseName)
        {
            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: pickaxe prefab not found at Resources/{PrefabResourcePath}.");
                return null;
            }

            int clampedTier = Mathf.Clamp(tier, 0, MaxMiningTier);
            var pickaxe = Object.Instantiate(prefab, parent, false);
            pickaxe.name = clampedTier > 0 ? $"{baseName}_T{clampedTier}" : baseName;
            pickaxe.transform.localPosition = localPosition;
            pickaxe.transform.localRotation = Quaternion.Euler(localEuler);
            pickaxe.transform.localScale = localScale;
            ApplyMaterial(pickaxe, clampedTier);
            DisableColliders(pickaxe);
            return pickaxe;
        }

        static void ApplyMaterial(GameObject root, int tier)
        {
            var material = GetHeldPickaxeMaterial(tier);
            if (material == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;
        }

        static Material GetHeldPickaxeMaterial(int tier)
        {
            if (tierMaterials == null)
                tierMaterials = new Material[MaxMiningTier + 1];

            int index = Mathf.Clamp(tier, 0, MaxMiningTier);
            if (tierMaterials[index] != null)
                return tierMaterials[index];

            var template = Resources.Load<Material>("Materials/DefaultSurface");
            if (template == null)
            {
                var urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit == null)
                    return null;
                template = new Material(urpLit);
            }

            var material = new Material(template);
            var baseMap = Resources.Load<Texture2D>(BaseColorTexturePath);
            var normalMap = Resources.Load<Texture2D>(NormalTexturePath);
            var maskMap = Resources.Load<Texture2D>(MaskTexturePath);
            var tintedAlbedo = GetTierAlbedo(baseMap, maskMap, index);

            if (tintedAlbedo != null)
            {
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", tintedAlbedo);
                else if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", tintedAlbedo);
            }
            else if (baseMap != null)
            {
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", baseMap);
                else if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", baseMap);
            }

            if (normalMap != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normalMap);
                material.EnableKeyword("_NORMALMAP");
            }

            if (maskMap != null && material.HasProperty("_MetallicGlossMap"))
            {
                material.SetTexture("_MetallicGlossMap", maskMap);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.45f + index * 0.05f);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            else if (material.HasProperty("_Color"))
                material.color = Color.white;

            tierMaterials[index] = material;
            return material;
        }

        static Texture2D GetTierAlbedo(Texture2D baseMap, Texture2D maskMap, int tier)
        {
            if (baseMap == null || maskMap == null)
                return null;

            if (tierAlbedoCache == null)
                tierAlbedoCache = new Texture2D[MaxMiningTier + 1];

            if (tierAlbedoCache[tier] != null)
                return tierAlbedoCache[tier];

            tierAlbedoCache[tier] = BakeHeadTintedAlbedo(baseMap, maskMap, HeadColors[tier]);
            return tierAlbedoCache[tier];
        }

        static Texture2D BakeHeadTintedAlbedo(Texture2D baseMap, Texture2D maskMap, Color headColor)
        {
            var readableBase = GetReadableCopy(baseMap);
            var readableMask = GetReadableCopy(maskMap);
            if (readableBase == null || readableMask == null)
                return null;

            var pixels = readableBase.GetPixels();
            var maskPixels = readableMask.GetPixels();
            int count = Mathf.Min(pixels.Length, maskPixels.Length);

            for (int i = 0; i < count; i++)
            {
                float metallic = maskPixels[i].r;
                if (metallic < 0.2f)
                    continue;

                float blend = Mathf.SmoothStep(0.2f, 0.55f, metallic);
                var shadedHead = new Color(
                    headColor.r * (0.35f + 0.65f * pixels[i].r),
                    headColor.g * (0.35f + 0.65f * pixels[i].g),
                    headColor.b * (0.35f + 0.65f * pixels[i].b),
                    pixels[i].a);
                pixels[i] = Color.Lerp(pixels[i], shadedHead, blend);
            }

            var baked = new Texture2D(readableBase.width, readableBase.height, TextureFormat.RGBA32, false);
            baked.SetPixels(pixels);
            baked.Apply(false, true);

            Object.Destroy(readableBase);
            Object.Destroy(readableMask);
            return baked;
        }

        static Texture2D GetReadableCopy(Texture2D source)
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

        static void DisableColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
        }
    }
}
