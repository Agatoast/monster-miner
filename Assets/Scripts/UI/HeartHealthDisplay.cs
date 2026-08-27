using UnityEngine;

namespace MonsterMiner.UI
{
    public static class HeartHealthDisplay
    {
        const string MaskResourcePath = "Textures/UI/HeartIconMask";

        static readonly Color FillColor = new Color(1f, 0f, 0f, 1f);
        static readonly Color EmptyColor = new Color(0.28f, 0.06f, 0.08f, 0.92f);

        static Texture2D heartMask;

        public static void Draw(float currentHealth, float maxHealth)
        {
            float fill = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
            float x = HudIconLayout.HeartX;
            float y = HudIconLayout.HeartY;
            float size = HudIconLayout.IconSize;
            var rect = new Rect(x, y, size, size);
            var mask = GetHeartMask();
            if (mask == null)
                return;

            HudIconOutline.Draw(rect, mask);

            GUI.color = EmptyColor;
            GUI.DrawTexture(rect, mask, ScaleMode.StretchToFill);

            float visibleHeight = size * fill;
            if (visibleHeight > 0f)
            {
                float clipY = y + (size - visibleHeight);
                GUI.BeginGroup(new Rect(x, clipY, size, visibleHeight));
                GUI.color = FillColor;
                GUI.DrawTexture(new Rect(0f, -(size - visibleHeight), size, size), mask, ScaleMode.StretchToFill);
                GUI.EndGroup();
            }

            GUI.color = Color.white;

            HudIconTooltip.DrawPercentRemainingIfHovered(rect, currentHealth, maxHealth);
        }

        static Texture2D GetHeartMask()
        {
            if (heartMask != null)
                return heartMask;

            heartMask = Resources.Load<Texture2D>(MaskResourcePath);
            if (heartMask == null)
            {
                Debug.LogError($"Heart icon mask not found at Resources/{MaskResourcePath}.");
                return null;
            }

            return heartMask;
        }
    }
}
