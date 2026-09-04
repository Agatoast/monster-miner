using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class SkyMetalLumpVisualFactory
    {
        public static GameObject CreateWorldDrop(Vector3 position, string displayName)
        {
            float size = WorldScale.Feet(1.4f);
            var lump = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Sphere,
                position,
                Vector3.one * size,
                SkyMetalDigSiteCatalog.DetectorBlue,
                displayName);
            StripCollider(lump);

            var renderer = lump.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = PrimitiveFactory.CreateColorMaterial(SkyMetalDigSiteCatalog.DetectorBlue, 0.35f);
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", SkyMetalDigSiteCatalog.DetectorBlue * 0.55f);
                renderer.sharedMaterial = material;
            }

            return lump;
        }

        static void StripCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }
    }
}
