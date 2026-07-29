using UnityEngine;
using PolyPets.Pets;

namespace PolyPets.Shop
{
    public enum FoodTier
    {
        Budget = 0,
        Medium = 1,
        Super = 2,
    }

    [CreateAssetMenu(menuName = "PolyPets/Food Item", fileName = "FoodItem")]
    public sealed class FoodItemDefinition : ScriptableObject
    {
        public string foodId = "cat_budget";
        public string displayName = "Budget Cat Food";
        [TextArea] public string blurb = "Cheap and cheerful.";
        public PetSpecies species = PetSpecies.Cat;
        public FoodTier tier = FoodTier.Budget;
        [Min(0)] public int priceCoins = 6;
        [Range(1, 100)] public float hungerRestore = 18f;
        [Range(0, 40)] public float happinessBonus = 3f;
        public Sprite icon;

        public string TierLabel => tier switch
        {
            FoodTier.Medium => "Medium",
            FoodTier.Super => "Super",
            _ => "Budget",
        };

        public string ShopLabel => $"{displayName}\n{priceCoins}c · +{hungerRestore:0} hunger";
    }
}
