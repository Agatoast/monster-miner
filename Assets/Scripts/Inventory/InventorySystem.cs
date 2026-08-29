using System;
using System.Collections.Generic;
using MonsterMiner.Data;
using MonsterMiner.World;
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
        public const int SlotTestTokenSlotIndex = 6;

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

        public void EnsureSlotTestToken(ItemDefinition token)
        {
            if (token == null)
                return;

            while (slots.Count <= SlotTestTokenSlotIndex)
                ExpandSlots(1);

            AssignSlot(SlotTestTokenSlotIndex, token, 1);
        }

        public void AssignSlot(int index, ItemDefinition item, int count = 1)
        {
            if (item == null || index < 0 || count <= 0)
                return;

            if (index == PickaxeSlotIndex)
            {
                SetReservedPickaxe(item);
                return;
            }

            while (slots.Count <= index)
                ExpandSlots(1);

            var slot = slots[index];
            slot.item = item;
            slot.count = count;
            slot.fromShopPurchase = false;
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

        public bool IsReservedSlotIndex(int index)
        {
            return index == PickaxeSlotIndex;
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

        public int GetEggHitsNeeded(int baseHits = MonsterEgg.BasePickaxeHits)
        {
            return Mathf.Max(1, baseHits - GetTotalMiningBonus());
        }

        public bool TryAdd(ItemDefinition item, int amount = 1, bool fromShopPurchase = false)
        {
            if (item == null || amount <= 0)
                return false;

            if (item.isMiningGlove)
            {
                if (amount > 1)
                    return false;

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

            if (fromShopPurchase)
                return TryAddShopPurchase(item);

            int remaining = amount;
            if (item.stackLimit > 1)
            {
                for (int i = FirstItemSlotIndex; i < slots.Count && remaining > 0; i++)
                {
                    var slot = slots[i];
                    if (slot.IsEmpty || slot.item != item)
                        continue;

                    int space = item.stackLimit - slot.count;
                    if (space <= 0)
                        continue;

                    int add = Mathf.Min(space, remaining);
                    slot.count += add;
                    remaining -= add;
                }
            }

            for (int i = FirstItemSlotIndex; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (!slot.IsEmpty)
                    continue;

                int add = Mathf.Min(Mathf.Max(1, item.stackLimit), remaining);
                slot.item = item;
                slot.count = add;
                slot.fromShopPurchase = fromShopPurchase;
                remaining -= add;
            }

            OnInventoryChanged?.Invoke();
            return remaining == 0;
        }

        bool TryAddShopPurchase(ItemDefinition item)
        {
            if (item.stackLimit > 1)
            {
                for (int i = FirstItemSlotIndex; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot.IsEmpty || slot.item != item || slot.count >= item.stackLimit)
                        continue;

                    slot.count++;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            for (int i = FirstItemSlotIndex; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsEmpty)
                    continue;

                slot.item = item;
                slot.count = 1;
                slot.fromShopPurchase = true;
                OnInventoryChanged?.Invoke();
                return true;
            }

            return false;
        }

        public bool CanAddShopPurchase(ItemDefinition item)
        {
            if (item == null)
                return false;

            if (item.isMiningGlove || IsPickaxeItem(item))
                return true;

            if (item.stackLimit > 1)
            {
                for (int i = FirstItemSlotIndex; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (!slot.IsEmpty && slot.item == item && slot.count < item.stackLimit)
                        return true;
                }
            }

            for (int i = FirstItemSlotIndex; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty)
                    return true;
            }

            return false;
        }

        public bool ContainsItem(ItemDefinition item)
        {
            if (item == null)
                return false;

            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty && slots[i].item == item)
                    return true;
            }

            return false;
        }

        public bool HasEmptyItemSlot()
        {
            for (int i = FirstItemSlotIndex; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty)
                    return true;
            }

            return false;
        }

        public bool CanAdd(ItemDefinition item, int amount = 1)
        {
            if (item == null || amount <= 0)
                return false;

            if (IsPentachickHeart(item) && ContainsItem(item))
                return false;

            if (item.isMiningGlove || IsPickaxeItem(item))
                return amount <= 1;

            int remaining = amount;
            if (item.stackLimit > 1)
            {
                for (int i = FirstItemSlotIndex; i < slots.Count && remaining > 0; i++)
                {
                    var slot = slots[i];
                    if (slot.IsEmpty || slot.item != item)
                        continue;

                    remaining -= Mathf.Max(0, item.stackLimit - slot.count);
                }
            }

            if (remaining <= 0)
                return true;

            int emptySlots = 0;
            for (int i = FirstItemSlotIndex; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty)
                    emptySlots++;
            }

            int perEmptySlot = Mathf.Max(1, item.stackLimit);
            return emptySlots * perEmptySlot >= remaining;
        }

        public void NotifyChanged() => OnInventoryChanged?.Invoke();

        public bool TryRemoveFromSelected(int amount = 1)
        {
            if (selectedIndex == PickaxeSlotIndex)
                return false;

            var slot = GetSelectedSlot();
            if (slot == null || slot.IsEmpty || slot.count < amount)
                return false;
            if (IsSlotTestToken(slot.item))
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
                if (slot.IsEmpty || IsSlotTestToken(slot.item))
                    continue;

                for (int n = 0; n < slot.count; n++)
                {
                    var dropPoint = position + UnityEngine.Random.insideUnitSphere * 0.6f;
                    dropPoint.y = position.y;
                    var pickup = WorldPickup.Spawn(slot.item, 1, dropPoint);
                    if (pickup != null && contentRoot != null)
                        pickup.transform.SetParent(contentRoot, true);
                }

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
                && (item.itemId.StartsWith("knife") || item.itemId == "legendary_blade");
        }

        public static string ResolveBaseWeaponId(ItemDefinition item) =>
            item == null ? null : ResolveBaseWeaponId(item.itemId);

        public static string ResolveBaseWeaponId(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId))
                return weaponId;

            if (weaponId == "legendary_blade")
                return "knife";

            const string prefix = "legendary_";
            return weaponId.StartsWith(prefix) ? weaponId.Substring(prefix.Length) : weaponId;
        }

        public static float GetWeaponDamage(ItemDefinition item)
        {
            if (item == null || item.weaponDamage <= 0)
                return 0f;

            float damage = item.weaponDamage;
            if (item.isLegendary)
                damage += 1f;

            return damage;
        }

        public static bool IsWeaponItem(ItemDefinition item)
        {
            return item != null
                && item.category == ItemCategory.Weapon
                && item.weaponDamage > 0
                && !IsGrenadeItem(item);
        }

        public static bool IsGrenadeItem(ItemDefinition item) =>
            item != null && ResolveBaseWeaponId(item) == "grenade";

        public static bool IsRangedWeaponItem(ItemDefinition item)
        {
            if (item == null)
                return false;

            return ResolveBaseWeaponId(item) switch
            {
                "pistol" or "shotgun" or "rifle" or "machinegun" => true,
                _ => false,
            };
        }

        public static bool IsSpearItem(ItemDefinition item) =>
            item != null && ResolveBaseWeaponId(item) == "spear";

        public static bool IsMachineGunItem(ItemDefinition item) =>
            item != null && ResolveBaseWeaponId(item) == "machinegun";

        public static bool IsPistolItem(ItemDefinition item) =>
            item != null && ResolveBaseWeaponId(item) == "pistol";

        public static bool IsShotgunItem(ItemDefinition item) =>
            item != null && ResolveBaseWeaponId(item) == "shotgun";

        public static bool IsRifleItem(ItemDefinition item) =>
            item != null && ResolveBaseWeaponId(item) == "rifle";

        public static bool IsPentachickHeart(ItemDefinition item) => item != null && item.itemId == "pentachick_heart";

        public static bool IsBossDrop(ItemDefinition item) => item != null && item.isBossDrop;

        public static bool IsMonsterMeat(ItemDefinition item) =>
            item != null && item.isMonsterDrop && item.isEdible && item.category == ItemCategory.Drop && item.itemId != "rare_core";

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

        public static bool IsSlotTestToken(ItemDefinition item) =>
            item != null && item.isSlotTestToken;

        public static bool IsEggFinder(ItemDefinition item) => item != null && item.isEggFinder;

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
