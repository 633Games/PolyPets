using System;
using UnityEngine;
using PolyPets.Audio;

namespace PolyPets.Economy
{
    /// <summary>
    /// Player wallet. Coins come from minigames and idle floor-coin pickups.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public sealed class EconomyService : MonoBehaviour
    {
        public static EconomyService Instance { get; private set; }

        [SerializeField] private int coins;
        [Tooltip("Starter coins so the player can buy one cheap snack after the first minigame, not before.")]
        [SerializeField] private int startingCoins;
        [SerializeField] private bool playSpendSfx = true;

        public int Coins => coins;

        public event Action<int> CoinsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[PolyPets] Duplicate EconomyService — keeping the first.");
                return;
            }

            Instance = this;
            if (coins < startingCoins)
                coins = startingCoins;
            CoinsChanged?.Invoke(coins);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetCoins(int value)
        {
            coins = Mathf.Max(0, value);
            CoinsChanged?.Invoke(coins);
        }

        public void AddCoins(int amount, string source = null)
        {
            if (amount <= 0)
                return;

            coins += amount;
            CoinsChanged?.Invoke(coins);

            // Idle floor coins play their own rising ding; everything else gets a payout cascade.
            bool idlePickup = !string.IsNullOrEmpty(source) && source.IndexOf("idle", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!idlePickup)
                JuicySfx.PlayCoinPayout(amount);

            Debug.Log($"[PolyPets] +{amount} coins{(string.IsNullOrEmpty(source) ? "" : $" ({source})")}. Total: {coins}");
        }

        public bool TrySpend(int amount, string sink = null)
        {
            if (amount <= 0)
                return true;
            if (coins < amount)
            {
                if (playSpendSfx)
                    JuicySfx.PlayDeny();
                return false;
            }

            coins -= amount;
            CoinsChanged?.Invoke(coins);
            if (playSpendSfx)
                JuicySfx.PlayPurchase();
            Debug.Log($"[PolyPets] -{amount} coins{(string.IsNullOrEmpty(sink) ? "" : $" ({sink})")}. Total: {coins}");
            return true;
        }

        public bool CanAfford(int amount) => coins >= amount;
    }
}
