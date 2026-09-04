using UnityEngine;

namespace MonsterMiner.Artillery
{
    public static class ArtilleryPauseDisplay
    {
        const float PanelWidth = 360f;
        const float ButtonWidth = 240f;
        const float ButtonHeight = 40f;
        const float ButtonGap = 10f;

        enum Page
        {
            Pause,
            Settings
        }

        static Page page;
        static bool isOpen;
        static GUIStyle headerStyle;
        static GUIStyle buttonStyle;

        public static bool IsOpen => isOpen;

        public static void Toggle()
        {
            if (isOpen)
                Close();
            else
                Open();
        }

        public static void Open()
        {
            isOpen = true;
            page = Page.Pause;
            UnlockCursor();
        }

        public static void Close()
        {
            isOpen = false;
            page = Page.Pause;
        }

        public static void HandleInput()
        {
            if (!ArtillerySession.IsActive)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!isOpen)
                    Open();
                else if (page == Page.Settings)
                    page = Page.Pause;
                else
                    Close();
            }

            if (isOpen)
                UnlockCursor();
        }

        public static void Draw()
        {
            if (!isOpen || !ArtillerySession.IsActive)
                return;

            EnsureStyles();
            UnlockCursor();

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            if (page == Page.Settings)
                DrawSettings();
            else
                DrawPause();

            GUI.color = Color.white;
        }

        static void DrawPause()
        {
            float height = 72f + (ButtonHeight + ButtonGap) * 4f + 16f;
            var panel = CenterPanel(height);
            DrawPanel(panel, "Paused");

            float x = panel.x + (panel.width - ButtonWidth) * 0.5f;
            float y = panel.y + 64f;
            if (GUI.Button(new Rect(x, y, ButtonWidth, ButtonHeight), "Resume", buttonStyle))
                Close();

            y += ButtonHeight + ButtonGap;
            if (GUI.Button(new Rect(x, y, ButtonWidth, ButtonHeight), "Settings", buttonStyle))
                page = Page.Settings;

            y += ButtonHeight + ButtonGap;
            if (GUI.Button(new Rect(x, y, ButtonWidth, ButtonHeight), "Main Menu", buttonStyle))
            {
                Close();
                ArtillerySession.LeaveToMainMenu();
            }

            y += ButtonHeight + ButtonGap;
            if (GUI.Button(new Rect(x, y, ButtonWidth, ButtonHeight), "Quit Game", buttonStyle))
                QuitGame();
        }

        static void DrawSettings()
        {
            var panel = CenterPanel(160f);
            DrawPanel(panel, "Settings");

            float x = panel.x + (panel.width - ButtonWidth) * 0.5f;
            if (GUI.Button(new Rect(x, panel.yMax - ButtonHeight - 18f, ButtonWidth, ButtonHeight), "Back", buttonStyle))
                page = Page.Pause;
        }

        static Rect CenterPanel(float height)
        {
            return new Rect(
                Screen.width * 0.5f - PanelWidth * 0.5f,
                Screen.height * 0.5f - height * 0.5f,
                PanelWidth,
                height);
        }

        static void DrawPanel(Rect panel, string title)
        {
            GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.96f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            DrawBorder(panel, new Color(0.75f, 0.72f, 0.55f));
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x, panel.y + 14f, panel.width, 36f), title, headerStyle);
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

        static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
