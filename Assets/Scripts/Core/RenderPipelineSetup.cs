using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.Core
{
    public static class RenderPipelineSetup
    {
        public static void Apply()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.44f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.39f, 0.36f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.26f, 0.24f);
            RenderSettings.ambientIntensity = 0.95f;
            RenderSettings.fog = false;
            RenderSettings.reflectionIntensity = 1f;

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

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.74f, 0.88f);
            camera.allowHDR = true;
            camera.allowMSAA = true;
        }
    }
}
