using MonsterMiner.Core;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class MinerTurnInPopupDisplay
    {
        const float PanelWidth = 1280f;
        const float PanelPadding = 48f;
        const float ButtonWidth = 360f;
        const float ButtonHeight = 76f;

        static GUIStyle bodyStyle;
        static GUIStyle footerStyle;
        static GUIStyle okButtonStyle;

        static bool isActive;
        static string body = string.Empty;
        static int shownFrame = -1;

        public static bool IsActive => isActive;

        public static void Show(string text)
        {
            body = text ?? string.Empty;
            isActive = !string.IsNullOrEmpty(body);
            shownFrame = Time.frameCount;
        }

        public static void Hide()
        {
            isActive = false;
            body = string.Empty;
            shownFrame = -1;
        }

        public static void HandleInput()
        {
            if (!isActive || Time.frameCount <= shownFrame)
                return;

            if (Input.GetKeyDown(KeyCode.M)
                && GameContext.Instance?.CaveProgression != null
                && GameContext.Instance.CaveProgression.HasWorldMap)
            {
                Hide();
                WorldMapDisplay.Show();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Escape)
                || Input.GetKeyDown(KeyCode.E))
                Hide();
        }

        public static void Draw()
        {
            if (!isActive)
                return;

            EnsureStyles();

            var content = new GUIContent(body);
            float textWidth = Mathf.Min(PanelWidth, Screen.width - 24f) - PanelPadding * 2f;
            float textHeight = bodyStyle.CalcHeight(content, textWidth);
            float panelWidth = Mathf.Min(PanelWidth, Screen.width - 24f);
            float panelHeight = PanelPadding + textHeight + 32f + ButtonHeight + 36f + 40f;
            panelHeight = Mathf.Min(panelHeight, Screen.height - 24f);
            float x = Screen.width * 0.5f - panelWidth * 0.5f;
            float y = Screen.height * 0.5f - panelHeight * 0.5f;
            var panelRect = new Rect(x, y, panelWidth, panelHeight);

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.96f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            DrawBorder(panelRect, new Color(0.55f, 0.45f, 0.25f));

            GUI.color = Color.white;
            GUI.Label(
                new Rect(
                    panelRect.x + PanelPadding,
                    panelRect.y + PanelPadding,
                    panelRect.width - PanelPadding * 2f,
                    textHeight),
                content,
                bodyStyle);

            var okRect = new Rect(
                panelRect.x + panelRect.width * 0.5f - ButtonWidth * 0.5f,
                panelRect.yMax - 36f - 32f - ButtonHeight,
                ButtonWidth,
                ButtonHeight);
            if (GUI.Button(okRect, "OK", okButtonStyle))
                Hide();

            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            GUI.Label(
                new Rect(panelRect.x, panelRect.yMax - 44f, panelRect.width, 32f),
                "E, Enter, or Esc to close",
                footerStyle);
            GUI.color = Color.white;
        }

        static void DrawBorder(Rect rect, Color color)
        {
            const float thickness = 2f;
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static void EnsureStyles()
        {
            if (bodyStyle != null)
                return;

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 36,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            footerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 26,
                normal = { textColor = new Color(0.75f, 0.75f, 0.75f) }
            };

            okButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
