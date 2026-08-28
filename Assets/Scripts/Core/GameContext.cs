using MonsterMiner.Combat;
using MonsterMiner.Economy;
using MonsterMiner.Inventory;
using MonsterMiner.Player;
using MonsterMiner.UI;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Core
{
    public class GameContext : MonoBehaviour
    {
        public static GameContext Instance { get; private set; }

        public CavernBounds CavernBounds { get; set; }
        public PlayerController Player { get; set; }
        public PlayerHealth PlayerHealth { get; set; }
        public PlayerThirst PlayerThirst { get; set; }
        public PlayerCombat PlayerCombat { get; set; }
        public RangedWeaponAmmo PlayerRangedAmmo { get; set; }
        public InventorySystem Inventory { get; set; }
        public CurrencyWallet Wallet { get; set; }
        public SpawnManager SpawnManager { get; set; }
        public ShopManager Shop { get; set; }
        public CaveProgression CaveProgression { get; set; }
        public Shopkeeper Shopkeeper { get; set; }
        public SlotMachine SlotMachine { get; set; }
        public HudController Hud { get; set; }
        public GameDatabase Database { get; set; }

        public Vector3 PlayerSpawnPoint { get; set; } = new Vector3(0f, 1.5f, 0f);

        public bool IsPlayerDead => PlayerHealth != null && PlayerHealth.IsDead;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void MakeAllMonstersFlee()
        {
            foreach (var monster in FindObjectsByType<Monster>(FindObjectsSortMode.None))
                monster.ForceFlee();
        }

        public void DespawnAllMonsters()
        {
            foreach (var monster in FindObjectsByType<Monster>(FindObjectsSortMode.None))
                Destroy(monster.gameObject);
        }
    }
}
