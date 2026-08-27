using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Inventory;
using MonsterMiner.Player;
using MonsterMiner.UI;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (FindFirstObjectByType<GameBootstrap>() != null)
                return;
            var go = new GameObject("MonsterMiner_Bootstrap");
            go.AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            if (FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            BuildGame();
        }

        void BuildGame()
        {
            RenderPipelineSetup.Apply();

            var contextGo = new GameObject("GameContext");
            var ctx = contextGo.AddComponent<GameContext>();
            ctx.Database = GameDatabase.CreateRuntimeDefaults();
            ctx.CaveProgression = contextGo.AddComponent<CaveProgression>();

            var hudGo = new GameObject("HudController");
            ctx.Hud = hudGo.AddComponent<HudController>();
            ctx.Hud.Build();

            var cavernGo = new GameObject("CavernBuilder");
            var builder = cavernGo.AddComponent<CavernBuilder>();
            ctx.CavernBounds = builder.Build(Vector3.zero);
            ctx.PlayerSpawnPoint = new Vector3(
                0f,
                ctx.CavernBounds.FloorTopWorldY + 1.25f,
                0f);

            var spawnGo = new GameObject("SpawnManager");
            ctx.SpawnManager = spawnGo.AddComponent<SpawnManager>();
            ctx.SpawnManager.Initialize(ctx.CavernBounds, ctx.Database);

            var walletGo = new GameObject("CurrencyWallet");
            ctx.Wallet = walletGo.AddComponent<CurrencyWallet>();
            ctx.Wallet.Add(10);

            var inventoryGo = new GameObject("InventorySystem");
            ctx.Inventory = inventoryGo.AddComponent<InventorySystem>();
            ctx.Inventory.Initialize(3);
            ctx.Inventory.SetReservedPickaxe(ctx.Database.pickaxeItem);
            ctx.Inventory.EquipGloves(ctx.Database.glovesGray);

            var playerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerGo.name = "Player";
            playerGo.transform.position = ctx.PlayerSpawnPoint;
            playerGo.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            Destroy(playerGo.GetComponent<CapsuleCollider>());
            var bodyCollider = playerGo.AddComponent<CapsuleCollider>();
            bodyCollider.height = 2f;
            bodyCollider.center = new Vector3(0f, 0f, 0f);
            var playerRb = playerGo.AddComponent<Rigidbody>();
            playerRb.mass = 70f;
            playerRb.constraints = RigidbodyConstraints.FreezeRotation;
            playerGo.GetComponent<Renderer>().enabled = false;

            ctx.Player = playerGo.AddComponent<PlayerController>();
            ctx.Player.Initialize(ctx.PlayerSpawnPoint);

            ctx.PlayerHealth = playerGo.AddComponent<PlayerHealth>();
            ctx.PlayerHealth.Initialize(ctx.Player, 100f);

            ctx.PlayerThirst = playerGo.AddComponent<PlayerThirst>();
            ctx.PlayerThirst.Initialize();

            var interactor = playerGo.AddComponent<Interactor>();
            interactor.Initialize(ctx.Player.ViewCamera);

            var hands = playerGo.AddComponent<PlayerHands>();

            ctx.PlayerCombat = playerGo.AddComponent<PlayerCombat>();
            ctx.PlayerCombat.Initialize(ctx.Player, hands);

            hands.Initialize(ctx.Player);

            var eggCarrier = playerGo.AddComponent<PlayerEggCarrier>();
            eggCarrier.Initialize(hands, hands.LeftHandAnchor);

            var input = playerGo.AddComponent<PlayerInput>();
            input.Initialize(interactor, eggCarrier);

            playerGo.AddComponent<PlayerCameraShake>();

            AttachSellStation(builder);
        }

        void AttachSellStation(CavernBuilder builder)
        {
            var shopkeeper = GameObject.Find("Shopkeeper");
            if (shopkeeper == null)
                return;

            var station = shopkeeper.GetComponent<ShopSellStation>();
            if (station == null)
                return;

            var ctx = GameContext.Instance;
            if (ctx?.Player?.ViewCamera != null)
                station.ConfigureHitbox(ctx.Player.ViewCamera);
        }
    }
}
