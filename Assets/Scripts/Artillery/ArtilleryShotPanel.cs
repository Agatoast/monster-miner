using System;
using UnityEngine;

namespace MonsterMiner.Artillery
{
    public static class ArtilleryShotPanel
    {
        enum Step
        {
            Angle,
            Power
        }

        const float PanelWidth = 320f;
        const float PanelHeight = 160f;
        const float FieldHeight = 28f;
        const float ButtonWidth = 120f;
        const float ButtonHeight = 34f;
        const string InputControlName = "ArtilleryShotInput";
        const string PlaceholderText = "?";
        const string AngleErrorMessage = "Angle must be between 20 and 89 degrees.";
        const string PowerErrorMessage = "Power must be between 1 and 100 percent.";
        const float ErrorPanelGap = 12f;

        static bool isOpen;
        static bool focusInput;
        static bool inputPlaceholderActive;
        static bool inputReplaceOnType;
        static Step step;
        static string inputText = string.Empty;
        static float pendingAngle;
        static float lastAngle = 45f;
        static float lastPower = 60f;
        static bool hasLastShot;
        static int handledOkFrame = -1;
        static int stepAdvancedFrame = -1;
        static Action<float, float> pendingConfirm;
        static string errorMessage;
        static GUIStyle promptStyle;
        static GUIStyle errorStyle;
        static GUIStyle fieldStyle;
        static GUIStyle placeholderStyle;
        static GUIStyle buttonStyle;

        public static bool IsOpen => isOpen;

        public static void Open()
        {
            isOpen = true;
            step = Step.Angle;
            focusInput = true;
            if (hasLastShot)
            {
                inputText = lastAngle.ToString("0");
                inputPlaceholderActive = false;
                inputReplaceOnType = true;
            }
            else
            {
                inputText = string.Empty;
                inputPlaceholderActive = true;
                inputReplaceOnType = false;
            }

            handledOkFrame = -1;
            stepAdvancedFrame = -1;
            errorMessage = null;
        }

        public static void Close()
        {
            isOpen = false;
            pendingConfirm = null;
        }

        public static void HandleKeyboardSubmit()
        {
            if (!isOpen || pendingConfirm == null)
                return;

            if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
                return;

            TryOk(pendingConfirm, true);
        }

        public static void Draw(Action<float, float> onConfirm)
        {
            if (!isOpen || !ArtillerySession.IsActive)
                return;

            pendingConfirm = onConfirm;
            EnsureStyles();

            var panel = new Rect(
                Screen.width * 0.5f - PanelWidth * 0.5f,
                Screen.height * 0.5f - PanelHeight * 0.5f,
                PanelWidth,
                PanelHeight);

            DrawErrorBanner(panel);

            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.96f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            DrawBorder(panel, new Color(0.75f, 0.72f, 0.55f));
            GUI.color = Color.white;

            string prompt = step == Step.Angle ? "What angle?" : "What % power?";
            GUI.Label(new Rect(panel.x, panel.y + 24f, panel.width, 30f), prompt, promptStyle);

            var fieldRect = new Rect(panel.x + 24f, panel.y + 72f, panel.width - 48f, FieldHeight);
            if (focusInput)
            {
                GUI.SetNextControlName(InputControlName);
                GUI.FocusControl(InputControlName);
            }

            ConsumeReplaceOnTypeCharacter();

            GUI.SetNextControlName(InputControlName);
            string edited = GUI.TextField(
                fieldRect,
                inputPlaceholderActive ? PlaceholderText : inputText,
                inputPlaceholderActive ? placeholderStyle : fieldStyle);

            if (focusInput && GUI.GetNameOfFocusedControl() == InputControlName)
                focusInput = false;

            if (inputPlaceholderActive)
            {
                PlacePlaceholderCursor();
                if (!string.IsNullOrEmpty(edited) && edited != PlaceholderText)
                {
                    inputPlaceholderActive = false;
                    inputText = edited.StartsWith(PlaceholderText)
                        ? edited.Substring(PlaceholderText.Length)
                        : edited;
                    errorMessage = null;
                }
                else
                {
                    inputText = string.Empty;
                }
            }
            else
            {
                if (inputText != edited)
                    errorMessage = null;
                inputText = edited;
            }

            float buttonX = panel.x + (panel.width - ButtonWidth) * 0.5f;
            float buttonY = panel.yMax - ButtonHeight - 20f;
            if (TryOk(onConfirm, GUI.Button(new Rect(buttonX, buttonY, ButtonWidth, ButtonHeight), "OK", buttonStyle)))
                return;
        }

        static bool TryOk(Action<float, float> onConfirm, bool pressed)
        {
            if (!pressed)
                return false;

            if (handledOkFrame == Time.frameCount)
                return true;

            if (stepAdvancedFrame == Time.frameCount)
                return true;

            handledOkFrame = Time.frameCount;
            HandleOk(onConfirm);
            return true;
        }

        static void ConsumeReplaceOnTypeCharacter()
        {
            if (Event.current.type != EventType.KeyDown)
                return;

            if (GUI.GetNameOfFocusedControl() != InputControlName && !focusInput)
                return;

            if (!inputPlaceholderActive && !inputReplaceOnType)
                return;

            if (Event.current.keyCode == KeyCode.Backspace || Event.current.keyCode == KeyCode.Delete)
            {
                inputPlaceholderActive = false;
                inputReplaceOnType = false;
                inputText = string.Empty;
                errorMessage = null;
                Event.current.Use();
                return;
            }

            char typed = Event.current.character;
            if (typed == '\0' || char.IsControl(typed))
                return;

            inputPlaceholderActive = false;
            inputReplaceOnType = false;
            inputText = string.Empty;
            errorMessage = null;
        }

        static void PlacePlaceholderCursor()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (GUI.GetNameOfFocusedControl() != InputControlName)
                return;

            var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (textEditor == null || textEditor.text != PlaceholderText)
                return;

            textEditor.cursorIndex = PlaceholderText.Length;
            textEditor.selectIndex = PlaceholderText.Length;
        }

        static void DrawErrorBanner(Rect panel)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return;

            float paddingX = 16f;
            float paddingY = 14f;
            float textWidth = panel.width - paddingX * 2f;
            var content = new GUIContent(errorMessage);
            float textHeight = errorStyle.CalcHeight(content, textWidth);
            float bannerHeight = textHeight + paddingY * 2f;
            var banner = new Rect(
                panel.x,
                panel.y - bannerHeight - ErrorPanelGap,
                panel.width,
                bannerHeight);

            GUI.color = new Color(0.45f, 0.1f, 0.1f, 0.98f);
            GUI.DrawTexture(banner, Texture2D.whiteTexture);
            DrawBorder(banner, new Color(0.95f, 0.55f, 0.45f));
            GUI.color = Color.white;
            GUI.Label(
                new Rect(banner.x + paddingX, banner.y + paddingY, textWidth, textHeight),
                errorMessage,
                errorStyle);
        }

        static void HandleOk(Action<float, float> onConfirm)
        {
            if (step == Step.Angle)
            {
                if (!TryParseAngle(out float angle))
                    return;

                pendingAngle = angle;
                step = Step.Power;
                focusInput = true;
                if (hasLastShot)
                {
                    inputText = lastPower.ToString("0");
                    inputPlaceholderActive = false;
                    inputReplaceOnType = true;
                }
                else
                {
                    inputText = string.Empty;
                    inputPlaceholderActive = true;
                    inputReplaceOnType = false;
                }

                stepAdvancedFrame = Time.frameCount;
                return;
            }

            if (!TryParsePower(out float power))
                return;

            lastAngle = pendingAngle;
            lastPower = power;
            hasLastShot = true;
            isOpen = false;
            onConfirm?.Invoke(pendingAngle, power);
        }

        static bool TryParseAngle(out float angle)
        {
            angle = 0f;
            if (inputPlaceholderActive || string.IsNullOrWhiteSpace(inputText))
            {
                ShowValidationError(AngleErrorMessage);
                return false;
            }

            if (!float.TryParse(inputText, out angle))
            {
                ShowValidationError(AngleErrorMessage);
                return false;
            }

            if (angle < ArtilleryCatapult.MinLaunchAngleDegrees
                || angle > ArtilleryCatapult.MaxLaunchAngleDegrees)
            {
                ShowValidationError(AngleErrorMessage);
                return false;
            }

            errorMessage = null;
            inputText = angle.ToString("0");
            inputPlaceholderActive = false;
            return true;
        }

        static bool TryParsePower(out float power)
        {
            power = 0f;
            if (inputPlaceholderActive || string.IsNullOrWhiteSpace(inputText))
            {
                ShowValidationError(PowerErrorMessage);
                return false;
            }

            if (!float.TryParse(inputText, out power))
            {
                ShowValidationError(PowerErrorMessage);
                return false;
            }

            if (power < 1f || power > 100f)
            {
                ShowValidationError(PowerErrorMessage);
                return false;
            }

            errorMessage = null;
            inputText = power.ToString("0");
            inputPlaceholderActive = false;
            return true;
        }

        static void ShowValidationError(string message)
        {
            errorMessage = message;
            inputText = string.Empty;
            inputPlaceholderActive = true;
            inputReplaceOnType = false;
            focusInput = true;
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
            if (promptStyle != null)
                return;

            promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            fieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            placeholderStyle = new GUIStyle(fieldStyle)
            {
                normal = { textColor = new Color(1f, 1f, 1f, 0.55f) },
                hover = { textColor = new Color(1f, 1f, 1f, 0.55f) },
                active = { textColor = new Color(1f, 1f, 1f, 0.55f) },
                focused = { textColor = new Color(1f, 1f, 1f, 0.55f) }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            errorStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.92f, 0.88f) }
            };
        }
    }
}
