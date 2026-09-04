using MonsterMiner.Combat;
using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Inventory;
using MonsterMiner.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterMiner.World
{
    public class SpawnManager : MonoBehaviour
    {
        const int MaxWorldPebbles = 5;
        const int InitialEggCount = 10;
        const int PlateauPebbleCount = 10;
        const int JarlLandPebbleCount = 10;
        const int PlateauEggCount = 20;
        const int JarlLandEggCount = PlateauEggCount * 3;
        const int LandQuarry3EggCount = 20;
        const int LandQuarry4EggCount = 20;
        const float PebbleRespawnDelay = 10f;
        const int EggSpawnAttempts = 64;

        const float PebbleClearanceRadius = 0.4f;
        const int PebbleSpawnAttempts = 32;
        const int PlateauSpawnAttempts = 64;
        const float LandEggClearance = 1.2f;

        CavernBounds bounds;
        GameDatabase database;
        Transform contentRoot;
        int activeWorldPebbleCount;
        int pendingPebbleRespawns;
        float pebbleRespawnTimer = -1f;
        bool huntGroundsSpawned;
        bool jarlLandEggsSpawned;
        bool landQuarry3EggsSpawned;
        bool landQuarry4EggsSpawned;

        public void Initialize(CavernBounds cavernBounds, GameDatabase gameDatabase)
        {
            bounds = cavernBounds;
            database = gameDatabase;
            contentRoot = cavernBounds != null ? cavernBounds.transform : null;
            gameObject.AddComponent<LandStreamingSpawner>().Initialize(cavernBounds, this);
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
            SpawnJarlLandPebbles();
            SpawnPlateauEggs();
            SpawnJarlLandEggs();
            SpawnLandQuarry3Eggs();
            SpawnLandQuarry4Eggs();
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

        public void SpawnHuntGroundEggs()
        {
            if (huntGroundsSpawned || bounds == null)
                return;

            huntGroundsSpawned = true;
            var locations = HuntLocationCatalog.All;
            for (int i = 0; i < locations.Length; i++)
            {
                Vector2 center = HuntLocationCatalog.GetLocalXZ(bounds, locations[i]);
                SpawnEggsAround(center.x, center.y, HuntLocationCatalog.EggsPerLocation);
            }
        }

        void SpawnEggsAround(float localX, float localZ, int count)
        {
            float ring = WorldScale.Feet(10f);
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float x = localX + Mathf.Cos(angle) * ring;
                float z = localZ + Mathf.Sin(angle) * ring;
                if (!bounds.TryResolveFloorWorldPoint(x, z, out var spawnPoint))
                    continue;

                FinishEggSpawn(spawnPoint);
            }
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

        void SpawnJarlLandPebbles()
        {
            if (GameContext.Instance?.CaveProgression == null || !GameContext.Instance.CaveProgression.HasLandQuarry2)
                return;

            if (database == null || bounds == null)
                return;

            var pebbleItem = database.items.Find(i => i.itemId == "shiny_pebble");
            if (pebbleItem == null)
                return;

            int spawned = 0;
            int attempts = 0;
            int maxAttempts = JarlLandPebbleCount * PlateauSpawnAttempts;
            while (spawned < JarlLandPebbleCount && attempts < maxAttempts)
            {
                attempts++;
                if (!bounds.TryGetRandomClearJarlLandPoint(PebbleClearanceRadius, 1, out var spawnPoint))
                    continue;

                var pickup = WorldPickup.Spawn(pebbleItem, 1, spawnPoint, trackAsWorldPebble: false);
                if (pickup == null)
                    continue;

                if (contentRoot != null)
                    pickup.transform.SetParent(contentRoot, true);

                spawned++;
            }
        }

        public bool TrySpawnLandEgg(Vector3 spawnPoint, Transform parent = null)
        {
            if (bounds == null || database == null)
                return false;

            if (!bounds.IsClearForSpawnAt(spawnPoint, LandEggClearance))
                return false;

            var egg = FinishLandEggSpawn(spawnPoint);
            if (egg == null)
                return false;

            if (parent != null)
                egg.transform.SetParent(parent, true);

            return true;
        }

        public bool TrySpawnLandCreature(Vector3 spawnPoint, Transform parent = null)
        {
            if (bounds == null || database == null)
                return false;

            if (!bounds.IsClearForSpawnAt(spawnPoint, LandEggClearance))
                return false;

            if (!TryPickMapCreature(spawnPoint, out var creatureTypeId, out var definition))
                return false;

            var monster = HatchMonster(spawnPoint, definition, creatureTypeId);
            if (monster == null)
                return false;

            if (parent != null)
                monster.transform.SetParent(parent, true);

            return true;
        }

        MonsterEgg FinishLandEggSpawn(Vector3 spawnPoint)
        {
            if (!TryPickMapCreature(spawnPoint, out var creatureTypeId, out var definition))
                return null;

            var egg = MonsterEgg.Spawn(spawnPoint, definition);
            if (egg == null)
                return null;

            egg.SetCreatureTypeId(creatureTypeId);
            if (contentRoot != null)
                egg.transform.SetParent(contentRoot, true);

            return egg;
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

        public void EnsureJarlLandEggsSpawned()
        {
            if (jarlLandEggsSpawned || bounds == null || database == null)
                return;

            SpawnJarlLandEggs();
        }

        void SpawnJarlLandEggs()
        {
            if (jarlLandEggsSpawned)
                return;

            if (GameContext.Instance?.CaveProgression == null || !GameContext.Instance.CaveProgression.HasLandQuarry2)
                return;

            const float jarlLandEggClearance = 1.2f;
            int spawned = 0;
            int attempts = 0;
            int maxAttempts = JarlLandEggCount * PlateauSpawnAttempts;
            while (spawned < JarlLandEggCount && attempts < maxAttempts)
            {
                attempts++;
                if (!bounds.TryGetRandomClearJarlLandPoint(jarlLandEggClearance, 1, out var spawnPoint, jarlLandEggClearance))
                    continue;

                if (FinishEggSpawn(spawnPoint) != null)
                    spawned++;
            }

            if (spawned > 0)
                jarlLandEggsSpawned = true;
        }

        public void EnsureLandQuarry3EggsSpawned()
        {
            if (landQuarry3EggsSpawned || bounds == null || database == null)
                return;

            SpawnLandQuarry3Eggs();
        }

        void SpawnLandQuarry3Eggs()
        {
            if (landQuarry3EggsSpawned)
                return;

            if (GameContext.Instance?.CaveProgression == null || !GameContext.Instance.CaveProgression.HasLandQuarry3)
                return;

            const float quarryEggClearance = 1.2f;
            int spawned = 0;
            int attempts = 0;
            int maxAttempts = LandQuarry3EggCount * PlateauSpawnAttempts;
            while (spawned < LandQuarry3EggCount && attempts < maxAttempts)
            {
                attempts++;
                if (!bounds.TryGetRandomClearLandQuarry3Point(
                        quarryEggClearance,
                        1,
                        out var spawnPoint,
                        quarryEggClearance,
                        forEggSpawn: true))
                    continue;

                if (FinishEggSpawn(spawnPoint) != null)
                    spawned++;
            }

            if (spawned > 0)
                landQuarry3EggsSpawned = true;

            CullEggsInSpawnExclusions();
        }

        public void EnsureLandQuarry4EggsSpawned()
        {
            if (landQuarry4EggsSpawned || bounds == null || database == null)
                return;

            SpawnLandQuarry4Eggs();
        }

        void SpawnLandQuarry4Eggs()
        {
            if (landQuarry4EggsSpawned)
                return;

            if (GameContext.Instance?.CaveProgression == null || !GameContext.Instance.CaveProgression.HasLandQuarry4)
                return;

            const float quarryEggClearance = 1.2f;
            int spawned = 0;
            int attempts = 0;
            int maxAttempts = LandQuarry4EggCount * PlateauSpawnAttempts;
            while (spawned < LandQuarry4EggCount && attempts < maxAttempts)
            {
                attempts++;
                if (!bounds.TryGetRandomClearLandQuarry4Point(
                        quarryEggClearance,
                        1,
                        out var spawnPoint,
                        quarryEggClearance,
                        forEggSpawn: true))
                    continue;

                if (FinishEggSpawn(spawnPoint) != null)
                    spawned++;
            }

            if (spawned > 0)
                landQuarry4EggsSpawned = true;

            CullEggsInSpawnExclusions();
        }

        public void CullEggsInSpawnExclusions()
        {
            if (bounds == null)
                return;

            foreach (var egg in FindObjectsByType<MonsterEgg>(FindObjectsSortMode.None))
            {
                if (egg == null || egg.IsCarried)
                    continue;

                Vector3 local = bounds.transform.InverseTransformPoint(egg.transform.position);
                if (!bounds.IsSpawnExcludedLocal(local.x, local.z, 1.2f))
                    continue;

                Object.Destroy(egg.gameObject);
            }
        }

        MonsterEgg FinishEggSpawn(Vector3 spawnPoint)
        {
            if (!TryPickMapCreature(spawnPoint, out var creatureTypeId, out var definition))
                return null;

            var egg = MonsterEgg.Spawn(spawnPoint, definition);
            if (egg == null)
                return null;

            egg.SetCreatureTypeId(creatureTypeId);
            if (contentRoot != null)
                egg.transform.SetParent(contentRoot, true);

            return egg;
        }

        void AssignStoredEggCreatureTypes()
        {
            var eggsByMap = new Dictionary<string, List<MonsterEgg>>();
            foreach (var egg in FindObjectsByType<MonsterEgg>(FindObjectsSortMode.None))
            {
                if (egg == null)
                    continue;

                string mapId = MapSpawnCatalog.GetMapIdForWorldPoint(bounds, egg.transform.position);
                if (!eggsByMap.TryGetValue(mapId, out var pool))
                {
                    pool = new List<MonsterEgg>();
                    eggsByMap.Add(mapId, pool);
                }

                pool.Add(egg);
            }

            AssignMapEggDistribution(eggsByMap, MapSpawnCatalog.Cave1MapId, new[]
            {
                ("iguana", Random.Range(3, 6)),
                ("rabbit", Random.Range(3, 6)),
                ("cave_lizard", Random.Range(3, 6)),
                ("gremlin", Random.Range(3, 6)),
                ("salamander", Random.Range(1, 36))
            });

            AssignJarlLandEggDistribution(eggsByMap);
            AssignLandQuarry3EggDistribution(eggsByMap);
            AssignLandQuarry4EggDistribution(eggsByMap);

            foreach (var pair in eggsByMap)
            {
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    var egg = pair.Value[i];
                    if (egg == null || !string.IsNullOrEmpty(egg.CreatureTypeId))
                        continue;

                    if (!TryPickMapCreature(egg.transform.position, out var creatureTypeId, out _))
                        continue;

                    egg.SetCreatureTypeId(creatureTypeId);
                }
            }
        }

        void AssignLandQuarry4EggDistribution(Dictionary<string, List<MonsterEgg>> eggsByMap)
        {
            if (!eggsByMap.TryGetValue(MapSpawnCatalog.LandQuarry4MapId, out var pool) || pool.Count == 0)
                return;

            var allowed = MapSpawnCatalog.GetCreaturesForMap(MapSpawnCatalog.LandQuarry4MapId);
            if (allowed.Count == 0)
                return;

            int perType = Mathf.Max(1, pool.Count / allowed.Count);
            for (int i = 0; i < allowed.Count; i++)
                AssignCreatureType(pool, allowed[i], perType);

            for (int i = 0; i < pool.Count; i++)
            {
                var egg = pool[i];
                if (egg == null || !string.IsNullOrEmpty(egg.CreatureTypeId))
                    continue;

                egg.SetCreatureTypeId(allowed[Random.Range(0, allowed.Count)]);
            }
        }

        void AssignLandQuarry3EggDistribution(Dictionary<string, List<MonsterEgg>> eggsByMap)
        {
            if (!eggsByMap.TryGetValue(MapSpawnCatalog.LandQuarry3MapId, out var pool) || pool.Count == 0)
                return;

            var allowed = MapSpawnCatalog.GetCreaturesForMap(MapSpawnCatalog.LandQuarry3MapId);
            if (allowed.Count == 0)
                return;

            int perType = Mathf.Max(1, pool.Count / allowed.Count);
            for (int i = 0; i < allowed.Count; i++)
                AssignCreatureType(pool, allowed[i], perType);

            for (int i = 0; i < pool.Count; i++)
            {
                var egg = pool[i];
                if (egg == null || !string.IsNullOrEmpty(egg.CreatureTypeId))
                    continue;

                egg.SetCreatureTypeId(allowed[Random.Range(0, allowed.Count)]);
            }
        }

        void AssignJarlLandEggDistribution(Dictionary<string, List<MonsterEgg>> eggsByMap)
        {
            if (!eggsByMap.TryGetValue(MapSpawnCatalog.JarlLandMapId, out var pool) || pool.Count == 0)
                return;

            var allowed = MapSpawnCatalog.GetCreaturesForMap(MapSpawnCatalog.JarlLandMapId);
            if (allowed.Count == 0)
                return;

            int perType = Mathf.Max(1, pool.Count / allowed.Count);
            for (int i = 0; i < allowed.Count; i++)
                AssignCreatureType(pool, allowed[i], perType);

            for (int i = 0; i < pool.Count; i++)
            {
                var egg = pool[i];
                if (egg == null || !string.IsNullOrEmpty(egg.CreatureTypeId))
                    continue;

                egg.SetCreatureTypeId(allowed[Random.Range(0, allowed.Count)]);
            }
        }

        void AssignMapEggDistribution(
            Dictionary<string, List<MonsterEgg>> eggsByMap,
            string mapId,
            (string creatureTypeId, int count)[] distribution)
        {
            if (!eggsByMap.TryGetValue(mapId, out var pool) || pool.Count == 0)
                return;

            for (int i = 0; i < distribution.Length; i++)
            {
                var entry = distribution[i];
                if (!MapSpawnCatalog.AllowsWorldSpawn(entry.creatureTypeId, mapId))
                    continue;

                AssignCreatureType(pool, entry.creatureTypeId, entry.count);
            }
        }

        static void AssignCreatureType(List<MonsterEgg> pool, string typeId, int count)
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

            var definition = ResolveMonsterDefinition(creatureTypeId);
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
            definition = ResolveMonsterDefinition(creatureTypeId) ?? definition;
            if (definition == null)
                return null;

            string mapId = MapSpawnCatalog.GetMapIdForWorldPoint(bounds, position);
            if (!definition.isQuestBoss && !MapSpawnCatalog.AllowsWorldSpawn(definition.monsterId, mapId))
                return null;

            return Monster.Spawn(definition, position);
        }

        bool TryPickMapCreature(Vector3 worldPoint, out string creatureTypeId, out MonsterDefinition definition)
        {
            creatureTypeId = null;
            definition = null;

            if (bounds == null || database == null)
                return false;

            string mapId = MapSpawnCatalog.GetMapIdForWorldPoint(bounds, worldPoint);
            creatureTypeId = MapSpawnCatalog.PickRandomCreatureForMap(mapId);
            if (string.IsNullOrEmpty(creatureTypeId))
                return false;

            definition = ResolveMonsterDefinition(creatureTypeId);
            return definition != null;
        }

        MonsterDefinition ResolveMonsterDefinition(string creatureTypeId)
        {
            if (string.IsNullOrEmpty(creatureTypeId) || database == null)
                return null;

            return database.monsters.Find(m => m.monsterId == creatureTypeId);
        }
    }
}
