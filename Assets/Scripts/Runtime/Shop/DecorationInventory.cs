using System;
using System.Collections.Generic;
using UnityEngine;
using PolyPets.Economy;

namespace PolyPets.Shop
{
    /// <summary>
    /// Owned decorations waiting to be placed, plus catalog for the décor shop.
    /// </summary>
    public sealed class DecorationInventory : MonoBehaviour
    {
        public static DecorationInventory Instance { get; private set; }

        [Serializable]
        public struct Stack
        {
            public DecorationDefinition item;
            public int count;
        }

        [SerializeField] private List<Stack> stacks = new();
        [SerializeField] private DecorationDefinition[] catalog;

        public DecorationDefinition[] Catalog => catalog;
        public IReadOnlyList<Stack> Stacks => stacks;

        public event Action InventoryChanged;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetCatalog(DecorationDefinition[] items) => catalog = items;

        public int GetCount(DecorationDefinition item)
        {
            if (item == null) return 0;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].item == item)
                    return stacks[i].count;
            }

            return 0;
        }

        public bool TryBuy(DecorationDefinition item, int qty = 1)
        {
            if (item == null || qty <= 0)
                return false;
            var economy = EconomyService.Instance;
            if (economy == null)
                return false;
            int cost = item.priceCoins * qty;
            if (!economy.TrySpend(cost, $"buy décor {item.displayName}"))
                return false;
            Add(item, qty);
            return true;
        }

        public void Add(DecorationDefinition item, int qty = 1)
        {
            if (item == null || qty <= 0)
                return;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].item == item)
                {
                    var s = stacks[i];
                    s.count += qty;
                    stacks[i] = s;
                    InventoryChanged?.Invoke();
                    return;
                }
            }

            stacks.Add(new Stack { item = item, count = qty });
            InventoryChanged?.Invoke();
        }

        public bool TryConsume(DecorationDefinition item, int qty = 1)
        {
            if (item == null || qty <= 0)
                return false;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].item != item)
                    continue;
                if (stacks[i].count < qty)
                    return false;
                var s = stacks[i];
                s.count -= qty;
                if (s.count <= 0)
                    stacks.RemoveAt(i);
                else
                    stacks[i] = s;
                InventoryChanged?.Invoke();
                return true;
            }

            return false;
        }
    }
}
