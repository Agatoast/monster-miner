using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Interaction;
using MonsterMiner.Player;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public enum EggState { Idle, Hatching }

    public class MonsterEgg : MonoBehaviour, IInteractable
    {
        const float MaxHealth = 15f;
        const float HatchDelaySeconds = 10f;

        float currentHealth;
        float hatchTimer;
        EggState state = EggState.Idle;
        bool isCarried;
        MonsterDefinition hatchDefinition;
        Collider eggCollider;

        public EggState State => state;
        public bool IsCarried => isCarried;
        public bool IsHatching => state == EggState.Hatching;

        public static MonsterEgg Spawn(Vector3 floorContactPoint, MonsterDefinition hatchDefinition)
        {
            var go = EggVisualFactory.CreateWorldEgg(floorContactPoint);
            FloorAnchor.SnapBottomToFloor(go, floorContactPoint.y, 0.01f);

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds != null)
                CavernInteriorEnforcer.EnsureInsideShell(go, bounds);

            var egg = go.GetComponent<MonsterEgg>();
            if (egg == null)
                egg = go.AddComponent<MonsterEgg>();
            egg.Initialize(hatchDefinition);
            return egg;
        }

        void Initialize(MonsterDefinition definition)
        {
            hatchDefinition = definition;
            currentHealth = MaxHealth;
            eggCollider = GetComponent<Collider>();
        }

        void Update()
        {
            if (state != EggState.Hatching)
                return;

            hatchTimer -= Time.deltaTime;
            if (hatchTimer <= 0f)
                CompleteHatch();
        }

        public void TakeDamage(float rawDamage, bool fromPickaxe)
        {
            if (state != EggState.Idle || isCarried || rawDamage <= 0f)
                return;

            float damage = fromPickaxe ? rawDamage : Mathf.Ceil(rawDamage * 0.5f);
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            GameContext.Instance?.Hud?.ShowEggHit();

            if (currentHealth <= 0f)
                BeginHatching();
        }

        void BeginHatching()
        {
            state = EggState.Hatching;
            hatchTimer = HatchDelaySeconds;
            GameContext.Instance?.Hud?.ShowHatchingMessage("It's hatching!");
        }

        public string GetPrompt()
        {
            if (state != EggState.Hatching || IsCarried)
                return string.Empty;

            return "Pick up egg [E]";
        }

        public bool CanInteract(GameObject interactor)
        {
            return state == EggState.Hatching && !IsCarried;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
                return;

            var carrier = interactor.GetComponent<PlayerEggCarrier>();
            if (carrier == null || !carrier.TryPickUp(this))
                return;
        }

        public void SetCarried(Transform leftHandAnchor)
        {
            isCarried = true;
            if (eggCollider != null)
                eggCollider.enabled = false;

            transform.SetParent(leftHandAnchor, false);
            transform.localPosition = EggVisualFactory.HeldEggLocalPosition;
            transform.localRotation = Quaternion.Euler(EggVisualFactory.HeldEggLocalEuler);
        }

        public void SetDropped(Vector3 worldPosition)
        {
            isCarried = false;
            transform.SetParent(null, false);
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * EggVisualFactory.WorldEggScale;

            if (!FloorAnchor.TryResolveFloorPoint(worldPosition, 16f, 32f, out var floorPoint))
                floorPoint = worldPosition;

            transform.position = floorPoint;
            FloorAnchor.SnapBottomToFloor(gameObject, floorPoint.y, 0.01f);

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds != null)
                CavernInteriorEnforcer.EnsureInsideShell(gameObject, bounds);

            if (eggCollider != null)
                eggCollider.enabled = true;
        }

        void CompleteHatch()
        {
            var ctx = GameContext.Instance;
            var pos = transform.position;

            if (IsCarried)
                ctx?.Player?.GetComponent<PlayerEggCarrier>()?.ForceReleaseWithoutDrop();

            ctx?.Hud?.ClearHatchingMessage();
            ctx?.SpawnManager?.HatchMonster(pos, hatchDefinition);
            Destroy(gameObject);
        }
    }
}
