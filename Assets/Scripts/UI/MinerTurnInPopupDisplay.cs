using System;
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
        const float ConfirmButtonGap = 24f;

        static GUIStyle bodyStyle;
        static GUIStyle centeredBodyStyle;
        static GUIStyle footerStyle;
        static GUIStyle okButtonStyle;

        static bool isActive;
        static bool centerBodyText;
        static bool okOnlyDismiss;
        static bool confirmationMode;
        static string body = string.Empty;
        static int shownFrame = -1;
        static Action onDismiss;
        static Action onConfirm;
        static Action onCancel;
        static Action onSecondary;
        static string secondaryButtonLabel;
        static string okButtonHint;
        static string secondaryButtonHint;

        public static bool IsActive => isActive;

        public static void Show(
            string text,
            bool centerBody = false,
            Action dismissCallback = null,
            bool okOnly = false,
            string secondaryButtonLabel = null,
            string okButtonHint = null,
            string secondaryButtonHint = null,
            Action secondaryCallback = null)
        {
            ResetCallbacks();
            body = text ?? string.Empty;
            centerBodyText = centerBody;
            okOnlyDismiss = okOnly;
            confirmationMode = false;
            onDismiss = dismissCallback;
            MinerTurnInPopupDisplay.secondaryButtonLabel = secondaryButtonLabel;
            MinerTurnInPopupDisplay.okButtonHint = okButtonHint;
            MinerTurnInPopupDisplay.secondaryButtonHint = secondaryButtonHint;
            onSecondary = secondaryCallback;
            isActive = !string.IsNullOrEmpty(body);
            shownFrame = Time.frameCount;
        }

        public static void ShowConfirmation(string text, bool centerBody = false, Action confirmCallback = null, Action cancelCallback = null)
        {
            ResetCallbacks();
            body = text ?? string.Empty;
            centerBodyText = centerBody;
            okOnlyDismiss = false;
            confirmationMode = true;
            onConfirm = confirmCallback;
            onCancel = cancelCallback;
            isActive = !string.IsNullOrEmpty(body);
            shownFrame = Time.frameCount;
        }

        public static void Hide()
        {
            if (!isActive)
                return;

            var callback = onDismiss;
            ClearState();
            callback?.Invoke();
        }

        public static void HandleInput()
        {
            if (!isActive || Time.frameCount <= shownFrame)
                return;

            if (confirmationMode)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                    DismissConfirmation(confirmed: false);
                return;
            }

            if (okOnlyDismiss)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    Hide();
                return;
            }

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

            var style = centerBodyText ? centeredBodyStyle : bodyStyle;
            var content = new GUIContent(body);
            float textWidth = Mathf.Min(PanelWidth, Screen.width - 24f) - PanelPadding * 2f;
            float textHeight = style.CalcHeight(content, textWidth);
            float panelWidth = Mathf.Min(PanelWidth, Screen.width - 24f);
            float panelHeight = PanelPadding + textHeight + 32f + ButtonHeight + 36f + 40f;
            if (!string.IsNullOrEmpty(secondaryButtonLabel))
                panelHeight += string.IsNullOrEmpty(okButtonHint) && string.IsNullOrEmpty(secondaryButtonHint)
                    ? 40f
                    : 48f;
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
                style);

            float buttonY = panelRect.yMax - 36f - 32f - ButtonHeight;
            if (confirmationMode)
            {
                float pairWidth = ButtonWidth * 2f + ConfirmButtonGap;
                var yesRect = new Rect(
                    panelRect.x + panelRect.width * 0.5f - pairWidth * 0.5f,
                    buttonY,
                    ButtonWidth,
                    ButtonHeight);
                var noRect = new Rect(yesRect.xMax + ConfirmButtonGap, buttonY, ButtonWidth, ButtonHeight);

                if (GUI.Button(yesRect, "Yes", okButtonStyle))
                    DismissConfirmation(confirmed: true);

                if (GUI.Button(noRect, "No", okButtonStyle))
                    DismissConfirmation(confirmed: false);
            }
            else if (!string.IsNullOrEmpty(secondaryButtonLabel))
            {
                float pairWidth = ButtonWidth * 2f + ConfirmButtonGap;
                var okRect = new Rect(
                    panelRect.x + panelRect.width * 0.5f - pairWidth * 0.5f,
                    buttonY,
                    ButtonWidth,
                    ButtonHeight);
                var secondaryRect = new Rect(okRect.xMax + ConfirmButtonGap, buttonY, ButtonWidth, ButtonHeight);

                if (GUI.Button(okRect, "OK", okButtonStyle))
                    Hide();

                if (GUI.Button(secondaryRect, secondaryButtonLabel, okButtonStyle))
                    InvokeSecondary();

                if (!string.IsNullOrEmpty(okButtonHint) || !string.IsNullOrEmpty(secondaryButtonHint))
                {
                    float hintY = okRect.yMax + 6f;
                    GUI.color = new Color(0.75f, 0.75f, 0.75f);
                    if (!string.IsNullOrEmpty(okButtonHint))
                    {
                        GUI.Label(
                            new Rect(okRect.x, hintY, okRect.width, 32f),
                            okButtonHint,
                            footerStyle);
                    }

                    if (!string.IsNullOrEmpty(secondaryButtonHint))
                    {
                        GUI.Label(
                            new Rect(secondaryRect.x, hintY, secondaryRect.width, 32f),
                            secondaryButtonHint,
                            footerStyle);
                    }

                    GUI.color = Color.white;
                }
            }
            else
            {
                var okRect = new Rect(
                    panelRect.x + panelRect.width * 0.5f - ButtonWidth * 0.5f,
                    buttonY,
                    ButtonWidth,
                    ButtonHeight);
                if (GUI.Button(okRect, "OK", okButtonStyle))
                    Hide();
            }

            bool usePerButtonHints = !string.IsNullOrEmpty(secondaryButtonLabel)
                && (!string.IsNullOrEmpty(okButtonHint) || !string.IsNullOrEmpty(secondaryButtonHint));
            if (!usePerButtonHints)
            {
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                string footer = confirmationMode
                    ? "Choose Yes or No"
                    : !string.IsNullOrEmpty(secondaryButtonLabel)
                        ? $"Click OK to continue or {secondaryButtonLabel} to try the minigame"
                        : okOnlyDismiss ? "Click OK to continue" : "E, Enter, or Esc to close";
                GUI.Label(
                    new Rect(panelRect.x, panelRect.yMax - 44f, panelRect.width, 32f),
                    footer,
                    footerStyle);
                GUI.color = Color.white;
            }
        }

        static void InvokeSecondary()
        {
            if (!isActive || string.IsNullOrEmpty(secondaryButtonLabel))
                return;

            var callback = onSecondary;
            ClearState();
            callback?.Invoke();
        }

        static void DismissConfirmation(bool confirmed)
        {
            if (!isActive || !confirmationMode)
                return;

            var callback = confirmed ? onConfirm : onCancel;
            ClearState();
            callback?.Invoke();
        }

        static void ResetCallbacks()
        {
            onDismiss = null;
            onConfirm = null;
            onCancel = null;
            onSecondary = null;
            secondaryButtonLabel = null;
            okButtonHint = null;
            secondaryButtonHint = null;
        }

        static void ClearState()
        {
            isActive = false;
            body = string.Empty;
            shownFrame = -1;
            okOnlyDismiss = false;
            confirmationMode = false;
            ResetCallbacks();
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

            centeredBodyStyle = new GUIStyle(bodyStyle)
            {
                alignment = TextAnchor.MiddleCenter
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
