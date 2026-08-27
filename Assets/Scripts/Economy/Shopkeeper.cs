using System.Collections;
using MonsterMiner.Core;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class Shopkeeper : MonoBehaviour
    {
        const float SpeechDuration = 2.5f;
        const float WaveDuration = 2.5f;

        static readonly string[] SalePhrases =
        {
            "Thank you!",
            "Wow, nice one!",
            "You are my best customer!",
            "Good to see you again!",
            "Bring me more of those!"
        };

        Animator animator;
        float speechTimer;
        string speechText = string.Empty;

        void Awake()
        {
            animator = GetComponent<Animator>();
            var ctx = GameContext.Instance;
            if (ctx != null)
                ctx.Shopkeeper = this;
        }

        void OnDestroy()
        {
            if (GameContext.Instance?.Shopkeeper == this)
                GameContext.Instance.Shopkeeper = null;
        }

        public void ThankCustomer(bool fromSale = false)
        {
            speechText = fromSale
                ? SalePhrases[Random.Range(0, SalePhrases.Length)]
                : "Thank you!";

            if (animator != null)
            {
                animator.SetTrigger("wave");
                StopAllCoroutines();
                StartCoroutine(ReturnToIdleAfterWave());
            }

            speechTimer = SpeechDuration;
        }

        IEnumerator ReturnToIdleAfterWave()
        {
            yield return new WaitForSeconds(WaveDuration);
            if (animator != null)
                animator.SetTrigger("idle");
        }

        void Update()
        {
            if (speechTimer > 0f)
                speechTimer -= Time.deltaTime;
        }

        void OnGUI()
        {
            if (speechTimer <= 0f)
                return;

            var camera = GameContext.Instance?.Player?.ViewCamera;
            if (camera == null)
                return;

            Vector3 worldPoint = transform.position + Vector3.up * 2.1f;
            Vector3 screen = camera.WorldToScreenPoint(worldPoint);
            if (screen.z <= 0f)
                return;

            float guiY = Screen.height - screen.y;
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.black }
            };

            var content = new GUIContent(speechText);
            var size = style.CalcSize(content);
            const float pad = 8f;
            var bubble = new Rect(
                screen.x - size.x * 0.5f - pad,
                guiY - size.y * 0.5f - pad,
                size.x + pad * 2f,
                size.y + pad * 2f);

            GUI.color = new Color(1f, 1f, 1f, 0.95f);
            GUI.DrawTexture(bubble, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(bubble, speechText, style);
        }
    }
}
