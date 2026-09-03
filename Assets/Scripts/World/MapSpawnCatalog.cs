using System.Collections.Generic;
using MonsterMiner.Data;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class MapSpawnCatalog
    {
        public const string Cave1MapId = "cave_1";
        public const string Cave2MapId = "cave_2";
        public const string JarlLandMapId = "jarl_land";
        public const string WildernessMapId = "wilderness";

        static readonly string[] Cave1Creatures =
        {
            "iguana",
            "rabbit",
            "cave_lizard",
            "gremlin",
            "salamander"
        };

        static readonly string[] Cave2Creatures =
        {
        };

        static string[] wildernessCreatures;

        public static string GetMapIdForWorldPoint(CavernBounds bounds, Vector3 worldPoint)
        {
            if (bounds == null)
                return Cave1MapId;

            var local = bounds.transform.InverseTransformPoint(worldPoint);
            if (bounds.IsInCave2ZoneLocal(local.x, local.z))
                return Cave2MapId;

            if (QuarryCatalog.IsLandQuarry2Local(local.x, local.z))
                return JarlLandMapId;

            if (PlateauBoundary.IsOnPlateau(local.x, local.z, bounds.Radius))
                return Cave1MapId;

            return WildernessMapId;
        }

        public static IReadOnlyList<string> GetCreaturesForMap(string mapId)
        {
            if (mapId == JarlLandMapId)
                return GetJarlLandCreatures();

            if (mapId == WildernessMapId)
                return GetWildernessCreatures();

            if (mapId == Cave2MapId)
                return Cave2Creatures;

            return Cave1Creatures;
        }

        static IReadOnlyList<string> GetJarlLandCreatures()
        {
            var level2 = Level2CreatureCatalog.GetMonsterIds();
            if (level2.Length > 0)
                return level2;

            return Cave1Creatures;
        }

        static IReadOnlyList<string> GetWildernessCreatures()
        {
            if (wildernessCreatures != null)
                return wildernessCreatures;

            var level2 = Level2CreatureCatalog.GetMonsterIds();
            if (level2.Length == 0)
            {
                wildernessCreatures = Cave1Creatures;
                return wildernessCreatures;
            }

            var combined = new string[Cave1Creatures.Length + level2.Length];
            Cave1Creatures.CopyTo(combined, 0);
            level2.CopyTo(combined, Cave1Creatures.Length);
            wildernessCreatures = combined;
            return wildernessCreatures;
        }

        public static bool AllowsWorldSpawn(string creatureTypeId, string mapId)
        {
            if (string.IsNullOrEmpty(creatureTypeId))
                return false;

            var allowed = GetCreaturesForMap(mapId);
            for (int i = 0; i < allowed.Count; i++)
            {
                if (allowed[i] == creatureTypeId)
                    return true;
            }

            return false;
        }

        public static string PickRandomCreatureForMap(string mapId)
        {
            var allowed = GetCreaturesForMap(mapId);
            if (allowed.Count == 0)
                return null;

            return allowed[Random.Range(0, allowed.Count)];
        }
    }
}
