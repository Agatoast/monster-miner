namespace MonsterMiner.World
{
    public static class SkyMetalAlienCatalog
    {
        public const string IdPrefix = "sky_metal_alien";
        public const int TierCount = 8;
        public const float Alien1Scale = 0.2f;
        public const float Alien1MaxHealth = 120f;
        public const float Alien1MoveSpeedMph = 11.5f;
        public const float Alien1AttackDamage = 12f;
        public const float Alien8MaxHealth = 1000f;
        public const string CrabPrefabResourcePath = "Models/Creatures/crab_monster";

        public static bool IsSkyMetalAlienId(string monsterId)
        {
            if (string.IsNullOrEmpty(monsterId))
                return false;

            if (!monsterId.StartsWith(IdPrefix))
                return false;

            return TryParseTier(monsterId, out _);
        }

        public static bool TryParseTier(string monsterId, out int tier)
        {
            tier = 0;
            if (string.IsNullOrEmpty(monsterId) || !monsterId.StartsWith(IdPrefix))
                return false;

            string suffix = monsterId.Substring(IdPrefix.Length);
            return int.TryParse(suffix, out tier) && tier >= 1 && tier <= TierCount;
        }

        public static string GetMonsterId(int tier) => $"{IdPrefix}{tier}";

        public static float GetScaleForTier(int tier)
        {
            return tier switch
            {
                1 or 2 or 3 => Alien1Scale,
                4 => Alien1Scale * 2f,
                5 => Alien1Scale * 3f,
                6 => Alien1Scale * 6f,
                7 => Alien1Scale * 11f,
                8 => Alien1Scale * 21f,
                _ => Alien1Scale
            };
        }

        public static float GetMaxHealthForTier(int tier) =>
            tier == 8 ? Alien8MaxHealth : Alien1MaxHealth;

        public static int GetSpawnCountOnDeath(int tier)
        {
            return tier switch
            {
                2 or 3 or 4 or 5 => 8,
                6 => 2,
                7 => 1,
                _ => 0
            };
        }

        public static int GetSpawnTierOnDeath(int tier) =>
            tier >= 1 && tier < TierCount ? tier + 1 : 0;

        public static bool SpawnsOnRangedHit(int tier) => tier == 1;

        public static int GetSpawnCountOnRangedHit(int tier) => tier == 1 ? 8 : 0;

        public static int GetSpawnTierOnRangedHit(int tier) => tier == 1 ? 2 : 0;
    }
}
