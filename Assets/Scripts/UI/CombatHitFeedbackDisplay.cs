using System.Collections.Generic;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class CombatHitFeedbackDisplay
    {
        sealed class HitPopup
        {
            public Vector3 WorldPoint;
            public int Damage;
            public float TimeLeft;
            public float Duration;
            public Vector2 DriftVelocity;
            public float IndicatorTimeLeft;
            public float IndicatorDuration;
        }

        static readonly List<HitPopup> ActiveHits = new();
        static GUIStyle damageStyle;
        static Texture2D indicatorTexture;

        const float DisplayDuration = 0.9f;
        const float IndicatorDuration = 0.35f;
        const int MaxActiveHits = 16;

        public static void Show(Vector3 worldPoint, float damage)
        {
            Vector2 drift = Random.insideUnitCircle;
            if (drift.sqrMagnitude < 0.01f)
                drift = Vector2.up;
            drift = drift.normalized * Random.Range(90f, 170f);

            ActiveHits.Add(new HitPopup
            {
                WorldPoint = worldPoint,
                Damage = Mathf.Max(1, Mathf.RoundToInt(damage)),
                TimeLeft = DisplayDuration,
                Duration = DisplayDuration,
                DriftVelocity = drift,
                IndicatorTimeLeft = IndicatorDuration,
                IndicatorDuration = IndicatorDuration
            });

            if (ActiveHits.Count > MaxActiveHits)
                ActiveHits.RemoveAt(0);
        }

        public static void Tick(float deltaTime)
        {
            for (int i = ActiveHits.Count - 1; i >= 0; i--)
            {
                ActiveHits[i].TimeLeft -= deltaTime;
                ActiveHits[i].IndicatorTimeLeft -= deltaTime;
                if (ActiveHits[i].TimeLeft <= 0f)
                    ActiveHits.RemoveAt(i);
            }
        }

        public static void Draw(Camera camera)
        {
            if (ActiveHits.Count == 0 || camera == null)
                return;

            EnsureStyles();

            foreach (var hit in ActiveHits)
            {
                Vector3 screen = camera.WorldToScreenPoint(hit.WorldPoint);
                if (screen.z <= 0f)
                    continue;

                float elapsed = hit.Duration - hit.TimeLeft;
                float guiX = screen.x + hit.DriftVelocity.x * elapsed;
                float guiY = Screen.height - screen.y + hit.DriftVelocity.y * elapsed - elapsed * 28f;

                if (hit.IndicatorTimeLeft > 0f)
                    DrawHitIndicator(guiX, guiY, hit.IndicatorTimeLeft, hit.IndicatorDuration);

                float alpha = Mathf.Clamp01(hit.TimeLeft / (hit.Duration * 0.45f));
                damageStyle.normal.textColor = new Color(1f, 0.82f, 0.25f, alpha);
                GUI.Label(new Rect(guiX - 40f, guiY - 20f, 80f, 40f), hit.Damage.ToString(), damageStyle);
            }

            damageStyle.normal.textColor = new Color(1f, 0.82f, 0.25f, 1f);
        }

        static void DrawHitIndicator(float guiX, float guiY, float timeLeft, float duration)
        {
            float t = 1f - Mathf.Clamp01(timeLeft / duration);
            float alpha = Mathf.Clamp01(timeLeft / (duration * 0.6f));
            float size = Mathf.Lerp(18f, 42f, t);

            GUI.color = new Color(1f, 0.25f, 0.15f, alpha * 0.85f);
            var rect = new Rect(guiX - size * 0.5f, guiY - size * 0.5f, size, size);
            GUI.DrawTexture(rect, GetIndicatorTexture(), ScaleMode.StretchToFill);

            GUI.color = new Color(1f, 0.95f, 0.9f, alpha);
            float crossSize = size * 0.55f;
            float crossThickness = Mathf.Max(2f, size * 0.12f);
            var hRect = new Rect(guiX - crossSize * 0.5f, guiY - crossThickness * 0.5f, crossSize, crossThickness);
            var vRect = new Rect(guiX - crossThickness * 0.5f, guiY - crossSize * 0.5f, crossThickness, crossSize);
            GUI.DrawTexture(hRect, Texture2D.whiteTexture);
            GUI.DrawTexture(vRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static void EnsureStyles()
        {
            if (damageStyle != null)
                return;

            damageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.82f, 0.25f) }
            };
        }

        static Texture2D GetIndicatorTexture()
        {
            if (indicatorTexture != null)
                return indicatorTexture;

            indicatorTexture = Texture2D.whiteTexture;
            return indicatorTexture;
        }
    }
}
