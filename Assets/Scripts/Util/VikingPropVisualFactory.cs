using MonsterMiner.Economy;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class VikingPropVisualFactory
    {
        const string TreeResourcePath = "Models/Props/h_b_tree_a";
        const string CharacterResourcePath = "Models/Props/i_1_brown";
        const float ArmForwardDegrees = 115f;
        const float ArmForwardMuscle = ArmForwardDegrees / 90f;
        const float ArmPronationDegrees = 75f;
        const float ArmPronationMuscle = ArmPronationDegrees / 90f;
        public static readonly Quaternion FaceNorthRotation = Quaternion.Euler(0f, -90f, 0f);
        public static readonly Quaternion CharacterWorldRotation = Quaternion.Euler(0f, 220f, 0f);

        public static GameObject CreateTreeAtLocalPoint(
            Transform parent,
            Vector3 localPosition,
            float floorWorldY,
            Quaternion localRotation)
        {
            return CreatePropAtLocalPoint(
                TreeResourcePath,
                "H_B_Tree_A",
                parent,
                localPosition,
                floorWorldY,
                localRotation,
                addCollision: true);
        }

        public static GameObject CreateCharacterAtLocalPoint(
            Transform parent,
            Vector3 localPosition,
            float floorWorldY,
            Quaternion localRotation,
            string objectName)
        {
            var prefab = Resources.Load<GameObject>(CharacterResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: Viking character prefab not found at Resources/{CharacterResourcePath}.");
                return null;
            }

            var character = Object.Instantiate(prefab, parent, false);
            character.name = objectName;
            character.transform.localRotation = localRotation;
            character.transform.localPosition = localPosition;
            ApplyStandingPose(character);
            KnifeVisualFactory.ApplyUrpMaterials(character);
            PrepareSkinnedRenderers(character);
            StripImportedColliders(character);
            AlignFeetToLocalPoint(character, parent, localPosition);
            FloorAnchor.SnapBottomToFloor(character, floorWorldY);
            AddTightCollision(character);
            character.AddComponent<JarlQuestNpc>();
            return character;
        }

        static void ApplyStandingPose(GameObject character)
        {
            var animator = character.GetComponentInChildren<Animator>();
            if (animator != null && animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman)
            {
                animator.enabled = true;
                var handler = new HumanPoseHandler(animator.avatar, animator.transform);
                var pose = new HumanPose();
                handler.GetHumanPose(ref pose);

                for (int i = 0; i < pose.muscles.Length; i++)
                {
                    switch (HumanTrait.MuscleName[i])
                    {
                        case "Left Arm Down-Up":
                        case "Right Arm Down-Up":
                            pose.muscles[i] = 1f;
                            break;
                        case "Left Arm Front-Back":
                        case "Right Arm Front-Back":
                            pose.muscles[i] = ArmForwardMuscle;
                            break;
                        case "Left Arm In-Out":
                        case "Right Arm In-Out":
                            pose.muscles[i] = 0f;
                            break;
                        case "Left Arm Roll":
                        case "Left Forearm Roll":
                            pose.muscles[i] = ArmPronationMuscle;
                            break;
                        case "Right Arm Roll":
                        case "Right Forearm Roll":
                            pose.muscles[i] = -ArmPronationMuscle;
                            break;
                    }
                }

                handler.SetHumanPose(ref pose);
                animator.enabled = false;
                Physics.SyncTransforms();
                return;
            }

            ApplyStandingPoseFallback(character.transform);
        }

        static void ApplyArmPronation(Transform characterRoot)
        {
            RotateBoneLocal(characterRoot, "mixamorig:LeftForeArm", new Vector3(0f, ArmPronationDegrees, 0f));
            RotateBoneLocal(characterRoot, "mixamorig:RightForeArm", new Vector3(0f, -ArmPronationDegrees, 0f));
        }

        static void ApplyStandingPoseFallback(Transform characterRoot)
        {
            foreach (var childAnimator in characterRoot.GetComponentsInChildren<Animator>(true))
                childAnimator.enabled = false;

            RotateBoneLocal(characterRoot, "mixamorig:LeftArm", new Vector3(0f, 0f, 90f));
            RotateBoneLocal(characterRoot, "mixamorig:RightArm", new Vector3(0f, 0f, -90f));
            RotateBoneLocal(characterRoot, "mixamorig:LeftArm", new Vector3(ArmForwardDegrees, 0f, 0f));
            RotateBoneLocal(characterRoot, "mixamorig:RightArm", new Vector3(ArmForwardDegrees, 0f, 0f));
            ApplyArmPronation(characterRoot);
            Physics.SyncTransforms();
        }

        static void RotateBoneLocal(Transform root, string boneName, Vector3 eulerDelta)
        {
            var bone = FindBone(root, boneName);
            if (bone == null)
                return;

            bone.localRotation *= Quaternion.Euler(eulerDelta);
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

        static void PrepareSkinnedRenderers(GameObject root)
        {
            foreach (var skinnedRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skinnedRenderer.updateWhenOffscreen = true;
            }
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

        static GameObject CreatePropAtLocalPoint(
            string resourcePath,
            string objectName,
            Transform parent,
            Vector3 localPosition,
            float floorWorldY,
            Quaternion localRotation,
            bool addCollision)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: Viking prop not found at Resources/{resourcePath}.");
                return null;
            }

            var prop = Object.Instantiate(prefab, parent, false);
            prop.name = objectName;
            prop.transform.localRotation = localRotation;
            prop.transform.localPosition = Vector3.zero;
            KnifeVisualFactory.ApplyUrpMaterials(prop);
            StripImportedColliders(prop);
            AlignCenterToLocalPoint(prop, parent, localPosition);
            FloorAnchor.SnapBottomToFloor(prop, floorWorldY);
            if (addCollision)
                AddTightCollision(prop);
            return prop;
        }

        static void AlignCenterToLocalPoint(GameObject prop, Transform parent, Vector3 targetLocal)
        {
            Physics.SyncTransforms();
            if (!TryGetRendererBounds(prop, out var bounds))
            {
                prop.transform.localPosition = targetLocal;
                return;
            }

            Vector3 centerLocal = parent.InverseTransformPoint(bounds.center);
            prop.transform.localPosition += targetLocal - centerLocal;
        }

        static void StripImportedColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
                Object.Destroy(collider);
            }
        }

        static void AddTightCollision(GameObject prop)
        {
            var box = prop.AddComponent<BoxCollider>();
            if (!TryGetRendererBounds(prop, out var bounds))
                return;

            var root = prop.transform;
            Vector3 localCenter = root.InverseTransformPoint(bounds.center);
            Vector3 localSize = root.InverseTransformVector(bounds.size);
            box.center = localCenter;
            box.size = new Vector3(
                Mathf.Abs(localSize.x),
                Mathf.Abs(localSize.y),
                Mathf.Abs(localSize.z));
        }

        public static bool TryGetLocalBounds(GameObject root, Transform parent, out Bounds localBounds)
        {
            localBounds = default;
            if (root == null || parent == null || !TryGetRendererBounds(root, out var worldBounds))
                return false;

            Vector3 minLocal = parent.InverseTransformPoint(worldBounds.min);
            Vector3 maxLocal = parent.InverseTransformPoint(worldBounds.max);
            localBounds.SetMinMax(minLocal, maxLocal);
            return true;
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
