namespace MonsterMiner.World
{
    public static class WorldScale
    {
        public const float FeetPerUnit = 3f;
        public const float FeetPerMile = 5280f;
        public const float SecondsPerHour = 3600f;

        public const float CharacterHeightFeet = 6f;
        public const float CharacterHeightUnits = CharacterHeightFeet / FeetPerUnit;

        public const float PlateauNominalRadiusFeet = 136f;
        public const float PlateauEdgeVariationFeet = 20f;
        public const float PlateauApproxDiameterFeet = 100f;

        public const float PlateauCliffHeightFeet = 500f;
        public const float PlateauWallTaperDegreesPerHundredFeet = 1f;
        public const float EdgeBarrierInsetFeet = 2f;

        public const float ShopDistanceFromSpawnFeet = 20f;

        public const float SpawnDropHeightFeet = 1f / 12f;
        public const float PlateauGroundThicknessFeet = 1f;

        public static float SpawnDropHeight => Feet(SpawnDropHeightFeet);
        public static float PlateauGroundThickness => Feet(PlateauGroundThicknessFeet);

        public const float GrenadeMaxThrowFeet = 60f;
        public const float GrenadeBlastRadiusFeet = 30f;
        public const float GrenadeMinThrowFeet = 6f;

        public static float GrenadeMaxThrowDistance => Feet(GrenadeMaxThrowFeet);
        public static float GrenadeBlastRadius => Feet(GrenadeBlastRadiusFeet);
        public static float GrenadeMinThrowDistance => Feet(GrenadeMinThrowFeet);

        public static float Feet(float feet) => feet / FeetPerUnit;

        public static float MilesPerHour(float mph)
        {
            float feetPerSecond = mph * FeetPerMile / SecondsPerHour;
            return Feet(feetPerSecond);
        }
    }
}
