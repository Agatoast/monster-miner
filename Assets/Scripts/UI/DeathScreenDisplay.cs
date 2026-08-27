using System;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class DeathScreenDisplay
    {
        const float PanelWidth = 420f;
        const float PanelHeight = 170f;

        static GUIStyle headerStyle;
        static GUIStyle bodyStyle;
        static GUIStyle okButtonStyle;

        static Action onConfirm;
        static bool isActive;

        public static bool IsActive => isActive;

        public static void Show(Action confirm)
        {
            isActive = true;
            onConfirm = confirm;
        }

        public static void HandleInput()
        {
            if (!isActive)
                return;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                Confirm();
        }

        public static void Draw()
        {
            if (!isActive)
                return;

            EnsureStyles();

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            float x = Screen.width * 0.5f - PanelWidth * 0.5f;
            float y = Screen.height * 0.5f - PanelHeight * 0.5f;
            var panelRect = new Rect(x, y, PanelWidth, PanelHeight);

            GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.96f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            DrawBorder(panelRect, new Color(0.75f, 0.2f, 0.18f));

            GUI.color = Color.white;
            GUI.Label(new Rect(panelRect.x, panelRect.y + 16f, panelRect.width, 36f), "You died", headerStyle);
            GUI.Label(
                new Rect(panelRect.x + 20f, panelRect.y + 56f, panelRect.width - 40f, 48f),
                "Your items were dropped where you fell.",
                bodyStyle);

            var okRect = new Rect(panelRect.x + PanelWidth * 0.5f - 90f, panelRect.yMax - 56f, 180f, 38f);
            if (GUI.Button(okRect, "OK", okButtonStyle))
                Confirm();

            GUI.color = Color.white;
        }

        static void Confirm()
        {
            if (!isActive)
                return;

            isActive = false;
            var callback = onConfirm;
            onConfirm = null;
            callback?.Invoke();
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
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.45f, 0.4f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            okButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
