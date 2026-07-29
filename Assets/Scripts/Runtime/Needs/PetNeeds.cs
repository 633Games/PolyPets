using System;
using UnityEngine;
using PolyPets.Shop;

namespace PolyPets.Needs
{
    /// <summary>
    /// Hunger + happiness for a pet. Food must be bought; happiness recovers from feeding and minigames.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PetNeeds : MonoBehaviour
    {
        [Header("Meters (0-100)")]
        [Range(0, 100)] [SerializeField] private float hunger = 70f;
        [Range(0, 100)] [SerializeField] private float happiness = 70f;

        [Header("Decay (points per real minute)")]
        [SerializeField] private float hungerDecayPerMinute = 4f;
        [SerializeField] private float happinessDecayPerMinute = 2f;
        [SerializeField] private float extraHappinessDecayWhenHungry = 3f;
        [SerializeField] private float hungryThreshold = 30f;

        [Header("Minigame rewards to needs")]
        [SerializeField] private float happinessFromMinigame = 18f;

        public float Hunger => hunger;
        public float Happiness => happiness;
        public bool IsHungry => hunger <= hungryThreshold;
        public bool IsStarving => hunger <= 10f;
        public bool IsSad => happiness <= 25f;

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

        public bool TryFeed(FoodItemDefinition food)
        {
            if (food == null)
                return false;

            SetHunger(hunger + food.hungerRestore);
            SetHappiness(happiness + food.happinessBonus);
            // Extra happiness if they were hungry and you fed them.
            if (hunger > hungryThreshold)
                SetHappiness(happiness + 4f);

            Debug.Log($"[PolyPets] Fed {name} with {food.displayName}. Hunger={hunger:0} Happy={happiness:0}");
            return true;
        }

        public void NotifyMinigameCompleted(float scoreNormalized01 = 1f)
        {
            float bonus = happinessFromMinigame * Mathf.Clamp01(scoreNormalized01);
            SetHappiness(happiness + bonus);
            // Playing burns a little hunger.
            SetHunger(hunger - 4f);
        }

        public string StatusLabel()
        {
            if (IsStarving) return "Starving…";
            if (IsHungry && IsSad) return "Hungry & sad";
            if (IsHungry) return "Hungry";
            if (IsSad) return "Needs cheer";
            if (happiness >= 80f && hunger >= 60f) return "Content";
            return "Okay";
        }
    }
}
