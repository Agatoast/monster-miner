using System.Collections;
using MonsterMiner.Core;
using MonsterMiner.UI;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        const float HydratedRegenPerSecond = 1f;
        const float RespawnFleeDelay = 1.35f;

        public float MaxHealth { get; private set; } = 100f;
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        public event System.Action<float, float> OnHealthChanged;
        public event System.Action OnDeath;

        PlayerController controller;
        Rigidbody rb;
        bool wasKinematic;

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
            Vector3 deathPos = transform.position;

            if (ctx != null)
            {
                var eggCarrier = GetComponent<PlayerEggCarrier>();
                eggCarrier?.DropEggAt(deathPos);
                GetComponent<PlayerWingsFlight>()?.CancelFlightAndRestoreWings();
                ctx.Inventory?.DropEquippedGlovesAt(deathPos);
                ctx.Inventory?.DropAllAt(deathPos);
                ctx.MakeAllMonstersFlee();
            }

            SetPlayerDisabled(true);
            OnDeath?.Invoke();
            DeathScreenDisplay.Show(BeginRespawn);
        }

        void BeginRespawn()
        {
            StartCoroutine(RespawnAfterMonstersFlee());
        }

        IEnumerator RespawnAfterMonstersFlee()
        {
            yield return new WaitForSeconds(RespawnFleeDelay);

            var ctx = GameContext.Instance;
            Vector3 spawnPoint = ctx != null ? ctx.PlayerSpawnPoint : Vector3.up;

            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            controller?.Respawn(spawnPoint);
            ctx?.Inventory?.EnsureStarterPickaxeIfMissing(ctx.Database?.pickaxeItem);
            SetPlayerDisabled(false);
            IsDead = false;
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

            if (controller != null)
                controller.enabled = !disabled;

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
