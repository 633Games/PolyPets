using System;
using System.Collections.Generic;
using UnityEngine;
using PolyPets.Economy;
using PolyPets.Pets;

namespace PolyPets.Shop
{
    /// <summary>
    /// Buys species food with minigame coins and tracks inventory stacks.
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

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetCatalog(FoodItemDefinition[] items) => catalog = items;

        public int GetCount(FoodItemDefinition item)
        {
            if (item == null) return 0;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].item == item)
                    return stacks[i].count;
            }

            return 0;
        }

        public int CountForSpecies(PetSpecies species)
        {
            int total = 0;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].item != null && stacks[i].item.species == species)
                    total += stacks[i].count;
            }

            return total;
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

        /// <summary>
        /// Best owned food for a species (highest hunger restore, Super &gt; Medium &gt; Budget).
        /// </summary>
        public bool TryConsumeBestForSpecies(PetSpecies species, out FoodItemDefinition consumed)
        {
            consumed = null;
            FoodItemDefinition best = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < stacks.Count; i++)
            {
                var item = stacks[i].item;
                if (stacks[i].count <= 0 || item == null || item.species != species)
                    continue;

                float score = item.hungerRestore * 10f + (int)item.tier;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = item;
                }
            }

            return best != null && TryConsume(best, out consumed);
        }

        public IEnumerable<FoodItemDefinition> CatalogForSpecies(PetSpecies species)
        {
            if (catalog == null)
                yield break;
            for (int i = 0; i < catalog.Length; i++)
            {
                if (catalog[i] != null && catalog[i].species == species)
                    yield return catalog[i];
            }
        }
    }
}
