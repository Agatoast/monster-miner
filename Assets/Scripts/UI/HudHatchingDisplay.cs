using UnityEngine;

namespace MonsterMiner.UI
{
    public static class HudHatchingDisplay
    {
        public static void Draw(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 100,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };

            float y = HudIconLayout.HeartY + HudIconLayout.IconSize + 8f;
            GUI.Label(new Rect(0f, y, Screen.width, 130f), message, style);
        }
    }
}
