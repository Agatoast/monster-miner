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
            SetupLandStart(ctx);
            if (ctx.PlayerSpawnPoint == Vector3.zero)
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

            var itemSkinsGo = new GameObject("ItemSkinCollection");
            ctx.ItemSkins = itemSkinsGo.AddComponent<ItemSkinCollection>();
            ctx.ItemSkins.Initialize(ctx.Database);

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
            playerGo.AddComponent<QuarryVisitTracker>();

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

            var creatureCarrier = playerGo.AddComponent<PlayerCreatureCarrier>();
            creatureCarrier.Initialize(hands.LeftHandAnchor);

            var finderLocator = playerGo.AddComponent<EggFinderLocator>();
            finderLocator.Initialize();

            var vehicleMount = playerGo.AddComponent<PlayerVehicleMount>();
            vehicleMount.Initialize();

            var input = playerGo.AddComponent<PlayerInput>();
            input.Initialize(interactor, eggCarrier, creatureCarrier);

            playerGo.AddComponent<PlayerCameraShake>();
            playerGo.AddComponent<GrenadeThrowController>();
            playerGo.AddComponent<PlateauEdgeGuard>();
            playerGo.AddComponent<PlainsGroundSupport>();
            playerGo.AddComponent<LakeTraversalGuard>();
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

            var spawn = ctx.PlayerSpawnPoint;
            if (spawn == Vector3.zero)
                spawn = ResolvePlayerSpawnPoint(ctx.CavernBounds, Vector3.zero);
            ctx.PlayerSpawnPoint = spawn;
            if (ctx.Player != null)
            {
                ctx.Player.Respawn(spawn);
                if (ctx.CaveProgression != null && ctx.CaveProgression.HasLandQuarry2 && ctx.CavernBounds != null)
                {
                    Vector3 boatLocal = LakeCatalog.GetBoatBeachContentLocal(
                        WorldScale.Feet(0.12f),
                        0f);
                    Vector3 lookTarget = ctx.CavernBounds.transform.TransformPoint(boatLocal);
                    Vector3 toTarget = lookTarget - ctx.Player.transform.position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.01f)
                        ctx.Player.transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                }
                else if (ctx.PlayerTruck != null)
                {
                    Vector3 toTruck = ctx.PlayerTruck.transform.position - ctx.Player.transform.position;
                    toTruck.y = 0f;
                    if (toTruck.sqrMagnitude > 0.01f)
                        ctx.Player.transform.rotation = Quaternion.LookRotation(toTruck.normalized, Vector3.up);
                }
            }
            else
                playerGo.transform.position = spawn;

            ctx.CaveProgression?.CompleteMinerHeartTurnIn();
            DevTestLoadout.Apply(ctx);

            yield return null;
            EnsureTruckRenderersEnabled(ctx.PlayerTruck);
        }

        static void EnsureTruckRenderersEnabled(DriveableTruck truck)
        {
            if (truck == null)
                return;

            foreach (var renderer in truck.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
        }

        static void SetupLandStart(GameContext ctx)
        {
            if (ctx?.CavernBounds == null)
                return;

            if (!TryResolveLandTruckStart(ctx.CavernBounds, out var truckPoint, out var truckRotation, out _))
                return;

            ctx.CaveProgression?.GrantWorldMap();
            ctx.CaveProgression?.NotifyLandedOnLand();

            var truck = IndustrialSmallTruckVisualFactory.CreateOnGround(
                ctx.CavernBounds.transform,
                truckPoint,
                truckRotation);
            if (truck != null)
                ctx.PlayerTruck = truck;

            ctx.PlayerSpawnPoint = QuarryCatalog.ResolveHallFrontSpawnWorld(ctx.CavernBounds);
            if (ctx.CaveProgression != null && ctx.CaveProgression.HasLandQuarry2
                && !QuarryCatalog.SpawnPlayerOnIslandForTesting
                && !QuarryCatalog.SpawnPlayerAtJarlLandShopForTesting)
            {
                ctx.PlayerSpawnPoint = LakeCatalog.ResolveBoatSandSpawnWorld(
                    ctx.CavernBounds,
                    WorldScale.Feet(0.12f),
                    0f,
                    0f,
                    LakeCatalog.BoatPlayerSpawnShoreInsetFeet);
            }
        }

        static bool TryResolveLandTruckStart(
            CavernBounds bounds,
            out Vector3 truckFloorContact,
            out Quaternion truckRotation,
            out Vector3 playerSpawn)
        {
            truckFloorContact = Vector3.zero;
            truckRotation = Quaternion.identity;
            playerSpawn = Vector3.zero;

            const float southAngle = -Mathf.PI * 0.5f;
            float wall = PlateauWallGeometry.GetWallBaseOutwardRadius(southAngle, bounds.Radius);
            float distance = wall + WorldScale.Feet(140f);
            float localX = Mathf.Cos(southAngle) * distance;
            float localZ = Mathf.Sin(southAngle) * distance;
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(localX, localZ, plainsBaseY);

            truckFloorContact = bounds.transform.TransformPoint(new Vector3(localX, groundY, localZ));
            Vector3 toPlateau = bounds.transform.position - truckFloorContact;
            toPlateau.y = 0f;
            if (toPlateau.sqrMagnitude < 0.01f)
                toPlateau = bounds.transform.forward;
            truckRotation = Quaternion.LookRotation(toPlateau.normalized, Vector3.up);

            Vector3 driverSide = truckRotation * Vector3.left;
            playerSpawn = truckFloorContact + driverSide * WorldScale.Feet(5f);
            playerSpawn = PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                playerSpawn,
                WorldScale.CharacterHeightUnits * 0.5f);
            return true;
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
