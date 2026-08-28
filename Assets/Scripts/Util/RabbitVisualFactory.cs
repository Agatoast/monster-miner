using MonsterMiner.Combat;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class RabbitVisualFactory
    {
        const string ResourcePath = "Models/Creatures/rabbit";
        const float TargetBodyHeight = WorldScale.CharacterHeightUnits * 0.42f;

        public static GameObject CreateMonster(Vector3 position, float scaleMultiplier, string displayName)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: rabbit prefab not found at Resources/{ResourcePath}.");
                return null;
            }

            var rabbit = Object.Instantiate(prefab);
            rabbit.name = displayName;
            rabbit.transform.position = position;
            rabbit.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float prefabHeight = EstimatePrefabHeight(rabbit);
            float scale = prefabHeight > 0.01f
                ? TargetBodyHeight / prefabHeight * scaleMultiplier
                : scaleMultiplier;
            rabbit.transform.localScale = Vector3.one * scale;

            PrepareForGameplay(rabbit);
            return rabbit;
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

            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || behaviour is RabbitLocomotion)
                    continue;

                if (behaviour.GetType().Name == "AnimatorParamatersChange")
                    behaviour.enabled = false;
            }

            foreach (var childCamera in root.GetComponentsInChildren<Camera>(true))
                childCamera.gameObject.SetActive(false);

            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                animator.applyRootMotion = false;

            if (root.GetComponent<RabbitLocomotion>() == null)
                root.AddComponent<RabbitLocomotion>();

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
            float radius = Mathf.Max(0.1f, Mathf.Max(Mathf.Abs(localSize.x), Mathf.Abs(localSize.z)) * 0.22f);
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
