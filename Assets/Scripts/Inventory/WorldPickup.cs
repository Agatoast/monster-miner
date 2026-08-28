using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Interaction;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.Inventory
{
    public class WorldPickup : MonoBehaviour, IInteractable
    {
        public ItemDefinition Item { get; private set; }
        public int Count { get; private set; }
        public bool TrackAsWorldPebble { get; private set; } = true;

        public static WorldPickup Spawn(ItemDefinition item, int count, Vector3 position, bool trackAsWorldPebble = true)
        {
            if (item == null || count <= 0)
                return null;

            GameObject go;
            if (item.itemId == "shiny_pebble")
            {
                go = PebbleVisualFactory.CreateWorldPebble(position, item.displayName);
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.05f;
                rb.useGravity = false;
                rb.isKinematic = true;
            }
            else if (InventorySystem.IsMonsterMeat(item))
            {
                go = MeatVisualFactory.CreateWorldMeat(position, item);
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.05f;
                rb.useGravity = false;
                rb.isKinematic = true;
            }
            else
            {
                go = PrimitiveFactory.CreatePrimitive(PrimitiveType.Sphere, position, Vector3.one * 0.35f, item.worldColor, item.displayName);
                PrimitiveFactory.EnsureRigidbody(go, 0.3f);
            }
            var pickup = go.AddComponent<WorldPickup>();
            pickup.Initialize(item, count, trackAsWorldPebble);
            return pickup;
        }

        public void Initialize(ItemDefinition item, int count, bool trackAsWorldPebble = true)
        {
            Item = item;
            Count = count;
            TrackAsWorldPebble = trackAsWorldPebble;
        }

        public string GetPrompt()
        {
            string name = Count > 1 ? $"{Item.displayName} x{Count}" : Item.displayName;
            return $"Pick up {name} [E]";
        }

        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor)
        {
            var ctx = GameContext.Instance;
            if (ctx == null || ctx.Inventory == null)
                return;

            if (ctx.Inventory.TryAdd(Item, Count))
            {
                if (Item.itemId == "shiny_pebble" && TrackAsWorldPebble)
                    ctx.SpawnManager?.NotifyPebblePickedUp();
                Destroy(gameObject);
                return;
            }

            if (InventorySystem.IsPentachickHeart(Item))
            {
                if (ctx.Inventory.ContainsItem(Item))
                    ctx.Hud?.ShowMessage("You already have the Pentachick Heart.");
                else
                    ctx.Hud?.ShowMessage("Need an empty inventory slot for the Pentachick Heart.");
            }
        }
    }
}
