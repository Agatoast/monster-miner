using UnityEngine;

namespace MonsterMiner.UI
{
    public static class HudIconTooltip
    {
        static GUIStyle tooltipStyle;

        public static void DrawNumericRemainingIfHovered(Rect iconRect, float current, float max)
        {
            if (max <= 0f || Event.current == null)
                return;

            if (!iconRect.Contains(Event.current.mousePosition))
                return;

            int currentValue = Mathf.CeilToInt(current);
            int maxValue = Mathf.CeilToInt(max);
            DrawBelow(iconRect, $"{currentValue} / {maxValue}");
        }

        public static void DrawPercentRemainingIfHovered(Rect iconRect, float current, float max)
        {
            if (max <= 0f || Event.current == null)
                return;

            if (!iconRect.Contains(Event.current.mousePosition))
                return;

            int percent = Mathf.RoundToInt(Mathf.Clamp01(current / max) * 100f);
            DrawBelow(iconRect, $"{percent}% remaining");
        }

        static void DrawBelow(Rect anchor, string text)
        {
            EnsureStyle();

            var content = new GUIContent(text);
            float width = tooltipStyle.CalcSize(content).x + 14f;
            float height = tooltipStyle.CalcHeight(content, width) + 8f;
            var tooltipRect = new Rect(
                anchor.center.x - width * 0.5f,
                anchor.yMax + 6f,
                width,
                height);

            GUI.color = new Color(0f, 0f, 0f, 0.88f);
            GUI.DrawTexture(tooltipRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(tooltipRect, text, tooltipStyle);
        }

        static void EnsureStyle()
        {
            if (tooltipStyle != null)
                return;

            tooltipStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = Color.white }
            };
        }
    }
}
