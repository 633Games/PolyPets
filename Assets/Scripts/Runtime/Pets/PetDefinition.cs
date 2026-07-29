using UnityEngine;

namespace PolyPets.Pets
{
    [CreateAssetMenu(menuName = "PolyPets/Pet Definition", fileName = "PetDefinition")]
    public sealed class PetDefinition : ScriptableObject
    {
        public string petId = "cat";
        public string displayName = "Cat";
        [TextArea] public string personality = "Curious, lazy";
        public string signatureMinigame = "Fishing";
        [Tooltip("Legacy idle rate — coins now come from minigames only.")]
        public float baseCoinsPerSecond = 0f;
        public Color primaryColor = new(0.85f, 0.55f, 0.25f);
        public Color secondaryColor = new(0.2f, 0.18f, 0.16f);
    }
}
