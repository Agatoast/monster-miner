using UnityEngine;

namespace MonsterMiner.Util
{
    public static class GrenadeVisualFactory
    {
        const string PrefabResourcePath = "Models/Tools/m67_grenade";

        static readonly Vector3 HeldLocalPosition = new Vector3(0.04f, -0.02f, 0.05f);
        static readonly Vector3 HeldLocalEuler = new Vector3(-20f, 180f, 90f);
        static readonly Vector3 HeldLocalScale = Vector3.one * 1.15f;
        static readonly Vector3 WorldScaleMultiplier = Vector3.one * 1.15f;

        static GameObject templateRoot;

        public static GameObject CreateHeldGrenade(Transform parent)
        {
            return CreateVisual(parent, HeldLocalPosition, Quaternion.Euler(HeldLocalEuler), HeldLocalScale, "HeldGrenade");
        }

        public static GameObject CreateWorldGrenade(Vector3 position, Quaternion rotation, Vector3 scale, Transform parent = null)
        {
            EnsureTemplate();
            if (templateRoot == null)
                return null;

            var grenade = Object.Instantiate(templateRoot, parent);
            grenade.SetActive(true);
            grenade.name = "GrenadeProjectileVisual";
            grenade.transform.SetPositionAndRotation(position, rotation);
            grenade.transform.localScale = Vector3.Scale(scale, WorldScaleMultiplier);
            DisableColliders(grenade);
            return grenade;
        }

        static GameObject CreateVisual(Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, string objectName)
        {
            EnsureTemplate();
            if (templateRoot == null)
                return null;

            var grenade = Object.Instantiate(templateRoot, parent, false);
            grenade.SetActive(true);
            grenade.name = objectName;
            grenade.transform.localPosition = localPosition;
            grenade.transform.localRotation = localRotation;
            grenade.transform.localScale = localScale;
            DisableColliders(grenade);
            return grenade;
        }

        static void EnsureTemplate()
        {
            if (templateRoot != null)
                return;

            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: M67 grenade prefab missing at Resources/{PrefabResourcePath}.");
                return;
            }

            templateRoot = prefab;
        }

        static void DisableColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
        }
    }
}
