using MonsterMiner.Core;
using MonsterMiner.UI;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        const float HydratedRegenPerSecond = 1f;

        public float MaxHealth { get; private set; } = 100f;
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        public event System.Action<float, float> OnHealthChanged;
        public event System.Action OnDeath;

        PlayerController controller;
        Rigidbody rb;
        bool wasKinematic;
        Vector3 lastDeathPosition;
        int walletBalanceAtDeath;

        public void Initialize(PlayerController playerController, float maxHealth)
        {
            controller = playerController;
            rb = GetComponent<Rigidbody>();
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        void Update()
        {
            if (IsDead || CurrentHealth <= 0f || CurrentHealth >= MaxHealth)
                return;

            var thirst = GameContext.Instance?.PlayerThirst;
            if (thirst == null || !thirst.IsHydratedEnoughForHealthRegen)
                return;

            Heal(HydratedRegenPerSecond * Time.deltaTime);
        }

        public void IncreaseMaxHealth(float amount)
        {
            MaxHealth += amount;
            CurrentHealth += amount;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void Heal(float amount)
        {
            if (IsDead || CurrentHealth <= 0f || amount <= 0f)
                return;

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || CurrentHealth <= 0f)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            if (CurrentHealth <= 0f)
                Die();
        }

        void Die()
        {
            if (IsDead)
                return;

            IsDead = true;
            var ctx = GameContext.Instance;
            lastDeathPosition = transform.position;

            if (ctx != null)
            {
                walletBalanceAtDeath = ctx.Wallet != null ? ctx.Wallet.Balance : 0;

                var eggCarrier = GetComponent<PlayerEggCarrier>();
                eggCarrier?.DropEggAt(lastDeathPosition);
                GetComponent<PlayerWingsFlight>()?.CancelFlightAndRestoreWings();
                ctx.Inventory?.DropEquippedGlovesAt(lastDeathPosition);
                ctx.Inventory?.DropAllAt(lastDeathPosition);
                ctx.MakeAllMonstersFlee();
            }

            SetPlayerDisabled(true);
            OnDeath?.Invoke();
            DeathScreenDisplay.Show(RespawnNow);
        }

        void RespawnNow()
        {
            var ctx = GameContext.Instance;
            Vector3 spawnPoint = ResolveRespawnPoint(ctx);

            CurrentHealth = MaxHealth;
            IsDead = false;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            SetPlayerDisabled(false);
            controller?.Respawn(spawnPoint);
            ctx?.Wallet?.RestoreBalance(walletBalanceAtDeath);
            ctx?.Inventory?.EnsureStarterPickaxeIfMissing(ctx.Database?.pickaxeItem);
        }

        Vector3 ResolveRespawnPoint(GameContext ctx)
        {
            var bounds = ctx?.CavernBounds;
            if (bounds == null)
                return lastDeathPosition;

            var visitTracker = GetComponent<QuarryVisitTracker>();
            if (ShouldRespawnNearDroppedEquipment(bounds, visitTracker, lastDeathPosition))
                return QuarryCatalog.ResolveNearDroppedEquipmentSpawnWorld(bounds, lastDeathPosition);

            int quarryIndex = visitTracker != null
                ? visitTracker.LastVisitedQuarryIndex
                : QuarryCatalog.PlateauQuarryIndex;
            return QuarryCatalog.ResolveQuarryShopRespawnWorld(bounds, quarryIndex);
        }

        static bool ShouldRespawnNearDroppedEquipment(CavernBounds bounds, QuarryVisitTracker visitTracker, Vector3 deathPosition)
        {
            if (visitTracker == null || visitTracker.LastVisitedQuarryIndex != QuarryCatalog.PlateauQuarryIndex)
                return false;

            Vector3 local = bounds.transform.InverseTransformPoint(deathPosition);
            return !bounds.IsOnPlateauLocal(local.x, local.z);
        }

        void SetPlayerDisabled(bool disabled)
        {
            if (rb != null)
            {
                if (disabled)
                {
                    wasKinematic = rb.isKinematic;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
                else
                {
                    rb.isKinematic = wasKinematic;
                }
            }

            // Keep PlayerController enabled while dead so ApplyCursorState unlocks the cursor for the death UI.

            var combat = GetComponent<PlayerCombat>();
            if (combat != null)
                combat.enabled = !disabled;

            var input = GetComponent<PlayerInput>();
            if (input != null)
                input.enabled = !disabled;

            var interactor = GetComponent<Interactor>();
            if (interactor != null)
                interactor.enabled = !disabled;
        }
    }
}
