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
        public const int BasePickaxeHits = 15;
        const float MaxHealth = BasePickaxeHits;
        const float HatchDelayMinSeconds = 1f;
        const float HatchDelayMaxSeconds = 5f;

        float currentHealth;
        float hatchTimer;
        EggState state = EggState.Idle;
        bool isCarried;
        MonsterDefinition hatchDefinition;
        string creatureTypeId;
        Collider eggCollider;

        public EggState State => state;
        public bool IsCarried => isCarried;
        public bool IsHatching => state == EggState.Hatching;
        public string CreatureTypeId => creatureTypeId;

        public static MonsterEgg Spawn(Vector3 floorContactPoint, MonsterDefinition hatchDefinition)
        {
            var bounds = GameContext.Instance?.CavernBounds;
            var go = EggVisualFactory.CreateWorldEgg(floorContactPoint);
            if (ShouldEnforcePlateauShell(go, bounds))
                CavernInteriorEnforcer.EnsureInsideShell(go, bounds);
            FloorAnchor.PlaceOnFloor(go, go.transform.position, bounds);

            var egg = go.GetComponent<MonsterEgg>();
            if (egg == null)
                egg = go.AddComponent<MonsterEgg>();
            egg.Initialize(hatchDefinition);
            return egg;
        }

        void Initialize(MonsterDefinition definition)
        {
            hatchDefinition = definition;
            creatureTypeId = definition?.monsterId;
            currentHealth = MaxHealth;
            eggCollider = GetComponent<Collider>();
            RefreshEggSkin();
        }

        public void SetCreatureTypeId(string typeId)
        {
            creatureTypeId = typeId;
            RefreshEggSkin();
        }

        void RefreshEggSkin()
        {
            if (!IsBossEgg())
                return;

            EggMaterialFactory.ApplyGoldDragonScaleMaterial(gameObject);
        }

        bool IsBossEgg()
        {
            if (string.IsNullOrEmpty(creatureTypeId))
                return hatchDefinition != null && hatchDefinition.isQuestBoss;

            var monsters = GameContext.Instance?.Database?.monsters;
            if (monsters == null)
                return false;

            foreach (var monster in monsters)
            {
                if (monster != null && monster.monsterId == creatureTypeId)
                    return monster.isQuestBoss;
            }

            return false;
        }

        public bool MatchesFinderTarget(string targetCreatureId)
        {
            return !string.IsNullOrEmpty(targetCreatureId)
                && creatureTypeId == targetCreatureId;
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
            hatchTimer = Random.Range(HatchDelayMinSeconds, HatchDelayMaxSeconds);
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
            FloorAnchor.PlaceOnFloor(gameObject, floorPoint);

            var bounds = GameContext.Instance?.CavernBounds;
            if (ShouldEnforcePlateauShell(gameObject, bounds))
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
            EggShellBurstEffect.Spawn(pos, gameObject);
            ctx?.SpawnManager?.HatchMonster(pos, hatchDefinition, creatureTypeId);
            Destroy(gameObject);
        }

        static bool ShouldEnforcePlateauShell(GameObject root, CavernBounds bounds)
        {
            if (root == null || bounds == null)
                return false;

            var local = bounds.transform.InverseTransformPoint(root.transform.position);
            return PlateauBoundary.IsOnPlateau(local.x, local.z, bounds.Radius);
        }
    }
}
