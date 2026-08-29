using System.Collections.Generic;
using MonsterMiner.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.Util
{
    public static class MeatVisualFactory
    {
        const float PebbleWorldScaleMultiplier = 2f;
        const float HeldDropScaleMultiplier = 1.1f;
        const float ExtrusionDepthRatio = 0.34f;
        public const float DropVisualScaleMultiplier = 2f;
        const float AlphaThreshold = 0.08f;
        static readonly Color PlaceholderColor = new Color(0.85f, 0.2f, 0.2f);

        static readonly Dictionary<string, OpaqueBounds> opaqueBoundsCache = new();
        static readonly Dictionary<int, Mesh> slabMeshCache = new();
        static readonly Dictionary<string, string> ImportedMeatMeshByItemId = new()
        {
            { "salamander_meat", "Models/Meat/1" },
            { "iguana_meat", "Models/Meat/32" },
            { "cave_lizard_meat", "Models/Meat/13" },
            { "rabbit_meat", "Models/Meat/16" },
            { "gremlin_meat", "Models/Meat/27" },
        };

        readonly struct OpaqueBounds
        {
            public readonly float MinU;
            public readonly float MinV;
            public readonly float MaxU;
            public readonly float MaxV;

            public OpaqueBounds(float minU, float minV, float maxU, float maxV)
            {
                MinU = minU;
                MinV = minV;
                MaxU = maxU;
                MaxV = maxV;
            }

            public float Width => Mathf.Max(0.05f, MaxU - MinU);
            public float Height => Mathf.Max(0.05f, MaxV - MinV);
            public float MaxExtent => Mathf.Max(Width, Height);
            public float Aspect => Height / Width;

            public static OpaqueBounds Full => new OpaqueBounds(0f, 0f, 1f, 1f);
        }

        public static GameObject CreateWorldMeat(Vector3 worldPoint, ItemDefinition item)
        {
            string name = item != null ? item.displayName : "Meat";

            if (!FloorAnchor.TryResolveFloorPoint(worldPoint, 16f, 32f, out var floorPoint))
                floorPoint = worldPoint;

            int seed = Mathf.Abs((floorPoint * 1000f).GetHashCode());
            var go = CreateMeatSlab(
                name,
                floorPoint,
                Quaternion.identity,
                item,
                seed,
                world: true,
                includeCollider: true);

            FloorAnchor.PlaceOnFloor(go, floorPoint);
            return go;
        }

        public static GameObject CreateHeldMonsterDrop(ItemDefinition item, Transform parent, Vector3 localPosition)
        {
            int seed = item.itemId.GetHashCode();

            var state = Random.state;
            Random.InitState(seed);
            var rotation = Random.rotation;
            Random.state = state;

            var go = CreateMeatSlab(
                $"Held_{item.displayName}",
                parent.position,
                rotation,
                item,
                seed,
                world: false,
                includeCollider: false);

            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        static GameObject CreateMeatSlab(
            string name,
            Vector3 position,
            Quaternion rotation,
            ItemDefinition item,
            int seed,
            bool world,
            bool includeCollider)
        {
            var imported = TryCreateImportedMeat(name, position, rotation, item, seed, world, includeCollider);
            if (imported != null)
                return imported;

            float baseLinear = GetBaseLinearScale(seed, world);
            string texturePath = ResolveMeatTexturePath(item);
            OpaqueBounds bounds = GetOpaqueBounds(texturePath);

            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = new Vector3(
                baseLinear * bounds.Width,
                baseLinear * bounds.Width,
                baseLinear * bounds.MaxExtent * ExtrusionDepthRatio);

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetSlabMesh(bounds.Aspect);

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMeatMaterial(texturePath, bounds);

            if (includeCollider)
            {
                var collider = go.AddComponent<BoxCollider>();
                collider.size = Vector3.one;
                collider.center = Vector3.zero;
            }

            return go;
        }

        static Mesh GetSlabMesh(float aspect)
        {
            int key = Mathf.RoundToInt(aspect * 1000f);
            if (slabMeshCache.TryGetValue(key, out var cached))
                return cached;

            float halfWidth = 0.5f;
            float halfHeight = 0.5f * aspect;
            const float halfDepth = 0.5f;

            var vertices = new[]
            {
                new Vector3(-halfWidth, -halfHeight, halfDepth),
                new Vector3(halfWidth, -halfHeight, halfDepth),
                new Vector3(halfWidth, halfHeight, halfDepth),
                new Vector3(-halfWidth, halfHeight, halfDepth),
                new Vector3(halfWidth, -halfHeight, -halfDepth),
                new Vector3(-halfWidth, -halfHeight, -halfDepth),
                new Vector3(-halfWidth, halfHeight, -halfDepth),
                new Vector3(halfWidth, halfHeight, -halfDepth),
            };

            var uvs = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };

            var triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                5, 6, 4, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                1, 2, 7, 1, 7, 4,
                2, 3, 6, 2, 6, 7,
                3, 0, 5, 3, 5, 6,
            };

            var mesh = new Mesh { name = $"MeatSlab_{key}" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            slabMeshCache[key] = mesh;
            return mesh;
        }

        static float GetBaseLinearScale(int seed, bool world)
        {
            Vector3 pebbleScale = PebbleVisualFactory.GetPebbleScale(seed);
            float linear = (pebbleScale.x + pebbleScale.y + pebbleScale.z) / 3f;
            if (world)
                linear *= PebbleWorldScaleMultiplier;

            return linear * (world ? 1f : HeldDropScaleMultiplier) * DropVisualScaleMultiplier;
        }

        static string ResolveMeatTexturePath(ItemDefinition item)
        {
            if (item != null && !string.IsNullOrEmpty(item.iconResourcePath))
                return item.iconResourcePath;

            return "Textures/Creatures/Meat/rabbit";
        }

        static string ResolveMeatMeshPath(ItemDefinition item)
        {
            if (item == null || string.IsNullOrEmpty(item.itemId))
                return null;

            return ImportedMeatMeshByItemId.TryGetValue(item.itemId, out var path) ? path : null;
        }

        static GameObject TryCreateImportedMeat(
            string name,
            Vector3 position,
            Quaternion rotation,
            ItemDefinition item,
            int seed,
            bool world,
            bool includeCollider)
        {
            string meshPath = ResolveMeatMeshPath(item);
            if (string.IsNullOrEmpty(meshPath))
                return null;

            var prefab = Resources.Load<GameObject>(meshPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: meat mesh not found at Resources/{meshPath}.");
                return null;
            }

            var go = Object.Instantiate(prefab);
            go.name = name;
            DisableImportedExtras(go);
            if (!HasRenderableMesh(go))
            {
                Object.Destroy(go);
                Debug.LogWarning($"Monster Miner: meat mesh at Resources/{meshPath} has no faces.");
                return null;
            }

            ApplyImportedMeatMaterials(go, ResolveMeatTexturePath(item));
            Quaternion onBack = GetOnBackRotation(go);
            go.transform.rotation = onBack;
            FitImportedMeatScale(go, GetBaseLinearScale(seed, world));
            go.transform.SetPositionAndRotation(position, rotation * onBack);
            AttachOrClearColliders(go, includeCollider);
            return go;
        }

        static void DisableImportedExtras(GameObject root)
        {
            foreach (var light in root.GetComponentsInChildren<Light>(true))
                light.enabled = false;

            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                camera.enabled = false;
        }

        static bool HasRenderableMesh(GameObject root)
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || mesh.vertexCount <= 0 || mesh.subMeshCount <= 0)
                    continue;
                if (mesh.GetIndexCount(0) > 0)
                    return true;
            }

            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null || mesh.vertexCount <= 0 || mesh.subMeshCount <= 0)
                    continue;
                if (mesh.GetIndexCount(0) > 0)
                    return true;
            }

            return false;
        }

        static Quaternion GetOnBackRotation(GameObject go)
        {
            var previousRotation = go.transform.rotation;
            var previousScale = go.transform.localScale;
            go.transform.SetPositionAndRotation(go.transform.position, Quaternion.identity);
            go.transform.localScale = Vector3.one;

            Vector3 size = ComputeRendererBoundsLocal(go).size;
            go.transform.SetPositionAndRotation(go.transform.position, previousRotation);
            go.transform.localScale = previousScale;

            if (size.y <= size.x && size.y <= size.z)
                return Quaternion.identity;
            if (size.x <= size.y && size.x <= size.z)
                return Quaternion.Euler(0f, 0f, 90f);
            return Quaternion.Euler(-90f, 0f, 0f);
        }

        static void ApplyImportedMeatMaterials(GameObject root, string texturePath)
        {
            var texture = Resources.Load<Texture2D>(texturePath);
            var template = Resources.Load<Material>("Materials/DefaultSurface");
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (template == null && urpLit == null)
                return;

            var material = template != null ? new Material(template) : new Material(urpLit);
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", texture);
                else if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            else if (material.HasProperty("_Color"))
                material.color = Color.white;

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.35f);

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var remapped = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
                for (int i = 0; i < remapped.Length; i++)
                    remapped[i] = material;
                renderer.sharedMaterials = remapped;
            }
        }

        static void FitImportedMeatScale(GameObject go, float targetLinear)
        {
            go.transform.localScale = Vector3.one;
            Bounds bounds = ComputeRendererBoundsLocal(go);
            float maxExtent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxExtent < 0.0001f)
                return;

            go.transform.localScale = Vector3.one * (targetLinear / maxExtent);
        }

        static void AttachOrClearColliders(GameObject go, bool includeCollider)
        {
            foreach (var collider in go.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);

            if (!includeCollider)
                return;

            Bounds localBounds = ComputeRendererBoundsLocal(go);
            var box = go.AddComponent<BoxCollider>();
            box.center = localBounds.center;
            box.size = Vector3.Max(localBounds.size, Vector3.one * 0.05f);
        }

        static Bounds ComputeRendererBoundsLocal(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one * 0.1f);

            Bounds world = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                world.Encapsulate(renderers[i].bounds);

            Vector3 localCenter = root.transform.InverseTransformPoint(world.center);
            Vector3 lossy = root.transform.lossyScale;
            Vector3 localSize = new Vector3(
                lossy.x > 0.0001f ? world.size.x / lossy.x : world.size.x,
                lossy.y > 0.0001f ? world.size.y / lossy.y : world.size.y,
                lossy.z > 0.0001f ? world.size.z / lossy.z : world.size.z);
            return new Bounds(localCenter, localSize);
        }

        static OpaqueBounds GetOpaqueBounds(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return OpaqueBounds.Full;

            if (opaqueBoundsCache.TryGetValue(resourcePath, out var cached))
                return cached;

            var source = Resources.Load<Texture2D>(resourcePath);
            var bounds = source != null ? MeasureOpaqueBounds(source) : OpaqueBounds.Full;
            opaqueBoundsCache[resourcePath] = bounds;
            return bounds;
        }

        static OpaqueBounds MeasureOpaqueBounds(Texture2D source)
        {
            Texture2D readable = CreateReadableCopy(source);
            if (readable == null)
                return OpaqueBounds.Full;

            int width = readable.width;
            int height = readable.height;
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;

            var pixels = readable.GetPixels32();
            Object.Destroy(readable);

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[row + x].a / 255f < AlphaThreshold)
                        continue;

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
                return OpaqueBounds.Full;

            float padU = 0.5f / width;
            float padV = 0.5f / height;
            return new OpaqueBounds(
                Mathf.Clamp01(minX / (float)width - padU),
                Mathf.Clamp01(minY / (float)height - padV),
                Mathf.Clamp01((maxX + 1) / (float)width + padU),
                Mathf.Clamp01((maxY + 1) / (float)height + padV));
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

        static Material CreateMeatMaterial(string resourcePath, OpaqueBounds bounds)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            var mat = PrimitiveFactory.CreateColorMaterial(texture != null ? Color.white : PlaceholderColor, 0.35f);
            if (texture == null)
                return mat;

            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", texture);
                mat.SetTextureScale("_BaseMap", new Vector2(bounds.Width, bounds.Height));
                mat.SetTextureOffset("_BaseMap", new Vector2(bounds.MinU, bounds.MinV));
            }
            else if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", texture);
                mat.SetTextureScale("_MainTex", new Vector2(bounds.Width, bounds.Height));
                mat.SetTextureOffset("_MainTex", new Vector2(bounds.MinU, bounds.MinV));
            }

            ConfigureTransparentIconMaterial(mat);
            return mat;
        }

        static void ConfigureTransparentIconMaterial(Material mat)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);

            if (!mat.HasProperty("_Surface"))
                return;

            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
