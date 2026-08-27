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

        public static WorldPickup Spawn(ItemDefinition item, int count, Vector3 position)
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
            else if (item.itemId == "monster_meat")
            {
                go = MeatVisualFactory.CreateWorldMeat(position, item.displayName);
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
            pickup.Initialize(item, count);
            return pickup;
        }

        public void Initialize(ItemDefinition item, int count)
        {
            Item = item;
            Count = count;
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
                if (Item.itemId == "shiny_pebble")
                    ctx.SpawnManager?.NotifyPebblePickedUp();
                Destroy(gameObject);
            }
        }
    }
}
