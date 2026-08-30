using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class KnarrVisualFactory
    {
        const string ResourcePath = "Models/Props/d_knarr_wood";
        const float BoatInsetFromShoreFeet = 18f;

        public static GameObject CreateAtBeach(Transform lakeRoot, float waterLocalY)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: knarr boat mesh not found at Resources/{ResourcePath}.");
                return null;
            }

            if (lakeRoot == null)
                return null;

            var lakeCenter = LakeCatalog.GetCenterLocal();
            var beachCenter = LakeCatalog.GetBeachCenterContentLocal();
            float boatInset = WorldScale.Feet(BoatInsetFromShoreFeet);
            float boatZ = LakeCatalog.GetBeachNorthEdgeZ() - boatInset - lakeCenter.y;
            Vector3 localPosition = new Vector3(
                beachCenter.x - lakeCenter.x,
                waterLocalY + WorldScale.Feet(0.5f),
                boatZ);

            var boat = Object.Instantiate(prefab, lakeRoot, false);
            boat.name = "WarrensonsBoat";
            boat.transform.localPosition = localPosition;
            boat.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            KnifeVisualFactory.ApplyUrpMaterials(boat);
            StripImportedColliders(boat);
            AddHullCollider(boat);
            return boat;
        }

        const float WaterSurfaceDepressionFeet = 1.5f;

        static void StripImportedColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
        }

        static void AddHullCollider(GameObject boat)
        {
            if (!TryGetRendererBounds(boat, out var bounds))
                return;

            var box = boat.AddComponent<BoxCollider>();
            var root = boat.transform;
            Vector3 localCenter = root.InverseTransformPoint(bounds.center);
            Vector3 localSize = root.InverseTransformVector(bounds.size);
            box.center = localCenter;
            box.size = new Vector3(
                Mathf.Abs(localSize.x),
                Mathf.Abs(localSize.y),
                Mathf.Abs(localSize.z));
        }

        static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return false;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled)
                    continue;

                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }
    }
}
