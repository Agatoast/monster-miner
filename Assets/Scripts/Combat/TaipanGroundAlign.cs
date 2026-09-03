using MonsterMiner.Core;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Combat
{
    [DefaultExecutionOrder(100)]
    public class TaipanGroundAlign : MonoBehaviour
    {
        const float GroundRestOffset = 0.02f;
        const float VerticalSmoothTime = 0.05f;

        Rigidbody body;
        float rootLiftAboveVisualBottom;
        float verticalVelocity;
        bool measuredLift;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        void LateUpdate()
        {
            AlignVisualBottomToIsland(immediate: false);
        }

        public void AlignVisualBottomToIsland(bool immediate)
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return;

            EnsureRootLiftMeasured();

            Vector3 worldPos = body != null ? body.position : transform.position;
            Vector3 contentLocal = bounds.transform.InverseTransformPoint(worldPos);
            if (!TrySampleIslandGroundWorldY(bounds, contentLocal.x, contentLocal.z, out float surfaceWorldY))
                return;

            float targetY = surfaceWorldY + GroundRestOffset + rootLiftAboveVisualBottom;
            float nextY = immediate
                ? targetY
                : Mathf.SmoothDamp(
                    worldPos.y,
                    targetY,
                    ref verticalVelocity,
                    VerticalSmoothTime,
                    float.MaxValue,
                    Time.deltaTime);

            if (Mathf.Abs(nextY - worldPos.y) < 0.0001f)
                return;

            Vector3 snapped = new Vector3(worldPos.x, nextY, worldPos.z);
            transform.position = snapped;
            if (body != null)
            {
                body.position = snapped;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        void EnsureRootLiftMeasured()
        {
            if (measuredLift)
                return;

            TaipanVisualFactory.PrepareAnimatedGroundSample(gameObject);
            Physics.SyncTransforms();

            foreach (var skinned in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                skinned.forceMatrixRecalculationPerRender = true;

            Physics.SyncTransforms();
            float visualBottom = FloorAnchor.GetVisualBottomY(gameObject);
            rootLiftAboveVisualBottom = transform.position.y - visualBottom;
            if (rootLiftAboveVisualBottom < WorldScale.Feet(0.5f))
                rootLiftAboveVisualBottom = WorldScale.Feet(0.5f);

            measuredLift = true;
        }

        static bool TrySampleIslandGroundWorldY(CavernBounds bounds, float localX, float localZ, out float worldY)
        {
            worldY = 0f;
            if (bounds == null)
                return false;

            if (LakeCatalog.IsLakeIslandLocal(localX, localZ)
                && LakeIslandVisualFactory.TrySampleWorldY(localX, localZ, bounds.transform, out worldY))
                return true;

            worldY = PlainsGroundSupport.SampleSupportGroundWorldY(bounds, localX, localZ);
            return true;
        }
    }
}
