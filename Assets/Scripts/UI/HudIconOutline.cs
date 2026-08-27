using UnityEngine;

namespace MonsterMiner.UI
{
    static class HudIconOutline
    {
        public static readonly Color Color = new Color(0.9f, 0.9f, 0.9f, 1f);
        const float Offset = 1f;

        public static void Draw(Rect rect, Texture2D shape)
        {
            GUI.color = Color;
            GUI.DrawTexture(new Rect(rect.x - Offset, rect.y, rect.width, rect.height), shape, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(rect.x + Offset, rect.y, rect.width, rect.height), shape, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(rect.x, rect.y - Offset, rect.width, rect.height), shape, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(rect.x, rect.y + Offset, rect.width, rect.height), shape, ScaleMode.StretchToFill);
        }
    }
}
