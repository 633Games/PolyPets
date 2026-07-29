#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PolyPets.Pets;

namespace PolyPets.EditorTools
{
    public static class PetCatalogFactory
    {
        private const string Folder = "Assets/ScriptableObjects/Pets";

        public static (PetDefinition cat, PetDefinition dog, PetDefinition rabbit) EnsureStarterPets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Pets");

            var cat = GetOrCreate("PetDefinition_Cat", "cat", "Cat", PetSpecies.Cat, MinigameId.Fishing,
                "Curious, lazy",
                "Fishing QTE — wait for the bite, then mash Space / click.",
                new Color(0.86f, 0.55f, 0.28f), new Color(0.18f, 0.15f, 0.13f));

            var dog = GetOrCreate("PetDefinition_Dog", "dog", "Dog", PetSpecies.Dog, MinigameId.DigSnap,
                "Loyal, dig-happy",
                "Dig Snap — dig the garden for the buried toy, wrong holes fill in, then Snap!",
                new Color(0.72f, 0.55f, 0.35f), new Color(0.25f, 0.18f, 0.12f));

            var rabbit = GetOrCreate("PetDefinition_Rabbit", "rabbit", "Rabbit", PetSpecies.Rabbit, MinigameId.CarrotFarm,
                "Energetic, snacky",
                "Carrot Farm — run the field and collect carrots as they grow.",
                new Color(0.9f, 0.82f, 0.78f), new Color(0.55f, 0.35f, 0.4f));

            // Migrate legacy path if present
            var legacy = AssetDatabase.LoadAssetAtPath<PetDefinition>("Assets/ScriptableObjects/Pets/PetDefinition_Cat.asset");
            _ = legacy;

            AssetDatabase.SaveAssets();
            return (cat, dog, rabbit);
        }

        private static PetDefinition GetOrCreate(
            string fileName,
            string id,
            string display,
            PetSpecies species,
            MinigameId minigame,
            string personality,
            string blurb,
            Color primary,
            Color secondary)
        {
            var path = $"{Folder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PetDefinition>(path);
            if (existing != null)
            {
                existing.petId = id;
                existing.displayName = display;
                existing.species = species;
                existing.minigame = minigame;
                existing.personality = personality;
                existing.minigameBlurb = blurb;
                existing.primaryColor = primary;
                existing.secondaryColor = secondary;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var def = ScriptableObject.CreateInstance<PetDefinition>();
            def.petId = id;
            def.displayName = display;
            def.species = species;
            def.minigame = minigame;
            def.personality = personality;
            def.minigameBlurb = blurb;
            def.primaryColor = primary;
            def.secondaryColor = secondary;
            AssetDatabase.CreateAsset(def, path);
            return def;
        }
    }
}
#endif
