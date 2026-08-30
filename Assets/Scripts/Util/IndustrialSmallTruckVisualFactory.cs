using MonsterMiner.Core;
using MonsterMiner.Inventory;
using MonsterMiner.Player;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class IndustrialSmallTruckVisualFactory
    {
        const string ResourcePath = "Models/Vehicles/industrial_small_truck";
        // Driver eye point: inside the cab, left seat, looking out the windshield.
        static readonly Vector3 SeatLocalPosition = new Vector3(
            -0.28f,
            WorldScale.Feet(7.4f),
            WorldScale.Feet(4.1f));
        static readonly Vector3 SeatLocalEuler = new Vector3(0f, 0f, 0f);
        static readonly Vector3 CargoBedLocalPosition = new Vector3(0f, 1.08f, -1.55f);
        static readonly Vector3 CargoBedColliderSize = new Vector3(2.05f, 0.15f, 2.35f);
        static readonly Vector3 CabTriggerCenter = new Vector3(-0.35f, 1.05f, 1.05f);
        static readonly Vector3 CabTriggerSize = new Vector3(1.35f, 1.4f, 1.8f);
        static readonly Vector3 BedTriggerCenter = new Vector3(0f, 1.2f, -1.55f);
        static readonly Vector3 BedTriggerSize = new Vector3(2.05f, 1.1f, 2.35f);

        public static DriveableTruck CreateOnGround(
            Transform parent,
            Vector3 floorContactPoint,
            Quaternion worldRotation)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Monster Miner: truck prefab not found at Resources/{ResourcePath}.");
                return null;
            }

            var truckRoot = Object.Instantiate(prefab, parent, false);
            truckRoot.name = "PlayerTruck";
            truckRoot.transform.SetPositionAndRotation(floorContactPoint, worldRotation);
            ApplyUrpMaterials(truckRoot);
            FloorAnchor.SnapBottomToFloor(truckRoot, floorContactPoint.y);

            var body = truckRoot.transform.Find("SmallTruck");
            if (body == null && truckRoot.transform.childCount > 0)
                body = truckRoot.transform.GetChild(0);

            var seat = new GameObject("DriverSeat").transform;
            seat.SetParent(truckRoot.transform, false);
            seat.localPosition = SeatLocalPosition;
            seat.localRotation = Quaternion.Euler(SeatLocalEuler);

            var cargoBed = new GameObject("CargoBed").transform;
            cargoBed.SetParent(truckRoot.transform, false);
            cargoBed.localPosition = CargoBedLocalPosition;
            cargoBed.localRotation = Quaternion.identity;

            var bedFloor = cargoBed.gameObject.AddComponent<BoxCollider>();
            bedFloor.isTrigger = false;
            bedFloor.size = CargoBedColliderSize;
            bedFloor.center = new Vector3(0f, CargoBedColliderSize.y * 0.5f, 0f);

            ConfigurePhysics(truckRoot, body != null ? body.gameObject : truckRoot);
            AddInteractTrigger<TruckCabInteract>(truckRoot.transform, CabTriggerCenter, CabTriggerSize);
            AddInteractTrigger<TruckBedInteract>(truckRoot.transform, BedTriggerCenter, BedTriggerSize);

            var driveable = truckRoot.GetComponent<DriveableTruck>();
            if (driveable == null)
                driveable = truckRoot.AddComponent<DriveableTruck>();
            driveable.Initialize(seat, cargoBed);
            truckRoot.AddComponent<LakeTraversalGuard>();

            var cabInteract = truckRoot.GetComponentInChildren<TruckCabInteract>();
            cabInteract?.Initialize(driveable);
            var bedInteract = truckRoot.GetComponentInChildren<TruckBedInteract>();
            bedInteract?.Initialize(driveable);

            ApplyEquippedSkin(truckRoot);
            return driveable;
        }

        public static void ApplyEquippedSkin(GameObject truckRoot)
        {
            if (truckRoot == null)
                return;

            var skins = GameContext.Instance?.ItemSkins;
            var skinId = skins?.GetEquippedTruckSkinId();
            Color tint = Color.white;
            if (!string.IsNullOrEmpty(skinId))
            {
                var skin = skins.FindSkin(PlayerTruckIds.DefaultTruckId, skinId);
                if (skin != null)
                    tint = skin.previewColor;
            }

            foreach (var renderer in truckRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.name.Contains("Tyre"))
                    continue;

                var material = renderer.sharedMaterial;
                if (material == null)
                    continue;

                var instance = new Material(material);
                if (instance.HasProperty("_BaseColor"))
                {
                    Color baseColor = instance.GetColor("_BaseColor");
                    instance.SetColor("_BaseColor", Color.Lerp(baseColor, tint, 0.55f));
                }

                renderer.sharedMaterial = instance;
            }
        }

        static void ConfigurePhysics(GameObject truckRoot, GameObject meshRoot)
        {
            foreach (var collider in truckRoot.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);

            var rb = truckRoot.GetComponent<Rigidbody>();
            if (rb == null)
                rb = truckRoot.AddComponent<Rigidbody>();
            rb.mass = 1400f;
            rb.linearDamping = 0.2f;
            rb.angularDamping = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var box = truckRoot.AddComponent<BoxCollider>();
            FitBoxCollider(meshRoot, box, 0.08f);
        }

        static void AddInteractTrigger<T>(Transform parent, Vector3 localCenter, Vector3 localSize) where T : Component
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            var trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = localCenter;
            trigger.size = localSize;
            go.AddComponent<T>();
        }

        static void FitBoxCollider(GameObject meshRoot, BoxCollider collider, float padding)
        {
            var renderers = meshRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                collider.center = new Vector3(0f, 0.8f, 0f);
                collider.size = new Vector3(2.2f, 1.6f, 5.5f);
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i].name.Contains("Tyre"))
                    continue;
                bounds.Encapsulate(renderers[i].bounds);
            }

            Transform root = collider.transform;
            collider.center = root.InverseTransformPoint(bounds.center);
            Vector3 localSize = root.InverseTransformVector(bounds.size);
            collider.size = new Vector3(
                Mathf.Abs(localSize.x),
                Mathf.Abs(localSize.y),
                Mathf.Abs(localSize.z)) + Vector3.one * (padding * 2f);
        }

        static void ApplyUrpMaterials(GameObject root)
        {
            var template = Resources.Load<Material>("Materials/DefaultSurface");
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (template == null && urpLit == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                var source = renderer.sharedMaterial;
                var material = template != null ? new Material(template) : new Material(urpLit);
                bool isTyre = renderer.name.Contains("Tyre");

                if (!isTyre && source != null)
                {
                    Texture albedo = null;
                    if (source.HasProperty("_MainTex"))
                        albedo = source.GetTexture("_MainTex");
                    if (albedo == null && source.HasProperty("_BaseMap"))
                        albedo = source.GetTexture("_BaseMap");

                    if (albedo != null)
                    {
                        if (material.HasProperty("_BaseMap"))
                            material.SetTexture("_BaseMap", albedo);
                        else if (material.HasProperty("_MainTex"))
                            material.SetTexture("_MainTex", albedo);
                    }

                    if (material.HasProperty("_BaseColor"))
                    {
                        if (source.HasProperty("_BaseColor"))
                            material.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                        else if (source.HasProperty("_Color"))
                            material.SetColor("_BaseColor", source.color);
                    }
                }
                else if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.12f));
                }

                renderer.sharedMaterial = material;
            }
        }
    }
}
