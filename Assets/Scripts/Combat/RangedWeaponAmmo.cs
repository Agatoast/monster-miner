using System.Collections.Generic;
using UnityEngine;

namespace MonsterMiner.Combat
{
    public class RangedWeaponAmmo : MonoBehaviour
    {
        readonly Dictionary<string, int> roundsInMagazine = new();

        public int GetRounds(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId))
                return 0;

            if (!roundsInMagazine.TryGetValue(weaponId, out int rounds))
            {
                if (!RangedWeaponStats.TryGetConfig(weaponId, out var config))
                    return 0;

                rounds = config.MagazineSize;
                roundsInMagazine[weaponId] = rounds;
            }

            return rounds;
        }

        public bool NeedsReload(string weaponId) => GetRounds(weaponId) <= 0;

        public bool TryConsume(string weaponId, int amount, out int consumed)
        {
            consumed = 0;
            if (amount <= 0 || string.IsNullOrEmpty(weaponId))
                return false;

            int available = GetRounds(weaponId);
            if (available <= 0)
                return false;

            consumed = Mathf.Min(amount, available);
            roundsInMagazine[weaponId] = available - consumed;
            return consumed > 0;
        }

        public void Reload(string weaponId)
        {
            if (!RangedWeaponStats.TryGetConfig(weaponId, out var config))
                return;

            roundsInMagazine[weaponId] = config.MagazineSize;
        }
    }
}
