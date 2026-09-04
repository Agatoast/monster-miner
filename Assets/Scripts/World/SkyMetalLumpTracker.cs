using MonsterMiner.Core;
using MonsterMiner.Inventory;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class SkyMetalLumpTracker
    {
        static WorldPickup activeWorldLump;

        public static bool HasWorldLump => activeWorldLump != null;

        public static bool TryGetWorldLumpLocal(out Vector2 localXZ)
        {
            localXZ = Vector2.zero;
            if (activeWorldLump == null)
                return false;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return false;

            Vector3 local = bounds.transform.InverseTransformPoint(activeWorldLump.transform.position);
            localXZ = new Vector2(local.x, local.z);
            return true;
        }

        public static void SpawnWorldDrop(Vector3 origin)
        {
            var item = GameContext.Instance?.Database?.skyMetalLumpItem;
            if (item == null)
            {
                Debug.LogWarning("Monster Miner: sky_metal_lump item definition missing.");
                return;
            }

            Vector3 dropOrigin = origin + new Vector3(Random.Range(-0.35f, 0.35f), 0f, Random.Range(-0.35f, 0.35f));
            if (!FloorAnchor.TryResolveFloorPoint(dropOrigin, 16f, 32f, out var dropPoint))
                dropPoint = dropOrigin;

            if (activeWorldLump != null)
                Object.Destroy(activeWorldLump.gameObject);

            activeWorldLump = WorldPickup.Spawn(item, 1, dropPoint, trackAsWorldPebble: false);
            if (activeWorldLump == null)
                return;

            var contentRoot = GameContext.Instance?.CavernBounds?.transform;
            if (contentRoot != null)
                activeWorldLump.transform.SetParent(contentRoot, true);

            GameContext.Instance?.Hud?.ShowMessage("Sky-Metal Lump dropped.");
        }

        public static void NotifyPickedUp(WorldPickup pickup)
        {
            if (pickup != null && pickup == activeWorldLump)
                activeWorldLump = null;
        }

        public static void NotifyDestroyed(WorldPickup pickup)
        {
            if (pickup != null && pickup == activeWorldLump)
                activeWorldLump = null;
        }
    }
}
