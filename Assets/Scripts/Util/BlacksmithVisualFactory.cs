using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class BlacksmithVisualFactory
    {
        public const string BlacksmithObjectName = "Blacksmith";
        const string VillageHouseResourcePath = "Models/Architecture/ForestVillage02";
        const string PropResourceRoot = "Models/Blacksmith/";

        readonly struct PropPlacement
        {
            public readonly string ResourceName;
            public readonly float XFeet;
            public readonly float ZFeet;
            public readonly float YawDegrees;

            public PropPlacement(string resourceName, float xFeet, float zFeet, float yawDegrees = 0f)
            {
                ResourceName = resourceName;
                XFeet = xFeet;
                ZFeet = zFeet;
                YawDegrees = yawDegrees;
            }
        }

        static readonly PropPlacement[] CompoundPlacements =
        {
            new("Smithy", 0f, -10f),
            new("BasicHouse", -32f, -30f),
            new("Stable_2horse", -36f, 16f, 90f),
            new("fi_vil_farm_trough02_2m", -44f, 22f, 90f),
            new("fi_vil_farm_trough02_1m", -44f, 12f, 90f),

            new("fi_vil_deck_2_0mX4_0m", 12f, -16f, 90f),
            new("fi_vil_deck_pole_3m_A", 14f, -12f),

            new("fi_vil_fence02_gate_frame_gate", 20f, 0f, 90f),
            new("fi_vil_fence_parts_post01", -40f, -40f),
            new("fi_vil_fence_parts_post01", -40f, 40f),
            new("fi_vil_fence_parts_post01", 20f, 40f),
            new("fi_vil_fence_parts_post01", 20f, -40f),
            new("fi_vil_fence_parts_cross03_X1", 20f, 10f, 90f),
            new("fi_vil_fence_parts_cross03_X1", 20f, -10f, 90f),

            new("fi_vil_fence02_2_5m_B", -40f, 40f, 0f),
            new("fi_vil_fence02_2_5m_B", -32f, 40f, 0f),
            new("fi_vil_fence02_2_5m_B", -24f, 40f, 0f),
            new("fi_vil_fence02_2_5m_B", -16f, 40f, 0f),
            new("fi_vil_fence02_2_5m_B", -8f, 40f, 0f),
            new("fi_vil_fence02_2_5m_B", 0f, 40f, 0f),
            new("fi_vil_fence02_2_5m_B", 8f, 40f, 0f),
            new("fi_vil_fence02_2_5m_B", 16f, 40f, 0f),

            new("fi_vil_fence02_2_5m_C", -40f, -40f, 180f),
            new("fi_vil_fence02_2_5m_C", -32f, -40f, 180f),
            new("fi_vil_fence02_2_5m_C", -24f, -40f, 180f),
            new("fi_vil_fence02_2_5m_C", -16f, -40f, 180f),
            new("fi_vil_fence02_2_5m_C", -8f, -40f, 180f),
            new("fi_vil_fence02_2_5m_C", 0f, -40f, 180f),
            new("fi_vil_fence02_2_5m_C", 8f, -40f, 180f),
            new("fi_vil_fence02_2_5m_C", 16f, -40f, 180f),

            new("fi_vil_fence02_2_5m_B", -40f, -32f, 90f),
            new("fi_vil_fence02_2_5m_C", -40f, -24f, 90f),
            new("fi_vil_fence02_2_5m_B", -40f, -16f, 90f),
            new("fi_vil_fence02_2_5m_C", -40f, -8f, 90f),
            new("fi_vil_fence02_2_5m_B", -40f, 0f, 90f),
            new("fi_vil_fence02_2_5m_C", -40f, 8f, 90f),
            new("fi_vil_fence02_2_5m_B", -40f, 16f, 90f),
            new("fi_vil_fence02_2_5m_C", -40f, 24f, 90f),
            new("fi_vil_fence02_2_5m_B", -40f, 32f, 90f),

            new("fi_vil_fence01_1m_C", 20f, 24f, 90f),
            new("fi_vil_fence01_1m_C", 20f, 20f, 90f),
            new("fi_vil_fence01_1m_C", 20f, -20f, 90f),
            new("fi_vil_fence01_1m_C", 20f, -24f, 90f),
        };

        public static GameObject CreateAtLocalPoint(
            Transform parent,
            Vector3 localPosition,
            float floorWorldY,
            Quaternion localRotation)
        {
            var compoundRoot = new GameObject(BlacksmithObjectName).transform;
            compoundRoot.SetParent(parent, false);
            compoundRoot.localRotation = localRotation;
            compoundRoot.localPosition = Vector3.zero;

            SpawnVillageHouse(compoundRoot, new Vector3(0f, 0f, WorldScale.Feet(26f)));

            for (int i = 0; i < CompoundPlacements.Length; i++)
            {
                var placement = CompoundPlacements[i];
                SpawnProp(
                    compoundRoot,
                    placement.ResourceName,
                    FeetPlacement(placement.XFeet, placement.ZFeet),
                    Quaternion.Euler(0f, placement.YawDegrees, 0f));
            }

            KnifeVisualFactory.ApplyUrpMaterials(compoundRoot.gameObject);
            StripImportedColliders(compoundRoot.gameObject);
            AlignBaseCenterToLocalPoint(compoundRoot.gameObject, parent, localPosition);
            FloorAnchor.SnapBottomToFloor(compoundRoot.gameObject, floorWorldY, restOffset: 0f);
            return compoundRoot.gameObject;
        }

        static Vector3 FeetPlacement(float xFeet, float zFeet) =>
            new Vector3(WorldScale.Feet(xFeet), 0f, WorldScale.Feet(zFeet));

        static void SpawnVillageHouse(Transform parent, Vector3 localPosition)
        {
            var prefab = Resources.Load<GameObject>(VillageHouseResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"Monster Miner: village house prefab not found at Resources/{VillageHouseResourcePath}.");
                return;
            }

            var house = Object.Instantiate(prefab, parent, false);
            house.name = "ForestVillage02";
            house.transform.localPosition = localPosition;
            house.transform.localRotation = Quaternion.identity;
            SnapBottomToLocalGround(house, parent);
        }

        static GameObject SpawnProp(
            Transform parent,
            string resourceName,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            var prefab = Resources.Load<GameObject>(PropResourceRoot + resourceName);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"Monster Miner: Blacksmith prop not found at Resources/{PropResourceRoot}{resourceName}.");
                return null;
            }

            var prop = Object.Instantiate(prefab, parent, false);
            prop.name = resourceName;
            prop.transform.localPosition = localPosition;
            prop.transform.localRotation = localRotation;
            SnapBottomToLocalGround(prop, parent);
            return prop;
        }

        static void SnapBottomToLocalGround(GameObject prop, Transform parent)
        {
            Physics.SyncTransforms();
            if (!TryGetRendererBounds(prop, out var bounds))
                return;

            Vector3 bottomLocal = parent.InverseTransformPoint(
                new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            prop.transform.localPosition += new Vector3(0f, -bottomLocal.y, 0f);
            Physics.SyncTransforms();
        }

        static void AlignBaseCenterToLocalPoint(GameObject building, Transform parent, Vector3 targetLocal)
        {
            Physics.SyncTransforms();
            if (!TryGetRendererBounds(building, out var bounds))
            {
                building.transform.localPosition = targetLocal;
                return;
            }

            Vector3 baseCenterLocal = parent.InverseTransformPoint(
                new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            building.transform.localPosition += targetLocal - baseCenterLocal;
        }

        static void StripImportedColliders(GameObject root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null)
                    continue;

                if (Application.isPlaying)
                    Object.DestroyImmediate(collider);
                else
                    Object.Destroy(collider);
            }

            Physics.SyncTransforms();
        }

        static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
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
