using System;
using UnityEngine;
using PolyPets.Shop;

namespace PolyPets.Needs
{
    /// <summary>
    /// Hunger + happiness. Food is bought in the shop and served via the pet's bowl.
    /// If the pet is full, feeding is refused until hunger drops.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PetNeeds : MonoBehaviour
    {
        [Header("Meters (0-100)")]
        [Range(0, 100)] [SerializeField] private float hunger = 55f;
        [Range(0, 100)] [SerializeField] private float happiness = 70f;

        [Header("Decay (points per real minute)")]
        [SerializeField] private float hungerDecayPerMinute = 4f;
        [SerializeField] private float happinessDecayPerMinute = 2f;
        [SerializeField] private float extraHappinessDecayWhenHungry = 3f;
        [SerializeField] private float hungryThreshold = 35f;
        [Tooltip("At or above this, the bowl refuses food until hunger drops.")]
        [SerializeField] private float fullThreshold = 92f;

        [Header("Minigame rewards to needs")]
        [SerializeField] private float happinessFromMinigame = 18f;

        public float Hunger => hunger;
        public float Happiness => happiness;
        public float FullThreshold => fullThreshold;
        public bool IsHungry => hunger <= hungryThreshold;
        public bool IsFull => hunger >= fullThreshold;
        public bool IsStarving => hunger <= 10f;
        public bool IsSad => happiness <= 25f;
        public bool CanAcceptFood => !IsFull;

        public event Action<PetNeeds> NeedsChanged;

        private void Update()
        {
            float dtMin = Time.deltaTime / 60f;
            float happyDecay = happinessDecayPerMinute;
            if (IsHungry)
                happyDecay += extraHappinessDecayWhenHungry;

            SetHunger(hunger - hungerDecayPerMinute * dtMin);
            SetHappiness(happiness - happyDecay * dtMin);
        }

        public void SetHunger(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, 100f);
            if (Mathf.Approximately(clamped, hunger))
                return;
            hunger = clamped;
            NeedsChanged?.Invoke(this);
        }

        public void SetHappiness(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, 100f);
            if (Mathf.Approximately(clamped, happiness))
                return;
            happiness = clamped;
            NeedsChanged?.Invoke(this);
        }

        public enum FeedRejectReason
        {
            None,
            NullFood,
            Full,
        }

        public bool TryFeed(FoodItemDefinition food, out FeedRejectReason reject)
        {
            reject = FeedRejectReason.None;
            if (food == null)
            {
                reject = FeedRejectReason.NullFood;
                return false;
            }

            if (IsFull)
            {
                reject = FeedRejectReason.Full;
                return false;
            }

            SetHunger(hunger + food.hungerRestore);
            SetHappiness(happiness + food.happinessBonus);
            if (hunger > hungryThreshold)
                SetHappiness(happiness + 3f);

            Debug.Log($"[PolyPets] Fed with {food.displayName}. Hunger={hunger:0} Happy={happiness:0}");
            return true;
        }

        public bool TryFeed(FoodItemDefinition food) => TryFeed(food, out _);

        public void NotifyMinigameCompleted(float scoreNormalized01 = 1f)
        {
            float bonus = happinessFromMinigame * Mathf.Clamp01(scoreNormalized01);
            SetHappiness(happiness + bonus);
            SetHunger(hunger - 4f);
        }

        public string StatusLabel()
        {
            if (IsStarving) return "Starving…";
            if (IsFull) return "Full — wait a bit";
            if (IsHungry && IsSad) return "Hungry & sad";
            if (IsHungry) return "Hungry — use the bowl";
            if (IsSad) return "Needs cheer";
            if (happiness >= 80f && hunger >= 60f) return "Content";
            return "Okay";
        }
    }
}
