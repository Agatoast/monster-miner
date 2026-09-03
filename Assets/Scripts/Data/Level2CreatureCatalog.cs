using UnityEngine;

namespace MonsterMiner.Data
{
    public static class Level2CreatureCatalog
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

        public static readonly Spec[] Creatures =
        {
            new("draugr", "Draugr", 5, 10, 49f, 10f, bodyColor: new Color(0.55f, 0.58f, 0.62f)),
            new("troll", "Troll", 6, 12, 61f, 11f, bodyColor: new Color(0.45f, 0.55f, 0.38f)),
            new("hildra", "Hildra", 7, 14, 76f, 12f, bodyColor: new Color(0.42f, 0.65f, 0.78f)),
            new("hilder", "Hilder", 7, 14, 76f, 12f, bodyColor: new Color(0.38f, 0.58f, 0.72f)),
            new("thursar", "Thursar", 8, 16, 91f, 13f, bodyColor: new Color(0.62f, 0.58f, 0.52f)),
            new("ironwood_wolf", "Ironwood Wolf", 8, 16, 91f, 13f, bodyColor: new Color(0.35f, 0.32f, 0.28f)),
            new("mara", "Mara", 9, 18, 106f, 14f, bodyColor: new Color(0.42f, 0.28f, 0.48f)),
            new("lesser_sea_monster", "Lesser Sea Monster", 9, 18, 106f, 14f, bodyColor: new Color(0.28f, 0.52f, 0.58f)),
            new("ormar", "Ormar", 10, 24, 121f, 15f, bodyColor: new Color(0.72f, 0.55f, 0.32f)),
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
