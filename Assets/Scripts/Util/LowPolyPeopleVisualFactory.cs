using MonsterMiner.Economy;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class LowPolyPeopleVisualFactory
    {
        const string NormalManResourcePath = "Models/People/normal_man";
        const string StrongManResourcePath = "Models/People/strong_man_a";
        const string RightHandBoneName = "Hand.R";

        public static GameObject CreateShopkeeper(Transform parent, Vector3 localPosition, Quaternion localRotation, float floorWorldY)
        {
            var character = CreateCharacter(
                NormalManResourcePath,
                "Shopkeeper",
                parent,
                localPosition,
                localRotation,
                floorWorldY);
            if (character == null)
                return null;

            character.AddComponent<Shopkeeper>();

            var interactCollider = character.AddComponent<BoxCollider>();
            FitBoxColliderToRenderers(character, interactCollider);
            character.AddComponent<ShopSellStation>();
            return character;
        }

        public static GameObject CreateMinerNpc(Transform parent, Vector3 localPosition, Quaternion localRotation, float floorWorldY)
        {
            var character = CreateCharacter(
                StrongManResourcePath,
                "MinerNpc",
                parent,
                localPosition,
                localRotation,
                floorWorldY);
            if (character == null)
                return null;

            AttachPickaxeToRightHand(character.transform);

            var interactCollider = character.AddComponent<BoxCollider>();
            FitBoxColliderToCharacterBody(character, interactCollider);
            interactCollider.isTrigger = true;
            character.AddComponent<MinerQuestNpc>();
            return character;
        }

        static GameObject CreateCharacter(
            string resourcePath,
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            float floorWorldY)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: character prefab not found at Resources/{resourcePath}.");
                return null;
            }

            var character = Object.Instantiate(prefab, parent, false);
            character.name = objectName;
            character.transform.localPosition = localPosition;
            character.transform.localRotation = localRotation;
            ApplyUrpMaterials(character);
            DisableColliders(character);
            FloorAnchor.SnapBottomToFloor(character, floorWorldY);

            var animator = character.GetComponent<Animator>();
            if (animator != null)
                animator.SetTrigger("idle");

            return character;
        }

        static void AttachPickaxeToRightHand(Transform characterRoot)
        {
            Transform hand = FindDeepChild(characterRoot, RightHandBoneName);
            if (hand == null)
            {
                var fallback = new GameObject("RightHandPickaxeAnchor").transform;
                fallback.SetParent(characterRoot, false);
                fallback.localPosition = new Vector3(0.34f, 1.05f, 0.12f);
                fallback.localRotation = Quaternion.Euler(10f, 0f, 0f);
                hand = fallback;
            }

            PickaxeVisualFactory.CreateNpcHeldPickaxe(hand);
        }

        static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindDeepChild(parent.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        static void FitBoxColliderToCharacterBody(GameObject character, BoxCollider collider, float padding = 0.08f)
        {
            var renderers = character.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0 || collider == null)
                return;

            bool hasBounds = false;
            Bounds worldBounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (IsAttachedItemRenderer(renderers[i]))
                    continue;

                if (!hasBounds)
                {
                    worldBounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(renderers[i].bounds);
                }
            }

            if (!hasBounds)
                return;

            var transform = character.transform;
            Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = transform.InverseTransformVector(worldBounds.size);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

            collider.center = localCenter;
            collider.size = localSize + Vector3.one * (padding * 2f);
        }

        static bool IsAttachedItemRenderer(Renderer renderer)
        {
            if (renderer == null)
                return false;

            Transform current = renderer.transform;
            while (current != null)
            {
                string name = current.name;
                if (name.Contains("Pickaxe") || name.Contains("NpcPickaxe") || name.Contains("Knife"))
                    return true;
                current = current.parent;
            }

            return false;
        }

        static void FitBoxColliderToRenderers(GameObject character, BoxCollider collider, float padding = 0.08f)
        {
            var renderers = character.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0 || collider == null)
                return;

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                worldBounds.Encapsulate(renderers[i].bounds);

            var transform = character.transform;
            Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = transform.InverseTransformVector(worldBounds.size);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

            collider.center = localCenter;
            collider.size = localSize + Vector3.one * (padding * 2f);
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

                if (material.HasProperty("_BaseColor") && source.HasProperty("_Color"))
                    material.SetColor("_BaseColor", source.color);

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
