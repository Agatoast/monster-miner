using System.Collections.Generic;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class MapSpawnCatalog
    {
        public const string Cave1MapId = "cave_1";
        public const string Cave2MapId = "cave_2";

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

        static readonly IReadOnlyDictionary<string, string[]> CreaturesByMap =
            new Dictionary<string, string[]>
            {
                { Cave1MapId, Cave1Creatures },
                { Cave2MapId, Cave2Creatures }
            };

        public static string GetMapIdForWorldPoint(CavernBounds bounds, Vector3 worldPoint)
        {
            if (bounds == null)
                return Cave1MapId;

            var local = bounds.transform.InverseTransformPoint(worldPoint);
            return bounds.IsInCave2ZoneLocal(local.x, local.z) ? Cave2MapId : Cave1MapId;
        }

        public static IReadOnlyList<string> GetCreaturesForMap(string mapId)
        {
            if (mapId != null && CreaturesByMap.TryGetValue(mapId, out var creatures))
                return creatures;

            return Cave1Creatures;
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
