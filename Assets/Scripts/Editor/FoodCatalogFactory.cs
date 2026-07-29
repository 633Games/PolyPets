#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PolyPets.Shop;

namespace PolyPets.EditorTools
{
    public static class FoodCatalogFactory
    {
        private const string Folder = "Assets/ScriptableObjects/Food";

        public static FoodItemDefinition[] EnsureDefaultFoods()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Food");

            var snack = GetOrCreate("Food_Kibble", "kibble", "Kibble", FoodCategory.Snack, 8, 22f, 4f,
                "Cheap crunchy bits. Buy with minigame coins.");
            var meal = GetOrCreate("Food_FishBowl", "fish_bowl", "Fish Bowl", FoodCategory.Meal, 18, 45f, 10f,
                "A proper meal. Worth a good fishing run.");
            var treat = GetOrCreate("Food_CatnipCookie", "catnip_cookie", "Catnip Cookie", FoodCategory.Treat, 14, 12f, 20f,
                "Not filling, but a big happiness bump.");

            AssetDatabase.SaveAssets();
            return new[] { snack, meal, treat };
        }

        private static FoodItemDefinition GetOrCreate(
            string fileName, string id, string display, FoodCategory cat, int price, float hunger, float happy, string blurb)
        {
            var path = $"{Folder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<FoodItemDefinition>(path);
            if (existing != null)
                return existing;

            var item = ScriptableObject.CreateInstance<FoodItemDefinition>();
            item.foodId = id;
            item.displayName = display;
            item.category = cat;
            item.priceCoins = price;
            item.hungerRestore = hunger;
            item.happinessBonus = happy;
            item.blurb = blurb;
            AssetDatabase.CreateAsset(item, path);
            return item;
        }
    }
}
#endif
