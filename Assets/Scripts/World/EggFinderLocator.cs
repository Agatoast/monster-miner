using System.Collections.Generic;
using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Inventory;
using UnityEngine;

namespace MonsterMiner.World
{
    public class EggFinderLocator : MonoBehaviour
    {
        readonly List<EggFinderMarker> markers = new();
        InventorySystem inventory;

        public void Initialize()
        {
            inventory = GameContext.Instance?.Inventory;
            if (inventory == null)
                return;

            inventory.OnSelectedChanged += OnSelectedChanged;
        }

        void OnDestroy()
        {
            if (inventory == null)
                return;

            inventory.OnSelectedChanged -= OnSelectedChanged;
        }

        void OnSelectedChanged(int _)
        {
            var finder = GetSelectedFinder();
            if (finder != null)
                ActivateFinder(finder);
        }

        void ActivateFinder(ItemDefinition finder)
        {
            if (finder == null || inventory == null)
                return;

            if (!inventory.TryRemoveFromSelected(1))
                return;

            RefreshLocate(finder);
        }

        ItemDefinition GetSelectedFinder()
        {
            var slot = inventory?.GetSelectedSlot();
            if (slot == null || slot.IsEmpty || !InventorySystem.IsEggFinder(slot.item))
                return null;

            return slot.item;
        }

        void RefreshLocate(ItemDefinition finder)
        {
            ClearMarkers();
            if (finder == null)
                return;

            string creatureLabel = ResolveCreatureDisplayName(finder.finderTargetCreatureId);
            var matches = CollectFinderEggs(finder);

            if (matches.Count == 0)
                return;

            int locateCount = finder.finderTargetCreatureId == "pentachick"
                ? 1
                : Random.Range(finder.finderLocateMin, finder.finderLocateMax + 1);
            locateCount = Mathf.Clamp(locateCount, 1, matches.Count);

            for (int i = 0; i < locateCount; i++)
            {
                int pick = Random.Range(i, matches.Count);
                (matches[i], matches[pick]) = (matches[pick], matches[i]);

                var marker = EggFinderMarker.Create(matches[i], finder.worldColor, creatureLabel);
                if (marker != null)
                    markers.Add(marker);
            }
        }

        List<MonsterEgg> CollectFinderEggs(ItemDefinition finder)
        {
            var matches = new List<MonsterEgg>();
            if (finder.finderTargetCreatureId == "pentachick")
            {
                var spawned = GameContext.Instance?.SpawnManager?.SpawnFinderEggAtMapCenter("pentachick");
                if (spawned != null)
                    matches.Add(spawned);
                return matches;
            }

            foreach (var egg in FindObjectsByType<MonsterEgg>(FindObjectsSortMode.None))
            {
                if (egg != null && egg.MatchesFinderTarget(finder.finderTargetCreatureId))
                    matches.Add(egg);
            }

            return matches;
        }

        static string ResolveCreatureDisplayName(string creatureTypeId)
        {
            if (string.IsNullOrEmpty(creatureTypeId))
                return string.Empty;

            var monsters = GameContext.Instance?.Database?.monsters;
            if (monsters == null)
                return creatureTypeId;

            foreach (var monster in monsters)
            {
                if (monster != null && monster.monsterId == creatureTypeId)
                    return monster.displayName;
            }

            return creatureTypeId;
        }

        void ClearMarkers()
        {
            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i] != null)
                    Destroy(markers[i].gameObject);
            }

            markers.Clear();
        }
    }
}
