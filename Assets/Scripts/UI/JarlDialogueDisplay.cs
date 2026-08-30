using MonsterMiner.Economy;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class JarlDialogueDisplay
    {
        const float PanelWidth = 720f;
        const float PanelPadding = 16f;
        static readonly Color PanelBackground = new Color(0f, 0f, 0f, 0.88f);

        static GUIStyle bodyStyle;

        public static void Draw(JarlQuestNpc jarl, Camera camera)
        {
            if (jarl == null || camera == null)
                return;

            EnsureStyles();

            string prompt = InteractPromptDisplay.FormatPrompt(jarl.GetPrompt());
            var content = new GUIContent(prompt);
            float textHeight = bodyStyle.CalcHeight(content, PanelWidth - PanelPadding * 2f);
            float panelHeight = textHeight + PanelPadding * 2f;

            if (!jarl.TryGetDialogueAnchorScreenPoint(camera, out var shoulderGui))
            {
                float fallbackX = Screen.width * 0.5f - PanelWidth * 0.5f;
                float fallbackY = Screen.height * 0.5f + 34f;
                DrawPanel(new Rect(fallbackX, fallbackY, PanelWidth, panelHeight), prompt);
                return;
            }

            const float shoulderGapX = 14f;
            const float shoulderGapY = 18f;
            float panelX = shoulderGui.x - PanelWidth - shoulderGapX;
            float panelY = shoulderGui.y - panelHeight - shoulderGapY;
            panelX = Mathf.Clamp(panelX, 12f, Screen.width - PanelWidth - 12f);
            panelY = Mathf.Clamp(panelY, 12f, Screen.height - panelHeight - 12f);

            DrawPanel(new Rect(panelX, panelY, PanelWidth, panelHeight), prompt);
        }

        static void DrawPanel(Rect panelRect, string prompt)
        {
            var textRect = new Rect(
                panelRect.x + PanelPadding,
                panelRect.y + PanelPadding,
                panelRect.width - PanelPadding * 2f,
                panelRect.height - PanelPadding * 2f);

            GUI.color = PanelBackground;
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(textRect, prompt, bodyStyle);
        }

        static void EnsureStyles()
        {
            if (bodyStyle != null)
                return;

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = InteractPromptDisplay.PromptFontSize,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
        }
    }
}
