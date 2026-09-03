using MonsterMiner.Combat;
using MonsterMiner.World;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonsterMiner.Util
{
    public static class TaipanVisualFactory
    {
        const string PrefabEditorPath = "Assets/Quirky Series Ultimate/FREE/Prefabs/Taipan.prefab";
        const string PrefabResourcePath = "Models/Creatures/taipan";
        const float TargetLengthFeet = 50f;

        public static GameObject CreateMonster(Vector3 position, float scaleMultiplier, string displayName)
        {
            GameObject prefab = LoadPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("Monster Miner: Taipan prefab missing.");
                return null;
            }

            var taipan = Object.Instantiate(prefab);
            taipan.name = displayName;
            taipan.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

            if (taipan.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                Debug.LogWarning("Monster Miner: Taipan prefab has no renderers.");
                Object.Destroy(taipan);
                return null;
            }

            float targetLength = WorldScale.Feet(TargetLengthFeet);
            float prefabLength = EstimatePrefabLength(taipan);
            float scale = prefabLength > 0.01f
                ? targetLength / prefabLength * scaleMultiplier
                : scaleMultiplier;
            taipan.transform.localScale = Vector3.one * scale;

            PrepareForGameplay(taipan);
            return taipan;
        }

        public static void PrepareAnimatedGroundSample(GameObject root)
        {
            if (root == null)
                return;

            var animator = root.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.Play("Idle_A", 0, 0f);
                animator.Update(0f);
            }

            Physics.SyncTransforms();
        }

        static float EstimatePrefabLength(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return 1f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 localSize = root.transform.InverseTransformVector(bounds.size);
            return Mathf.Max(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }

        static void PrepareForGameplay(GameObject root)
        {
            KnifeVisualFactory.ApplyUrpMaterials(root);

            foreach (var childCamera in root.GetComponentsInChildren<Camera>(true))
                childCamera.gameObject.SetActive(false);

            var lodGroup = root.GetComponent<LODGroup>();
            if (lodGroup != null)
                lodGroup.enabled = false;

            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                animator.applyRootMotion = false;

            ConfigureSkinnedMeshRenderers(root);

            if (root.GetComponent<TaipanLocomotion>() == null)
                root.AddComponent<TaipanLocomotion>();

            if (root.GetComponent<TaipanGroundAlign>() == null)
                root.AddComponent<TaipanGroundAlign>();

            RemoveImportedColliders(root);
            PrepareAnimatedGroundSample(root);
            EnsureGameplayCollider(root);

            var body = root.GetComponent<Rigidbody>();
            if (body == null)
                body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        static void ConfigureSkinnedMeshRenderers(GameObject root)
        {
            foreach (var skinnedMesh in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                bool isLod0 = skinnedMesh.gameObject.name.IndexOf("LOD0", System.StringComparison.OrdinalIgnoreCase) >= 0;
                skinnedMesh.gameObject.SetActive(isLod0);
                skinnedMesh.enabled = isLod0;
                if (isLod0)
                    skinnedMesh.updateWhenOffscreen = true;
            }
        }

        static void RemoveImportedColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
        }

        static void EnsureGameplayCollider(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = root.transform.InverseTransformVector(bounds.size);
            float absX = Mathf.Abs(localSize.x);
            float absZ = Mathf.Abs(localSize.z);
            float length = Mathf.Max(absX, absZ);
            float bellyThickness = Mathf.Max(WorldScale.Feet(1.5f), Mathf.Min(absX, absZ) * 0.35f);

            var box = root.GetComponent<BoxCollider>();
            if (box == null)
                box = root.AddComponent<BoxCollider>();

            if (absZ >= absX)
            {
                box.center = localCenter;
                box.size = new Vector3(Mathf.Max(absX, bellyThickness), bellyThickness, length);
            }
            else
            {
                box.center = localCenter;
                box.size = new Vector3(length, bellyThickness, Mathf.Max(absZ, bellyThickness));
            }

            box.isTrigger = false;
        }

        static GameObject LoadPrefab()
        {
            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab != null)
                return prefab;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabEditorPath);
#else
            return null;
#endif
        }
    }
}
