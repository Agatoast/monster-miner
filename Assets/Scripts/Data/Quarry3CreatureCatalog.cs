using UnityEngine;

namespace MonsterMiner.Data
{
    public static class Quarry3CreatureCatalog
    {
        public readonly struct Spec
        {
            public readonly string MonsterId;
            public readonly string DisplayName;
            public readonly int FinderPrice;
            public readonly int MeatSellPrice;
            public readonly float Hp;
            public readonly float DamagePerSecond;
            public readonly float MoveSpeedMph;
            public readonly bool Flees;
            public readonly Color BodyColor;
            public readonly string PrefabResourcePath;

            public Spec(
                string monsterId,
                string displayName,
                int finderPrice,
                int meatSellPrice,
                float hp,
                float damagePerSecond,
                float moveSpeedMph = 11f,
                bool flees = false,
                Color bodyColor = default,
                string prefabResourcePath = null)
            {
                MonsterId = monsterId;
                DisplayName = displayName;
                FinderPrice = finderPrice;
                MeatSellPrice = meatSellPrice;
                Hp = hp;
                DamagePerSecond = damagePerSecond;
                MoveSpeedMph = moveSpeedMph;
                Flees = flees;
                BodyColor = bodyColor == default ? new Color(0.55f, 0.62f, 0.72f) : bodyColor;
                PrefabResourcePath = prefabResourcePath ?? $"Models/Creatures/{monsterId}";
            }
        }

        // Populate when Quarry 3 creature art and stats are ready.
        public static readonly Spec[] Creatures =
        {
        };

        public static string[] GetMonsterIds()
        {
            if (Creatures == null || Creatures.Length == 0)
                return System.Array.Empty<string>();

            var ids = new string[Creatures.Length];
            for (int i = 0; i < Creatures.Length; i++)
                ids[i] = Creatures[i].MonsterId;
            return ids;
        }
    }
}
