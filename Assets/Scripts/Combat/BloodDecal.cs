using MonsterMiner.Core;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.Combat
{
    public class BloodDecal : MonoBehaviour
    {
        const float FloorRestOffset = 0.01f;

        public static void Spawn(Vector3 worldPoint)
        {
            var pos = ResolveFloorAnchor(worldPoint);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "BloodDecal";
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            go.transform.localScale = Vector3.one * Random.Range(0.35f, 0.7f);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = PrimitiveFactory.CreateColorMaterial(new Color(0.45f, 0.02f, 0.02f), 0f);
            Destroy(go.GetComponent<Collider>());

            var contentRoot = GameContext.Instance?.CavernBounds?.transform;
            if (contentRoot != null)
                go.transform.SetParent(contentRoot, true);

            var decal = go.AddComponent<BloodDecal>();
            decal.lifetime = 8f;
        }

        static Vector3 ResolveFloorAnchor(Vector3 worldPoint)
        {
            if (FloorAnchor.TryResolveFloorPoint(worldPoint, 16f, 32f, out var floorPoint))
                return floorPoint + Vector3.up * FloorRestOffset;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds != null)
                return new Vector3(worldPoint.x, bounds.FloorTopWorldY + FloorRestOffset, worldPoint.z);

            return worldPoint;
        }

        float lifetime;

        void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
                Destroy(gameObject);
        }
    }
}
