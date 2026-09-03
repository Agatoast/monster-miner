using System.Collections;
using MonsterMiner.Combat;
using MonsterMiner.Core;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public class IslandTaipanSpawner : MonoBehaviour
    {
        void Start()
        {
            StartCoroutine(SpawnWhenReady());
        }

        IEnumerator SpawnWhenReady()
        {
            while (GameContext.Instance?.CavernBounds == null || !LakeCatalog.HasLakeIsland)
                yield return null;

            if (GetComponentInChildren<Monster>(true) != null)
                yield break;

            var definition = GameContext.Instance?.Database?.GetMonster("island_taipan");
            if (definition == null)
            {
                Debug.LogWarning("Monster Miner: island Taipan definition missing.");
                yield break;
            }

            var bounds = GameContext.Instance.CavernBounds;
            Vector3 spawnWorld = ResolveIslandSpawnWorld(bounds);
            var taipan = Monster.Spawn(definition, spawnWorld);
            if (taipan == null)
            {
                Debug.LogWarning("Monster Miner: failed to spawn island Taipan prefab.");
                yield break;
            }

            taipan.transform.SetParent(transform, true);
            taipan.AlignToIslandSurface(bounds);
        }

        Vector3 ResolveIslandSpawnWorld(CavernBounds bounds)
        {
            Vector3 world = transform.position;
            if (bounds == null)
                return world;

            Vector3 contentLocal = bounds.transform.InverseTransformPoint(world);
            if (!LakeIslandVisualFactory.TrySampleWorldY(contentLocal.x, contentLocal.z, bounds.transform, out float surfaceWorldY))
                surfaceWorldY = PlainsGroundSupport.SampleSupportGroundWorldY(bounds, contentLocal.x, contentLocal.z);

            world.y = surfaceWorldY;
            return world;
        }
    }
}
