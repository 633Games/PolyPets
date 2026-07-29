using System;
using UnityEngine;
using PolyPets.House;

namespace PolyPets.Needs
{
    /// <summary>
    /// Hunger, happiness, cleanliness. Pets never die — they only get hungry, sad, and dirty.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PetNeeds : MonoBehaviour
    {
        [Header("Meters (0-100)")]
        [Range(0, 100)] [SerializeField] private float hunger = 55f;
        [Range(0, 100)] [SerializeField] private float happiness = 70f;
        [Range(0, 100)] [SerializeField] private float cleanliness = 80f;

        [Header("Decay (points per real minute)")]
        [SerializeField] private float hungerDecayPerMinute = 4f;
        [SerializeField] private float happinessDecayPerMinute = 2f;
        [SerializeField] private float cleanlinessDecayPerMinute = 3.5f;
        [SerializeField] private float extraHappinessDecayWhenHungry = 3f;
        [SerializeField] private float extraHappinessDecayWhenDirty = 4f;
        [SerializeField] private float hungryThreshold = 35f;
        [SerializeField] private float dirtyThreshold = 40f;
        [Tooltip("At or above this, the bowl refuses food until hunger drops.")]
        [SerializeField] private float fullThreshold = 92f;

        [Header("Minigame / clean rewards")]
        [SerializeField] private float happinessFromMinigame = 18f;
        [SerializeField] private float happinessFromCleaning = 12f;

        public float Hunger => hunger;
        public float Happiness => happiness;
        public float Cleanliness => cleanliness;
        public float FullThreshold => fullThreshold;
        public bool IsHungry => hunger <= hungryThreshold;
        public bool IsFull => hunger >= fullThreshold;
        public bool IsVeryHungry => hunger <= 10f;
        public bool IsSad => happiness <= 25f;
        public bool IsDirty => cleanliness <= dirtyThreshold;
        public bool IsFilthy => cleanliness <= 15f;
        public bool CanAcceptFood => !IsFull;

        /// <summary>0 = clean, 1 = filthy — drives shader dirt overlay.</summary>
        public float DirtAmount01 => 1f - Mathf.Clamp01(cleanliness / 100f);

        public event Action<PetNeeds> NeedsChanged;

        private void Update()
        {
            float dtMin = Time.deltaTime / 60f;
            float happyDecay = happinessDecayPerMinute;

            // Room decorations can soften happiness decay / add passive cheer.
            var buffs = HouseBuffs.Instance;
            if (buffs != null)
            {
                happyDecay *= buffs.HappinessDecayMultiplier;
                SetHappiness(happiness + buffs.HappinessPassivePerMinute * dtMin);
            }

            if (IsHungry)
                happyDecay += extraHappinessDecayWhenHungry;
            if (IsDirty)
                happyDecay += extraHappinessDecayWhenDirty;

            SetHunger(hunger - hungerDecayPerMinute * dtMin);
            SetCleanliness(cleanliness - cleanlinessDecayPerMinute * dtMin);
            SetHappiness(happiness - happyDecay * dtMin);
        }

        public void SetHunger(float value) => SetMeter(ref hunger, value);
        public void SetHappiness(float value) => SetMeter(ref happiness, value);
        public void SetCleanliness(float value) => SetMeter(ref cleanliness, value);

        private void SetMeter(ref float meter, float value)
        {
            float clamped = Mathf.Clamp(value, 0f, 100f);
            if (Mathf.Approximately(clamped, meter))
                return;
            meter = clamped;
            NeedsChanged?.Invoke(this);
        }

        public enum FeedRejectReason
        {
            None,
            NullFood,
            Full,
        }

        public bool TryFeed(Shop.FoodItemDefinition food, out FeedRejectReason reject)
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

        public bool TryFeed(Shop.FoodItemDefinition food) => TryFeed(food, out _);

        public void NotifyMinigameCompleted(float scoreNormalized01 = 1f)
        {
            float bonus = happinessFromMinigame * Mathf.Clamp01(scoreNormalized01);
            SetHappiness(happiness + bonus);
            SetHunger(hunger - 4f);
            SetCleanliness(cleanliness - 6f); // play gets them a bit dusty
        }

        /// <summary>Scrub restores cleanliness. Pets never die — only cheer up and get cleaner.</summary>
        public void ApplyScrub(float cleanlinessGain, float happinessGainScale = 1f)
        {
            SetCleanliness(cleanliness + cleanlinessGain);
            if (cleanlinessGain > 0f)
                SetHappiness(happiness + happinessFromCleaning * 0.15f * happinessGainScale);
        }

        public void FinishCleaningSession(float scrubCoverage01)
        {
            float gain = Mathf.Lerp(8f, 45f, Mathf.Clamp01(scrubCoverage01));
            SetCleanliness(cleanliness + gain);
            SetHappiness(happiness + happinessFromCleaning * Mathf.Clamp01(scrubCoverage01));
        }

        public string StatusLabel()
        {
            // Never imply death — only mood / mess / hunger.
            if (IsFilthy && IsSad) return "Filthy & unhappy";
            if (IsFilthy) return "Filthy — time to Clean";
            if (IsVeryHungry && IsDirty) return "Very hungry & dirty";
            if (IsVeryHungry) return "Very hungry";
            if (IsFull) return "Full — wait a bit";
            if (IsHungry && IsDirty) return "Hungry & dirty";
            if (IsHungry && IsSad) return "Hungry & sad";
            if (IsHungry) return "Hungry — use the bowl";
            if (IsDirty && IsSad) return "Dirty & sulking";
            if (IsDirty) return "Dirty — scrub them";
            if (IsSad) return "Needs cheer";
            if (happiness >= 80f && hunger >= 60f && cleanliness >= 70f) return "Content & tidy";
            return "Okay";
        }
    }
}
