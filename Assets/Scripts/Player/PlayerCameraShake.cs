using UnityEngine;

namespace MonsterMiner.Player
{
    public class PlayerCameraShake : MonoBehaviour
    {
        PlayerController controller;
        float shakeTimer;
        float shakeDuration;
        float shakeIntensity;

        public void BeginViolentShake(float duration, float intensity = 0.35f)
        {
            controller ??= GetComponent<PlayerController>();
            shakeDuration = duration;
            shakeTimer = duration;
            shakeIntensity = intensity;
        }

        void LateUpdate()
        {
            if (controller?.ViewCamera == null)
                return;

            if (shakeTimer <= 0f)
            {
                controller.ViewCamera.transform.localPosition = Vector3.zero;
                return;
            }

            shakeTimer -= Time.deltaTime;
            float t = shakeDuration > 0f ? shakeTimer / shakeDuration : 0f;
            float strength = shakeIntensity * (0.35f + 0.65f * t);

            var cam = controller.ViewCamera.transform;
            cam.localPosition = new Vector3(
                Random.Range(-strength, strength),
                Random.Range(-strength, strength),
                0f);
        }
    }
}
