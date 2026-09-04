using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Interaction;
using MonsterMiner.Util;
using MonsterMiner.World;
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
                AttachStaticRigidbody(go);
            }
            else if (item.itemId == "jormungandr_skull")
            {
                go = SkullVisualFactory.CreateWorldDrop(position, item.displayName);
                AttachStaticRigidbody(go, 0.3f);
            }
            else if (item.itemId == "sky_metal_lump")
            {
                go = SkyMetalLumpVisualFactory.CreateWorldDrop(position, item.displayName);
                AttachStaticRigidbody(go, 0.35f);
            }
            else
            {
                go = MeatVisualFactory.CreateWorldItemDrop(position, item);
                AttachStaticRigidbody(go, 0.3f);
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

        void OnDestroy()
        {
            if (InventorySystem.IsSkyMetalLump(Item))
                SkyMetalLumpTracker.NotifyDestroyed(this);
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
                if (InventorySystem.IsSkyMetalLump(Item))
                    SkyMetalLumpTracker.NotifyPickedUp(this);
                Destroy(gameObject);
                return;
            }

            if (InventorySystem.IsSkyMetalLump(Item))
            {
                if (ctx.Inventory.ContainsItem(Item))
                    ctx.Hud?.ShowMessage("You already have the Sky-Metal Lump.");
                else
                    ctx.Hud?.ShowMessage("Need an empty inventory slot for the Sky-Metal Lump.");
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

        static void AttachStaticRigidbody(GameObject go, float mass = 0.05f)
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();

            rb.mass = mass;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }
}
