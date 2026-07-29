using System;
using System.Collections.Generic;
using UnityEngine;
using PolyPets.Economy;

namespace PolyPets.Shop
{
    /// <summary>
    /// Buys food with minigame-earned coins and keeps a simple inventory count per food id.
    /// </summary>
    public sealed class FoodInventory : MonoBehaviour
    {
        public static FoodInventory Instance { get; private set; }

        [Serializable]
        public struct Stack
        {
            public FoodItemDefinition item;
            public int count;
        }

        [SerializeField] private List<Stack> stacks = new();
        [SerializeField] private FoodItemDefinition[] catalog;

        public IReadOnlyList<Stack> Stacks => stacks;
        public FoodItemDefinition[] Catalog => catalog;

        public event Action InventoryChanged;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetCatalog(FoodItemDefinition[] items)
        {
            catalog = items;
        }

        public int GetCount(FoodItemDefinition item)
        {
            if (item == null)
                return 0;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].item == item)
                    return stacks[i].count;
            }

            return 0;
        }

        public bool TryBuy(FoodItemDefinition item, int qty = 1)
        {
            if (item == null || qty <= 0)
                return false;

            var economy = EconomyService.Instance;
            if (economy == null)
                return false;

            int cost = item.priceCoins * qty;
            if (!economy.TrySpend(cost, $"buy {item.displayName} x{qty}"))
                return false;

            Add(item, qty);
            return true;
        }

        public void Add(FoodItemDefinition item, int qty)
        {
            if (item == null || qty <= 0)
                return;

            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].item != item)
                    continue;

                var s = stacks[i];
                s.count += qty;
                stacks[i] = s;
                InventoryChanged?.Invoke();
                return;
            }

            stacks.Add(new Stack { item = item, count = qty });
            InventoryChanged?.Invoke();
        }

        public bool TryConsume(FoodItemDefinition item, out FoodItemDefinition consumed)
        {
            consumed = null;
            if (item == null)
                return false;

            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].item != item || stacks[i].count <= 0)
                    continue;

                var s = stacks[i];
                s.count--;
                if (s.count <= 0)
                    stacks.RemoveAt(i);
                else
                    stacks[i] = s;

                consumed = item;
                InventoryChanged?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>Consume the first available food, preferring highest hunger restore.</summary>
        public bool TryConsumeBestAvailable(out FoodItemDefinition consumed)
        {
            consumed = null;
            FoodItemDefinition best = null;
            float bestRestore = -1f;

            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].count <= 0 || stacks[i].item == null)
                    continue;
                if (stacks[i].item.hungerRestore > bestRestore)
                {
                    bestRestore = stacks[i].item.hungerRestore;
                    best = stacks[i].item;
                }
            }

            return best != null && TryConsume(best, out consumed);
        }
    }
}
