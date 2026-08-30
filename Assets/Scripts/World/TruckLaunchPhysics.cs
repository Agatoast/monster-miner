using MonsterMiner.Combat;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class TruckLaunchPhysics
    {
        const float CreatureMinLaunchHeightFeet = 50f;
        const float CreatureMaxLaunchHeightFeet = 125f;
        const float CreatureMinLaunchAngleDegrees = 30f;
        const float CreatureMaxLaunchAngleDegrees = 55f;

        static PhysicsMaterial launchMaterial;

        public static void LaunchCreature(Monster monster, Vector3 truckVelocity)
        {
            if (monster == null)
                return;

            ClearExistingLaunch(monster.gameObject);
            monster.BeginAirborneThrow();

            Vector3 launchVelocity = BuildCreatureLaunchVelocity(truckVelocity);
            var rb = EnsureRigidbody(monster.gameObject, 12f);
            ApplyTruckVelocity(rb, launchVelocity);
            ApplyLaunchPhysicsMaterial(monster.gameObject);

            var tracker = monster.gameObject.AddComponent<TruckLaunchedRigidbody>();
            tracker.InitializeCreature(monster, launchVelocity);
        }

        public static void LaunchTree(PlainsTreeObstacle tree, Vector3 truckVelocity)
        {
            if (tree == null)
                return;

            ClearExistingLaunch(tree.gameObject);
            tree.BeginAirborne();

            var rb = EnsureRigidbody(tree.gameObject, 18f);
            ApplyTruckVelocity(rb, truckVelocity);
            ApplyLaunchPhysicsMaterial(tree.gameObject);

            var tracker = tree.gameObject.AddComponent<TruckLaunchedRigidbody>();
            tracker.InitializeTree(tree, truckVelocity);
        }

        static void ClearExistingLaunch(GameObject target)
        {
            var existingFlight = target.GetComponent<ThrownCreatureFlight>();
            if (existingFlight != null)
                Object.Destroy(existingFlight);

            var existingTracker = target.GetComponent<TruckLaunchedRigidbody>();
            if (existingTracker != null)
                Object.Destroy(existingTracker);
        }

        static Rigidbody EnsureRigidbody(GameObject go, float mass)
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();

            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            return rb;
        }

        static void ApplyTruckVelocity(Rigidbody rb, Vector3 velocity)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.linearVelocity = velocity;
            rb.angularVelocity = Vector3.zero;
            rb.WakeUp();
        }

        static Vector3 BuildCreatureLaunchVelocity(Vector3 truckVelocity)
        {
            Vector3 flat = truckVelocity;
            flat.y = 0f;

            Vector3 flatDir = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward;

            float launchAngle = Random.Range(CreatureMinLaunchAngleDegrees, CreatureMaxLaunchAngleDegrees) * Mathf.Deg2Rad;
            float peakHeight = WorldScale.Feet(Random.Range(CreatureMinLaunchHeightFeet, CreatureMaxLaunchHeightFeet));
            float gravity = Mathf.Abs(Physics.gravity.y);
            float sinAngle = Mathf.Sin(launchAngle);
            float cosAngle = Mathf.Cos(launchAngle);
            float launchSpeed = Mathf.Sqrt(2f * gravity * peakHeight) / sinAngle;

            return flatDir * (launchSpeed * cosAngle) + Vector3.up * (launchSpeed * sinAngle);
        }

        static void ApplyLaunchPhysicsMaterial(GameObject go)
        {
            var material = GetLaunchMaterial();
            foreach (var collider in go.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null || collider.isTrigger)
                    continue;

                collider.material = material;
            }
        }

        static PhysicsMaterial GetLaunchMaterial()
        {
            if (launchMaterial != null)
                return launchMaterial;

            launchMaterial = new PhysicsMaterial("TruckLaunch")
            {
                bounciness = 0.32f,
                dynamicFriction = 0.42f,
                staticFriction = 0.48f,
                bounceCombine = PhysicsMaterialCombine.Multiply,
                frictionCombine = PhysicsMaterialCombine.Average
            };
            return launchMaterial;
        }
    }

    sealed class TruckLaunchedRigidbody : MonoBehaviour
    {
        const float SettleLinearSpeed = 0.75f;
        const float SettleAngularSpeed = 1.1f;
        const float SettleHoldSeconds = 0.45f;
        const float MaxLaunchSeconds = 20f;

        enum TargetKind
        {
            Creature,
            Tree
        }

        TargetKind kind;
        Monster monster;
        PlainsTreeObstacle tree;
        Vector3 launchVelocity;
        Rigidbody rb;
        float elapsed;
        float settledTime;
        bool hasGroundContact;

        public void InitializeCreature(Monster launchedMonster, Vector3 truckVelocity)
        {
            kind = TargetKind.Creature;
            monster = launchedMonster;
            launchVelocity = truckVelocity;
            rb = monster != null ? monster.GetComponent<Rigidbody>() : null;
        }

        public void InitializeTree(PlainsTreeObstacle launchedTree, Vector3 truckVelocity)
        {
            kind = TargetKind.Tree;
            tree = launchedTree;
            launchVelocity = truckVelocity;
            rb = tree != null ? tree.GetComponent<Rigidbody>() : null;
        }

        void OnCollisionEnter(Collision collision)
        {
            RegisterGroundContact(collision);
        }

        void OnCollisionStay(Collision collision)
        {
            RegisterGroundContact(collision);
        }

        void RegisterGroundContact(Collision collision)
        {
            if (collision == null || collision.collider == null || collision.collider.isTrigger)
                return;

            if (FloorColliderUtility.IsFloorCollider(collision.collider))
                hasGroundContact = true;
        }

        void FixedUpdate()
        {
            if (rb == null)
            {
                CompleteLaunch(force: true);
                return;
            }

            elapsed += Time.fixedDeltaTime;
            if (elapsed >= MaxLaunchSeconds)
            {
                CompleteLaunch(force: true);
                return;
            }

            if (IsSettled())
            {
                settledTime += Time.fixedDeltaTime;
                if (settledTime >= SettleHoldSeconds)
                    CompleteLaunch(force: false);
            }
            else
            {
                settledTime = 0f;
            }
        }

        bool IsSettled()
        {
            if (!hasGroundContact)
                return false;

            return rb.linearVelocity.magnitude <= SettleLinearSpeed
                && rb.angularVelocity.magnitude <= SettleAngularSpeed;
        }

        void CompleteLaunch(bool force)
        {
            Vector3 landPoint = rb != null ? rb.position : transform.position;
            if (FloorColliderUtility.TryResolveFloorPoint(landPoint + Vector3.up * 2f, 24f, 64f, out var floorPoint))
                landPoint = floorPoint;

            if (kind == TargetKind.Creature && monster != null)
                monster.CompleteTruckLaunchRecovery(landPoint, force);
            else if (kind == TargetKind.Tree && tree != null)
                tree.CompleteFallLanding(landPoint, GetFlatLaunchDirection());

            Destroy(this);
        }

        Vector3 GetFlatLaunchDirection()
        {
            Vector3 flat = launchVelocity;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f)
                return Vector3.forward;

            return flat.normalized;
        }
    }
}
