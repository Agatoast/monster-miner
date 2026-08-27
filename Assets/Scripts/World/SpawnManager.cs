using MonsterMiner.Combat;
using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Inventory;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public class SpawnManager : MonoBehaviour
    {
        const int MaxWorldPebbles = 5;
        const int InitialEggCount = 10;
        const float PebbleRespawnDelay = 10f;
        const int EggSpawnAttempts = 64;

        const float PebbleClearanceRadius = 0.4f;
        const int PebbleSpawnAttempts = 32;

        CavernBounds bounds;
        GameDatabase database;
        Transform contentRoot;
        readonly System.Collections.Generic.List<MonsterDefinition> weightedMonsters = new();
        int activeWorldPebbleCount;
        int pendingPebbleRespawns;
        float pebbleRespawnTimer = -1f;

        public void Initialize(CavernBounds cavernBounds, GameDatabase gameDatabase)
        {
            bounds = cavernBounds;
            database = gameDatabase;
            contentRoot = cavernBounds != null ? cavernBounds.transform : null;
            RebuildMonsterTable();
        }

        void Start()
        {
            SpawnInitialContent();
            SpawnInitialPebbles();
            if (contentRoot != null && bounds != null)
                CavernInteriorEnforcer.DisableOutsideRenderers(contentRoot, bounds);
        }

        void RebuildMonsterTable()
        {
            weightedMonsters.Clear();
            if (database == null)
                return;

            foreach (var monster in database.monsters)
            {
                int copies = Mathf.Max(1, Mathf.RoundToInt(monster.spawnWeight * 100f));
                for (int i = 0; i < copies; i++)
                    weightedMonsters.Add(monster);
            }
        }

        public void SpawnInitialContent()
        {
            for (int i = 0; i < InitialEggCount; i++)
                SpawnEgg();
        }

        public void SpawnExtraEggs(int eggCount)
        {
            for (int i = 0; i < eggCount; i++)
                SpawnEgg();
        }

        void Update()
        {
            if (pendingPebbleRespawns <= 0)
                return;

            if (pebbleRespawnTimer < 0f)
                pebbleRespawnTimer = PebbleRespawnDelay;

            pebbleRespawnTimer -= Time.deltaTime;
            if (pebbleRespawnTimer > 0f)
                return;

            if (TrySpawnShinyPebble())
            {
                pendingPebbleRespawns--;
                pebbleRespawnTimer = pendingPebbleRespawns > 0 ? PebbleRespawnDelay : -1f;
            }
        }

        void SpawnInitialPebbles()
        {
            while (activeWorldPebbleCount < MaxWorldPebbles)
            {
                if (!TrySpawnShinyPebble())
                    break;
            }
        }

        public void NotifyPebblePickedUp()
        {
            activeWorldPebbleCount = Mathf.Max(0, activeWorldPebbleCount - 1);
        }

        public void NotifyPebbleSold()
        {
            pendingPebbleRespawns++;
            if (pebbleRespawnTimer < 0f)
                pebbleRespawnTimer = PebbleRespawnDelay;
        }

        bool TrySpawnShinyPebble()
        {
            if (activeWorldPebbleCount >= MaxWorldPebbles || database == null || bounds == null)
                return false;

            var pebbleItem = database.items.Find(i => i.itemId == "shiny_pebble");
            if (pebbleItem == null)
                return false;

            if (!bounds.TryGetRandomClearFloorPoint(PebbleClearanceRadius, PebbleSpawnAttempts, out var spawnPoint))
                return false;

            var pickup = WorldPickup.Spawn(pebbleItem, 1, spawnPoint);
            if (pickup == null)
                return false;

            if (contentRoot != null)
                pickup.transform.SetParent(contentRoot, true);

            activeWorldPebbleCount++;
            return true;
        }

        public MonsterEgg SpawnEgg()
        {
            if (bounds == null)
                return null;

            float eggRadius = EggVisualFactory.GetWorldHorizontalRadius();
            if (!bounds.TryGetRandomClearFloorPoint(eggRadius, EggSpawnAttempts, out var spawnPoint, eggRadius))
                return null;

            var egg = MonsterEgg.Spawn(spawnPoint, PickRandomMonsterDefinition());

            if (egg != null && contentRoot != null)
                egg.transform.SetParent(contentRoot, true);

            return egg;
        }

        public Monster HatchMonster(Vector3 position, MonsterDefinition definition)
        {
            if (definition == null)
                definition = PickRandomMonsterDefinition();
            if (definition == null)
                return null;

            return Monster.Spawn(definition, position);
        }

        MonsterDefinition PickRandomMonsterDefinition()
        {
            if (weightedMonsters.Count == 0)
                RebuildMonsterTable();
            if (weightedMonsters.Count == 0)
                return null;

            return weightedMonsters[Random.Range(0, weightedMonsters.Count)];
        }
    }
}
