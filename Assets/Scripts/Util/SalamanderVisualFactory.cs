using MonsterMiner.Combat;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class SalamanderVisualFactory
    {
        const string ResourcePath = "Models/Creatures/salamander";
        const float TargetBodyHeight = WorldScale.CharacterHeightUnits * 0.58f;

        public static GameObject CreateMonster(Vector3 position, float scaleMultiplier, string displayName)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: salamander prefab not found at Resources/{ResourcePath}.");
                return null;
            }

            var salamander = Object.Instantiate(prefab);
            salamander.name = displayName;
            salamander.transform.position = position;
            salamander.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float prefabHeight = EstimatePrefabHeight(salamander);
            float scale = prefabHeight > 0.01f
                ? TargetBodyHeight / prefabHeight * scaleMultiplier
                : scaleMultiplier;
            salamander.transform.localScale = Vector3.one * scale;

            PrepareForGameplay(salamander);
            return salamander;
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
            KnifeVisualFactory.ApplyUrpMaterials(root);

            foreach (var childCamera in root.GetComponentsInChildren<Camera>(true))
                childCamera.gameObject.SetActive(false);

            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                animator.applyRootMotion = false;

            if (root.GetComponent<SalamanderLocomotion>() == null)
                root.AddComponent<SalamanderLocomotion>();

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
            float radius = Mathf.Max(0.16f, Mathf.Max(Mathf.Abs(localSize.x), Mathf.Abs(localSize.z)) * 0.2f);
            float height = Mathf.Max(radius * 2.1f, Mathf.Abs(localSize.y));

            var capsule = root.GetComponent<CapsuleCollider>();
            if (capsule == null)
                capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.center = localCenter;
            capsule.height = height;
            capsule.radius = radius;
        }
    }
}
