using MonsterMiner.Core;
using MonsterMiner.Player;
using UnityEngine;

namespace MonsterMiner.World
{
    [DefaultExecutionOrder(160)]
    public class LakeTraversalGuard : MonoBehaviour
    {
        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (PlayerController.IsGameplayBlocked())
                return;

            if (GetComponent<PlayerWingsFlight>()?.IsFlying == true)
                return;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return;

            Vector3 local = bounds.transform.InverseTransformPoint(transform.position);
            if (!LakeCatalog.IsOpenWaterLocal(local.x, local.z))
                return;

            Vector2 shore = LakeCatalog.GetNearestShoreLocal(local.x, local.z);
            Vector3 targetLocal = new Vector3(shore.x, local.y, shore.y);
            Vector3 targetWorld = bounds.transform.TransformPoint(targetLocal);
            transform.position = targetWorld;

            if (rb != null)
            {
                Vector3 velocity = rb.linearVelocity;
                velocity.y = 0f;
                rb.linearVelocity = velocity;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
