using System;
using UnityEngine;

namespace MonsterMiner.Player
{
    public class PlayerThirst : MonoBehaviour
    {
        const float DefaultMaxThirst = 100f;
        const float PercentLostPerDrain = 1f;
        const float SecondsPerPercentLost = 10f;
        const float DefaultDrainPerSecond = DefaultMaxThirst * (PercentLostPerDrain / 100f) / SecondsPerPercentLost;
        public const float HealthRegenThreshold = 0.5f;

        public float MaxThirst { get; private set; } = DefaultMaxThirst;
        public float CurrentThirst { get; private set; }
        public bool IsHydratedEnoughForHealthRegen =>
            MaxThirst > 0f && CurrentThirst / MaxThirst >= HealthRegenThreshold;

        public event Action<float, float> OnThirstChanged;

        float drainPerSecond = DefaultDrainPerSecond;

        public void Initialize(float maxThirst = DefaultMaxThirst, float drainPerSecond = DefaultDrainPerSecond)
        {
            MaxThirst = maxThirst;
            this.drainPerSecond = drainPerSecond;
            CurrentThirst = maxThirst;
            OnThirstChanged?.Invoke(CurrentThirst, MaxThirst);
        }

        void Update()
        {
            if (CurrentThirst <= 0f || drainPerSecond <= 0f)
                return;

            float previous = CurrentThirst;
            CurrentThirst = Mathf.Max(0f, CurrentThirst - drainPerSecond * Time.deltaTime);
            if (!Mathf.Approximately(previous, CurrentThirst))
                OnThirstChanged?.Invoke(CurrentThirst, MaxThirst);
        }

        public void Drink(float amount)
        {
            if (amount <= 0f)
                return;

            CurrentThirst = Mathf.Min(MaxThirst, CurrentThirst + amount);
            OnThirstChanged?.Invoke(CurrentThirst, MaxThirst);
        }
    }
}
