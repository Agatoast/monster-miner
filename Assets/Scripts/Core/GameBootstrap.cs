using MonsterMiner.Combat;
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
            Physics.SyncTransforms();
            ctx.PlayerSpawnPoint = ResolvePlayerSpawnPoint(ctx.CavernBounds, Vector3.zero);

            var spawnGo = new GameObject("SpawnManager");
            ctx.SpawnManager = spawnGo.AddComponent<SpawnManager>();
            ctx.SpawnManager.Initialize(ctx.CavernBounds, ctx.Database);

            var walletGo = new GameObject("CurrencyWallet");
            ctx.Wallet = walletGo.AddComponent<CurrencyWallet>();

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
            bodyCollider.height = WorldScale.CharacterHeightUnits;
            bodyCollider.center = new Vector3(0f, 0f, 0f);
            var playerRb = playerGo.AddComponent<Rigidbody>();
            playerRb.mass = 70f;
            playerRb.constraints = RigidbodyConstraints.FreezeRotation;
            playerRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            playerRb.interpolation = RigidbodyInterpolation.Interpolate;
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
            ctx.PlayerRangedAmmo = playerGo.AddComponent<RangedWeaponAmmo>();
            ctx.PlayerCombat.Initialize(ctx.Player, hands);

            hands.Initialize(ctx.Player);

            var eggCarrier = playerGo.AddComponent<PlayerEggCarrier>();
            eggCarrier.Initialize(hands, hands.LeftHandAnchor);

            var finderLocator = playerGo.AddComponent<EggFinderLocator>();
            finderLocator.Initialize();

            var input = playerGo.AddComponent<PlayerInput>();
            input.Initialize(interactor, eggCarrier);

            playerGo.AddComponent<PlayerCameraShake>();
            playerGo.AddComponent<GrenadeThrowController>();
            playerGo.AddComponent<PlateauEdgeGuard>();
            var wingsFlight = playerGo.AddComponent<PlayerWingsFlight>();
            wingsFlight.Initialize();

            AttachSellStation(builder);
            StartCoroutine(FinishPlayerSpawn(playerGo, ctx));
        }

        System.Collections.IEnumerator FinishPlayerSpawn(GameObject playerGo, GameContext ctx)
        {
            yield return null;
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();

            var spawn = ResolvePlayerSpawnPoint(ctx.CavernBounds, Vector3.zero);
            ctx.PlayerSpawnPoint = spawn;
            if (ctx.Player != null)
                ctx.Player.Respawn(spawn);
            else
                playerGo.transform.position = spawn;
        }

        static Vector3 ResolvePlayerSpawnPoint(CavernBounds bounds, Vector3 localXZ)
        {
            float halfHeight = WorldScale.CharacterHeightUnits * 0.5f;
            if (bounds.TryResolveFloorWorldPoint(localXZ.x, localXZ.z, out var floorPoint))
                return floorPoint + Vector3.up * (halfHeight + WorldScale.SpawnDropHeight);

            float floorY = bounds.SampleFloorWorldY(localXZ.x, localXZ.z);
            var worldXZ = bounds.transform.TransformPoint(new Vector3(localXZ.x, 0f, localXZ.z));
            return new Vector3(worldXZ.x, floorY + halfHeight + WorldScale.SpawnDropHeight * 2f, worldXZ.z);
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
