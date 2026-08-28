using System.Collections.Generic;
using MonsterMiner.Core;
using MonsterMiner.Economy;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class ShopBuyMenuDisplay
    {
        const float PanelWidth = 420f;
        const float RowHeight = 34f;
        const float HeaderHeight = 44f;
        const float FooterHeight = 28f;

        static GUIStyle headerStyle;
        static GUIStyle rowButtonStyle;
        static GUIStyle rowButtonHoverStyle;
        static GUIStyle rowDisabledStyle;
        static GUIStyle footerStyle;

        public static void Draw(ShopManager shop)
        {
            if (shop == null || !shop.IsMenuOpen)
                return;

            EnsureStyles();
            var entries = shop.GetMenuEntries();
            float panelHeight = HeaderHeight + entries.Count * RowHeight + FooterHeight + 16f;
            float x = Screen.width * 0.5f - PanelWidth * 0.5f;
            float y = Screen.height * 0.5f - panelHeight * 0.5f;
            var panelRect = new Rect(x, y, PanelWidth, panelHeight);

            GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            DrawBorder(panelRect, new Color(0.55f, 0.45f, 0.25f));

            GUI.color = Color.white;
            GUI.Label(new Rect(panelRect.x, panelRect.y + 8f, panelRect.width, HeaderHeight), "Shop", headerStyle);

            float rowY = panelRect.y + HeaderHeight;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var rowRect = new Rect(panelRect.x + 12f, rowY, panelRect.width - 24f, RowHeight - 4f);
                string line = entry.DisplayLine;

                if (entry.canPurchase)
                {
                    bool hovered = rowRect.Contains(Event.current.mousePosition);
                    GUI.color = hovered ? new Color(0.28f, 0.28f, 0.34f, 1f) : new Color(0.18f, 0.18f, 0.22f, 1f);
                    GUI.DrawTexture(rowRect, Texture2D.whiteTexture);

                    GUI.color = Color.white;
                    if (GUI.Button(rowRect, line, hovered ? rowButtonHoverStyle : rowButtonStyle))
                        shop.TryPurchase(i);
                }
                else
                {
                    GUI.color = new Color(0.14f, 0.14f, 0.16f, 1f);
                    GUI.DrawTexture(rowRect, Texture2D.whiteTexture);

                    GUI.color = Color.white;
                    GUI.Label(rowRect, line, rowDisabledStyle);
                }

                rowY += RowHeight;
            }

            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            GUI.Label(
                new Rect(panelRect.x, panelRect.yMax - FooterHeight - 4f, panelRect.width, FooterHeight),
                "Click to buy · E, Esc, or right-click to close",
                footerStyle);
            GUI.color = Color.white;
        }

        public static void HandleInput(ShopManager shop)
        {
            if (shop == null || !shop.IsMenuOpen)
                return;

            if (Input.GetKeyDown(KeyCode.Escape)
                || Input.GetKeyDown(KeyCode.E)
                || Input.GetMouseButtonDown(1))
            {
                shop.CloseMenu();
                return;
            }

            IReadOnlyList<ShopMenuEntry> entries = shop.GetMenuEntries();
            for (int i = 0; i < Mathf.Min(9, entries.Count); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    shop.TryPurchase(i);
            }
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
            if (headerStyle != null)
                return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            rowButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 18,
                padding = new RectOffset(10, 10, 0, 0),
                normal = { textColor = Color.white, background = Texture2D.blackTexture },
                hover = { textColor = Color.white, background = Texture2D.blackTexture },
                active = { textColor = Color.white, background = Texture2D.blackTexture },
                focused = { textColor = Color.white, background = Texture2D.blackTexture }
            };

            rowButtonHoverStyle = new GUIStyle(rowButtonStyle)
            {
                normal = { textColor = new Color(1f, 0.95f, 0.75f) }
            };

            rowDisabledStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 18,
                padding = new RectOffset(10, 10, 0, 0),
                normal = { textColor = new Color(0.55f, 0.55f, 0.55f) }
            };

            footerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = new Color(0.75f, 0.75f, 0.75f) }
            };
        }
    }
}
