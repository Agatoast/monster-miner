using UnityEngine;

namespace MonsterMiner.Player
{
    public class VrHandsVisualFactory
    {
        const string PrefabResourcePath = "Models/Hands/vr_hands";
        static readonly Vector3 HandsLocalPosition = new Vector3(0f, -0.24f, 0.38f);
        static readonly Vector3 HandsLocalEuler = Vector3.zero;
        const float HandsLocalScale = 0.72f;
        static readonly Vector3 PinchGripLocalOffset = new Vector3(-0.012f, -0.018f, 0.024f);
        const float KnifeStabScreenPixels = 600f;

        readonly Transform cameraTransform;
        GameObject handsInstance;
        Transform rightArmRoot;
        Transform rightHandAnchor;
        Transform leftHandAnchor;
        Transform pickaxeGripAnchor;
        Transform knifeGripAnchor;
        Transform spearGripAnchor;
        SkinnedMeshRenderer meshRenderer;
        Material handsMaterial;
        Quaternion rightArmRestRotation;
        Vector3 rightArmRestLocalPosition;
        Vector3 rightArmKnifeStabRestLocalPosition;
        Quaternion rightArmKnifeStabRestLocalRotation;
        bool hasKnifeStabRestPose;
        Quaternion pickaxeGripRestRotation;
        Vector3 pickaxeGripRestLocalPosition;
        Vector3 handsRestLocalPosition;
        Transform handsRestParent;
        Quaternion handsRestLocalRotation;
        bool attachedToSteeringWheel;

        public Transform RightArmRoot => rightArmRoot != null ? rightArmRoot : handsInstance?.transform;
        public Transform RightHandAnchor => rightHandAnchor != null ? rightHandAnchor : RightArmRoot;
        public Transform LeftHandAnchor => leftHandAnchor != null ? leftHandAnchor : RightHandAnchor;
        public Transform PickaxeGripAnchor => pickaxeGripAnchor != null ? pickaxeGripAnchor : RightHandAnchor;
        public Transform KnifeGripAnchor => knifeGripAnchor != null ? knifeGripAnchor : RightHandAnchor;
        public Transform SpearGripAnchor => spearGripAnchor != null ? spearGripAnchor : KnifeGripAnchor;
        public bool HasMesh => handsInstance != null;

        public void SetVisible(bool visible)
        {
            if (handsInstance != null)
                handsInstance.SetActive(visible);
        }

        public void ApplySteeringGrip(float wheelZDegrees)
        {
            if (handsInstance == null)
                return;

            SetAnimationEnabled(false);
            handsInstance.SetActive(true);
            handsInstance.transform.SetParent(cameraTransform, false);
            handsInstance.transform.localPosition = new Vector3(0f, -0.10f, 0.43f);
            handsInstance.transform.localRotation = Quaternion.Euler(18f, 0f, wheelZDegrees);
            attachedToSteeringWheel = true;
        }

        public void DetachFromSteeringWheel()
        {
            if (handsInstance == null || !attachedToSteeringWheel)
                return;

            handsInstance.transform.SetParent(handsRestParent != null ? handsRestParent : cameraTransform, false);
            handsInstance.transform.localPosition = handsRestLocalPosition;
            handsInstance.transform.localRotation = handsRestLocalRotation;
            attachedToSteeringWheel = false;
            SetAnimationEnabled(true);
        }

        public VrHandsVisualFactory(Transform cameraTransform)
        {
            this.cameraTransform = cameraTransform;
            BuildHands();
        }

        void BuildHands()
        {
            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: VR hands prefab not found at Resources/{PrefabResourcePath}.");
                return;
            }

            handsInstance = Object.Instantiate(prefab, cameraTransform);
            handsInstance.name = "PlayerHands";
            handsInstance.transform.localPosition = HandsLocalPosition;
            handsInstance.transform.localRotation = Quaternion.Euler(HandsLocalEuler);
            handsInstance.transform.localScale = Vector3.one * HandsLocalScale;
            handsRestParent = cameraTransform;
            handsRestLocalPosition = HandsLocalPosition;
            handsRestLocalRotation = Quaternion.Euler(HandsLocalEuler);

            DisableColliders(handsInstance);

            rightArmRoot = FindDeepChild(handsInstance.transform, "J_Right");
            rightHandAnchor = FindDeepChild(handsInstance.transform, "J_Right_Hand");
            leftHandAnchor = FindDeepChild(handsInstance.transform, "J_Left_Hand");
            if (rightArmRoot != null)
            {
                rightArmRestRotation = rightArmRoot.localRotation;
                rightArmRestLocalPosition = rightArmRoot.localPosition;
            }

            pickaxeGripAnchor = CreateGripAnchor("PickaxeGripAnchor");
            knifeGripAnchor = CreateGripAnchor("KnifeGripAnchor");
            spearGripAnchor = CreateFingerPinchAnchor("SpearGripAnchor");
            if (pickaxeGripAnchor != null)
            {
                pickaxeGripRestRotation = pickaxeGripAnchor.localRotation;
                pickaxeGripRestLocalPosition = pickaxeGripAnchor.localPosition;
            }

            meshRenderer = handsInstance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.updateWhenOffscreen = true;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }
        }

        Transform CreateGripAnchor(string anchorName)
        {
            if (rightHandAnchor == null)
                return null;

            var thumb = FindDeepChild(rightHandAnchor, "J_Right_HandThumb1");
            var index = FindDeepChild(rightHandAnchor, "J_Right_HandIndex1");

            var anchorGo = new GameObject(anchorName);
            var anchor = anchorGo.transform;
            anchor.SetParent(rightHandAnchor, false);
            anchor.localRotation = Quaternion.identity;

            if (thumb != null && index != null)
                anchor.localPosition = (thumb.localPosition + index.localPosition) * 0.5f + PinchGripLocalOffset;
            else
                anchor.localPosition = PinchGripLocalOffset;

            return anchor;
        }

        Transform CreateFingerPinchAnchor(string anchorName)
        {
            if (rightHandAnchor == null)
                return null;

            var thumb = FindDeepChild(rightHandAnchor, "J_Right_HandThumb2");
            var index = FindDeepChild(rightHandAnchor, "J_Right_HandIndex2");
            if (thumb == null)
                thumb = FindDeepChild(rightHandAnchor, "J_Right_HandThumb1");
            if (index == null)
                index = FindDeepChild(rightHandAnchor, "J_Right_HandIndex1");

            var anchorGo = new GameObject(anchorName);
            var anchor = anchorGo.transform;
            anchor.SetParent(rightHandAnchor, false);
            anchor.localRotation = Quaternion.identity;

            if (thumb != null && index != null)
            {
                Vector3 pinchWorld = (thumb.position + index.position) * 0.5f;
                anchor.position = pinchWorld;
            }
            else
                anchor.localPosition = PinchGripLocalOffset;

            return anchor;
        }

        public Vector3 GetScreenWorldOffset(Camera camera, Vector3 worldReference, Vector3 pixelOffset)
        {
            return ScreenPixelsToWorldOffset(camera, worldReference, pixelOffset);
        }

        public void SetAnimationEnabled(bool enabled)
        {
            if (handsInstance == null)
                return;

            foreach (var animator in handsInstance.GetComponentsInChildren<Animator>(true))
                animator.enabled = enabled;
        }

        public void BeginKnifeStab()
        {
            if (rightArmRoot == null)
                return;

            rightArmKnifeStabRestLocalPosition = rightArmRoot.localPosition;
            rightArmKnifeStabRestLocalRotation = rightArmRoot.localRotation;
            hasKnifeStabRestPose = true;
            SetAnimationEnabled(false);
        }

        public void ApplyKnifeStab(Camera camera, float curve)
        {
            if (camera == null || curve <= 0f || rightArmRoot == null || !hasKnifeStabRestPose)
                return;

            Transform armParent = rightArmRoot.parent;
            Vector3 referenceWorld = armParent != null
                ? armParent.TransformPoint(rightArmKnifeStabRestLocalPosition)
                : rightArmRoot.position;

            float depth = Vector3.Dot(referenceWorld - camera.transform.position, camera.transform.forward);
            if (depth <= 0.001f)
                return;

            float worldPerPixel = 2f * depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) / camera.pixelHeight;
            Vector3 worldStabOffset = camera.transform.forward * (curve * KnifeStabScreenPixels * worldPerPixel);
            Vector3 localStabOffset = armParent != null
                ? armParent.InverseTransformDirection(worldStabOffset)
                : worldStabOffset;

            rightArmRoot.localPosition = rightArmKnifeStabRestLocalPosition + localStabOffset;
            rightArmRoot.localRotation = rightArmKnifeStabRestLocalRotation;
        }

        public void EndKnifeStab()
        {
            if (rightArmRoot != null && hasKnifeStabRestPose)
            {
                rightArmRoot.localPosition = rightArmKnifeStabRestLocalPosition;
                rightArmRoot.localRotation = rightArmKnifeStabRestLocalRotation;
            }

            hasKnifeStabRestPose = false;
            SetAnimationEnabled(true);
        }

        public void ApplyPickaxeForwardSwing(Camera camera, float normalizedTime)
        {
            float curve = Mathf.Sin(normalizedTime * Mathf.PI);
            float armFollow = curve * 22f;

            if (rightArmRoot != null && camera != null)
            {
                Vector3 armAxisLocal = rightArmRoot.parent.InverseTransformDirection(camera.transform.right);
                if (armAxisLocal.sqrMagnitude > 0.0001f)
                {
                    armAxisLocal.Normalize();
                    rightArmRoot.localRotation = rightArmRestRotation * Quaternion.AngleAxis(armFollow, armAxisLocal);
                }
            }

            if (pickaxeGripAnchor != null)
            {
                pickaxeGripAnchor.localRotation = pickaxeGripRestRotation;
                pickaxeGripAnchor.localPosition = pickaxeGripRestLocalPosition;
            }
        }

        public void ResetPickaxeSwing()
        {
            if (rightArmRoot != null)
                rightArmRoot.localRotation = rightArmRestRotation;

            if (pickaxeGripAnchor != null)
            {
                pickaxeGripAnchor.localRotation = pickaxeGripRestRotation;
                pickaxeGripAnchor.localPosition = pickaxeGripRestLocalPosition;
            }
        }

        public void ApplySwingRotation(float angleDegrees)
        {
            if (rightArmRoot == null)
                return;

            rightArmRoot.localRotation = rightArmRestRotation * Quaternion.Euler(angleDegrees, 0f, 0f);
        }

        public void SetGloveColor(Color color)
        {
            if (meshRenderer == null)
                return;

            if (handsMaterial == null)
                handsMaterial = meshRenderer.material;

            var tint = Color.Lerp(Color.white, color, 0.45f);
            if (handsMaterial.HasProperty("_BaseColor"))
                handsMaterial.SetColor("_BaseColor", tint);
            else
                handsMaterial.color = tint;
        }

        static Vector3 ScreenPixelsToWorldOffset(Camera camera, Vector3 worldReference, Vector3 pixelOffset)
        {
            float depth = Vector3.Dot(worldReference - camera.transform.position, camera.transform.forward);
            if (depth <= 0.001f)
                return Vector3.zero;

            float worldPerPixel = 2f * depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) / camera.pixelHeight;
            // X/Y = screen plane; +Z = out of screen toward the player.
            return camera.transform.right * (pixelOffset.x * worldPerPixel)
                 + camera.transform.up * (pixelOffset.y * worldPerPixel)
                 - camera.transform.forward * (pixelOffset.z * worldPerPixel);
        }

        static void DisableColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
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
    }
}
