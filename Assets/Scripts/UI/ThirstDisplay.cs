using MonsterMiner.Player;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class ThirstDisplay
    {
        const string IconResourcePath = "Textures/UI/ThirstIcon";
        const string MaskResourcePath = "Textures/UI/ThirstIconMask";

        static readonly Color HealthyEmptyTint = new Color(0.22f, 0.28f, 0.38f, 0.55f);
        static readonly Color IllEmptyTint = new Color(0.18f, 0.32f, 0.16f, 0.55f);
        static readonly Color IllFillTint = new Color(0.45f, 0.95f, 0.4f, 1f);

        static Texture2D thirstIcon;
        static Texture2D thirstMask;

        public static void Draw(float currentThirst, float maxThirst)
        {
            float fill = maxThirst > 0f ? Mathf.Clamp01(currentThirst / maxThirst) : 0f;
            bool isHydrated = fill >= PlayerThirst.HealthRegenThreshold;
            float x = HudIconLayout.ThirstX;
            float y = HudIconLayout.ThirstY;
            float size = HudIconLayout.IconSize;
            var rect = new Rect(x, y, size, size);
            var icon = GetThirstIcon();
            var mask = GetThirstMask();
            if (icon == null || mask == null)
                return;

            HudIconOutline.Draw(rect, mask);

            GUI.color = isHydrated ? HealthyEmptyTint : IllEmptyTint;
            GUI.DrawTexture(rect, mask, ScaleMode.StretchToFill);

            float visibleHeight = size * fill;
            if (visibleHeight > 0f)
            {
                float clipY = y + (size - visibleHeight);
                GUI.BeginGroup(new Rect(x, clipY, size, visibleHeight));
                GUI.color = isHydrated ? Color.white : IllFillTint;
                GUI.DrawTexture(new Rect(0f, -(size - visibleHeight), size, size), icon, ScaleMode.StretchToFill);
                GUI.EndGroup();
            }

            GUI.color = Color.white;

            HudIconTooltip.DrawPercentRemainingIfHovered(rect, currentThirst, maxThirst);
        }

        static Texture2D GetThirstIcon()
        {
            if (thirstIcon != null)
                return thirstIcon;

            thirstIcon = Resources.Load<Texture2D>(IconResourcePath);
            if (thirstIcon == null)
                Debug.LogError($"Thirst icon not found at Resources/{IconResourcePath}.");

            return thirstIcon;
        }

        static Texture2D GetThirstMask()
        {
            if (thirstMask != null)
                return thirstMask;

            thirstMask = Resources.Load<Texture2D>(MaskResourcePath);
            if (thirstMask == null)
                Debug.LogError($"Thirst icon mask not found at Resources/{MaskResourcePath}.");

            return thirstMask;
        }
    }
}
