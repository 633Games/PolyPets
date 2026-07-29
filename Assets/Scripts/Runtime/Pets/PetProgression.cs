using System;
using UnityEngine;

namespace PolyPets.Pets
{
    /// <summary>
    /// Pet XP / levels. Levels speed idle coin drip and add a small minigame coin bonus.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PetProgression : MonoBehaviour
    {
        [SerializeField] private int level = 1;
        [SerializeField] private float xp;
        [SerializeField] private int maxLevel = 20;

        public int Level => level;
        public float Xp => xp;
        public float XpToNext => 18f + level * 14f;
        public float XpNormalized => XpToNext <= 0f ? 1f : Mathf.Clamp01(xp / XpToNext);

        /// <summary>Multiplies idle drop rate (higher = more frequent floor coins).</summary>
        public float IdleDropRateMultiplier => 1f + (level - 1) * 0.12f;

        /// <summary>Extra fraction on minigame payouts (0.05 per level above 1).</summary>
        public float MinigameCoinBonus => (level - 1) * 0.05f;

        public event Action<PetProgression> ProgressionChanged;

        public void AddXp(float amount, string source = null)
        {
            if (amount <= 0f || level >= maxLevel)
                return;

            xp += amount;
            bool leveled = false;
            while (xp >= XpToNext && level < maxLevel)
            {
                xp -= XpToNext;
                level++;
                leveled = true;
                Debug.Log($"[PolyPets] {GetComponent<PetAgent>()?.PetName ?? "Pet"} reached level {level}!");
            }

            if (level >= maxLevel)
                xp = 0f;

            ProgressionChanged?.Invoke(this);
            if (leveled)
                Feel.FeelBridge.TryPlayFeedback(gameObject, "LevelUp");
        }

        public string StatusLabel() => level >= maxLevel
            ? $"Lv {level} MAX"
            : $"Lv {level}  {xp:0}/{XpToNext:0} XP";
    }
}
