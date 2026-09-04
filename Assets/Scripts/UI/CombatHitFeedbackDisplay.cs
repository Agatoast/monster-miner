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
        static GUIStyle hitXStyle;

        const float DisplayDuration = 0.9f;
        const float IndicatorDuration = 0.45f;
        const int MaxActiveHits = 16;

        public static void Show(Vector3 worldPoint, float damage)
        {
            ShowImpact(worldPoint, damage);
        }

        public static void ShowImpact(Vector3 worldPoint, float damage = 0f)
        {
            Vector2 drift = Random.insideUnitCircle;
            if (drift.sqrMagnitude < 0.01f)
                drift = Vector2.up;
            drift = drift.normalized * Random.Range(90f, 170f);

            ActiveHits.Add(new HitPopup
            {
                WorldPoint = worldPoint,
                Damage = damage > 0f ? Mathf.Max(1, Mathf.RoundToInt(damage)) : 0,
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
                float indicatorX = screen.x;
                float indicatorY = Screen.height - screen.y;
                float guiX = indicatorX + hit.DriftVelocity.x * elapsed;
                float guiY = indicatorY + hit.DriftVelocity.y * elapsed - elapsed * 28f;

                if (hit.IndicatorTimeLeft > 0f)
                    DrawHitIndicator(indicatorX, indicatorY, hit.IndicatorTimeLeft, hit.IndicatorDuration);

                if (hit.Damage <= 0)
                    continue;

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
            float fontSize = Mathf.Lerp(28f, 52f, t);

            EnsureStyles();
            hitXStyle.fontSize = Mathf.RoundToInt(fontSize);
            hitXStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);

            float boxSize = fontSize * 1.15f;
            GUI.Label(
                new Rect(guiX - boxSize * 0.5f, guiY - boxSize * 0.5f, boxSize, boxSize),
                "x",
                hitXStyle);
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

            hitXStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }
    }
}
