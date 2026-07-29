using UnityEngine;

namespace PolyPets.Shop
{
    public enum FoodCategory
    {
        Snack = 0,
        Meal = 1,
        Treat = 2,
    }

    [CreateAssetMenu(menuName = "PolyPets/Food Item", fileName = "FoodItem")]
    public sealed class FoodItemDefinition : ScriptableObject
    {
        public string foodId = "kibble";
        public string displayName = "Kibble";
        [TextArea] public string blurb = "A basic bowl of food.";
        public FoodCategory category = FoodCategory.Snack;
        [Min(0)] public int priceCoins = 8;
        [Range(1, 100)] public float hungerRestore = 25f;
        [Range(0, 50)] public float happinessBonus = 5f;
        public Sprite icon;
    }
}
