using MonsterMiner.Combat;
using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Inventory;
using MonsterMiner.Util;
using System.Collections;
using UnityEngine;

namespace MonsterMiner.World
{
    public class SpawnManager : MonoBehaviour
    {
        const int MaxWorldPebbles = 5;
        const int InitialEggCount = 10;
        const int PlateauPebbleCount = 10;
        const int PlateauEggCount = 20;
        const float PebbleRespawnDelay = 10f;
        const int EggSpawnAttempts = 64;

        const float PebbleClearanceRadius = 0.4f;
        const int PebbleSpawnAttempts = 32;
        const int PlateauSpawnAttempts = 64;

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
            StartCoroutine(SpawnInitialContentAfterPhysicsReady());
        }

        IEnumerator SpawnInitialContentAfterPhysicsReady()
        {
            yield return null;
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();
            SpawnInitialContent();
            SpawnInitialPebbles();
            SpawnPlateauPebbles();
            SpawnPlateauEggs();
            AssignStoredEggCreatureTypes();
            ResnapFloorObjects();
            if (contentRoot != null && bounds != null)
                CavernInteriorEnforcer.DisableOutsideRenderers(contentRoot, bounds);
        }

        void ResnapFloorObjects()
        {
            if (bounds == null)
                return;

            foreach (var egg in FindObjectsByType<MonsterEgg>(FindObjectsSortMode.None))
            {
                if (egg == null || egg.IsCarried)
                    continue;

                FloorAnchor.PlaceOnFloor(egg.gameObject, egg.transform.position, bounds);
            }

            foreach (var pickup in FindObjectsByType<WorldPickup>(FindObjectsSortMode.None))
            {
                if (pickup == null)
                    continue;

                FloorAnchor.PlaceOnFloor(pickup.gameObject, pickup.transform.position, bounds);
            }
        }

        void RebuildMonsterTable()
        {
            weightedMonsters.Clear();
            if (database == null)
                return;

            foreach (var monster in database.monsters)
            {
                if (monster.spawnWeight <= 0f)
                    continue;

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
            if (!bounds.TryGetRandomClearFloorPoint(eggRadius, EggSpawnAttempts, out var spawnPoint, eggRadius, forEggSpawn: true))
                return null;

            return FinishEggSpawn(spawnPoint);
        }

        void SpawnPlateauPebbles()
        {
            if (database == null || bounds == null)
                return;

            var pebbleItem = database.items.Find(i => i.itemId == "shiny_pebble");
            if (pebbleItem == null)
                return;

            int spawned = 0;
            int attempts = 0;
            int maxAttempts = PlateauPebbleCount * PlateauSpawnAttempts;
            while (spawned < PlateauPebbleCount && attempts < maxAttempts)
            {
                attempts++;
                if (!bounds.TryGetRandomClearPlateauPoint(PebbleClearanceRadius, 1, out var spawnPoint))
                    continue;

                var pickup = WorldPickup.Spawn(pebbleItem, 1, spawnPoint, trackAsWorldPebble: false);
                if (pickup == null)
                    continue;

                if (contentRoot != null)
                    pickup.transform.SetParent(contentRoot, true);

                spawned++;
            }
        }

        void SpawnPlateauEggs()
        {
            const float plateauEggClearance = 1.2f;
            int spawned = 0;
            int attempts = 0;
            int maxAttempts = PlateauEggCount * PlateauSpawnAttempts;
            while (spawned < PlateauEggCount && attempts < maxAttempts)
            {
                attempts++;
                if (!bounds.TryGetRandomClearPlateauPoint(plateauEggClearance, 1, out var spawnPoint, forEggSpawn: true))
                    continue;

                if (FinishEggSpawn(spawnPoint) != null)
                    spawned++;
            }
        }

        MonsterEgg FinishEggSpawn(Vector3 spawnPoint)
        {
            var egg = MonsterEgg.Spawn(spawnPoint, PickRandomMonsterDefinition());
            if (egg != null && contentRoot != null)
                egg.transform.SetParent(contentRoot, true);

            return egg;
        }

        void AssignStoredEggCreatureTypes()
        {
            var eggs = new System.Collections.Generic.List<MonsterEgg>(
                FindObjectsByType<MonsterEgg>(FindObjectsSortMode.None));

            AssignCreatureType(eggs, "cave_rat", Random.Range(3, 6));
            AssignCreatureType(eggs, "iguana", Random.Range(3, 6));
            AssignCreatureType(eggs, "rabbit", Random.Range(3, 6));
            AssignCreatureType(eggs, "cave_lizard", Random.Range(3, 6));
            AssignCreatureType(eggs, "gremlin", Random.Range(3, 6));
            AssignCreatureType(eggs, "salamander", Random.Range(1, 36));
            AssignCreatureType(eggs, "pentachick", Random.Range(1, 3));
        }

        static void AssignCreatureType(System.Collections.Generic.List<MonsterEgg> pool, string typeId, int count)
        {
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = Random.Range(0, pool.Count);
                pool[index].SetCreatureTypeId(typeId);
                pool.RemoveAt(index);
            }
        }

        public MonsterEgg SpawnFinderEggAtMapCenter(string creatureTypeId)
        {
            if (bounds == null || string.IsNullOrEmpty(creatureTypeId))
                return null;

            if (!bounds.TryResolveFloorWorldPoint(0f, 0f, out var spawnPoint))
                return null;

            var definition = ResolveMonsterDefinition(creatureTypeId) ?? PickRandomMonsterDefinition();
            if (definition == null)
                return null;

            var egg = MonsterEgg.Spawn(spawnPoint, definition);
            if (egg == null)
                return null;

            egg.SetCreatureTypeId(creatureTypeId);
            if (contentRoot != null)
                egg.transform.SetParent(contentRoot, true);

            return egg;
        }

        public Monster HatchMonster(Vector3 position, MonsterDefinition definition, string creatureTypeId = null)
        {
            definition = ResolveMonsterDefinition(creatureTypeId) ?? definition ?? PickRandomMonsterDefinition();
            if (definition == null)
                return null;

            return Monster.Spawn(definition, position);
        }

        MonsterDefinition ResolveMonsterDefinition(string creatureTypeId)
        {
            if (string.IsNullOrEmpty(creatureTypeId) || database == null)
                return null;

            return database.monsters.Find(m => m.monsterId == creatureTypeId);
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
