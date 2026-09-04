using UnityEngine;

namespace MonsterMiner.UI
{
    public static class MainMenuDisplay
    {
        const float PanelWidth = 360f;
        const float ButtonWidth = 240f;
        const float ButtonHeight = 40f;
        const float ButtonGap = 10f;

        static bool isActive;
        static GUIStyle headerStyle;
        static GUIStyle buttonStyle;

        public static bool IsActive => isActive;

        public static void Show()
        {
            isActive = true;
            UnlockCursor();
        }

        public static void Hide()
        {
            isActive = false;
        }

        public static void HandleInput()
        {
            if (!isActive)
                return;

            UnlockCursor();
        }

        public static void Draw()
        {
            if (!isActive)
                return;

            EnsureStyles();
            UnlockCursor();

            GUI.color = new Color(0.04f, 0.05f, 0.07f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            float height = 72f + (ButtonHeight + ButtonGap) * 2f + 16f;
            var panel = new Rect(
                Screen.width * 0.5f - PanelWidth * 0.5f,
                Screen.height * 0.5f - height * 0.5f,
                PanelWidth,
                height);

            GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.96f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(0.75f, 0.72f, 0.55f);
            const float thickness = 2f;
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(panel.x, panel.yMax - thickness, panel.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(panel.x, panel.y, thickness, panel.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(panel.xMax - thickness, panel.y, thickness, panel.height), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x, panel.y + 14f, panel.width, 36f), "Monster Miner", headerStyle);

            float x = panel.x + (panel.width - ButtonWidth) * 0.5f;
            float y = panel.y + 64f;
            if (GUI.Button(new Rect(x, y, ButtonWidth, ButtonHeight), "Play", buttonStyle))
                Hide();

            y += ButtonHeight + ButtonGap;
            if (GUI.Button(new Rect(x, y, ButtonWidth, ButtonHeight), "Quit Game", buttonStyle))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        static void EnsureStyles()
        {
            if (headerStyle != null)
                return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
