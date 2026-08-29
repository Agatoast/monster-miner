using MonsterMiner.Combat;
using MonsterMiner.Core;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class PlayerCreatureCarrier : MonoBehaviour
    {
        const float ThrowDistanceFeet = 22f;

        static readonly Vector3 HeldCreatureLocalPosition = new Vector3(0f, -0.04f, 0.14f);
        static readonly Vector3 HeldCreatureLocalEuler = new Vector3(0f, 180f, 0f);

        Transform leftHandAnchor;
        PlayerController controller;
        Monster carriedMonster;

        public bool IsCarrying => carriedMonster != null;
        public Monster CarriedMonster => carriedMonster;

        public void Initialize(Transform leftHand)
        {
            leftHandAnchor = leftHand;
            controller = GetComponent<PlayerController>();
        }

        public bool TryPickUp(Monster monster)
        {
            if (monster == null || carriedMonster != null || !monster.CanBePickedUp)
                return false;

            var eggCarrier = GetComponent<PlayerEggCarrier>();
            if (eggCarrier != null && eggCarrier.IsCarryingEgg)
                return false;

            carriedMonster = monster;
            var anchor = leftHandAnchor != null ? leftHandAnchor : transform;
            monster.SetCarried(anchor, HeldCreatureLocalPosition, HeldCreatureLocalEuler);
            return true;
        }

        public void ThrowCarriedCreature()
        {
            if (carriedMonster == null)
                return;

            var monster = carriedMonster;
            carriedMonster = null;

            Vector3 start = monster.transform.position;
            Vector3 target = GetThrowTarget();
            ThrownCreatureFlight.Begin(monster, start, target, monster.IsNonAggressiveMonster);
        }

        public void ForceRelease()
        {
            carriedMonster = null;
        }

        Vector3 GetThrowTarget()
        {
            Vector3 forward = GetFlatForward();
            Vector3 probe = transform.position + Vector3.up * 1f + forward * WorldScale.Feet(ThrowDistanceFeet);

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds != null)
            {
                var local = bounds.transform.InverseTransformPoint(probe);
                if (bounds.TryResolveFloorWorldPoint(local.x, local.z, out var floorPoint))
                    return floorPoint + Vector3.up * 0.03f;
            }

            if (FloorAnchor.TryResolveFloorPoint(probe + Vector3.up * 24f, 0f, 48f, out var hit))
                return hit + Vector3.up * 0.03f;

            return probe;
        }

        Vector3 GetFlatForward()
        {
            if (controller?.ViewCamera != null)
            {
                Vector3 cameraForward = controller.ViewCamera.transform.forward;
                cameraForward.y = 0f;
                if (cameraForward.sqrMagnitude > 0.001f)
                    return cameraForward.normalized;
            }

            Vector3 forward = transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        }
    }
}
