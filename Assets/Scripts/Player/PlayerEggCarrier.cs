using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class PlayerEggCarrier : MonoBehaviour
    {
        public static Vector3 HeldEggLocalPosition => EggVisualFactory.HeldEggLocalPosition;

        PlayerHands hands;
        Transform leftHandAnchor;
        MonsterEgg carriedEgg;

        public bool IsCarryingEgg => carriedEgg != null;
        public MonsterEgg CarriedEgg => carriedEgg;

        public void Initialize(PlayerHands playerHands, Transform leftHand)
        {
            hands = playerHands;
            leftHandAnchor = leftHand;
        }

        public bool TryPickUp(MonsterEgg egg)
        {
            if (egg == null || carriedEgg != null || egg.State != EggState.Hatching)
                return false;

            carriedEgg = egg;
            var anchor = leftHandAnchor != null ? leftHandAnchor : transform;
            egg.SetCarried(anchor);
            return true;
        }

        public void DropEgg()
        {
            DropEggAt(GetDropPoint());
        }

        public void DropEggAt(Vector3 dropPoint)
        {
            if (carriedEgg == null)
                return;

            if (FloorAnchor.TryResolveFloorPoint(dropPoint, 16f, 32f, out var floorPoint))
                dropPoint = floorPoint;

            carriedEgg.SetDropped(dropPoint);
            carriedEgg = null;
        }

        public void ForceReleaseWithoutDrop()
        {
            carriedEgg = null;
        }

        Vector3 GetDropPoint()
        {
            var controller = GetComponent<PlayerController>();
            Vector3 point;
            if (controller?.ViewCamera == null)
                point = transform.position + transform.forward * 1.2f;
            else
            {
                var cam = controller.ViewCamera.transform;
                point = cam.position + cam.forward * 1.4f;
            }

            if (FloorAnchor.TryResolveFloorPoint(point, 16f, 32f, out var floorPoint))
                return floorPoint;

            return point;
        }
    }
}
