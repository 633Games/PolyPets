#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PolyPets.Pets;
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

            var list = new List<FoodItemDefinition>();

            // Cat
            list.Add(GetOrCreate("Food_Cat_Budget", "cat_budget", "Budget Cat Food", PetSpecies.Cat, FoodTier.Budget,
                6, 18f, 2f, "Basic kibble. Gets the job done."));
            list.Add(GetOrCreate("Food_Cat_Medium", "cat_medium", "Medium Cat Food", PetSpecies.Cat, FoodTier.Medium,
                12, 34f, 6f, "Tuna mix. Solid mid-tier meal."));
            list.Add(GetOrCreate("Food_Cat_Super", "cat_super", "Super Cat Food", PetSpecies.Cat, FoodTier.Super,
                22, 55f, 12f, "Gourmet fish feast."));

            // Dog
            list.Add(GetOrCreate("Food_Dog_Budget", "dog_budget", "Budget Dog Food", PetSpecies.Dog, FoodTier.Budget,
                6, 18f, 2f, "Dry chow for good boys."));
            list.Add(GetOrCreate("Food_Dog_Medium", "dog_medium", "Medium Dog Food", PetSpecies.Dog, FoodTier.Medium,
                12, 34f, 6f, "Meat & biscuit blend."));
            list.Add(GetOrCreate("Food_Dog_Super", "dog_super", "Super Dog Food", PetSpecies.Dog, FoodTier.Super,
                22, 55f, 12f, "Steak dinner. Tail-wag guaranteed."));

            // Rabbit
            list.Add(GetOrCreate("Food_Rabbit_Budget", "rabbit_budget", "Budget Rabbit Food", PetSpecies.Rabbit, FoodTier.Budget,
                6, 18f, 2f, "Hay pellets."));
            list.Add(GetOrCreate("Food_Rabbit_Medium", "rabbit_medium", "Medium Rabbit Food", PetSpecies.Rabbit, FoodTier.Medium,
                12, 34f, 6f, "Veggie medley."));
            list.Add(GetOrCreate("Food_Rabbit_Super", "rabbit_super", "Super Rabbit Food", PetSpecies.Rabbit, FoodTier.Super,
                22, 55f, 12f, "Garden banquet with carrot glaze."));

            AssetDatabase.SaveAssets();
            return list.ToArray();
        }

        private static FoodItemDefinition GetOrCreate(
            string fileName,
            string id,
            string display,
            PetSpecies species,
            FoodTier tier,
            int price,
            float hunger,
            float happy,
            string blurb)
        {
            var path = $"{Folder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<FoodItemDefinition>(path);
            if (existing != null)
            {
                existing.foodId = id;
                existing.displayName = display;
                existing.species = species;
                existing.tier = tier;
                existing.priceCoins = price;
                existing.hungerRestore = hunger;
                existing.happinessBonus = happy;
                existing.blurb = blurb;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var item = ScriptableObject.CreateInstance<FoodItemDefinition>();
            item.foodId = id;
            item.displayName = display;
            item.species = species;
            item.tier = tier;
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
