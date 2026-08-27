using System;
using System.Collections.Generic;
using MonsterMiner.Data;
using UnityEngine;

namespace MonsterMiner.Inventory
{
    [Serializable]
    public class InventorySlot
    {
        public ItemDefinition item;
        public int count;
        public bool fromShopPurchase;
        public bool IsEmpty => item == null || count <= 0;
    }

    public class InventorySystem : MonoBehaviour
    {
        public const int PickaxeSlotIndex = 0;

        readonly List<InventorySlot> slots = new();
        int selectedIndex;
        ItemDefinition equippedGloves;

        public IReadOnlyList<InventorySlot> Slots => slots;
        public int SelectedIndex => selectedIndex;
        public int SlotCount => slots.Count;
        public int MaxSlots { get; private set; } = 3;
        public ItemDefinition EquippedGloves => equippedGloves;

        public event Action OnInventoryChanged;
        public event Action<int> OnSelectedChanged;
        public event Action OnGlovesChanged;

        public void Initialize(int startingSlots)
        {
            MaxSlots = Mathf.Max(1, startingSlots);
            slots.Clear();
            for (int i = 0; i < MaxSlots; i++)
                slots.Add(new InventorySlot());
            selectedIndex = PickaxeSlotIndex;
            OnInventoryChanged?.Invoke();
        }

        public void SetReservedPickaxe(ItemDefinition pickaxe)
        {
            if (pickaxe == null || slots.Count == 0)
                return;

            slots[PickaxeSlotIndex].item = pickaxe;
            slots[PickaxeSlotIndex].count = 1;
            OnInventoryChanged?.Invoke();
        }

        public void EnsureStarterPickaxeIfMissing(ItemDefinition pickaxe)
        {
            if (pickaxe == null || slots.Count == 0 || !slots[PickaxeSlotIndex].IsEmpty)
                return;

            SetReservedPickaxe(pickaxe);
            selectedIndex = PickaxeSlotIndex;
            OnSelectedChanged?.Invoke(selectedIndex);
        }

        public InventorySlot GetSelectedSlot()
        {
            if (slots.Count == 0)
                return null;
            return slots[Mathf.Clamp(selectedIndex, 0, slots.Count - 1)];
        }

        public void EquipGloves(ItemDefinition gloves)
        {
            if (gloves == null || !gloves.isMiningGlove)
                return;

            equippedGloves = gloves;
            OnGlovesChanged?.Invoke();
        }

        public int GetMiningGloveBonus() => equippedGloves?.miningBonus ?? 0;

        public int GetTotalMiningBonus()
        {
            int pickaxeBonus = Core.GameContext.Instance?.PlayerCombat?.GetPickaxeMiningBonus() ?? 0;
            return GetMiningGloveBonus() + pickaxeBonus;
        }

        public bool TryAdd(ItemDefinition item, int amount = 1, bool fromShopPurchase = false)
        {
            if (item == null || amount <= 0)
                return false;

            if (item.isMiningGlove)
            {
                EquipGloves(item);
                return true;
            }

            if (IsPickaxeItem(item))
            {
                var pickaxeSlot = slots[PickaxeSlotIndex];
                if (pickaxeSlot.IsEmpty)
                {
                    pickaxeSlot.item = item;
                    pickaxeSlot.count = 1;
                    OnInventoryChanged?.Invoke();
                }

                return amount <= 1;
            }

            if (!CanAdd(item, amount))
                return false;

            int remaining = amount;
            for (int i = FirstItemSlotIndex; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot.item == item && slot.count < item.stackLimit)
                {
                    int space = item.stackLimit - slot.count;
                    int add = Mathf.Min(space, remaining);
                    slot.count += add;
                    remaining -= add;
                    if (fromShopPurchase)
                        slot.fromShopPurchase = true;
                }
            }

            for (int i = FirstItemSlotIndex; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty)
                {
                    slot.item = item;
                    int add = Mathf.Min(item.stackLimit, remaining);
                    slot.count = add;
                    remaining -= add;
                    slot.fromShopPurchase = fromShopPurchase;
                }
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool CanAdd(ItemDefinition item, int amount = 1)
        {
            if (item == null || amount <= 0)
                return false;

            if (item.isMiningGlove || IsPickaxeItem(item))
                return amount <= 1;

            int remaining = amount;
            for (int i = FirstItemSlotIndex; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot.item == item && slot.count < item.stackLimit)
                {
                    int space = item.stackLimit - slot.count;
                    int add = Mathf.Min(space, remaining);
                    remaining -= add;
                }
            }

            for (int i = FirstItemSlotIndex; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty)
                {
                    int add = Mathf.Min(item.stackLimit, remaining);
                    remaining -= add;
                }
            }

            return remaining <= 0;
        }

        public void NotifyChanged() => OnInventoryChanged?.Invoke();

        public bool TryRemoveFromSelected(int amount = 1)
        {
            if (selectedIndex == PickaxeSlotIndex)
                return false;

            var slot = GetSelectedSlot();
            if (slot == null || slot.IsEmpty || slot.count < amount)
                return false;
            slot.count -= amount;
            if (slot.count <= 0)
            {
                slot.item = null;
                slot.count = 0;
                slot.fromShopPurchase = false;
            }
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool CanEatSelected()
        {
            var slot = GetSelectedSlot();
            return slot != null && !slot.IsEmpty && slot.item.isEdible;
        }

        public int GetSelectedEatHealAmount()
        {
            var slot = GetSelectedSlot();
            if (slot == null || slot.IsEmpty || !slot.item.isEdible)
                return 0;
            return slot.item.sellValue;
        }

        public bool TryRemove(ItemDefinition item, int amount = 1)
        {
            if (item == null || amount <= 0)
                return false;
            int total = 0;
            foreach (var slot in slots)
                if (slot.item == item)
                    total += slot.count;
            if (total < amount)
                return false;

            int remaining = amount;
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (i == PickaxeSlotIndex && IsPickaxeItem(item))
                    continue;

                var slot = slots[i];
                if (slot.item != item)
                    continue;
                int take = Mathf.Min(slot.count, remaining);
                slot.count -= take;
                remaining -= take;
                if (slot.count <= 0)
                {
                    slot.item = null;
                    slot.count = 0;
                    slot.fromShopPurchase = false;
                }
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public void ExpandSlots(int count)
        {
            for (int i = 0; i < count; i++)
                slots.Add(new InventorySlot());
            MaxSlots = slots.Count;
            OnInventoryChanged?.Invoke();
        }

        public void SelectSlot(int index)
        {
            if (slots.Count == 0)
                return;
            selectedIndex = Mathf.Clamp(index, 0, slots.Count - 1);
            OnSelectedChanged?.Invoke(selectedIndex);
        }

        public void MoveSelected(int direction)
        {
            if (slots.Count <= 1)
                return;
            int next = selectedIndex + direction;
            if (next < 0)
                next = slots.Count - 1;
            if (next >= slots.Count)
                next = 0;
            SelectSlot(next);
        }

        public void SwapAdjacent(int direction)
        {
            int other = selectedIndex + direction;
            if (other < 0 || other >= slots.Count)
                return;
            if (selectedIndex == PickaxeSlotIndex || other == PickaxeSlotIndex)
                return;
            (slots[selectedIndex], slots[other]) = (slots[other], slots[selectedIndex]);
            OnInventoryChanged?.Invoke();
        }

        public bool TrySwapSlots(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex)
                return false;
            if (fromIndex < 0 || toIndex < 0 || fromIndex >= slots.Count || toIndex >= slots.Count)
                return false;
            if (fromIndex == PickaxeSlotIndex || toIndex == PickaxeSlotIndex)
                return false;

            var from = slots[fromIndex];
            var to = slots[toIndex];
            (from.item, to.item) = (to.item, from.item);
            (from.count, to.count) = (to.count, from.count);
            (from.fromShopPurchase, to.fromShopPurchase) = (to.fromShopPurchase, from.fromShopPurchase);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void DropAllAt(Vector3 position)
        {
            var contentRoot = Core.GameContext.Instance?.CavernBounds?.transform;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty)
                    continue;

                var dropPoint = position + UnityEngine.Random.insideUnitSphere * 0.6f;
                dropPoint.y = position.y;
                var pickup = WorldPickup.Spawn(slot.item, slot.count, dropPoint);
                if (pickup != null && contentRoot != null)
                    pickup.transform.SetParent(contentRoot, true);

                slot.item = null;
                slot.count = 0;
                slot.fromShopPurchase = false;
            }

            OnInventoryChanged?.Invoke();
        }

        public void DropEquippedGlovesAt(Vector3 position)
        {
            if (equippedGloves == null)
                return;

            var contentRoot = Core.GameContext.Instance?.CavernBounds?.transform;
            var dropPoint = position + UnityEngine.Random.insideUnitSphere * 0.45f;
            dropPoint.y = position.y;
            var pickup = WorldPickup.Spawn(equippedGloves, 1, dropPoint);
            if (pickup != null && contentRoot != null)
                pickup.transform.SetParent(contentRoot, true);

            equippedGloves = null;
            OnGlovesChanged?.Invoke();
        }

        static int FirstItemSlotIndex => PickaxeSlotIndex + 1;

        public static bool IsKnifeItem(ItemDefinition item)
        {
            return item != null
                && item.category == ItemCategory.Weapon
                && item.itemId != null
                && item.itemId.StartsWith("knife");
        }

        public static bool IsWeaponItem(ItemDefinition item)
        {
            return item != null
                && item.category == ItemCategory.Weapon
                && item.weaponDamage > 0;
        }

        public bool TryUpgradeKnife(ItemDefinition nextKnife)
        {
            if (nextKnife == null || !IsKnifeItem(nextKnife))
                return false;

            for (int i = FirstItemSlotIndex; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!IsKnifeItem(slot.item))
                    continue;

                slot.item = nextKnife;
                slot.count = 1;
                OnInventoryChanged?.Invoke();
                return true;
            }

            return false;
        }

        public ItemDefinition GetOwnedKnifeItem()
        {
            for (int i = FirstItemSlotIndex; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (IsKnifeItem(slot.item))
                    return slot.item;
            }

            return null;
        }

        static bool IsPickaxeItem(ItemDefinition item) => item != null && item.itemId == "pickaxe";

        void Update()
        {
            for (int i = 0; i < Mathf.Min(9, slots.Count); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    SelectSlot(i);
            }

            if (Input.GetKeyDown(KeyCode.LeftBracket))
                SwapAdjacent(-1);
            if (Input.GetKeyDown(KeyCode.RightBracket))
                SwapAdjacent(1);
        }
    }
}
