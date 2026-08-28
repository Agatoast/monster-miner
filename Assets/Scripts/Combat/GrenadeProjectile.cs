using MonsterMiner.Core;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Combat
{
    public class GrenadeProjectile : MonoBehaviour
    {
        const float FlightDuration = 0.55f;

        Vector3 start;
        Vector3 target;
        float damage;
        float blastRadius;
        float elapsed;
        Transform visual;

        public static void Launch(Vector3 startPoint, Vector3 targetPoint, float blastRadiusUnits, float explosionDamage)
        {
            var go = new GameObject("GrenadeProjectile");
            var contentRoot = GameContext.Instance?.CavernBounds?.transform;
            if (contentRoot != null)
                go.transform.SetParent(contentRoot, true);

            var projectile = go.AddComponent<GrenadeProjectile>();
            projectile.Initialize(startPoint, targetPoint, blastRadiusUnits, explosionDamage);
        }

        void Initialize(Vector3 startPoint, Vector3 targetPoint, float blastRadiusUnits, float explosionDamage)
        {
            start = startPoint;
            target = targetPoint;
            blastRadius = blastRadiusUnits;
            damage = explosionDamage;

            var visualGo = GrenadeVisualFactory.CreateWorldGrenade(
                startPoint,
                Quaternion.identity,
                Vector3.one * 1.15f,
                transform);
            visual = visualGo != null ? visualGo.transform : null;
            if (visual == null)
            {
                visual = PrimitiveFactory.CreatePrimitive(
                    PrimitiveType.Sphere,
                    startPoint,
                    Vector3.one * 0.18f,
                    new Color(0.35f, 0.75f, 0.25f),
                    "GrenadeVisualFallback",
                    transform).transform;
                Destroy(visual.GetComponent<Collider>());
            }
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FlightDuration);
            float height = Mathf.Sin(t * Mathf.PI) * WorldScale.Feet(12f);
            Vector3 pos = Vector3.Lerp(start, target, t) + Vector3.up * height;
            if (visual != null)
                visual.position = pos;

            if (t < 1f)
                return;

            Explode(target);
            Destroy(gameObject);
        }

        void Explode(Vector3 center)
        {
            var hits = Physics.OverlapSphere(center, blastRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                var egg = hit.GetComponentInParent<MonsterEgg>();
                if (egg != null)
                {
                    egg.TakeDamage(damage, fromPickaxe: false);
                    continue;
                }

                var monster = hit.GetComponentInParent<Monster>();
                if (monster != null)
                {
                    Vector3 toTarget = monster.transform.position - center;
                    monster.TakeDamage(damage, center, toTarget.normalized);
                }
            }

            DamagePlayerIfInRadius(center);
            GameContext.Instance?.Hud?.ShowMessage("Boom!");
        }

        void DamagePlayerIfInRadius(Vector3 center)
        {
            var ctx = GameContext.Instance;
            var player = ctx?.Player;
            var playerHealth = ctx?.PlayerHealth;
            if (player == null || playerHealth == null || playerHealth.IsDead)
                return;

            if (Vector3.Distance(player.transform.position, center) > blastRadius)
                return;

            playerHealth.TakeDamage(damage);
        }
    }
}
