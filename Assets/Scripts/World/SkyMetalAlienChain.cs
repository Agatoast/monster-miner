using MonsterMiner.Combat;
using MonsterMiner.Core;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class SkyMetalAlienChain
    {
        static bool alien8SpawnedFromAlien7;

        public static void ResetForNewSession() => alien8SpawnedFromAlien7 = false;

        public static void HandleAlienDamaged(Monster monster, bool fromRangedWeapon)
        {
            if (monster == null || !fromRangedWeapon)
                return;

            if (!SkyMetalAlienCatalog.TryParseTier(monster.MonsterId, out int tier))
                return;

            if (!SkyMetalAlienCatalog.SpawnsOnRangedHit(tier))
                return;

            int spawnTier = SkyMetalAlienCatalog.GetSpawnTierOnRangedHit(tier);
            int spawnCount = SkyMetalAlienCatalog.GetSpawnCountOnRangedHit(tier);
            SpawnTierCluster(monster.transform.position, spawnTier, spawnCount);
        }

        public static void HandleAlienDeath(Monster monster)
        {
            if (monster == null)
                return;

            if (!SkyMetalAlienCatalog.TryParseTier(monster.MonsterId, out int tier))
                return;

            if (tier == SkyMetalAlienCatalog.TierCount)
            {
                SkyMetalLumpTracker.SpawnWorldDrop(monster.transform.position);
                return;
            }

            int spawnTier = SkyMetalAlienCatalog.GetSpawnTierOnDeath(tier);
            int spawnCount = SkyMetalAlienCatalog.GetSpawnCountOnDeath(tier);
            if (spawnTier <= 0 || spawnCount <= 0)
                return;

            if (tier == 7)
            {
                if (alien8SpawnedFromAlien7)
                    return;

                alien8SpawnedFromAlien7 = true;
                spawnCount = 1;
            }

            SpawnTierCluster(monster.transform.position, spawnTier, spawnCount);
        }

        static void SpawnTierCluster(Vector3 origin, int tier, int count)
        {
            var definition = GameContext.Instance?.Database?.GetMonster(SkyMetalAlienCatalog.GetMonsterId(tier));
            if (definition == null)
            {
                Debug.LogWarning($"Monster Miner: missing sky-metal alien definition for tier {tier}.");
                return;
            }

            float spread = WorldScale.Feet(tier >= 7 ? 5f : 3f);
            for (int i = 0; i < count; i++)
            {
                Vector2 offset2D = Random.insideUnitCircle * spread;
                Vector3 spawnPoint = origin + new Vector3(offset2D.x, 0f, offset2D.y);
                spawnPoint = SnapToPlains(spawnPoint);
                Monster.Spawn(definition, spawnPoint);
            }
        }

        static Vector3 SnapToPlains(Vector3 worldPoint)
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return worldPoint;

            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                worldPoint,
                WorldScale.CharacterHeightUnits * 0.5f);
        }
    }
}
