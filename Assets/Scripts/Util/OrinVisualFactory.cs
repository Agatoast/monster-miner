using MonsterMiner.Economy;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class OrinVisualFactory
    {
        public const string CharacterName = "Orin";

        const string OrinModelResourcePath = "Models/Props/thor_god";
        const string MjolnirResourcePath = "Models/Props/mjolnir";
        const string RightHandBoneName = "DEF-hand.R";
        const float TargetBodyHeight = WorldScale.CharacterHeightUnits * 0.75f;

        static readonly Vector3 MjolnirHandLocalPosition = new Vector3(0f, 0.018f, 0.03f);
        static readonly Vector3 MjolnirHandLocalEuler = new Vector3(0f, 90f, -90f);
        static readonly Vector3 MjolnirHandLocalScale = Vector3.one * 0.01f;

        public static GameObject CreateAtLocalPoint(
            Transform parent,
            Vector3 localPosition,
            float floorWorldY,
            Quaternion localRotation)
        {
            var modelPrefab = Resources.Load<GameObject>(OrinModelResourcePath);
            if (modelPrefab == null)
            {
                Debug.LogWarning($"Monster Miner: Orin model prefab not found at Resources/{OrinModelResourcePath}.");
                return null;
            }

            var orin = Object.Instantiate(modelPrefab, parent, false);
            orin.name = CharacterName;
            orin.transform.localRotation = localRotation;
            orin.transform.localPosition = localPosition;

            float prefabHeight = EstimatePrefabHeight(orin);
            if (prefabHeight > 0.01f)
            {
                float scale = TargetBodyHeight / prefabHeight;
                orin.transform.localScale = Vector3.one * scale;
            }

            PrepareOrin(orin);
            AttachMjolnir(orin);
            AlignFeetToLocalPoint(orin, parent, localPosition);
            FloorAnchor.SnapBottomToFloor(orin, floorWorldY, restOffset: 0f);
            ConfigureInteract(orin);
            return orin;
        }

        static void ConfigureInteract(GameObject orin)
        {
            StripImportedColliders(orin);
            StripRigidbodies(orin);

            var interactCollider = orin.AddComponent<BoxCollider>();
            FitInteractCollider(orin, interactCollider);
            interactCollider.isTrigger = true;

            if (orin.GetComponent<OrinQuestNpc>() == null)
                orin.AddComponent<OrinQuestNpc>();
        }

        static void FitInteractCollider(GameObject orin, BoxCollider collider, float padding = 0.08f)
        {
            var renderers = orin.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0 || collider == null)
                return;

            bool hasBounds = false;
            Bounds worldBounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (IsMjolnirRenderer(renderers[i]))
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

            var transform = orin.transform;
            Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = transform.InverseTransformVector(worldBounds.size);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

            collider.center = localCenter;
            collider.size = localSize + Vector3.one * (padding * 2f);
        }

        static bool IsMjolnirRenderer(Renderer renderer)
        {
            if (renderer == null)
                return false;

            Transform current = renderer.transform;
            while (current != null)
            {
                if (current.name == "Mjolnir")
                    return true;
                current = current.parent;
            }

            return false;
        }

        static void PrepareOrin(GameObject orin)
        {
            KnifeVisualFactory.ApplyUrpMaterials(orin);

            foreach (var animator in orin.GetComponentsInChildren<Animator>(true))
            {
                animator.applyRootMotion = false;
                animator.enabled = false;
            }

            foreach (var skinnedRenderer in orin.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                skinnedRenderer.updateWhenOffscreen = true;

            ApplyHammerHoldPose(orin.transform);
            StripImportedColliders(orin);
        }

        static void AttachMjolnir(GameObject orin)
        {
            var mjolnirPrefab = Resources.Load<GameObject>(MjolnirResourcePath);
            if (mjolnirPrefab == null)
            {
                Debug.LogWarning($"Monster Miner: Mjolnir prefab not found at Resources/{MjolnirResourcePath}.");
                return;
            }

            Transform hand = FindBone(orin.transform, RightHandBoneName);
            if (hand == null)
            {
                Debug.LogWarning("Monster Miner: Orin right hand bone not found for Mjolnir attachment.");
                return;
            }

            var mjolnir = Object.Instantiate(mjolnirPrefab, hand, false);
            mjolnir.name = "Mjolnir";
            mjolnir.transform.localPosition = MjolnirHandLocalPosition;
            mjolnir.transform.localRotation = Quaternion.Euler(MjolnirHandLocalEuler);
            mjolnir.transform.localScale = MjolnirHandLocalScale;
            KnifeVisualFactory.ApplyUrpMaterials(mjolnir);
            StripImportedColliders(mjolnir);
        }

        static void ApplyHammerHoldPose(Transform root)
        {
            RotateBoneLocal(root, "DEF-upper_arm.R", new Vector3(-40f, 15f, -20f));
            RotateBoneLocal(root, "DEF-forearm.R", new Vector3(-65f, 0f, 0f));
            RotateBoneLocal(root, "DEF-hand.R", new Vector3(10f, 0f, 0f));
            Physics.SyncTransforms();
        }

        static void AlignFeetToLocalPoint(GameObject character, Transform parent, Vector3 targetLocal)
        {
            Physics.SyncTransforms();
            if (!TryGetRendererBounds(character, out var bounds))
                return;

            Vector3 feetWorld = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Vector3 feetLocal = parent.InverseTransformPoint(feetWorld);
            Vector3 delta = targetLocal - feetLocal;
            delta.y = 0f;
            character.transform.localPosition += delta;
            Physics.SyncTransforms();
        }

        static float EstimatePrefabHeight(GameObject root)
        {
            if (!TryGetRendererBounds(root, out var bounds))
                return 1f;

            return bounds.size.y;
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

        static void StripRigidbodies(GameObject root)
        {
            var rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                var rigidbody = rigidbodies[i];
                if (rigidbody == null)
                    continue;

                if (Application.isPlaying)
                    Object.DestroyImmediate(rigidbody);
                else
                    Object.Destroy(rigidbody);
            }
        }

        static void StripColliders(GameObject root)
        {
            StripImportedColliders(root);
        }

        static Transform FindBone(Transform root, string boneName)
        {
            if (root.name == boneName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var match = FindBone(root.GetChild(i), boneName);
                if (match != null)
                    return match;
            }

            return null;
        }

        static void RotateBoneLocal(Transform root, string boneName, Vector3 eulerDelta)
        {
            var bone = FindBone(root, boneName);
            if (bone == null)
                return;

            bone.localRotation *= Quaternion.Euler(eulerDelta);
        }
    }
}
