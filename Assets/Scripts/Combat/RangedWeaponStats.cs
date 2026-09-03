namespace MonsterMiner.Combat
{
    public static class RangedWeaponStats
    {
        public const float FireIntervalSeconds = 1f;
        public const int MachineGunBurstCount = 3;

        public static bool TryGetConfig(string weaponId, out RangedWeaponConfig config)
        {
            weaponId = Inventory.InventorySystem.ResolveBaseWeaponId(weaponId);
            config = weaponId switch
            {
                "pistol" => new RangedWeaponConfig(15f, 9, false, 1, 1f),
                "rifle" => new RangedWeaponConfig(25f, 5, false, 1, 2f),
                "shotgun" => new RangedWeaponConfig(20f, 5, true, 1, 3f),
                "machinegun" => new RangedWeaponConfig(15f, 30, false, 3, 3f),
                _ => default
            };

            return config.IsValid;
        }
    }

    public readonly struct RangedWeaponConfig
    {
        public readonly float DamagePerShot;
        public readonly int MagazineSize;
        public readonly bool HitsAllInView;
        public readonly int RoundsPerTrigger;
        public readonly float RecoilDegrees;

        public RangedWeaponConfig(
            float damagePerShot,
            int magazineSize,
            bool hitsAllInView,
            int roundsPerTrigger,
            float recoilDegrees)
        {
            DamagePerShot = damagePerShot;
            MagazineSize = magazineSize;
            HitsAllInView = hitsAllInView;
            RoundsPerTrigger = roundsPerTrigger;
            RecoilDegrees = recoilDegrees;
        }

        public bool IsValid => MagazineSize > 0 && RoundsPerTrigger > 0;
    }
}
