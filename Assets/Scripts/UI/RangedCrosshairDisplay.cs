using MonsterMiner.Combat;
using MonsterMiner.Core;
using MonsterMiner.Inventory;
using MonsterMiner.Player;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class RangedCrosshairDisplay
    {
        public const float SizePixels = 80f;
        const string ReloadPrompt = "Push R to reload";

        const float CrossArmLength = 20f;
        const float CrossCenterGap = 5f;
        const float CrossLineThickness = 2f;

        static GUIStyle ammoStyle;
        static GUIStyle reloadStyle;

        public static void Draw(GameContext ctx)
        {
            if (!ShouldShow(ctx))
                return;

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            DrawCrosshair(centerX, centerY);

            float textY = centerY + SizePixels * 0.5f + 8f;
            textY = DrawAmmoCounter(ctx, centerX, textY);

            if (ShouldShowReloadPrompt(ctx))
                DrawReloadPrompt(centerX, textY);
        }

        static void DrawCrosshair(float centerX, float centerY)
        {
            var color = new Color(0.95f, 0.15f, 0.1f, 1f);
            GUI.color = color;

            float halfThickness = CrossLineThickness * 0.5f;

            GUI.DrawTexture(
                new Rect(centerX - halfThickness, centerY - CrossCenterGap - CrossArmLength, CrossLineThickness, CrossArmLength),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(centerX - halfThickness, centerY + CrossCenterGap, CrossLineThickness, CrossArmLength),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(centerX - CrossCenterGap - CrossArmLength, centerY - halfThickness, CrossArmLength, CrossLineThickness),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(centerX + CrossCenterGap, centerY - halfThickness, CrossArmLength, CrossLineThickness),
                Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        static float DrawAmmoCounter(GameContext ctx, float centerX, float y)
        {
            var slot = ctx.Inventory?.GetSelectedSlot();
            if (slot?.item == null || !RangedWeaponStats.TryGetConfig(slot.item.itemId, out var config))
                return y;

            int current = ctx.PlayerRangedAmmo != null
                ? ctx.PlayerRangedAmmo.GetRounds(slot.item.itemId)
                : config.MagazineSize;

            string text = $"{current} / {config.MagazineSize}";
            var style = GetAmmoStyle();
            float width = 120f;
            float height = style.CalcHeight(new GUIContent(text), width);
            float x = centerX - width * 0.5f;
            GUI.Label(new Rect(x, y, width, height), text, style);
            return y + height + 4f;
        }

        static void DrawReloadPrompt(float centerX, float y)
        {
            var style = GetReloadStyle();
            float width = 240f;
            float height = style.CalcHeight(new GUIContent(ReloadPrompt), width);
            float x = centerX - width * 0.5f;
            GUI.Label(new Rect(x, y, width, height), ReloadPrompt, style);
        }

        static bool ShouldShowReloadPrompt(GameContext ctx)
        {
            var slot = ctx.Inventory?.GetSelectedSlot();
            if (!InventorySystem.IsRangedWeaponItem(slot?.item))
                return false;

            return ctx.PlayerRangedAmmo != null && ctx.PlayerRangedAmmo.NeedsReload(slot.item.itemId);
        }

        static bool ShouldShow(GameContext ctx)
        {
            if (ctx == null || ctx.IsPlayerDead)
                return false;

            if (ctx.Shop != null && ctx.Shop.IsMenuOpen)
                return false;

            if (DeathScreenDisplay.IsActive || SellConfirmationDisplay.IsActive)
                return false;

            if (GrenadeThrowController.IsGrenadeEquipped)
                return false;

            var slot = ctx.Inventory?.GetSelectedSlot();
            return InventorySystem.IsRangedWeaponItem(slot?.item);
        }

        static GUIStyle GetAmmoStyle()
        {
            if (ammoStyle != null)
                return ammoStyle;

            ammoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            return ammoStyle;
        }

        static GUIStyle GetReloadStyle()
        {
            if (reloadStyle != null)
                return reloadStyle;

            reloadStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.35f, 1f) }
            };
            return reloadStyle;
        }
    }
}
