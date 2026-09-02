using MonsterMiner.Core;
using MonsterMiner.Player;
using UnityEngine;

namespace MonsterMiner.World
{
    [DefaultExecutionOrder(160)]
    public class LakeTraversalGuard : MonoBehaviour
    {
        const string WaterBlockedMessage = "You need a boat to do that.";
        const int MessageCooldownFrames = 45;

        Rigidbody rb;
        Vector3 lastValidWorldPosition;
        bool hasLastValid;
        int lastMessageFrame = -999;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Start()
        {
            lastValidWorldPosition = transform.position;
            hasLastValid = true;
        }

        public void RefreshValidPosition()
        {
            lastValidWorldPosition = transform.position;
            hasLastValid = true;
        }

        void FixedUpdate()
        {
            if (PlayerController.IsGameplayBlocked())
                return;

            if (GetComponent<PlayerWingsFlight>()?.IsFlying == true)
                return;

            if (GetComponent<PlayerVehicleMount>()?.IsMounted == true)
                return;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return;

            Vector3 local = bounds.transform.InverseTransformPoint(transform.position);
            if (LakeCatalog.IsWalkableLandLocal(local.x, local.z, bounds.transform)
                || LandQuarry2Boundary.IsSnowGroundLocal(local.x, local.z))
            {
                lastValidWorldPosition = transform.position;
                hasLastValid = true;
                return;
            }

            if (!LakeCatalog.IsOpenWaterLocal(local.x, local.z))
            {
                lastValidWorldPosition = transform.position;
                hasLastValid = true;
                return;
            }

            if (hasLastValid)
                transform.position = lastValidWorldPosition;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (Time.frameCount - lastMessageFrame < MessageCooldownFrames)
                return;

            GameContext.Instance?.Hud?.ShowMessage(WaterBlockedMessage);
            lastMessageFrame = Time.frameCount;
        }
    }
}
