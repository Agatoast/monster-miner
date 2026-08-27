using MonsterMiner.Interaction;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] float interactRange = 4f;
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
                    if (!Physics.Raycast(ray, out var hit, interactRange, Physics.DefaultRaycastLayers, InteractTriggerQuery))
                        continue;

                    float screenDistanceSq = (px - centerX) * (px - centerX) + (py - centerY) * (py - centerY);
                    var interactables = hit.collider.GetComponentsInParent<IInteractable>();
                    foreach (var interactable in interactables)
                    {
                        if (!interactable.CanInteract(gameObject))
                            continue;

                        if (hit.distance < bestHitDistance ||
                            (Mathf.Approximately(hit.distance, bestHitDistance) && screenDistanceSq < bestScreenDistanceSq))
                        {
                            best = interactable;
                            bestHitDistance = hit.distance;
                            bestScreenDistanceSq = screenDistanceSq;
                        }

                        break;
                    }
                }
            }

            return best;
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
                    if (!Physics.Raycast(ray, out var hit, interactRange, Physics.DefaultRaycastLayers, InteractTriggerQuery))
                        continue;

                    var interactables = hit.collider.GetComponentsInParent<IInteractable>();
                    foreach (var interactable in interactables)
                    {
                        if (interactable == target)
                            return true;
                        break;
                    }
                }
            }

            return false;
        }
    }
}
