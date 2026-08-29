using MonsterMiner.Core;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public sealed class EggShellBurstEffect : MonoBehaviour
    {
        const float LifetimeSeconds = 3f;
        const float BurstDurationSeconds = 0.38f;
        const float FloorRestOffset = 0.015f;
        const int FragmentCount = 12;
        const float SpreadRadiusMin = 0.35f;
        const float SpreadRadiusMax = 1.15f;
        const float BurstHeight = 0.55f;

        struct Fragment
        {
            public Transform Transform;
            public Vector3 StartPosition;
            public Vector3 EndPosition;
            public Quaternion StartRotation;
            public Quaternion EndRotation;
            public float Elapsed;
            public bool Settled;
        }

        Fragment[] fragments;
        float age;
        Material shellMaterial;

        public static void Spawn(Vector3 hatchPoint, GameObject eggRoot)
        {
            var root = new GameObject("EggShellBurst");
            var effect = root.AddComponent<EggShellBurstEffect>();
            effect.Initialize(hatchPoint, eggRoot);
        }

        void Initialize(Vector3 hatchPoint, GameObject eggRoot)
        {
            shellMaterial = ResolveShellMaterial(eggRoot);
            hatchPoint = ResolveFloorAnchor(hatchPoint) + Vector3.up * 0.18f;

            var contentRoot = GameContext.Instance?.CavernBounds?.transform;
            if (contentRoot != null)
                transform.SetParent(contentRoot, true);

            transform.position = hatchPoint;
            fragments = new Fragment[FragmentCount];

            for (int i = 0; i < FragmentCount; i++)
                fragments[i] = CreateFragment(hatchPoint, i);
        }

        static Material ResolveShellMaterial(GameObject eggRoot)
        {
            if (eggRoot != null)
            {
                var renderer = eggRoot.GetComponentInChildren<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                    return renderer.sharedMaterial;
            }

            return PrimitiveFactory.CreateColorMaterial(new Color(0.93f, 0.78f, 0.11f), 0.55f);
        }

        Fragment CreateFragment(Vector3 hatchPoint, int index)
        {
            float angle = (index / (float)FragmentCount) * Mathf.PI * 2f + Random.Range(-0.35f, 0.35f);
            float radius = Random.Range(SpreadRadiusMin, SpreadRadiusMax);
            var offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Vector3 endPoint = ResolveFloorAnchor(hatchPoint + offset) + Vector3.up * FloorRestOffset;

            var shard = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shard.name = "EggShellShard";
            shard.transform.SetParent(transform, false);
            shard.transform.position = hatchPoint;
            shard.transform.localScale = Vector3.one * Random.Range(0.12f, 0.24f);

            var collider = shard.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            var renderer = shard.GetComponent<Renderer>();
            if (renderer != null && shellMaterial != null)
                renderer.sharedMaterial = shellMaterial;

            return new Fragment
            {
                Transform = shard.transform,
                StartPosition = hatchPoint,
                EndPosition = endPoint,
                StartRotation = Random.rotation,
                EndRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), Random.Range(-18f, 18f)),
                Elapsed = Random.Range(0f, 0.06f),
                Settled = false
            };
        }

        static Vector3 ResolveFloorAnchor(Vector3 worldPoint)
        {
            if (FloorAnchor.TryResolveFloorPoint(worldPoint, 16f, 32f, out var floorPoint))
                return floorPoint;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds != null)
                return new Vector3(worldPoint.x, bounds.FloorTopWorldY, worldPoint.z);

            return worldPoint;
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;
            age += deltaTime;

            for (int i = 0; i < fragments.Length; i++)
            {
                ref Fragment fragment = ref fragments[i];
                if (fragment.Transform == null)
                    continue;

                if (!fragment.Settled)
                {
                    fragment.Elapsed += deltaTime;
                    float t = Mathf.Clamp01(fragment.Elapsed / BurstDurationSeconds);
                    float eased = 1f - (1f - t) * (1f - t);

                    Vector3 pos = Vector3.Lerp(fragment.StartPosition, fragment.EndPosition, eased);
                    pos.y += BurstHeight * 4f * t * (1f - t);
                    fragment.Transform.position = pos;
                    fragment.Transform.rotation = Quaternion.Slerp(fragment.StartRotation, fragment.EndRotation, eased);

                    if (t >= 1f)
                    {
                        fragment.Transform.position = fragment.EndPosition;
                        fragment.Transform.rotation = fragment.EndRotation;
                        fragment.Settled = true;
                    }
                }
            }

            if (age >= LifetimeSeconds)
                Destroy(gameObject);
        }
    }
}
