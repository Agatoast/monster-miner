using MonsterMiner.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.Core
{
    public static class RenderPipelineSetup
    {
        static Material skyboxMaterial;

        public static void Apply()
        {
            ConfigureSkybox();

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.68f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.70f, 0.82f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.34f, 0.28f);
            RenderSettings.ambientIntensity = 1.05f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.68f, 0.78f, 0.88f);
            RenderSettings.fogStartDistance = 180f;
            RenderSettings.fogEndDistance = WorldScale.Feet(2200f);
            RenderSettings.reflectionIntensity = 0.65f;

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional)
                    continue;

                if (light.gameObject.name == "Directional Light")
                {
                    light.enabled = false;
                    continue;
                }

                light.intensity = 1.75f;
                light.color = new Color(1f, 0.97f, 0.9f);
                light.shadows = LightShadows.Soft;
            }
        }

        public static void ConfigureCamera(Camera camera)
        {
            if (camera == null)
                return;

            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.45f, 0.68f, 0.94f);
            camera.allowHDR = true;
            camera.allowMSAA = true;
        }

        static void ConfigureSkybox()
        {
            if (skyboxMaterial == null)
            {
                var shader = Shader.Find("Skybox/Procedural");
                if (shader != null)
                    skyboxMaterial = new Material(shader);
            }

            if (skyboxMaterial == null)
                return;

            skyboxMaterial.SetFloat("_SunSize", 0.045f);
            skyboxMaterial.SetFloat("_SunSizeConvergence", 5f);
            skyboxMaterial.SetFloat("_AtmosphereThickness", 1f);
            skyboxMaterial.SetFloat("_Exposure", 1.15f);
            skyboxMaterial.SetColor("_SkyTint", new Color(0.42f, 0.48f, 0.72f));
            skyboxMaterial.SetColor("_GroundColor", new Color(0.40f, 0.38f, 0.32f));
            RenderSettings.skybox = skyboxMaterial;
        }
    }
}
