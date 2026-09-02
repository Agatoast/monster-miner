using MonsterMiner.Util;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.World
{
    public static class CavernSurfaceMaterialFactory
    {
        const int TextureSize = 256;

        static Material shellMaterial;
        static Material wallMaterial;
        static Material floorMaterial;
        static Material grassMaterial;
        static Material dirtMaterial;
        static Material plainsGrassMaterial;
        static Material plainsMeadowMaterial;
        static Material plainsScrubMaterial;
        static Material snowMaterial;
        static Material waterMaterial;
        static Material sandMaterial;
        static Material shoreGroundOccluderMaterial;
        static Material beachSandVisualMaterial;
        static Material vistaCanopyMaterial;
        static Material unlitVistaCanopyMaterial;
        static Material unlitPlainsMaterial;

        public static Material GetShellMaterial()
        {
            if (shellMaterial != null)
                return shellMaterial;

            ColorUtility.TryParseHtmlString("#9a9690", out var color);
            shellMaterial = CreateStoneMaterial(color, smoothness: 0.06f, seed: 11.4f, tileScale: 6f);
            return shellMaterial;
        }

        public static Material GetWallMaterial()
        {
            if (wallMaterial != null)
                return wallMaterial;

            ColorUtility.TryParseHtmlString("#6f6a64", out var color);
            wallMaterial = CreateStoneMaterial(
                color,
                smoothness: 0.04f,
                seed: 41.2f,
                tileScale: 7f,
                shadeMin: 0.42f,
                shadeMax: 0.86f,
                crackStrength: 0.34f);
            return wallMaterial;
        }

        public static Material GetFloorMaterial()
        {
            if (floorMaterial != null)
                return floorMaterial;

            ColorUtility.TryParseHtmlString("#a8aba8", out var color);
            floorMaterial = CreateStoneMaterial(color, smoothness: 0.1f, seed: 27.9f, tileScale: 5f);
            return floorMaterial;
        }

        public static Material GetGrassMaterial()
        {
            if (grassMaterial != null)
                return grassMaterial;

            ColorUtility.TryParseHtmlString("#5f8f3a", out var color);
            grassMaterial = CreateStoneMaterial(
                color,
                smoothness: 0.02f,
                seed: 63.7f,
                tileScale: 12f,
                shadeMin: 0.72f,
                shadeMax: 1.12f,
                crackStrength: 0.08f);
            grassMaterial.name = "PlainsGrass";
            return grassMaterial;
        }

        public static Material GetDirtMaterial()
        {
            if (dirtMaterial != null)
                return dirtMaterial;

            ColorUtility.TryParseHtmlString("#7a6848", out var color);
            dirtMaterial = CreateStoneMaterial(
                color,
                smoothness: 0.03f,
                seed: 88.1f,
                tileScale: 8f,
                shadeMin: 0.58f,
                shadeMax: 1.05f,
                crackStrength: 0.14f);
            dirtMaterial.name = "QuarryDirt";
            return dirtMaterial;
        }

        public static Material GetPlainsGrassMaterial()
        {
            if (plainsGrassMaterial != null)
                return plainsGrassMaterial;

            ColorUtility.TryParseHtmlString("#4a7a2c", out var color);
            plainsGrassMaterial = CreateStoneMaterial(
                color,
                smoothness: 0.015f,
                seed: 144.2f,
                tileScale: 2.4f,
                shadeMin: 0.42f,
                shadeMax: 1.18f,
                crackStrength: 0.12f);
            plainsGrassMaterial.name = "PlainsGrass";
            return plainsGrassMaterial;
        }

        public static Material GetPlainsMeadowMaterial()
        {
            if (plainsMeadowMaterial != null)
                return plainsMeadowMaterial;

            ColorUtility.TryParseHtmlString("#6f9a3a", out var color);
            plainsMeadowMaterial = CreateStoneMaterial(
                color,
                smoothness: 0.02f,
                seed: 201.6f,
                tileScale: 2.1f,
                shadeMin: 0.48f,
                shadeMax: 1.22f,
                crackStrength: 0.06f);
            plainsMeadowMaterial.name = "PlainsMeadow";
            return plainsMeadowMaterial;
        }

        public static Material GetPlainsScrubMaterial()
        {
            if (plainsScrubMaterial != null)
                return plainsScrubMaterial;

            ColorUtility.TryParseHtmlString("#6a5a34", out var color);
            plainsScrubMaterial = CreateStoneMaterial(
                color,
                smoothness: 0.025f,
                seed: 77.3f,
                tileScale: 2.8f,
                shadeMin: 0.5f,
                shadeMax: 1.08f,
                crackStrength: 0.18f);
            plainsScrubMaterial.name = "PlainsScrub";
            return plainsScrubMaterial;
        }

        const string AntarcticaGroundResourcePath = "Textures/Terrain/antarctica_ground";
        const float SnowTileScale = 1.25f;

        public static Material GetSnowMaterial()
        {
            if (snowMaterial == null)
                snowMaterial = CreateSnowMaterial();
            else
                EnsureOpaque(snowMaterial);

            return snowMaterial;
        }

        static Material CreateSnowMaterial()
        {
            var mat = new Material(GetLitTemplate())
            {
                name = "QuarrySnow"
            };

            var albedo = Resources.Load<Texture2D>(AntarcticaGroundResourcePath);
            if (albedo == null)
            {
                Debug.LogWarning($"Monster Miner: snow texture missing at Resources/{AntarcticaGroundResourcePath}.");
                albedo = CreateSnowAlbedo();
            }

            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", albedo);
                mat.SetTextureScale("_BaseMap", new Vector2(SnowTileScale, SnowTileScale));
            }
            else if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", albedo);
                mat.SetTextureScale("_MainTex", new Vector2(SnowTileScale, SnowTileScale));
            }

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            else
                mat.color = Color.white;

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.04f);
            else if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", 0.04f);

            EnsureOpaque(mat);
            mat.SetInt("_Cull", (int)CullMode.Off);
            mat.doubleSidedGI = true;
            mat.renderQueue = (int)RenderQueue.Geometry + 1;
            return mat;
        }

        static Texture2D CreateSnowAlbedo()
        {
            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = "QuarrySnowAlbedo"
            };

            var pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = x / (float)TextureSize;
                    float v = y / (float)TextureSize;
                    float sparkle = Mathf.PerlinNoise(u * 24f + 44.2f, v * 24f + 19.7f);
                    float shade = 0.992f + sparkle * 0.008f;
                    pixels[y * TextureSize + x] = new Color(shade, shade, shade + 0.008f, 1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, true);
            return tex;
        }

        public static Material GetSandMaterial()
        {
            if (sandMaterial != null)
                return sandMaterial;

            ColorUtility.TryParseHtmlString("#c8b48a", out var color);
            sandMaterial = CreateStoneMaterial(
                color,
                smoothness: 0.04f,
                seed: 318.7f,
                tileScale: 4.5f,
                shadeMin: 0.82f,
                shadeMax: 1.08f,
                crackStrength: 0.05f);
            sandMaterial.name = "LakeSand";
            return sandMaterial;
        }

        public static Material GetBeachSandVisualMaterial()
        {
            if (beachSandVisualMaterial != null)
                return beachSandVisualMaterial;

            ColorUtility.TryParseHtmlString("#c8b48a", out var color);
            beachSandVisualMaterial = CreateUnlitMaterial(color, "BeachSandVisual");
            beachSandVisualMaterial.SetInt("_Cull", (int)CullMode.Off);
            if (beachSandVisualMaterial.HasProperty("_Cull"))
                beachSandVisualMaterial.SetFloat("_Cull", (float)CullMode.Off);
            return beachSandVisualMaterial;
        }

        public static Material GetShoreGroundOccluderMaterial()
        {
            if (shoreGroundOccluderMaterial != null)
                return shoreGroundOccluderMaterial;

            ColorUtility.TryParseHtmlString("#c4a882", out var color);
            shoreGroundOccluderMaterial = CreateStoneMaterial(
                color,
                smoothness: 0.02f,
                seed: 612.4f,
                tileScale: 3.5f,
                shadeMin: 0.88f,
                shadeMax: 1.02f,
                crackStrength: 0.03f);
            shoreGroundOccluderMaterial.name = "ShoreGroundOccluder";
            shoreGroundOccluderMaterial.renderQueue = (int)RenderQueue.Geometry + 5;
            return shoreGroundOccluderMaterial;
        }

        public static Material GetWaterMaterial()
        {
            if (waterMaterial != null)
                return waterMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            waterMaterial = shader != null ? new Material(shader) : PrimitiveFactory.CreateColorMaterial(Color.cyan);
            waterMaterial.name = "LakeWater";
            waterMaterial.SetFloat("_Surface", 1f);
            waterMaterial.SetFloat("_Blend", 0f);
            waterMaterial.SetFloat("_Smoothness", 0.85f);
            waterMaterial.SetColor("_BaseColor", new Color(0.12f, 0.38f, 0.62f, 0.72f));
            waterMaterial.renderQueue = (int)RenderQueue.Transparent;
            waterMaterial.SetOverrideTag("RenderType", "Transparent");
            waterMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            waterMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            waterMaterial.SetInt("_ZWrite", 0);
            waterMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            waterMaterial.EnableKeyword("_ALPHABLEND_ON");
            waterMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            return waterMaterial;
        }

        public static Material GetVistaCanopyMaterial()
        {
            if (vistaCanopyMaterial != null)
                return vistaCanopyMaterial;

            ColorUtility.TryParseHtmlString("#1f4a18", out var color);
            vistaCanopyMaterial = CreateStoneMaterial(
                color,
                smoothness: 0.01f,
                seed: 33.8f,
                tileScale: 1.6f,
                shadeMin: 0.72f,
                shadeMax: 1.05f,
                crackStrength: 0.04f);
            vistaCanopyMaterial.name = "VistaTreeCanopy";
            return vistaCanopyMaterial;
        }

        public static Material GetUnlitVistaCanopyMaterial()
        {
            if (unlitVistaCanopyMaterial != null)
                return unlitVistaCanopyMaterial;

            unlitVistaCanopyMaterial = CreateUnlitMaterial(new Color(0.08f, 0.34f, 0.06f), "UnlitVistaCanopy");
            return unlitVistaCanopyMaterial;
        }

        public static Material GetUnlitPlainsMaterial()
        {
            if (unlitPlainsMaterial != null)
                return unlitPlainsMaterial;

            unlitPlainsMaterial = CreateUnlitMaterial(new Color(0.34f, 0.58f, 0.18f), "UnlitPlains");
            return unlitPlainsMaterial;
        }

        static Material CreateUnlitMaterial(Color color, string materialName)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            var mat = shader != null ? new Material(shader) : CreateStoneMaterial(color, 0f, 0f, 1f);
            mat.name = materialName;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 1f));
            else
                mat.color = new Color(color.r, color.g, color.b, 1f);

            EnsureOpaque(mat);
            return mat;
        }

        static void EnsureOpaque(Material mat)
        {
            if (mat == null)
                return;

            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_AlphaClip"))
                mat.SetFloat("_AlphaClip", 0f);

            mat.renderQueue = mat.renderQueue >= (int)RenderQueue.Geometry
                && mat.renderQueue < (int)RenderQueue.AlphaTest
                    ? mat.renderQueue
                    : (int)RenderQueue.Geometry;
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.SetInt("_SrcBlend", (int)BlendMode.One);
            mat.SetInt("_DstBlend", (int)BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            if (mat.HasProperty("_ZWrite"))
                mat.SetFloat("_ZWrite", 1f);
            if (mat.HasProperty("_ZTest"))
                mat.SetFloat("_ZTest", (float)CompareFunction.LessEqual);

            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_SURFACE_TYPE_OPAQUE");

            if (mat.HasProperty("_BaseColor"))
            {
                Color baseColor = mat.GetColor("_BaseColor");
                mat.SetColor("_BaseColor", new Color(baseColor.r, baseColor.g, baseColor.b, 1f));
            }
            else if (mat.HasProperty("_Color"))
            {
                Color baseColor = mat.GetColor("_Color");
                mat.SetColor("_Color", new Color(baseColor.r, baseColor.g, baseColor.b, 1f));
            }
        }

        static Material CreateStoneMaterial(
            Color baseColor,
            float smoothness,
            float seed,
            float tileScale,
            float shadeMin = 0.55f,
            float shadeMax = 1.08f,
            float crackStrength = 0.22f)
        {
            var albedo = CreateStoneAlbedo(baseColor, seed, shadeMin, shadeMax, crackStrength);
            var normal = CreateStoneNormalMap(seed);

            var mat = new Material(GetLitTemplate());
            mat.name = "CavernStone";

            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", albedo);
            else if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", albedo);

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            else
                mat.color = Color.white;

            if (mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
                mat.SetFloat("_BumpScale", 0.85f);
            }

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            else if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", smoothness);

            mat.SetInt("_Cull", (int)CullMode.Off);
            mat.doubleSidedGI = true;
            mat.SetTextureScale("_BaseMap", new Vector2(tileScale, tileScale));
            if (mat.HasProperty("_BumpMap"))
                mat.SetTextureScale("_BumpMap", new Vector2(tileScale, tileScale));

            EnsureOpaque(mat);
            return mat;
        }

        static Material GetLitTemplate()
        {
            var template = Resources.Load<Material>("Materials/DefaultSurface");
            if (template != null)
                return template;

            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
                return new Material(urpLit);

            var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mat = fallback.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(fallback);
            return mat;
        }

        static Texture2D CreateStoneAlbedo(
            Color baseColor,
            float seed,
            float shadeMin,
            float shadeMax,
            float crackStrength)
        {
            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = "CavernStoneAlbedo"
            };

            float seedX = seed * 0.17f;
            float seedY = seed * 0.31f;
            var pixels = new Color[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = x / (float)TextureSize;
                    float v = y / (float)TextureSize;

                    float large = Fbm(u * 3.2f + seedX, v * 3.2f + seedY, 4);
                    float medium = Fbm(u * 9f + seedX * 1.7f, v * 9f + seedY * 1.7f, 3);
                    float fine = Fbm(u * 22f + seedX * 2.3f, v * 22f + seedY * 2.3f, 2);
                    float cracks = Mathf.Pow(Fbm(u * 14f - seedY, v * 14f + seedX, 2), 3f);

                    float shade = 0.78f + large * 0.16f + medium * 0.08f + fine * 0.04f - cracks * crackStrength;
                    shade = Mathf.Clamp(shade, shadeMin, shadeMax);

                    pixels[y * TextureSize + x] = new Color(
                        baseColor.r * shade,
                        baseColor.g * shade,
                        baseColor.b * shade,
                        1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, true);
            return tex;
        }

        static Texture2D CreateStoneNormalMap(float seed)
        {
            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = "CavernStoneNormal"
            };

            float seedX = seed * 0.23f;
            float seedY = seed * 0.41f;
            var pixels = new Color[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = x / (float)TextureSize;
                    float v = y / (float)TextureSize;

                    float hL = SampleHeight(u - 1f / TextureSize, v, seedX, seedY);
                    float hR = SampleHeight(u + 1f / TextureSize, v, seedX, seedY);
                    float hD = SampleHeight(u, v - 1f / TextureSize, seedX, seedY);
                    float hU = SampleHeight(u, v + 1f / TextureSize, seedX, seedY);

                    Vector3 normal = new Vector3(hL - hR, hD - hU, 0.35f).normalized;
                    pixels[y * TextureSize + x] = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f,
                        1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, true);
            return tex;
        }

        static float SampleHeight(float u, float v, float seedX, float seedY)
        {
            float large = Fbm(u * 3.2f + seedX, v * 3.2f + seedY, 4);
            float medium = Fbm(u * 9f + seedX * 1.7f, v * 9f + seedY * 1.7f, 3);
            float fine = Fbm(u * 22f + seedX * 2.3f, v * 22f + seedY * 2.3f, 2);
            return large * 0.65f + medium * 0.25f + fine * 0.1f;
        }

        static float Fbm(float x, float y, int octaves)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float frequency = 1f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                frequency *= 2f;
                amplitude *= 0.5f;
            }

            return sum;
        }
    }
}
