using MonsterMiner.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.Util
{
    public static class WaterWorksLakeVisualFactory
    {
        const string WaterMaterialResourcePath = "WaterWorks/SSR_Water";
        const float UnityPlaneWidthUnits = 10f;

        public static GameObject CreateLakeSurface(Transform parent, float localY, float diameterUnits)
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "LakeWater";
            water.transform.SetParent(parent, false);
            water.transform.localPosition = new Vector3(0f, localY, 0f);
            water.transform.localRotation = Quaternion.identity;

            float scale = Mathf.Max(1f, diameterUnits / UnityPlaneWidthUnits);
            water.transform.localScale = new Vector3(scale, 1f, scale);

            Object.Destroy(water.GetComponent<Collider>());

            var renderer = water.GetComponent<MeshRenderer>();
            if (renderer == null)
                return water;

            var material = Resources.Load<Material>(WaterMaterialResourcePath);
            if (material == null)
            {
                Debug.LogWarning($"Monster Miner: WaterWorks material missing at Resources/{WaterMaterialResourcePath}.");
                renderer.sharedMaterial = CavernSurfaceMaterialFactory.GetWaterMaterial();
            }
            else
            {
                var instance = Object.Instantiate(material);
                instance.name = "SSR_Water_Lake";
                ConfigureLakeMaterial(instance, diameterUnits);
                renderer.sharedMaterial = instance;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return water;
        }

        static void ConfigureLakeMaterial(Material material, float diameterUnits)
        {
            float halfDiameter = diameterUnits * 0.5f;
            material.SetFloat("_MaxDist", halfDiameter);
            material.SetFloat("_MaxWaveDist", halfDiameter);
            material.SetFloat("_Edge_Offset", 0f);
            material.SetFloat("_Displacement_Amount", 0.35f);
            material.SetFloat("_Wave_Speed", 1.4f);
        }
    }
}
