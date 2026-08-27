using System;
using UnityEngine;

namespace MonsterMiner.Inventory
{
    public class CurrencyWallet : MonoBehaviour
    {
        public int Balance { get; private set; }

        public event Action<int> OnBalanceChanged;

        public bool CanAfford(int amount) => Balance >= amount;

        public bool TrySpend(int amount)
        {
            if (!CanAfford(amount))
                return false;
            Balance -= amount;
            OnBalanceChanged?.Invoke(Balance);
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;
            Balance += amount;
            OnBalanceChanged?.Invoke(Balance);
        }
    }
}
