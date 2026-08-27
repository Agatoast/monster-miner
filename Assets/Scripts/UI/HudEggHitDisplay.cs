using System.Collections.Generic;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class HudEggHitDisplay
    {
        sealed class HitPopup
        {
            public string Text;
            public float TimeLeft;
            public float Duration;
            public Vector2 Offset;
        }

        static readonly string[] HitTexts = { "*tink*", "*chonk*", "*bink*", "*crack*" };
        static readonly List<HitPopup> ActiveHits = new();
        static GUIStyle labelStyle;

        const float DisplayDuration = 0.4f;
        const int MaxActiveHits = 10;

        public static void ShowRandomHit()
        {
            ShowHit(HitTexts[Random.Range(0, HitTexts.Length)]);
        }

        public static void ShowHit(string text)
        {
            ActiveHits.Add(new HitPopup
            {
                Text = text,
                TimeLeft = DisplayDuration,
                Duration = DisplayDuration,
                Offset = new Vector2(Random.Range(-56f, 56f), Random.Range(-32f, 32f))
            });

            if (ActiveHits.Count > MaxActiveHits)
                ActiveHits.RemoveAt(0);
        }

        public static void Tick(float deltaTime)
        {
            for (int i = ActiveHits.Count - 1; i >= 0; i--)
            {
                ActiveHits[i].TimeLeft -= deltaTime;
                if (ActiveHits[i].TimeLeft <= 0f)
                    ActiveHits.RemoveAt(i);
            }
        }

        public static void Draw()
        {
            if (ActiveHits.Count == 0)
                return;

            var style = GetLabelStyle();
            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f - 20f;

            foreach (var hit in ActiveHits)
            {
                float alpha = Mathf.Clamp01(hit.TimeLeft / hit.Duration);
                style.normal.textColor = new Color(1f, 1f, 1f, alpha);

                GUI.Label(
                    new Rect(centerX - 200f + hit.Offset.x, centerY + hit.Offset.y, 400f, 40f),
                    hit.Text,
                    style);
            }

            style.normal.textColor = Color.white;
        }

        static GUIStyle GetLabelStyle()
        {
            if (labelStyle != null)
                return labelStyle;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            return labelStyle;
        }
    }
}
