using UnityEngine;

namespace PolyPets.Pets
{
    public enum PetSpecies
    {
        Cat = 0,
        Dog = 1,
        Rabbit = 2,
    }

    public enum MinigameId
    {
        Fishing = 0,
        DigSnap = 1,
        CarrotFarm = 2,
    }

    [CreateAssetMenu(menuName = "PolyPets/Pet Definition", fileName = "PetDefinition")]
    public sealed class PetDefinition : ScriptableObject
    {
        public string petId = "cat";
        public string displayName = "Cat";
        public PetSpecies species = PetSpecies.Cat;
        public MinigameId minigame = MinigameId.Fishing;
        [TextArea] public string personality = "Curious, lazy";
        [TextArea] public string minigameBlurb = "Fishing QTE — hit the bite window.";
        [Tooltip("Legacy idle rate — coins come from minigames only.")]
        public float baseCoinsPerSecond = 0f;
        public Color primaryColor = new(0.85f, 0.55f, 0.25f);
        public Color secondaryColor = new(0.2f, 0.18f, 0.16f);

        public string SignatureMinigameName => minigame switch
        {
            MinigameId.DigSnap => "DigSnap",
            MinigameId.CarrotFarm => "CarrotFarm",
            _ => "Fishing",
        };
    }
}
