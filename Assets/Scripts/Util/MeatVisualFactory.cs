using MonsterMiner.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.Util
{
    public static class MeatVisualFactory
    {
        const float PebbleWorldScaleMultiplier = 2f;
        const float MeatScaleVsPebble = 2f;
        const float HeldDropScaleMultiplier = 1.35f;
        static readonly Color PlaceholderColor = new Color(0.85f, 0.2f, 0.2f);

        public static GameObject CreateWorldMeat(Vector3 worldPoint, ItemDefinition item)
        {
            string name = item != null ? item.displayName : "Monster Meat";

            if (!FloorAnchor.TryResolveFloorPoint(worldPoint, 16f, 32f, out var floorPoint))
                floorPoint = worldPoint;

            int seed = Mathf.Abs((floorPoint * 1000f).GetHashCode());
            Vector3 scale = PebbleVisualFactory.GetPebbleScale(seed)
                * PebbleWorldScaleMultiplier
                * MeatScaleVsPebble;

            var go = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                floorPoint,
                scale,
                PlaceholderColor,
                name);

            ApplyMeatMaterial(go, ResolveMeatTexturePath(item));
            FloorAnchor.PlaceOnFloor(go, floorPoint);
            return go;
        }

        public static GameObject CreateHeldMonsterDrop(ItemDefinition item, Transform parent, Vector3 localPosition)
        {
            int seed = item.itemId.GetHashCode();
            Vector3 scale = item.itemId == "monster_meat"
                ? PebbleVisualFactory.GetPebbleScale(seed) * MeatScaleVsPebble
                : PebbleVisualFactory.GetPebbleScale(seed) * HeldDropScaleMultiplier;

            var go = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                parent.position,
                scale,
                item.worldColor,
                $"Held_{item.displayName}");

            ApplyMeatMaterial(go, ResolveMeatTexturePath(item));

            var state = Random.state;
            Random.InitState(seed);
            go.transform.rotation = Random.rotation;
            Random.state = state;

            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        static string ResolveMeatTexturePath(ItemDefinition item)
        {
            if (item != null && !string.IsNullOrEmpty(item.iconResourcePath))
                return item.iconResourcePath;

            return "Textures/MonsterMeat";
        }

        static void ApplyMeatMaterial(GameObject go, string resourcePath)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;

            var texture = Resources.Load<Texture2D>(resourcePath);
            var mat = PrimitiveFactory.CreateColorMaterial(texture != null ? Color.white : PlaceholderColor, 0.35f);
            if (texture != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", texture);
                else if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", texture);

                ConfigureTransparentIconMaterial(mat);
            }

            renderer.sharedMaterial = mat;
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
