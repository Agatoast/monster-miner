using MonsterMiner.Interaction;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] float interactRange = 8f;
        const QueryTriggerInteraction InteractTriggerQuery = QueryTriggerInteraction.Collide;

        Camera viewCamera;

        public IInteractable CurrentTarget { get; private set; }
        public bool HasCenterTarget =>
            CurrentTarget != null && CurrentTarget.CanInteract(gameObject);

        public void Initialize(Camera camera)
        {
            viewCamera = camera;
        }

        void Update()
        {
            CurrentTarget = FindTargetInZone();
        }

        IInteractable FindTargetInZone()
        {
            if (viewCamera == null)
                return null;

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            float half = InteractionReticleZone.SizePixels * 0.5f;
            float zoneLeft = centerX - half;
            float zoneBottom = centerY - half;
            float step = InteractionReticleZone.SizePixels / InteractionReticleZone.RaySampleGrid;

            IInteractable best = null;
            float bestHitDistance = float.MaxValue;
            float bestScreenDistanceSq = float.MaxValue;

            for (int x = 0; x < InteractionReticleZone.RaySampleGrid; x++)
            {
                for (int y = 0; y < InteractionReticleZone.RaySampleGrid; y++)
                {
                    float px = zoneLeft + (x + 0.5f) * step;
                    float py = zoneBottom + (y + 0.5f) * step;
                    var ray = viewCamera.ScreenPointToRay(new Vector3(px, py, 0f));
                    if (!TryFindBestInteractableHit(ray, interactRange, gameObject, out var hit, out var interactable))
                        continue;

                    float screenDistanceSq = (px - centerX) * (px - centerX) + (py - centerY) * (py - centerY);

                    if (hit.distance < bestHitDistance ||
                        (Mathf.Approximately(hit.distance, bestHitDistance) && screenDistanceSq < bestScreenDistanceSq))
                    {
                        best = interactable;
                        bestHitDistance = hit.distance;
                        bestScreenDistanceSq = screenDistanceSq;
                    }
                }
            }

            return best;
        }

        static IInteractable ResolveInteractable(RaycastHit hit, GameObject interactor)
        {
            var direct = hit.collider.GetComponent<IInteractable>();
            if (direct != null)
                return direct;

            var parentInteractable = hit.collider.GetComponentInParent<IInteractable>();
            if (parentInteractable != null)
                return parentInteractable;

            var truck = hit.collider.GetComponentInParent<DriveableTruck>();
            if (truck == null)
                return null;

            var cab = truck.GetComponentInChildren<TruckCabInteract>(true);
            var bed = truck.GetComponentInChildren<TruckBedInteract>(true);
            Vector3 localHit = truck.transform.InverseTransformPoint(hit.point);
            IInteractable preferred = localHit.z >= -0.2f ? cab : bed;

            if (preferred != null && preferred.CanInteract(interactor))
                return preferred;

            if (cab != null && cab.CanInteract(interactor))
                return cab;

            if (bed != null && bed.CanInteract(interactor))
                return bed;

            return null;
        }

        static bool TryFindBestInteractableHit(
            Ray ray,
            float maxDistance,
            GameObject interactor,
            out RaycastHit bestHit,
            out IInteractable interactable)
        {
            bestHit = default;
            interactable = null;

            var hits = Physics.RaycastAll(ray, maxDistance, Physics.DefaultRaycastLayers, InteractTriggerQuery);
            if (hits.Length == 0)
                return false;

            float bestDistance = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                if (FloorColliderUtility.IsFloorCollider(hits[i].collider))
                    continue;

                var candidate = ResolveInteractable(hits[i], interactor);
                if (candidate == null || !candidate.CanInteract(interactor))
                    continue;

                if (hits[i].distance >= bestDistance)
                    continue;

                bestDistance = hits[i].distance;
                bestHit = hits[i];
                interactable = candidate;
            }

            return interactable != null;
        }

        public void TryInteract()
        {
            if (CurrentTarget != null && CurrentTarget.CanInteract(gameObject))
                CurrentTarget.Interact(gameObject);
        }

        public bool IsInInteractionRange(IInteractable target)
        {
            if (target == null || viewCamera == null)
                return false;

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            float half = InteractionReticleZone.SizePixels * 0.5f;
            float zoneLeft = centerX - half;
            float zoneBottom = centerY - half;
            float step = InteractionReticleZone.SizePixels / InteractionReticleZone.RaySampleGrid;

            for (int x = 0; x < InteractionReticleZone.RaySampleGrid; x++)
            {
                for (int y = 0; y < InteractionReticleZone.RaySampleGrid; y++)
                {
                    float px = zoneLeft + (x + 0.5f) * step;
                    float py = zoneBottom + (y + 0.5f) * step;
                    var ray = viewCamera.ScreenPointToRay(new Vector3(px, py, 0f));
                    if (!TryFindBestInteractableHit(ray, interactRange, gameObject, out _, out var interactable))
                        continue;

                    if (interactable == target)
                        return true;
                }
            }

            return false;
        }
    }
}
