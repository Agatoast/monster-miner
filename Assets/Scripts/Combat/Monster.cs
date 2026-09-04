using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Interaction;
using MonsterMiner.Inventory;
using MonsterMiner.Player;
using MonsterMiner.UI;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Combat
{
    public class Monster : MonoBehaviour, IInteractable
    {
        const float GroundRestOffset = 0.02f;
        const float PlayerNoticeDistanceFeet = 100f;
        const float WanderSpeedMultiplier = 0.55f;
        const float WanderMinSeconds = 1.4f;
        const float WanderMaxSeconds = 3.8f;
        const float WanderPauseChance = 0.18f;
        const float WanderPauseMinSeconds = 0.6f;
        const float WanderPauseMaxSeconds = 1.8f;

        MonsterDefinition definition;
        float currentHealth;
        float nextAttackTime;
        float groundOffset = 1f;
        float spawnHover;
        Transform player;
        Rigidbody rb;
        Collider bodyCollider;

        bool forceFlee;
        bool isCarried;
        bool isInAir;
        Vector3 wanderDirection;
        float wanderDirectionTimer;
        Vector3 islandMoveVelocity;

        public bool IsCarried => isCarried;
        public bool IsInAir => isInAir;
        public bool IsNonAggressiveMonster => IsNonAggressive(definition);
        public bool CanBePickedUp =>
            definition != null
            && currentHealth > 0f
            && spawnHover <= 0f
            && !isCarried
            && !isInAir
            && IsNonAggressive(definition);

        public static bool IsNonAggressive(MonsterDefinition def)
        {
            if (def == null || def.isQuestBoss)
                return false;

            return def.fleesFromPlayer || def.attackDamage <= 0f;
        }

        public void ForceFlee() => forceFlee = true;

        public void LaunchFromTruck(Vector3 truckVelocity)
        {
            if (isCarried || isInAir || currentHealth <= 0f)
                return;

            TruckLaunchPhysics.LaunchCreature(this, truckVelocity);
        }

        public static Monster Spawn(MonsterDefinition def, Vector3 position)
        {
            GameObject go = TryCreatePrefabMonster(def, position)
                ?? PrimitiveFactory.CreatePrimitive(PrimitiveType.Capsule, position, Vector3.one * def.scale, def.bodyColor, def.displayName);

            if (go.GetComponent<Rigidbody>() == null)
                PrimitiveFactory.EnsureRigidbody(go, 2f * def.scale);

            var monster = go.GetComponent<Monster>() ?? go.AddComponent<Monster>();
            monster.Initialize(def);
            monster.BeginSpawnDrop();
            monster.AlignToGround(position, immediate: true);

            EnforcePlateauShellIfNeeded(go);
            if (def?.monsterId != "island_taipan")
                monster.AlignToGround(go.transform.position, immediate: true);

            var body = go.GetComponent<Rigidbody>();
            if (body != null)
                body.linearVelocity = Vector3.zero;
            return monster;
        }

        static GameObject TryCreatePrefabMonster(MonsterDefinition def, Vector3 position)
        {
            if (def == null || string.IsNullOrEmpty(def.visualPrefabResourcePath))
                return null;

            if (def.visualPrefabResourcePath == "Models/Creatures/iguana")
                return IguanaVisualFactory.CreateMonster(position, def.scale, def.displayName);

            if (def.visualPrefabResourcePath == "Models/Creatures/rabbit")
                return RabbitVisualFactory.CreateMonster(position, def.scale, def.displayName);

            if (def.visualPrefabResourcePath == "Models/Creatures/cave_lizard")
                return CaveLizardVisualFactory.CreateMonster(position, def.scale, def.displayName);

            if (def.visualPrefabResourcePath == "Models/Creatures/gremlin")
                return GremlinVisualFactory.CreateMonster(position, def.scale, def.displayName);

            if (def.visualPrefabResourcePath == "Models/Creatures/salamander")
                return SalamanderVisualFactory.CreateMonster(position, def.scale, def.displayName);

            if (def.visualPrefabResourcePath == "Models/Creatures/pentachick")
                return PentachickVisualFactory.CreateMonster(position, def.scale, def.displayName);

            if (def.visualPrefabResourcePath == "Models/Creatures/taipan")
                return TaipanVisualFactory.CreateMonster(position, def.scale, def.displayName);

            if (def.visualPrefabResourcePath == "Models/Creatures/mara")
                return MaraVisualFactory.CreateMonster(position, def.scale, def.displayName);

            var prefab = Resources.Load<GameObject>(def.visualPrefabResourcePath);
            if (prefab == null)
                return null;

            var instance = Object.Instantiate(prefab, position, Quaternion.identity);
            instance.name = def.displayName;
            instance.transform.localScale = Vector3.one * def.scale;
            return instance;
        }

        void Initialize(MonsterDefinition def)
        {
            definition = def;
            currentHealth = def.maxHealth;
            rb = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<Collider>();
            if (bodyCollider != null)
                DriveableTruck.RegisterPassThroughObstacle(bodyCollider);
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            Physics.SyncTransforms();
            if (IsIslandTaipan)
                IgnoreIslandTerrainCollision();
            else
            {
                float bottomY = FloorAnchor.GetBottomY(gameObject);
                groundOffset = transform.position.y - bottomY + GroundRestOffset;
                if (groundOffset < GroundRestOffset)
                    groundOffset = GroundRestOffset;
            }

            var ctx = GameContext.Instance;
            if (ctx?.Player != null)
                player = ctx.Player.transform;
        }

        void TryMove(Vector3 worldDelta)
        {
            if (IsIslandTaipan)
            {
                TryMoveOnIsland(worldDelta);
                return;
            }

            Vector3 current = rb != null ? rb.position : transform.position;
            Vector3 next = current + worldDelta;
            if (worldDelta.sqrMagnitude > 0.0001f)
            {
                next = PlainsMovementCollision.ResolvePosition(
                    transform,
                    bodyCollider,
                    rb,
                    current,
                    worldDelta);
            }

            AlignToGround(next);
        }

        void TryMoveOnIsland(Vector3 worldDelta)
        {
            Vector3 current = transform.position;
            Vector3 target = current + worldDelta;
            target.y = current.y;

            const float smoothTime = 0.08f;
            Vector3 next = Vector3.SmoothDamp(current, target, ref islandMoveVelocity, smoothTime, float.MaxValue, Time.fixedDeltaTime);
            next.y = current.y;
            SetIslandWorldPosition(next);
        }

        void SetIslandWorldPosition(Vector3 worldPos)
        {
            transform.position = worldPos;
            if (rb != null)
            {
                rb.position = worldPos;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        void IgnoreIslandTerrainCollision()
        {
            if (bodyCollider == null)
                return;

            var islandCollider = LakeIslandVisualFactory.IslandTerrainCollider;
            if (islandCollider != null)
                Physics.IgnoreCollision(bodyCollider, islandCollider, true);
        }

        bool IsPentachick => definition != null && definition.monsterId == "pentachick";
        bool IsIslandTaipan => definition != null && definition.monsterId == "island_taipan";

        public void AlignToIslandSurface(CavernBounds bounds)
        {
            if (bounds == null)
                return;

            TaipanVisualFactory.PrepareAnimatedGroundSample(gameObject);
            GetComponent<TaipanGroundAlign>()?.AlignVisualBottomToIsland(immediate: true);
            IgnoreIslandTerrainCollision();
        }

        void BeginSpawnDrop()
        {
            spawnHover = IsPentachick || definition?.monsterId == "island_taipan"
                ? 0f
                : WorldScale.SpawnDropHeight;
        }

        void AlignToGround(Vector3 worldPos, bool immediate = false)
        {
            if (IsIslandTaipan)
            {
                SetIslandWorldPosition(new Vector3(worldPos.x, transform.position.y, worldPos.z));
                if (immediate)
                {
                    TaipanVisualFactory.PrepareAnimatedGroundSample(gameObject);
                    GetComponent<TaipanGroundAlign>()?.AlignVisualBottomToIsland(immediate: true);
                }

                return;
            }

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds != null)
            {
                var local = bounds.transform.InverseTransformPoint(worldPos);
                float surfaceY = SampleSurfaceWorldY(bounds, local.x, local.z);
                worldPos.y = IsPentachick
                    ? surfaceY + WorldScale.CharacterHeightUnits * 0.5f
                    : surfaceY + groundOffset + spawnHover;
            }

            if (rb != null && !immediate)
            {
                rb.MovePosition(worldPos);
                return;
            }

            if (rb != null)
                rb.position = worldPos;
            transform.position = worldPos;
        }

        static float SampleSurfaceWorldY(CavernBounds bounds, float localX, float localZ)
        {
            if (bounds == null)
                return 0f;

            return CreatureSurfaceSampler.SampleWorldY(bounds, localX, localZ);
        }

        public string GetPrompt()
        {
            if (!CanBePickedUp)
                return string.Empty;

            return $"Pick up {definition.displayName} [E]";
        }

        public bool CanInteract(GameObject interactor)
        {
            if (!CanBePickedUp)
                return false;

            var eggCarrier = interactor.GetComponent<PlayerEggCarrier>();
            if (eggCarrier != null && eggCarrier.IsCarryingEgg)
                return false;

            var creatureCarrier = interactor.GetComponent<PlayerCreatureCarrier>();
            return creatureCarrier != null && !creatureCarrier.IsCarrying;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
                return;

            interactor.GetComponent<PlayerCreatureCarrier>()?.TryPickUp(this);
        }

        public void SetCarried(Transform anchor, Vector3 localPosition, Vector3 localEuler)
        {
            isCarried = true;
            isInAir = false;
            if (bodyCollider != null)
                bodyCollider.enabled = false;

            transform.SetParent(anchor, false);
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(localEuler);
        }

        public void BeginAirborneThrow()
        {
            isCarried = false;
            isInAir = true;
            transform.SetParent(null, true);
            if (bodyCollider != null)
                bodyCollider.enabled = true;
        }

        public void CompleteTruckLaunchRecovery(Vector3 landPoint, bool _)
        {
            isInAir = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            AlignToGround(landPoint, immediate: true);
            EnforcePlateauShellIfNeeded(gameObject);
            AlignToGround(transform.position, immediate: true);
        }

        public void CompleteThrowLanding(Vector3 landPoint)
        {
            isInAir = false;
            if (FloorAnchor.TryResolveFloorPoint(landPoint, 16f, 32f, out var floorPoint))
                landPoint = floorPoint;

            AlignToGround(landPoint, immediate: true);
            EnforcePlateauShellIfNeeded(gameObject);
            AlignToGround(transform.position, immediate: true);
        }

        void FixedUpdate()
        {
            if (spawnHover > 0f)
            {
                spawnHover = Mathf.Max(0f, spawnHover - WorldScale.Feet(8f) * Time.fixedDeltaTime);
                AlignToGround(rb != null ? rb.position : transform.position, immediate: true);
            }

            if (isCarried || isInAir)
                return;

            if (player == null)
            {
                var ctx = GameContext.Instance;
                if (ctx?.Player != null)
                    player = ctx.Player.transform;
                else
                    Wander();

                return;
            }

            if (definition == null)
                return;

            if (IsIslandTaipan && !IsPlayerOnWalkableIsland())
            {
                WanderOnIsland();
                return;
            }

            if (GameContext.Instance?.IsPlayerDead == true)
            {
                if (forceFlee || definition.fleesFromPlayer)
                    FleeFromPlayer();
                return;
            }

            var toPlayer = player.position - transform.position;
            toPlayer.y = 0f;

            bool alwaysChase = definition.alwaysChasePlayer;
            if (!alwaysChase && definition.chaseWhenPlayerOnIsland)
                alwaysChase = IsPlayerOnWalkableIsland();

            if (!alwaysChase)
            {
                float noticeDistance = WorldScale.Feet(PlayerNoticeDistanceFeet);
                float noticeDistanceSqr = noticeDistance * noticeDistance;
                bool playerNear = toPlayer.sqrMagnitude <= noticeDistanceSqr;

                if (!playerNear)
                {
                    Wander();
                    return;
                }
            }

            if (toPlayer.sqrMagnitude <= 0.01f)
                return;

            if (forceFlee || definition.fleesFromPlayer)
            {
                FleeFromPlayer();
                return;
            }

            if (definition.attackDamage <= 0f)
                return;

            var dir = toPlayer.normalized;
            TryMove(dir * GetMoveUnitsPerSecond(definition.moveSpeedMph) * Time.fixedDeltaTime);
            transform.rotation = Quaternion.LookRotation(dir);

            if (toPlayer.magnitude <= definition.attackRange && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + definition.attackCooldown;
                GetComponent<CaveLizardLocomotion>()?.PlayAttack();
                GetComponent<GremlinLocomotion>()?.PlayAttack();
                GetComponent<SalamanderLocomotion>()?.PlayAttack();
                GetComponent<TaipanLocomotion>()?.PlayAttack();
                var health = player.GetComponent<Player.PlayerHealth>();
                health?.TakeDamage(definition.attackDamage);
                var playerRb = player.GetComponent<Rigidbody>();
                if (playerRb != null)
                    playerRb.AddForce((player.position - transform.position).normalized * definition.knockbackForce, ForceMode.Impulse);
            }
        }

        void FleeFromPlayer()
        {
            var toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            var fleeDir = definition.fleesOverPlateauEdge
                ? GetFleeOverPlateauEdgeDirection(toPlayer)
                : GetFleeTowardWallDirection(toPlayer);
            if (fleeDir.sqrMagnitude <= 0.001f)
                return;

            float speedMph = forceFlee ? definition.moveSpeedMph * 1.35f : definition.moveSpeedMph;
            TryMove(fleeDir * GetMoveUnitsPerSecond(speedMph) * Time.fixedDeltaTime);
            transform.rotation = Quaternion.LookRotation(fleeDir);
        }

        void WanderOnIsland()
        {
            if (definition == null)
                return;

            wanderDirectionTimer -= Time.fixedDeltaTime;
            if (wanderDirectionTimer <= 0f)
                PickNewIslandWanderDirection();

            if (wanderDirection.sqrMagnitude <= 0.001f)
                return;

            TryMove(wanderDirection * GetMoveUnitsPerSecond(definition.moveSpeedMph * WanderSpeedMultiplier) * Time.fixedDeltaTime);
            transform.rotation = Quaternion.LookRotation(wanderDirection);
            KeepOnIsland(ref wanderDirection);
        }

        void PickNewIslandWanderDirection()
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
            {
                PickNewWanderDirection();
                return;
            }

            if (Random.value < WanderPauseChance)
            {
                wanderDirection = Vector3.zero;
                wanderDirectionTimer = Random.Range(WanderPauseMinSeconds, WanderPauseMaxSeconds);
                return;
            }

            Vector3 contentLocal = bounds.transform.InverseTransformPoint(transform.position);
            Vector2 center = LakeCatalog.GetLakeIslandCenterLocal();
            Vector2 toCenter = center - new Vector2(contentLocal.x, contentLocal.z);
            if (toCenter.sqrMagnitude < 0.0001f)
                toCenter = Random.insideUnitCircle.normalized;
            else
                toCenter.Normalize();

            float wanderAngle = Random.Range(-70f, 70f);
            Vector3 tangent = new Vector3(-toCenter.y, 0f, toCenter.x);
            wanderDirection = (new Vector3(toCenter.x, 0f, toCenter.y) * 0.35f + tangent * Mathf.Sin(wanderAngle * Mathf.Deg2Rad)).normalized;
            if (wanderDirection.sqrMagnitude <= 0.001f)
                wanderDirection = new Vector3(toCenter.x, 0f, toCenter.y);

            wanderDirectionTimer = Random.Range(WanderMinSeconds, WanderMaxSeconds);
        }

        void KeepOnIsland(ref Vector3 direction)
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null || !LakeCatalog.HasLakeIsland)
                return;

            Vector3 contentLocal = bounds.transform.InverseTransformPoint(transform.position);
            if (LakeIslandVisualFactory.IsIslandWalkableLandLocal(contentLocal.x, contentLocal.z, bounds.transform))
                return;

            Vector2 center = LakeCatalog.GetLakeIslandCenterLocal();
            direction = new Vector3(center.x - contentLocal.x, 0f, center.y - contentLocal.z).normalized;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        static bool IsPlayerOnWalkableIsland()
        {
            var bounds = GameContext.Instance?.CavernBounds;
            var player = GameContext.Instance?.Player;
            if (bounds == null || player == null || !LakeCatalog.HasLakeIsland)
                return false;

            Vector3 contentLocal = bounds.transform.InverseTransformPoint(player.transform.position);
            return LakeIslandVisualFactory.IsIslandWalkableLandLocal(contentLocal.x, contentLocal.z, bounds.transform);
        }

        void Wander()
        {
            if (definition == null || definition.isQuestBoss)
                return;

            wanderDirectionTimer -= Time.fixedDeltaTime;
            if (wanderDirectionTimer <= 0f)
                PickNewWanderDirection();

            if (wanderDirection.sqrMagnitude <= 0.001f)
                return;

            TryMove(wanderDirection * GetMoveUnitsPerSecond(definition.moveSpeedMph * WanderSpeedMultiplier) * Time.fixedDeltaTime);
            transform.rotation = Quaternion.LookRotation(wanderDirection);
        }

        void PickNewWanderDirection()
        {
            if (Random.value < WanderPauseChance)
            {
                wanderDirection = Vector3.zero;
                wanderDirectionTimer = Random.Range(WanderPauseMinSeconds, WanderPauseMaxSeconds);
                return;
            }

            float angle = Random.Range(0f, Mathf.PI * 2f);
            wanderDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            wanderDirectionTimer = Random.Range(WanderMinSeconds, WanderMaxSeconds);
        }

        Vector3 GetFleeOverPlateauEdgeDirection(Vector3 toPlayer)
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return GetFleeTowardWallDirection(toPlayer);

            Vector3 local = bounds.transform.InverseTransformPoint(transform.position);
            Vector3 outward = new Vector3(local.x, 0f, local.z);
            if (outward.sqrMagnitude < 0.0001f)
                outward = Vector3.forward;
            outward.Normalize();

            Vector3 outwardWorld = bounds.transform.TransformDirection(outward);
            Vector3 awayFromPlayer = toPlayer.sqrMagnitude > 0.0001f ? -toPlayer.normalized : outwardWorld;
            float angle = Mathf.Atan2(local.z, local.x);
            float edgeDistance = PlateauBoundary.SamplePlateauEdgeDistance(angle, bounds.Radius);
            float distance = new Vector2(local.x, local.z).magnitude;
            bool onPlateauTop = PlateauBoundary.IsOnPlateau(local.x, local.z, bounds.Radius)
                && distance < edgeDistance - WorldScale.Feet(3f);

            float outwardWeight = onPlateauTop ? 0.88f : 0.55f;
            float awayWeight = 1f - outwardWeight;
            Vector3 worldDir = (outwardWorld * outwardWeight + awayFromPlayer * awayWeight).normalized;
            worldDir.y = 0f;
            return worldDir.sqrMagnitude > 0.001f ? worldDir.normalized : outwardWorld;
        }

        static float GetMoveUnitsPerSecond(float mph) => WorldScale.MilesPerHour(mph);

        static void EnforcePlateauShellIfNeeded(GameObject root)
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (root == null || bounds == null)
                return;

            var local = bounds.transform.InverseTransformPoint(root.transform.position);
            if (!PlateauBoundary.IsOnPlateau(local.x, local.z, bounds.Radius))
                return;

            CavernInteriorEnforcer.EnsureInsideShell(root, bounds);
        }

        Vector3 GetFleeTowardWallDirection(Vector3 toPlayer)
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return -toPlayer.normalized;

            Vector3 local = bounds.transform.InverseTransformPoint(transform.position);
            Vector3 outward = new Vector3(local.x, 0f, local.z);
            if (outward.sqrMagnitude < 0.0001f)
                outward = Vector3.forward;
            outward.Normalize();

            Vector3 awayFromPlayer = toPlayer.sqrMagnitude > 0.0001f ? -toPlayer.normalized : outward;
            return (outward * 0.7f + awayFromPlayer * 0.3f).normalized;
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (currentHealth <= 0f)
                return;

            currentHealth -= amount;
            BloodDecal.Spawn(hitPoint);
            CombatHitFeedbackDisplay.Show(hitPoint, amount);
            if (rb != null && !isCarried && !isInAir)
                TryMove(hitDirection.normalized * (definition.knockbackForce * 0.05f));

            if (currentHealth <= 0f)
                Die();
        }

        void Die()
        {
            if (isCarried)
                GameContext.Instance?.Player?.GetComponent<PlayerCreatureCarrier>()?.ForceRelease();

            if (definition.explodesOnDeath)
            {
                var hits = Physics.OverlapSphere(transform.position, definition.explosionRadius);
                foreach (var hit in hits)
                {
                    var rbHit = hit.attachedRigidbody;
                    if (rbHit != null)
                        rbHit.AddExplosionForce(definition.explosionForce, transform.position, definition.explosionRadius, 0.5f, ForceMode.Impulse);
                    var health = hit.GetComponentInParent<Player.PlayerHealth>();
                    health?.TakeDamage(definition.attackDamage * 0.75f);
                }
            }

            if (definition.dropItem != null)
                TryGrantOrDropLoot();

            Destroy(gameObject);
        }

        void TryGrantOrDropLoot()
        {
            var drop = definition.dropItem;
            var ctx = GameContext.Instance;
            if (InventorySystem.IsPentachickHeart(drop)
                && ctx?.Inventory != null
                && ctx.Inventory.TryAdd(drop, 1))
            {
                ctx.Hud?.ShowMessage("Pentachick Heart added to inventory.");
                return;
            }

            Vector3 dropOrigin = transform.position;
            dropOrigin += new Vector3(Random.Range(-0.35f, 0.35f), 0f, Random.Range(-0.35f, 0.35f));

            if (!FloorAnchor.TryResolveFloorPoint(dropOrigin, 16f, 32f, out var dropPoint))
                dropPoint = dropOrigin;

            var pickup = WorldPickup.Spawn(drop, 1, dropPoint);
            if (pickup == null)
                return;

            var contentRoot = ctx?.CavernBounds?.transform;
            if (contentRoot != null)
                pickup.transform.SetParent(contentRoot, true);

            if (InventorySystem.IsPentachickHeart(drop) && ctx?.Inventory != null)
            {
                if (ctx.Inventory.ContainsItem(drop))
                    ctx.Hud?.ShowMessage("Pentachick Heart dropped — only one can be carried.");
                else
                    ctx.Hud?.ShowMessage("Inventory full — Pentachick Heart dropped.");
            }
        }
    }
}
