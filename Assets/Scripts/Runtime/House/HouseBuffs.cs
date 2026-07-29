using System.Collections.Generic;
using UnityEngine;
using PolyPets.Shop;

namespace PolyPets.House
{
    /// <summary>
    /// Aggregates placed decorations for happiness comfort + minigame coin bonuses.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public sealed class HouseBuffs : MonoBehaviour
    {
        public static HouseBuffs Instance { get; private set; }

        [SerializeField] private HouseController house;

        public float HappinessPassivePerMinute { get; private set; }
        public float HappinessDecayMultiplier { get; private set; } = 1f;
        public float CoinEarnMultiplier { get; private set; } = 1f;

        private void Awake()
        {
            Instance = this;
            house ??= GetComponent<HouseController>() ?? Object.FindFirstObjectByType<HouseController>();
            Recalculate();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BindHouse(HouseController controller)
        {
            house = controller;
            Recalculate();
        }

        public void Recalculate()
        {
            float passive = 0f;
            float decayMul = 1f;
            float coinBonus = 0f;

            if (house?.Rooms != null)
            {
                for (int i = 0; i < house.Rooms.Count; i++)
                {
                    var room = house.Rooms[i];
                    if (room == null)
                        continue;
                    var placed = room.PlacedDecorations;
                    for (int d = 0; d < placed.Count; d++)
                    {
                        var def = placed[d];
                        if (def == null)
                            continue;
                        passive += def.happinessPassivePerMinute;
                        decayMul *= def.happinessDecayMultiplier;
                        coinBonus += def.coinEarnBonus;
                    }
                }
            }

            HappinessPassivePerMinute = passive;
            HappinessDecayMultiplier = Mathf.Clamp(decayMul, 0.45f, 1f);
            CoinEarnMultiplier = Mathf.Clamp(1f + coinBonus, 1f, 2.5f);
        }

        public int ApplyCoinBonus(int baseCoins)
        {
            if (baseCoins <= 0)
                return 0;
            return Mathf.Max(1, Mathf.RoundToInt(baseCoins * CoinEarnMultiplier));
        }
    }
}
