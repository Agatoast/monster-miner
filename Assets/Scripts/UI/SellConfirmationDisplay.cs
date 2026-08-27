using System;
using MonsterMiner.Economy;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class SellConfirmationDisplay
    {
        const float PanelWidth = 440f;
        const float PanelHeight = 150f;

        static GUIStyle headerStyle;
        static GUIStyle bodyStyle;
        static GUIStyle footerStyle;
        static GUIStyle confirmButtonStyle;
        static GUIStyle cancelButtonStyle;

        static ShopSellStation activeStation;
        static string itemName;
        static int sellValue;
        static Action onConfirm;

        public static bool IsActive => activeStation != null;

        public static void Show(ShopSellStation station, string itemDisplayName, int value, Action confirm)
        {
            activeStation = station;
            itemName = itemDisplayName;
            sellValue = value;
            onConfirm = confirm;
        }

        public static void Cancel()
        {
            activeStation = null;
            itemName = null;
            sellValue = 0;
            onConfirm = null;
        }

        public static bool IsForStation(ShopSellStation station) => activeStation == station;

        public static void HandleInput()
        {
            if (!IsActive)
                return;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                Cancel();
        }

        public static void Draw()
        {
            if (!IsActive)
                return;

            EnsureStyles();

            float x = Screen.width * 0.5f - PanelWidth * 0.5f;
            float y = Screen.height * 0.5f - PanelHeight * 0.5f;
            var panelRect = new Rect(x, y, PanelWidth, PanelHeight);

            GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.94f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            DrawBorder(panelRect, new Color(0.55f, 0.45f, 0.25f));

            GUI.color = Color.white;
            GUI.Label(new Rect(panelRect.x, panelRect.y + 10f, panelRect.width, 28f), "Are you sure?", headerStyle);
            GUI.Label(
                new Rect(panelRect.x + 16f, panelRect.y + 44f, panelRect.width - 32f, 40f),
                $"Sell {itemName} to the shopkeeper for ${sellValue}?",
                bodyStyle);

            var confirmRect = new Rect(panelRect.x + 24f, panelRect.yMax - 52f, 170f, 34f);
            var cancelRect = new Rect(panelRect.xMax - 194f, panelRect.yMax - 52f, 170f, 34f);

            if (GUI.Button(confirmRect, "Yes, sell", confirmButtonStyle))
                Confirm();

            if (GUI.Button(cancelRect, "No, keep it", cancelButtonStyle))
                Cancel();

            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            GUI.Label(
                new Rect(panelRect.x, panelRect.yMax - 18f, panelRect.width, 16f),
                "E to confirm · Esc or right-click to cancel",
                footerStyle);
            GUI.color = Color.white;
        }

        public static void Confirm()
        {
            if (!IsActive)
                return;

            onConfirm?.Invoke();
            Cancel();
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
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            footerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                normal = { textColor = new Color(0.75f, 0.75f, 0.75f) }
            };

            confirmButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            cancelButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16
            };
        }
    }
}
