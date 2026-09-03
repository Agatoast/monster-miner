using MonsterMiner.Economy;
using MonsterMiner.Interaction;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class JarlDialogueDisplay
    {
        const float PanelPadding = 16f;
        const float MaxPromptWidth = 420f;
        const float MinPromptWidth = 180f;
        const float HeadGapPixels = 8f;
        static readonly Color PanelBackground = new Color(0f, 0f, 0f, 0.88f);

        static GUIStyle bodyStyle;

        public static void Draw(JarlQuestNpc jarl, Camera camera)
        {
            if (jarl == null || camera == null)
                return;

            DrawNamePlaque(jarl, camera, InteractPromptDisplay.FormatPrompt(jarl.GetPrompt()));
        }

        public static void Draw(Quarry3QuestNpc guide, Camera camera)
        {
            if (guide == null || camera == null)
                return;

            DrawNamePlaque(guide, camera, InteractPromptDisplay.FormatPrompt(guide.GetPrompt()));
        }

        public static void Draw(WarrensonBoatNpc warrenson, Camera camera)
        {
            if (warrenson == null || camera == null)
                return;

            DrawNamePlaque(warrenson, camera, InteractPromptDisplay.FormatPrompt(warrenson.GetPrompt()));
        }

        static void DrawNamePlaque(IInteractPromptBounds npc, Camera camera, string prompt)
        {
            if (npc == null || camera == null || string.IsNullOrEmpty(prompt))
                return;

            if (!npc.TryGetPromptScreenRect(camera, out var headBounds))
                return;

            EnsureStyles();

            var content = new GUIContent(prompt);
            float textWidth = Mathf.Min(MaxPromptWidth, Mathf.Max(MinPromptWidth, headBounds.width)) - PanelPadding * 2f;
            float textHeight = bodyStyle.CalcHeight(content, textWidth);
            float panelWidth = textWidth + PanelPadding * 2f;
            float panelHeight = textHeight + PanelPadding * 2f;
            float panelX = headBounds.center.x - panelWidth * 0.5f;
            float panelY = headBounds.yMin - panelHeight - HeadGapPixels;

            DrawPanel(new Rect(panelX, panelY, panelWidth, panelHeight), prompt);
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
