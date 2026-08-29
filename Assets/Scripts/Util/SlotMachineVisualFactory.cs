using System.Reflection;
using ithappy.Casino;
using MonsterMiner.Core;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class SlotMachineVisualFactory
    {
        const string PrefabResourcePath = "Models/Props/interactive_slot_machine";
        const string NameplateTextureResourcePath = "Textures/SlotMachine/screens_1_monster";
        const float TargetHeight = 4f;

        public static GameObject CreateShopSlotMachine(
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            float floorWorldY)
        {
            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: slot machine prefab not found at Resources/{PrefabResourcePath}.");
                return null;
            }

            var slotMachine = Object.Instantiate(prefab, parent, false);
            slotMachine.name = "ShopSlotMachine";
            slotMachine.transform.localRotation = localRotation;
            slotMachine.transform.localScale = ComputeScale(slotMachine, TargetHeight);
            slotMachine.transform.localPosition = localPosition;
            FloorAnchor.SnapBottomToFloor(slotMachine, floorWorldY);

            DisableAutoPlay(slotMachine);
            HideCharacterPoint(slotMachine);
            EnsureInteractionCollider(slotMachine);
            ApplyMonsterMinerNameplates(slotMachine);
            ApplyMonsterMinerReelArt(slotMachine);

            return slotMachine;
        }

        static void ApplyMonsterMinerNameplates(GameObject slotMachineRoot)
        {
            var nameplateTexture = Resources.Load<Texture2D>(NameplateTextureResourcePath);
            if (nameplateTexture == null)
            {
                Debug.LogWarning(
                    $"Monster Miner: slot nameplate texture not found at Resources/{NameplateTextureResourcePath}.");
                return;
            }

            Material template = null;
            foreach (var renderer in slotMachineRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (TryGetNameplateMaterial(renderer, out template))
                    break;
            }

            if (template == null)
            {
                Debug.LogWarning("Monster Miner: slot machine has no Screens_1 nameplate material slot.");
                return;
            }

            var nameplateMaterial = CreateNameplateMaterial(template, nameplateTexture);

            foreach (var renderer in slotMachineRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!TryReplaceNameplateMaterials(renderer, nameplateMaterial))
                    continue;
            }
        }

        static Material CreateNameplateMaterial(Material template, Texture2D nameplateTexture)
        {
            var nameplateMaterial = new Material(template);
            if (nameplateMaterial.HasProperty("_BaseMap"))
                nameplateMaterial.SetTexture("_BaseMap", nameplateTexture);

            if (nameplateMaterial.HasProperty("_MainTex"))
                nameplateMaterial.SetTexture("_MainTex", nameplateTexture);

            if (nameplateMaterial.HasProperty("_EmissionMap"))
            {
                nameplateMaterial.SetTexture("_EmissionMap", nameplateTexture);
                if (nameplateMaterial.HasProperty("_EmissionColor"))
                    nameplateMaterial.SetColor("_EmissionColor", Color.white);
            }

            return nameplateMaterial;
        }

        static bool TryGetNameplateMaterial(Renderer renderer, out Material template)
        {
            template = null;
            if (renderer == null)
                return false;

            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (!IsNameplateMaterial(materials[i]))
                    continue;

                template = materials[i];
                return true;
            }

            return false;
        }

        static bool TryReplaceNameplateMaterials(Renderer renderer, Material nameplateMaterial)
        {
            if (renderer == null || nameplateMaterial == null)
                return false;

            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                if (!IsNameplateMaterial(materials[i]))
                    continue;

                materials[i] = nameplateMaterial;
                changed = true;
            }

            if (!changed)
                return false;

            renderer.sharedMaterials = materials;
            return true;
        }

        static bool IsNameplateMaterial(Material material)
        {
            if (material == null)
                return false;

            if (material.name.IndexOf("Screens_1", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return UsesTextureNamed(material, "_BaseMap", "Screens_1")
                || UsesTextureNamed(material, "_MainTex", "Screens_1")
                || UsesTextureNamed(material, "_EmissionMap", "Screens_1");
        }

        static bool UsesTextureNamed(Material material, string propertyName, string textureNamePart)
        {
            if (!material.HasProperty(propertyName))
                return false;

            var texture = material.GetTexture(propertyName);
            return texture != null
                && texture.name.IndexOf(textureNamePart, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static void ApplyMonsterMinerReelArt(GameObject slotMachineRoot)
        {
            var database = GameContext.Instance?.Database;
            if (database == null)
                return;

            var atlas = SlotReelAtlasBuilder.Build(database);
            if (atlas == null)
                return;

            Material template = null;
            foreach (var renderer in slotMachineRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && renderer.name.Contains("Spin"))
                {
                    template = renderer.sharedMaterial;
                    break;
                }
            }

            if (template == null)
                return;

            var reelMaterial = new Material(template);
            if (reelMaterial.HasProperty("_BaseMap"))
            {
                reelMaterial.SetTexture("_BaseMap", atlas);
                reelMaterial.SetTextureScale("_BaseMap", Vector2.one);
                reelMaterial.SetTextureOffset("_BaseMap", Vector2.zero);
            }

            if (reelMaterial.HasProperty("_MainTex"))
            {
                reelMaterial.SetTexture("_MainTex", atlas);
                reelMaterial.SetTextureScale("_MainTex", Vector2.one);
                reelMaterial.SetTextureOffset("_MainTex", Vector2.zero);
            }

            if (reelMaterial.HasProperty("_EmissionMap"))
            {
                reelMaterial.SetTexture("_EmissionMap", atlas);
                reelMaterial.SetTextureScale("_EmissionMap", Vector2.one);
                reelMaterial.SetTextureOffset("_EmissionMap", Vector2.zero);
            }

            if (reelMaterial.HasProperty("_EmissionColor"))
                reelMaterial.SetColor("_EmissionColor", Color.white);

            foreach (var renderer in slotMachineRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.name.Contains("Spin"))
                    continue;

                renderer.sharedMaterial = reelMaterial;
            }

            RefreshSlotReelDriver(slotMachineRoot);
        }

        static void RefreshSlotReelDriver(GameObject slotMachineRoot)
        {
            var visual = GetVisual(slotMachineRoot);
            if (visual == null)
                return;

            MethodInfo initializeReels = typeof(PresetUVSlotMachine).GetMethod(
                "InitializeReels",
                BindingFlags.Instance | BindingFlags.NonPublic);
            initializeReels?.Invoke(visual, null);

            MethodInfo previewPreset = typeof(PresetUVSlotMachine).GetMethod(
                "PreviewPreset",
                BindingFlags.Instance | BindingFlags.NonPublic);
            previewPreset?.Invoke(visual, null);
        }

        public static PresetUVSlotMachine GetVisual(GameObject slotMachineRoot)
        {
            return slotMachineRoot != null
                ? slotMachineRoot.GetComponentInChildren<PresetUVSlotMachine>(true)
                : null;
        }

        static Vector3 ComputeScale(GameObject instance, float targetHeight)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return Vector3.one;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float height = bounds.size.y;
            if (height < 0.001f)
                return Vector3.one;

            float scale = targetHeight / height;
            return Vector3.one * scale;
        }

        static void DisableAutoPlay(GameObject slotMachineRoot)
        {
            var visual = GetVisual(slotMachineRoot);
            if (visual == null)
                return;

            FieldInfo autoPlayField = typeof(PresetUVSlotMachine).GetField(
                "autoPlay",
                BindingFlags.Instance | BindingFlags.NonPublic);
            autoPlayField?.SetValue(visual, false);
        }

        static void HideCharacterPoint(GameObject slotMachineRoot)
        {
            var characterPoint = slotMachineRoot.transform.Find("CharacterPoint");
            if (characterPoint != null)
                characterPoint.gameObject.SetActive(false);
        }

        static void EnsureInteractionCollider(GameObject slotMachineRoot)
        {
            foreach (var collider in slotMachineRoot.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);

            var box = slotMachineRoot.AddComponent<BoxCollider>();
            FitCabinetBoxCollider(slotMachineRoot, box);
        }

        static void FitCabinetBoxCollider(GameObject root, BoxCollider collider, float padding = 0.05f)
        {
            if (collider == null)
                return;

            var meshFilter = root.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                collider.center = meshBounds.center;
                collider.size = meshBounds.size + Vector3.one * (padding * 2f);
                return;
            }

            var cabinetRenderer = root.GetComponent<Renderer>();
            if (cabinetRenderer != null)
            {
                FitBoxColliderToRenderer(root.transform, cabinetRenderer, collider, padding);
                return;
            }

            FitBoxColliderToCabinetRenderers(root, collider, padding);
        }

        static void FitBoxColliderToCabinetRenderers(GameObject root, BoxCollider collider, float padding)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds worldBounds = default;

            foreach (var renderer in renderers)
            {
                if (!IsCabinetRenderer(renderer))
                    continue;

                if (!hasBounds)
                {
                    worldBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
                return;

            Transform transform = root.transform;
            collider.center = transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = transform.InverseTransformVector(worldBounds.size);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            collider.size = localSize + Vector3.one * (padding * 2f);
        }

        static void FitBoxColliderToRenderer(Transform root, Renderer renderer, BoxCollider collider, float padding)
        {
            Bounds worldBounds = renderer.bounds;
            collider.center = root.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = root.InverseTransformVector(worldBounds.size);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            collider.size = localSize + Vector3.one * (padding * 2f);
        }

        static bool IsCabinetRenderer(Renderer renderer)
        {
            if (renderer == null || renderer is ParticleSystemRenderer)
                return false;

            Transform current = renderer.transform;
            while (current != null)
            {
                string name = current.name;
                if (name.Contains("Spin")
                    || name.Contains("Confetti")
                    || name.Contains("CharacterPoint"))
                {
                    return false;
                }

                current = current.parent;
            }

            return true;
        }
    }
}
