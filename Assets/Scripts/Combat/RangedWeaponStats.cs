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
                "pistol" => new RangedWeaponConfig(25f, 9, false, 1),
                "rifle" => new RangedWeaponConfig(200f, 5, false, 1),
                "shotgun" => new RangedWeaponConfig(30f, 5, true, 1),
                "machinegun" => new RangedWeaponConfig(40f, 30, false, 3),
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

        public RangedWeaponConfig(float damagePerShot, int magazineSize, bool hitsAllInView, int roundsPerTrigger)
        {
            DamagePerShot = damagePerShot;
            MagazineSize = magazineSize;
            HitsAllInView = hitsAllInView;
            RoundsPerTrigger = roundsPerTrigger;
        }

        public bool IsValid => MagazineSize > 0 && RoundsPerTrigger > 0;
    }
}
