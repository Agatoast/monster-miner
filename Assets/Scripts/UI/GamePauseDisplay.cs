using MonsterMiner.Artillery;
using MonsterMiner.Core;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class GamePauseDisplay
    {
        const float PanelWidth = 420f;
        const float ButtonWidth = 280f;
        const float ButtonHeight = 42f;
        const float ButtonGap = 10f;

        enum Page
        {
            Pause,
            MiniGames,
            MiniGameDetail
        }

        struct MiniGameInfo
        {
            public string Title;
            public string Location;
            public string Rules;
        }

        static readonly MiniGameInfo[] MiniGames =
        {
            new MiniGameInfo
            {
                Title = "Catapult Battle",
                Location = "Shogun's camp, one mile east of Jarl Land. Talk to Katsura Morinobu after he gives you the compass.",
                Rules = "Command the blue forces on the left. Destroy the red forces before they destroy yours.\n\n"
                    + "On your turn, press F to fire. Enter launch angle (20-89 degrees), then power (1-100%).\n\n"
                    + "Wind speed in MPH creates drag on each shot. Most enemies need more than one hit.\n\n"
                    + "Cavalry advances after each turn. If cavalry reaches the far side, it kills enemy units.\n\n"
                    + "After winning the trial, use Play Minigame on the Shogun to practice."
            },
            new MiniGameInfo
            {
                Title = "Slot Machine",
                Location = "Shop counters across the world: the Plateau shop, Jarl Land, the Shogun's shop, Orin's camp, and the sky-metal site 2 shop.",
                Rules = "Select a monster drop in your hotbar (or a Slot Test Token worth $10) and press E at the slot cabinet.\n\n"
                    + "50% chance: lose the item.\n"
                    + "40% chance: win half its value back in cash.\n"
                    + "0.1% chance: win a legendary weapon.\n"
                    + "Otherwise: unlock a random item skin."
            }
        };

        static Page page;
        static int selectedMiniGameIndex = -1;
        static bool isOpen;
        static GUIStyle headerStyle;
        static GUIStyle bodyStyle;
        static GUIStyle buttonStyle;

        public static bool IsOpen => isOpen;

        public static void HandleInput()
        {
            if (ArtillerySession.IsActive
                || MainMenuDisplay.IsActive
                || DeathScreenDisplay.IsActive)
                return;

            if (MinerTurnInPopupDisplay.IsActive
                || SellConfirmationDisplay.IsActive
                || WorldMapDisplay.IsActive)
                return;

            var ctx = GameContext.Instance;
            if (ctx?.Shop != null && ctx.Shop.IsMenuOpen)
                return;

            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (!isOpen)
            {
                Open();
                return;
            }

            if (page == Page.MiniGameDetail)
                page = Page.MiniGames;
            else if (page == Page.MiniGames)
                page = Page.Pause;
            else
                Close();
        }

        public static void Open()
        {
            isOpen = true;
            page = Page.Pause;
            selectedMiniGameIndex = -1;
            UnlockCursor();
        }

        public static void Close()
        {
            isOpen = false;
            page = Page.Pause;
            selectedMiniGameIndex = -1;
        }

        public static void Draw()
        {
            if (!isOpen)
                return;

            EnsureStyles();
            UnlockCursor();

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            switch (page)
            {
                case Page.MiniGames:
                    DrawMiniGamesPage();
                    break;
                case Page.MiniGameDetail:
                    DrawMiniGameDetailPage();
                    break;
                default:
                    DrawPausePage();
                    break;
            }

            GUI.color = Color.white;
        }

        static void DrawPausePage()
        {
            float height = 72f + (ButtonHeight + ButtonGap) * 3f + 16f;
            var panel = CenterPanel(height);
            DrawPanel(panel, "Paused");

            float x = panel.x + (panel.width - ButtonWidth) * 0.5f;
            float y = panel.y + 64f;
            if (GUI.Button(new Rect(x, y, ButtonWidth, ButtonHeight), "Resume", buttonStyle))
                Close();

            y += ButtonHeight + ButtonGap;
            if (GUI.Button(new Rect(x, y, ButtonWidth, ButtonHeight), "Mini Games", buttonStyle))
                page = Page.MiniGames;

            y += ButtonHeight + ButtonGap;
            if (GUI.Button(new Rect(x, y, ButtonWidth, ButtonHeight), "Quit Game", buttonStyle))
                QuitGame();
        }

        static void DrawMiniGamesPage()
        {
            float height = 72f + (ButtonHeight + ButtonGap) * (MiniGames.Length + 1) + 16f;
            var panel = CenterPanel(height);
            DrawPanel(panel, "Mini Games");

            float x = panel.x + (panel.width - ButtonWidth) * 0.5f;
            float y = panel.y + 64f;
            for (int i = 0; i < MiniGames.Length; i++)
            {
                if (GUI.Button(new Rect(x, y, ButtonWidth, ButtonHeight), MiniGames[i].Title, buttonStyle))
                {
                    selectedMiniGameIndex = i;
                    page = Page.MiniGameDetail;
                }

                y += ButtonHeight + ButtonGap;
            }

            if (GUI.Button(new Rect(x, y, ButtonWidth, ButtonHeight), "Back", buttonStyle))
                page = Page.Pause;
        }

        static void DrawMiniGameDetailPage()
        {
            if (selectedMiniGameIndex < 0 || selectedMiniGameIndex >= MiniGames.Length)
            {
                page = Page.MiniGames;
                return;
            }

            var game = MiniGames[selectedMiniGameIndex];
            float panelWidth = Mathf.Min(760f, Screen.width - 48f);
            float textWidth = panelWidth - 48f;
            string body = $"Where to find it:\n{game.Location}\n\nHow to play:\n{game.Rules}";
            float textHeight = bodyStyle.CalcHeight(new GUIContent(body), textWidth);
            float height = Mathf.Min(Screen.height - 48f, 120f + textHeight + ButtonHeight + 36f);
            var panel = new Rect(
                Screen.width * 0.5f - panelWidth * 0.5f,
                Screen.height * 0.5f - height * 0.5f,
                panelWidth,
                height);

            DrawPanel(panel, game.Title);
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 64f, textWidth, textHeight),
                body,
                bodyStyle);

            var backRect = new Rect(
                panel.x + (panel.width - ButtonWidth) * 0.5f,
                panel.yMax - ButtonHeight - 18f,
                ButtonWidth,
                ButtonHeight);
            if (GUI.Button(backRect, "Back", buttonStyle))
                page = Page.MiniGames;
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

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 18,
                wordWrap = true,
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
