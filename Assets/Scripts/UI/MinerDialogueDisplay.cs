using MonsterMiner.Economy;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class MinerDialogueDisplay
    {
        const float PanelWidth = 720f;
        const float PanelPadding = 16f;
        const string PhoenixHeartPhrase = "Heart of a Pentachick";
        static readonly Color PanelBackground = new Color(0f, 0f, 0f, 0.88f);
        static readonly Color HighlightOrange = new Color(1f, 0.55f, 0.12f);

        static GUIStyle bodyStyle;
        static GUIStyle richBodyStyle;
        static GUIStyle centeredBodyStyle;
        static GUIStyle centeredRichBodyStyle;

        public static void Draw(MinerQuestNpc miner, Camera camera)
        {
            if (miner == null || camera == null)
                return;

            EnsureStyles();

            string body = miner.GetDialogueBody();
            if (string.IsNullOrEmpty(body))
                return;

            string prompt = miner.ShouldShowInteractPrompt()
                ? InteractPromptDisplay.FormatPrompt(body + " [E]")
                : body;
            if (miner.ShouldHighlightPhoenixHeart())
                prompt = ApplyPhoenixHeartHighlight(prompt);

            var style = miner.ShouldHighlightPhoenixHeart()
                ? (miner.ShouldShowInteractPrompt() ? centeredRichBodyStyle : richBodyStyle)
                : (miner.ShouldShowInteractPrompt() ? centeredBodyStyle : bodyStyle);
            var content = new GUIContent(prompt);
            float textHeight = style.CalcHeight(content, PanelWidth - PanelPadding * 2f);
            float panelHeight = textHeight + PanelPadding * 2f;

            if (!miner.TryGetDialogueAnchorScreenPoint(camera, out var shoulderGui))
            {
                float fallbackX = Screen.width * 0.5f - PanelWidth * 0.5f;
                float fallbackY = Screen.height * 0.5f + 34f;
                DrawPanel(new Rect(fallbackX, fallbackY, PanelWidth, panelHeight), prompt, style);
                return;
            }

            const float shoulderGapX = 14f;
            const float shoulderGapY = 18f;
            float panelX = shoulderGui.x - PanelWidth - shoulderGapX;
            float panelY = shoulderGui.y - panelHeight - shoulderGapY;
            panelX = Mathf.Clamp(panelX, 12f, Screen.width - PanelWidth - 12f);
            panelY = Mathf.Clamp(panelY, 12f, Screen.height - panelHeight - 12f);

            DrawPanel(new Rect(panelX, panelY, PanelWidth, panelHeight), prompt, style);
        }

        static void DrawPanel(Rect panelRect, string prompt, GUIStyle style)
        {
            var textRect = new Rect(
                panelRect.x + PanelPadding,
                panelRect.y + PanelPadding,
                panelRect.width - PanelPadding * 2f,
                panelRect.height - PanelPadding * 2f);

            GUI.color = PanelBackground;
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(textRect, prompt, style);
        }

        static string ApplyPhoenixHeartHighlight(string prompt)
        {
            int phraseIndex = prompt.IndexOf(PhoenixHeartPhrase);
            if (phraseIndex < 0)
                return prompt;

            string before = prompt.Substring(0, phraseIndex);
            string after = prompt.Substring(phraseIndex + PhoenixHeartPhrase.Length);
            Color32 orange = HighlightOrange;
            string highlighted = $"<color=#{orange.r:X2}{orange.g:X2}{orange.b:X2}>{PhoenixHeartPhrase}</color>";
            return before + highlighted + after;
        }

        static void EnsureStyles()
        {
            if (bodyStyle != null)
                return;

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = InteractPromptDisplay.PromptFontSize,
                wordWrap = true,
                richText = false,
                normal = { textColor = Color.white }
            };

            richBodyStyle = new GUIStyle(bodyStyle)
            {
                richText = true
            };

            centeredBodyStyle = new GUIStyle(bodyStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            centeredRichBodyStyle = new GUIStyle(centeredBodyStyle)
            {
                richText = true
            };
        }
    }
}
