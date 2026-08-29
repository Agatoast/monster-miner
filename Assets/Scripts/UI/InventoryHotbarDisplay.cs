using System.Collections.Generic;
using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Inventory;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class InventoryHotbarDisplay
    {
        public const float BoxSize = 80f;
        const float BottomMargin = 16f;
        const float BoxSpacing = 8f;
        const float GlovesGapFromHotbar = 100f;
        const float SlotInset = 1f;

        static readonly Color EmptySlotBackground = Color.white;
        static readonly Color SelectedBorder = new Color(1f, 0.92f, 0.35f, 1f);
        static readonly Color DragHighlight = new Color(1f, 1f, 1f, 0.35f);

        static Texture2D pickaxeIcon;
        static Texture2D gloveIcon;
        static GUIStyle numberStyle;
        static GUIStyle bonusStyle;
        static GUIStyle tooltipStyle;
        static GUIStyle finderNameStyle;

        static int? dragSourceIndex;
        static ItemDefinition dragItem;
        static int dragCount;

        public static void Draw(GameContext ctx)
        {
            if (ctx?.Inventory == null)
                return;

            float rowY = Screen.height - BottomMargin - BoxSize;
            var layout = BuildLayout(ctx.Inventory.Slots.Count, rowY);
            var slotRects = DrawItemSlots(ctx, layout);
            var glovesRect = DrawGlovesBox(ctx, rowY);
            HandleDragAndDrop(ctx.Inventory, slotRects);
            DrawDragPreview(ctx);
            DrawTooltips(ctx, slotRects, glovesRect);
        }

        static HotbarLayout BuildLayout(int slotCount, float rowY)
        {
            float rowWidth = slotCount * BoxSize + (slotCount - 1) * BoxSpacing;
            float startX = Screen.width * 0.5f - rowWidth * 0.5f;
            return new HotbarLayout(startX, rowY, slotCount);
        }

        static List<(int index, Rect rect)> DrawItemSlots(GameContext ctx, HotbarLayout layout)
        {
            var inventory = ctx.Inventory;
            var slotRects = new List<(int, Rect)>(inventory.Slots.Count);

            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                var rect = layout.GetSlotRect(i);
                slotRects.Add((i, rect));
                var mousePos = Event.current != null ? Event.current.mousePosition : Vector2.zero;
                bool isDragTarget = dragSourceIndex.HasValue
                    && dragSourceIndex.Value != i
                    && i != InventorySystem.PickaxeSlotIndex
                    && rect.Contains(mousePos);
                DrawSlotBox(ctx, rect, i + 1, inventory.Slots[i], i == inventory.SelectedIndex, inventory.IsReservedSlotIndex(i), i, isDragTarget);
            }

            return slotRects;
        }

        static Rect DrawGlovesBox(GameContext ctx, float rowY)
        {
            var rect = GetGlovesRect(ctx, rowY);
            var gloves = ctx.Inventory.EquippedGloves ?? ctx.Database?.glovesGray;
            DrawBoxFrame(rect, GetGlovesBoxBackground(gloves), Color.clear, 1f);

            var iconRect = InsetRect(rect, SlotInset);
            var icon = GetGloveIcon();
            if (icon != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            if (gloves != null && gloves.miningBonus > 0)
                DrawBonusLabel(rect, $"+{gloves.miningBonus}");

            GUI.color = Color.white;
            return rect;
        }

        static void DrawTooltips(GameContext ctx, List<(int index, Rect rect)> slotRects, Rect glovesRect)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
                return;

            var mouse = Event.current.mousePosition;

            if (glovesRect.Contains(mouse))
            {
                var gloves = ctx.Inventory.EquippedGloves;
                if (gloves != null)
                    DrawTileTooltip(glovesRect, gloves.displayName);
                return;
            }

            foreach (var (index, rect) in slotRects)
            {
                if (!rect.Contains(mouse))
                    continue;

                string tooltip = null;
                if (index == InventorySystem.PickaxeSlotIndex)
                    tooltip = ctx.Database?.pickaxeItem?.displayName ?? "Pickaxe";
                else
                {
                    var slot = ctx.Inventory.Slots[index];
                    if (!slot.IsEmpty)
                        tooltip = slot.item.displayName;
                }

                if (!string.IsNullOrEmpty(tooltip))
                    DrawTileTooltip(rect, tooltip);
                break;
            }
        }

        static void DrawTileTooltip(Rect tileRect, string text)
        {
            var bgRect = InsetRect(tileRect, 4f);
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            var style = GetTooltipStyle();
            GUI.color = Color.white;
            GUI.Label(bgRect, text, style);
        }

        static Rect GetGlovesRect(GameContext ctx, float rowY)
        {
            var layout = BuildLayout(ctx.Inventory.Slots.Count, rowY);
            float x = layout.GetSlotRect(layout.SlotCount - 1).xMax + GlovesGapFromHotbar;
            x = Mathf.Clamp(x, 8f, Screen.width - BoxSize - 8f);
            return new Rect(x, rowY, BoxSize, BoxSize);
        }

        static void HandleDragAndDrop(InventorySystem inventory, List<(int index, Rect rect)> slotRects)
        {
            var evt = Event.current;
            if (evt == null)
                return;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                foreach (var (index, rect) in slotRects)
                {
                    if (!rect.Contains(evt.mousePosition))
                        continue;

                    inventory.SelectSlot(index);
                    if (inventory.IsReservedSlotIndex(index))
                        break;

                    var slot = inventory.Slots[index];
                    if (slot.IsEmpty)
                        break;

                    dragSourceIndex = index;
                    dragItem = slot.item;
                    dragCount = slot.count;
                    evt.Use();
                    break;
                }
            }

            if (evt.type == EventType.MouseUp && evt.button == 0 && dragSourceIndex.HasValue)
            {
                int source = dragSourceIndex.Value;
                int? targetIndex = null;

                foreach (var (index, rect) in slotRects)
                {
                    if (!rect.Contains(evt.mousePosition))
                        continue;
                    targetIndex = index;
                    break;
                }

                if (targetIndex.HasValue && targetIndex.Value != source)
                    inventory.TrySwapSlots(source, targetIndex.Value);

                dragSourceIndex = null;
                dragItem = null;
                dragCount = 0;
                evt.Use();
            }
        }

        static void DrawDragPreview(GameContext ctx)
        {
            if (!dragSourceIndex.HasValue || dragItem == null || Event.current.type != EventType.Repaint)
                return;

            float size = BoxSize - SlotInset * 2f;
            var rect = new Rect(Event.current.mousePosition.x - size * 0.5f, Event.current.mousePosition.y - size * 0.5f, size, size);
            if (!ItemIconUtility.TryDrawIcon(rect, dragItem))
            {
                GUI.color = new Color(dragItem.worldColor.r, dragItem.worldColor.g, dragItem.worldColor.b, 0.75f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
            }

            if (dragCount > 1)
                DrawCountLabel(rect, dragCount.ToString());

            GUI.color = Color.white;
        }

        static void DrawSlotBox(GameContext ctx, Rect rect, int number, InventorySlot slot, bool selected, bool isPickaxeSlot, int slotIndex, bool dragTarget)
        {
            Color slotBackground = isPickaxeSlot
                ? GetPickaxeBoxBackground(ctx)
                : EmptySlotBackground;
            Color slotBorder = selected ? SelectedBorder : Color.clear;
            DrawBoxFrame(rect, slotBackground, slotBorder, 1f);

            if (dragTarget)
            {
                GUI.color = DragHighlight;
                GUI.DrawTexture(InsetRect(rect, 2f), Texture2D.whiteTexture);
            }

            var iconRect = InsetRect(rect, SlotInset);
            if (isPickaxeSlot)
            {
                var icon = GetPickaxeIcon();
                if (icon != null)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                }
                else if (ctx.Database?.pickaxeItem != null)
                {
                    GUI.color = ctx.Database.pickaxeItem.worldColor;
                    GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
                }

                int pickaxeBonus = ctx.PlayerCombat?.PickaxeMiningTier ?? 0;
                if (pickaxeBonus > 0)
                    DrawBonusLabel(rect, $"+{pickaxeBonus}");
            }
            else if (!slot.IsEmpty && dragSourceIndex != slotIndex)
            {
                string creatureTopWord = null;
                string creatureSecondWord = null;
                bool drawMeatLabels = InventorySystem.IsMonsterMeat(slot.item)
                    && TryGetMeatTileCreatureWords(slot.item, out creatureTopWord, out creatureSecondWord);
                if (drawMeatLabels)
                    iconRect = InsetIconForMeatLabels(iconRect, !string.IsNullOrEmpty(creatureSecondWord));

                if (!ItemIconUtility.TryDrawIcon(iconRect, slot.item))
                {
                    GUI.color = slot.item.worldColor;
                    GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
                }

                if (drawMeatLabels)
                    DrawMeatTileLabels(rect, creatureTopWord, creatureSecondWord);

                if (slot.count > 1)
                    DrawCountLabel(rect, slot.count.ToString());

                if (InventorySystem.IsEggFinder(slot.item))
                    DrawFinderNameLabel(rect, slot.item.displayName);
            }

            DrawSlotNumber(rect, number);
            GUI.color = Color.white;
        }

        static void DrawBoxFrame(Rect rect, Color background, Color border, float borderThickness)
        {
            GUI.color = background;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            if (border.a > 0.01f)
                DrawBorder(rect, border, borderThickness);
            GUI.color = Color.white;
        }

        static void DrawSlotNumber(Rect rect, int number)
        {
            var numberRect = new Rect(rect.x + 4f, rect.y + rect.height - 22f, 26f, 18f);
            var style = GetNumberStyle();
            style.alignment = TextAnchor.LowerLeft;
            style.normal.textColor = Color.black;
            GUI.color = Color.black;
            GUI.Label(numberRect, number.ToString(), style);
            GUI.color = Color.white;
        }

        static void DrawCountLabel(Rect rect, string text)
        {
            var style = GetNumberStyle();
            style.alignment = TextAnchor.LowerRight;
            style.normal.textColor = Color.black;
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f), text, style);
            GUI.color = Color.white;
        }

        static void DrawBonusLabel(Rect rect, string text)
        {
            var style = GetBonusStyle();
            style.alignment = TextAnchor.UpperRight;
            style.normal.textColor = Color.black;
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f), text, style);
            GUI.color = Color.white;
        }

        static void DrawMeatTileLabels(Rect rect, string topWord, string secondWord)
        {
            var style = GetMeatLabelStyle();
            GUI.color = Color.black;
            style.alignment = TextAnchor.UpperCenter;
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, 12f), topWord, style);
            if (!string.IsNullOrEmpty(secondWord))
                GUI.Label(new Rect(rect.x + 2f, rect.y + 13f, rect.width - 4f, 12f), secondWord, style);
            style.alignment = TextAnchor.LowerCenter;
            GUI.Label(new Rect(rect.x + 2f, rect.yMax - 16f, rect.width - 4f, 14f), "Meat", style);
            GUI.color = Color.white;
        }

        static Rect InsetIconForMeatLabels(Rect iconRect, bool hasSecondWord)
        {
            float topInset = hasSecondWord ? 24f : 14f;
            return new Rect(iconRect.x, iconRect.y + topInset, iconRect.width, iconRect.height - topInset - 14f);
        }

        static bool TryGetMeatTileCreatureWords(ItemDefinition item, out string topWord, out string secondWord)
        {
            topWord = null;
            secondWord = null;
            if (item == null || !InventorySystem.IsMonsterMeat(item))
                return false;

            string creatureId = item.itemId;
            if (creatureId.EndsWith("_meat"))
                creatureId = creatureId.Substring(0, creatureId.Length - "_meat".Length);

            string creatureName = ResolveCreatureDisplayName(creatureId);
            if (string.IsNullOrEmpty(creatureName))
                creatureName = "Monster";

            int spaceIndex = creatureName.IndexOf(' ');
            if (spaceIndex > 0)
            {
                topWord = creatureName.Substring(0, spaceIndex);
                secondWord = creatureName.Substring(spaceIndex + 1).Trim();
            }
            else
            {
                topWord = creatureName;
            }

            return true;
        }

        static string ResolveCreatureDisplayName(string creatureTypeId)
        {
            if (string.IsNullOrEmpty(creatureTypeId))
                return string.Empty;

            var monsters = GameContext.Instance?.Database?.monsters;
            if (monsters == null)
                return creatureTypeId;

            foreach (var monster in monsters)
            {
                if (monster != null && monster.monsterId == creatureTypeId)
                    return monster.displayName;
            }

            return creatureTypeId;
        }

        static void DrawFinderNameLabel(Rect rect, string text)
        {
            var labelRect = InsetRect(rect, 6f);
            var style = GetFinderNameStyle();
            GUI.color = Color.black;
            GUI.Label(labelRect, FormatFinderName(text), style);
            GUI.color = Color.white;
        }

        static string FormatFinderName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return displayName;

            return displayName.Replace(' ', '\n');
        }

        static void DrawBorder(Rect rect, Color color, float thickness)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static Rect InsetRect(Rect rect, float inset)
        {
            return new Rect(rect.x + inset, rect.y + inset, rect.width - inset * 2f, rect.height - inset * 2f);
        }

        static GUIStyle GetNumberStyle()
        {
            if (numberStyle != null)
                return numberStyle;

            numberStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };
            return numberStyle;
        }

        static GUIStyle GetBonusStyle()
        {
            if (bonusStyle != null)
                return bonusStyle;

            bonusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };
            return bonusStyle;
        }

        static GUIStyle GetTooltipStyle()
        {
            if (tooltipStyle != null)
                return tooltipStyle;

            tooltipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            return tooltipStyle;
        }

        static GUIStyle GetMeatLabelStyle()
        {
            return new GUIStyle(GetFinderNameStyle())
            {
                fontSize = 9,
                alignment = TextAnchor.UpperCenter
            };
        }

        static GUIStyle GetFinderNameStyle()
        {
            if (finderNameStyle != null)
                return finderNameStyle;

            finderNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                normal = { textColor = Color.black }
            };
            return finderNameStyle;
        }

        static Color GetGlovesBoxBackground(ItemDefinition gloves)
        {
            int tier = gloves?.miningBonus ?? 0;
            return PickaxeVisualFactory.GetTierBackgroundColor(tier);
        }

        static Texture2D GetGloveIcon()
        {
            if (gloveIcon == null)
            {
                gloveIcon = ItemIconUtility.LoadIconWithTransparentBackground(
                    "Textures/Inventory/Glove",
                    ItemIconUtility.IconBackgroundKeyMode.Black);
            }

            return gloveIcon;
        }

        static Color GetPickaxeBoxBackground(GameContext ctx)
        {
            int tier = ctx.PlayerCombat?.PickaxeMiningTier ?? 0;
            return PickaxeVisualFactory.GetTierBackgroundColor(tier);
        }

        static Texture2D GetPickaxeIcon()
        {
            if (pickaxeIcon == null)
            {
                pickaxeIcon = ItemIconUtility.LoadIconWithTransparentBackground(
                    "Textures/Inventory/Pickaxe",
                    ItemIconUtility.IconBackgroundKeyMode.BlackAndWhite);
            }

            return pickaxeIcon;
        }

        readonly struct HotbarLayout
        {
            public readonly float StartX;
            public readonly float RowY;
            public readonly int SlotCount;

            public HotbarLayout(float startX, float rowY, int slotCount)
            {
                StartX = startX;
                RowY = rowY;
                SlotCount = slotCount;
            }

            public Rect GetSlotRect(int slotIndex)
            {
                float x = StartX + slotIndex * (BoxSize + BoxSpacing);
                return new Rect(x, RowY, BoxSize, BoxSize);
            }
        }
    }
}
