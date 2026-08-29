using MonsterMiner.Core;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class AngelWingsVisualFactory
    {
        const string ResourcePath = "Models/Wings/angel_wings";
        static readonly Vector3 EquippedLocalPosition = new Vector3(0f, 0.2f, -0.32f);
        static readonly Vector3 EquippedLocalEuler = new Vector3(0f, 180f, 0f);

        public static GameObject CreateOnGround(Transform parent, Vector3 floorContactPoint)
        {
            var wings = CreateWings("AngelWings", parent);
            if (wings == null)
                return null;

            FloorAnchor.PlaceOnFloor(wings, floorContactPoint, GameContext.Instance?.CavernBounds);
            wings.transform.position += Vector3.up * WorldScale.Feet(0.5f);
            Physics.SyncTransforms();
            AddGroundInteractCollider(wings);
            return wings;
        }

        public static GameObject CreateEquipped(Transform player)
        {
            var wings = CreateWings("EquippedAngelWings", player);
            if (wings == null)
                return null;

            ApplyEquippedLocalPose(wings.transform);
            return wings;
        }

        public static bool TryAttachWorldWingsToPlayer(GameObject worldWings, Transform player)
        {
            if (worldWings == null || player == null)
                return false;

            var pickup = worldWings.GetComponent<AngelWingsPickup>();
            if (pickup != null)
                Object.Destroy(pickup);

            DisableColliders(worldWings);
            worldWings.transform.SetParent(player, false);
            ApplyEquippedLocalPose(worldWings.transform);
            worldWings.name = "EquippedAngelWings";
            return true;
        }

        static void ApplyEquippedLocalPose(Transform wingsTransform)
        {
            wingsTransform.localPosition = EquippedLocalPosition;
            wingsTransform.localRotation = Quaternion.Euler(EquippedLocalEuler);
        }

        static GameObject CreateWings(string name, Transform parent)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: angel wings prefab not found at Resources/{ResourcePath}.");
                return null;
            }

            var wings = Object.Instantiate(prefab, parent, false);
            wings.name = name;
            ApplyUrpMaterials(wings);
            DisableColliders(wings);
            return wings;
        }

        static void AddGroundInteractCollider(GameObject wings)
        {
            var box = wings.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, WorldScale.Feet(3.5f), 0f);
            box.size = new Vector3(WorldScale.Feet(5f), WorldScale.Feet(7f), WorldScale.Feet(5f));
            box.isTrigger = true;
            wings.AddComponent<AngelWingsPickup>();
        }

        static void ApplyUrpMaterials(GameObject root)
        {
            var template = Resources.Load<Material>("Materials/DefaultSurface");
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (template == null && urpLit == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterial;
                if (source == null)
                    continue;

                var material = template != null ? new Material(template) : new Material(urpLit);
                var albedo = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
                if (albedo == null && source.HasProperty("_BaseMap"))
                    albedo = source.GetTexture("_BaseMap");

                if (albedo != null)
                {
                    if (material.HasProperty("_BaseMap"))
                        material.SetTexture("_BaseMap", albedo);
                    else if (material.HasProperty("_MainTex"))
                        material.SetTexture("_MainTex", albedo);
                }

                if (material.HasProperty("_BaseColor"))
                {
                    if (source.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                    else if (source.HasProperty("_Color"))
                        material.SetColor("_BaseColor", source.color);
                }

                renderer.sharedMaterial = material;
            }
        }

        static void DisableColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
        }
    }
}
