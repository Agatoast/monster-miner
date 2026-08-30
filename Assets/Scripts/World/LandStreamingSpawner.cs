using System.Collections.Generic;
using MonsterMiner.Core;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public class LandStreamingSpawner : MonoBehaviour
    {
        const float SpawnRadiusFeet = 2800f;
        const float DespawnRadiusFeet = 4200f;
        const int MaxChunkBuildsPerFrame = 2;

        readonly Dictionary<long, Transform> activeChunks = new Dictionary<long, Transform>(64);
        readonly Queue<(int chunkX, int chunkZ)> pendingChunks = new Queue<(int, int)>(32);
        readonly HashSet<long> queuedKeys = new HashSet<long>();

        CavernBounds bounds;
        SpawnManager spawnManager;
        Transform contentRoot;
        float plainsBaseY;
        float spawnRadius;
        float despawnRadius;
        float chunkSize;

        public void Initialize(CavernBounds cavernBounds, SpawnManager landSpawnManager)
        {
            bounds = cavernBounds;
            spawnManager = landSpawnManager;
            contentRoot = cavernBounds != null ? cavernBounds.transform : transform;
            plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            spawnRadius = WorldScale.Feet(SpawnRadiusFeet);
            despawnRadius = WorldScale.Feet(DespawnRadiusFeet);
            chunkSize = LandChunkPlacement.ChunkSize;
        }

        void Update()
        {
            if (bounds == null)
                return;

            var player = GameContext.Instance?.Player;
            if (player == null)
                return;

            Vector3 local = bounds.transform.InverseTransformPoint(player.transform.position);
            MaintainChunks(new Vector2(local.x, local.z));
            BuildPendingChunks();
        }

        void MaintainChunks(Vector2 playerLocal)
        {
            int playerChunkX = Mathf.FloorToInt(playerLocal.x / chunkSize);
            int playerChunkZ = Mathf.FloorToInt(playerLocal.y / chunkSize);
            int chunkRadius = Mathf.CeilToInt(spawnRadius / chunkSize) + 1;

            for (int dx = -chunkRadius; dx <= chunkRadius; dx++)
            {
                for (int dz = -chunkRadius; dz <= chunkRadius; dz++)
                {
                    int chunkX = playerChunkX + dx;
                    int chunkZ = playerChunkZ + dz;
                    Vector2 chunkCenter = new Vector2(
                        (chunkX + 0.5f) * chunkSize,
                        (chunkZ + 0.5f) * chunkSize);

                    if (Vector2.Distance(chunkCenter, playerLocal) > spawnRadius)
                        continue;

                    if (!LandChunkPlacement.ChunkIntersectsLand(chunkX, chunkZ, bounds))
                        continue;

                    QueueChunk(chunkX, chunkZ);
                }
            }

            var toRemove = new List<long>(8);
            foreach (var pair in activeChunks)
            {
                DecodeChunkKey(pair.Key, out int chunkX, out int chunkZ);
                Vector2 chunkCenter = new Vector2(
                    (chunkX + 0.5f) * chunkSize,
                    (chunkZ + 0.5f) * chunkSize);

                if (Vector2.Distance(chunkCenter, playerLocal) > despawnRadius)
                    toRemove.Add(pair.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
                DestroyChunk(toRemove[i]);
        }

        void QueueChunk(int chunkX, int chunkZ)
        {
            long key = EncodeChunkKey(chunkX, chunkZ);
            if (activeChunks.ContainsKey(key) || queuedKeys.Contains(key))
                return;

            queuedKeys.Add(key);
            pendingChunks.Enqueue((chunkX, chunkZ));
        }

        void BuildPendingChunks()
        {
            int built = 0;
            while (built < MaxChunkBuildsPerFrame && pendingChunks.Count > 0)
            {
                var (chunkX, chunkZ) = pendingChunks.Dequeue();
                long key = EncodeChunkKey(chunkX, chunkZ);
                queuedKeys.Remove(key);

                if (activeChunks.ContainsKey(key))
                    continue;

                BuildChunk(chunkX, chunkZ);
                built++;
            }
        }

        void BuildChunk(int chunkX, int chunkZ)
        {
            long key = EncodeChunkKey(chunkX, chunkZ);
            var chunkRoot = new GameObject($"LandChunk_{chunkX}_{chunkZ}").transform;
            chunkRoot.SetParent(contentRoot, false);

            var treeRoot = new GameObject("Trees").transform;
            treeRoot.SetParent(chunkRoot, false);
            var rockRoot = new GameObject("Rocks").transform;
            rockRoot.SetParent(chunkRoot, false);
            var eggRoot = new GameObject("Eggs").transform;
            eggRoot.SetParent(chunkRoot, false);
            var creatureRoot = new GameObject("Creatures").transform;
            creatureRoot.SetParent(chunkRoot, false);

            float outer = WorldRegion.GetLandOuterRadius(bounds.Radius);
            float SampleGround(float x, float z) => PlainsWorldBuilder.SamplePlainsLocalY(x, z, plainsBaseY);

            LandChunkPlacement.ForEachTreeInChunk(chunkX, chunkZ, bounds, (localX, localZ, copseSeed) =>
            {
                var center = new Vector2(localX, localZ);
                float sizeScale = center.magnitude < outer * 0.35f ? 1.15f : 1.7f;
                var randomState = Random.state;
                Random.InitState(copseSeed);
                PlainsTreeVisualFactory.CreateTreeCopse(
                    treeRoot,
                    center,
                    copseSeed,
                    SampleGround,
                    sizeScale: sizeScale);
                Random.state = randomState;
            });

            LandChunkPlacement.ForEachRockInChunk(chunkX, chunkZ, bounds, (localX, localZ, rockSeed) =>
            {
                float groundY = SampleGround(localX, localZ);
                StylizedRockVisualFactory.CreateOnGround(
                    rockRoot,
                    chunkRoot.TransformPoint(new Vector3(localX, groundY, localZ)),
                    rockSeed);
            });

            LandChunkPlacement.ForEachEggInChunk(chunkX, chunkZ, bounds, (localX, localZ) =>
            {
                if (!bounds.TryResolveFloorWorldPoint(localX, localZ, out var spawnPoint))
                    return;

                spawnManager?.TrySpawnLandEgg(spawnPoint, eggRoot);
            });

            LandChunkPlacement.ForEachCreatureInChunk(chunkX, chunkZ, bounds, (localX, localZ) =>
            {
                if (!bounds.TryResolveFloorWorldPoint(localX, localZ, out var spawnPoint))
                    return;

                spawnManager?.TrySpawnLandCreature(spawnPoint, creatureRoot);
            });

            AddChunkGroundCollider(chunkRoot, chunkX, chunkZ);

            activeChunks.Add(key, chunkRoot);
        }

        void AddChunkGroundCollider(Transform chunkRoot, int chunkX, int chunkZ)
        {
            float minX = chunkX * chunkSize;
            float minZ = chunkZ * chunkSize;
            float centerX = minX + chunkSize * 0.5f;
            float centerZ = minZ + chunkSize * 0.5f;
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(centerX, centerZ, plainsBaseY);
            float thickness = WorldScale.PlateauGroundThickness * 6f;

            var groundGo = new GameObject($"PlainsGroundCollider_{chunkX}_{chunkZ}");
            groundGo.transform.SetParent(chunkRoot, false);
            groundGo.transform.localPosition = new Vector3(centerX, groundY - thickness * 0.5f, centerZ);

            var box = groundGo.AddComponent<BoxCollider>();
            box.size = new Vector3(chunkSize, thickness, chunkSize);
        }

        void DestroyChunk(long key)
        {
            if (!activeChunks.TryGetValue(key, out var chunkRoot))
                return;

            if (chunkRoot != null)
                Destroy(chunkRoot.gameObject);

            activeChunks.Remove(key);
        }

        static long EncodeChunkKey(int chunkX, int chunkZ)
        {
            return ((long)chunkX << 32) | (uint)chunkZ;
        }

        static void DecodeChunkKey(long key, out int chunkX, out int chunkZ)
        {
            chunkX = (int)(key >> 32);
            chunkZ = (int)(key & 0xFFFFFFFF);
        }
    }
}
