using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Inventory;
using MonsterMiner.UI;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Combat
{
    public class Monster : MonoBehaviour
    {
        MonsterDefinition definition;
        float currentHealth;
        float nextAttackTime;
        Transform player;
        Rigidbody rb;

        bool forceFlee;

        public void ForceFlee() => forceFlee = true;

        public static Monster Spawn(MonsterDefinition def, Vector3 position)
        {
            var go = PrimitiveFactory.CreatePrimitive(PrimitiveType.Capsule, position, Vector3.one * def.scale, def.bodyColor, def.displayName);
            PrimitiveFactory.EnsureRigidbody(go, 2f * def.scale);
            var monster = go.AddComponent<Monster>();
            monster.Initialize(def);

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds != null)
                CavernInteriorEnforcer.EnsureInsideShell(go, bounds);

            var body = go.GetComponent<Rigidbody>();
            if (body != null)
                body.linearVelocity = Vector3.zero;
            return monster;
        }

        void Initialize(MonsterDefinition def)
        {
            definition = def;
            currentHealth = def.maxHealth;
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            var ctx = GameContext.Instance;
            if (ctx?.Player != null)
                player = ctx.Player.transform;
        }

        void TryMove(Vector3 worldDelta)
        {
            if (worldDelta.sqrMagnitude <= 0f)
                return;

            if (rb != null)
                rb.MovePosition(rb.position + worldDelta);
            else
                transform.position += worldDelta;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds != null)
                CavernInteriorEnforcer.EnsureInsideShell(gameObject, bounds);
        }

        void Update()
        {
            if (player == null)
            {
                var ctx = GameContext.Instance;
                if (ctx?.Player != null)
                    player = ctx.Player.transform;
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
            if (toPlayer.sqrMagnitude <= 0.01f)
                return;

            if (forceFlee || definition.fleesFromPlayer)
            {
                FleeFromPlayer();
                return;
            }

            var dir = toPlayer.normalized;
            TryMove(dir * definition.moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(dir);

            if (toPlayer.magnitude <= definition.attackRange && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + definition.attackCooldown;
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
            var fleeDir = GetFleeTowardWallDirection(toPlayer);
            if (fleeDir.sqrMagnitude <= 0.001f)
                return;

            float speed = forceFlee ? definition.moveSpeed * 1.35f : definition.moveSpeed;
            TryMove(fleeDir * speed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(fleeDir);
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
            if (rb != null)
                TryMove(hitDirection.normalized * (definition.knockbackForce * 0.05f));

            if (currentHealth <= 0f)
                Die();
        }

        void Die()
        {
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
            {
                Vector3 dropOrigin = transform.position;
                dropOrigin += new Vector3(Random.Range(-0.35f, 0.35f), 0f, Random.Range(-0.35f, 0.35f));

                if (!FloorAnchor.TryResolveFloorPoint(dropOrigin, 16f, 32f, out var dropPoint))
                    dropPoint = dropOrigin;

                var pickup = WorldPickup.Spawn(definition.dropItem, 1, dropPoint);
                if (pickup != null)
                {
                    var contentRoot = GameContext.Instance?.CavernBounds?.transform;
                    if (contentRoot != null)
                        pickup.transform.SetParent(contentRoot, true);
                }
            }

            Destroy(gameObject);
        }
    }
}
