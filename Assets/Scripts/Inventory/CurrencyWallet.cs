using System;
using UnityEngine;

namespace MonsterMiner.Inventory
{
    public class CurrencyWallet : MonoBehaviour
    {
        const bool UnlimitedMoneyForTesting = true;
        const int TestingBalance = 999_999;

        public int Balance { get; private set; }

        public event Action<int> OnBalanceChanged;

        void Awake()
        {
            if (!UnlimitedMoneyForTesting)
                return;

            Balance = TestingBalance;
            OnBalanceChanged?.Invoke(Balance);
        }

        public bool CanAfford(int amount) => UnlimitedMoneyForTesting || Balance >= amount;

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
                return true;

            if (UnlimitedMoneyForTesting)
                return true;

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

        public void RestoreBalance(int amount)
        {
            int restored = Mathf.Max(0, amount);
            if (Balance == restored)
                return;

            Balance = restored;
            OnBalanceChanged?.Invoke(Balance);
        }
    }
}
