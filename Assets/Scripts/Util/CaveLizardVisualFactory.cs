using MonsterMiner.Combat;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class CaveLizardVisualFactory
    {
        const string ResourcePath = "Models/Creatures/cave_lizard";
        const string MaterialResourcePath = "Materials/CaveLizard";
        const float TargetBodyHeight = WorldScale.CharacterHeightUnits * 0.5f;

        static Material caveLizardMaterial;

        public static GameObject CreateMonster(Vector3 position, float scaleMultiplier, string displayName)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: cave lizard prefab not found at Resources/{ResourcePath}.");
                return null;
            }

            var lizard = Object.Instantiate(prefab);
            lizard.name = displayName;
            lizard.transform.position = position;
            lizard.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            if (lizard.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                Debug.LogWarning("Monster Miner: cave lizard prefab has no renderers.");
                Object.Destroy(lizard);
                return null;
            }

            float prefabHeight = EstimatePrefabHeight(lizard);
            float scale = prefabHeight > 0.01f
                ? TargetBodyHeight / prefabHeight * scaleMultiplier
                : scaleMultiplier;
            lizard.transform.localScale = Vector3.one * scale;

            PrepareForGameplay(lizard);
            return lizard;
        }

        static float EstimatePrefabHeight(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return 1f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds.size.y;
        }

        static void PrepareForGameplay(GameObject root)
        {
            ApplyUrpMaterials(root);

            foreach (var childCamera in root.GetComponentsInChildren<Camera>(true))
                childCamera.gameObject.SetActive(false);

            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                animator.applyRootMotion = false;

            if (root.GetComponent<CaveLizardLocomotion>() == null)
                root.AddComponent<CaveLizardLocomotion>();

            EnsureCapsuleCollider(root);

            var body = root.GetComponent<Rigidbody>();
            if (body == null)
                body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        static void EnsureCapsuleCollider(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = root.transform.InverseTransformVector(bounds.size);
            float radius = Mathf.Max(0.12f, Mathf.Max(Mathf.Abs(localSize.x), Mathf.Abs(localSize.z)) * 0.22f);
            float height = Mathf.Max(radius * 2.1f, Mathf.Abs(localSize.y));

            var capsule = root.GetComponent<CapsuleCollider>();
            if (capsule == null)
                capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.center = localCenter;
            capsule.height = height;
            capsule.radius = radius;
        }

        static void ApplyUrpMaterials(GameObject root)
        {
            var material = GetCaveLizardMaterial();
            if (material == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = material;
                renderer.sharedMaterials = materials;
            }
        }

        static Material GetCaveLizardMaterial()
        {
            if (caveLizardMaterial != null)
                return caveLizardMaterial;

            caveLizardMaterial = Resources.Load<Material>(MaterialResourcePath);
            if (caveLizardMaterial != null)
                return caveLizardMaterial;

            var template = Resources.Load<Material>("Materials/DefaultSurface");
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (template == null && urpLit == null)
                return null;

            caveLizardMaterial = template != null ? new Material(template) : new Material(urpLit);
            return caveLizardMaterial;
        }
    }
}
