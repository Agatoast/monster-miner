using MonsterMiner.Combat;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class IguanaVisualFactory
    {
        const string ResourcePath = "Models/Creatures/iguana";
        const float TargetBodyHeight = WorldScale.CharacterHeightUnits * 0.55f;

        public static GameObject CreateMonster(Vector3 position, float scaleMultiplier, string displayName)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: iguana prefab not found at Resources/{ResourcePath}.");
                return null;
            }

            var iguana = Object.Instantiate(prefab);
            iguana.name = displayName;
            iguana.transform.position = position;
            iguana.transform.rotation = Quaternion.identity;

            float prefabHeight = EstimatePrefabHeight(iguana);
            float scale = prefabHeight > 0.01f
                ? TargetBodyHeight / prefabHeight * scaleMultiplier
                : scaleMultiplier;
            iguana.transform.localScale = Vector3.one * scale;

            PrepareForGameplay(iguana);
            return iguana;
        }

        static float EstimatePrefabHeight(GameObject root)
        {
            var capsule = root.GetComponent<CapsuleCollider>();
            if (capsule != null)
                return capsule.height * root.transform.lossyScale.y;

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

            foreach (var behaviour in root.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null)
                    continue;

                string typeName = behaviour.GetType().Name;
                if (typeName == "IguanaUserController" || typeName == "IguanaCameraScript")
                    behaviour.enabled = false;
            }

            foreach (var childCamera in root.GetComponentsInChildren<Camera>(true))
                childCamera.gameObject.SetActive(false);

            var animator = root.GetComponent<Animator>();
            if (animator != null)
                animator.applyRootMotion = false;

            if (root.GetComponent<IguanaLocomotion>() == null)
                root.AddComponent<IguanaLocomotion>();

            var body = root.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }
    }
}
