using UnityEngine;
using PolyPets.House;

namespace PolyPets.Pets
{
    /// <summary>
    /// Runtime pet instance. Visuals can be greybox primitives or a swapped mesh later.
    /// </summary>
    public sealed class PetAgent : MonoBehaviour
    {
        [SerializeField] private PetDefinition definition;
        [SerializeField] private string petName = "Mochi";
        [SerializeField] private RoomRoot currentRoom;
        [SerializeField] private Transform head;
        [SerializeField] private Transform body;

        [Header("Needs (0-100)")]
        [Range(0, 100)] [SerializeField] private float hunger = 80f;
        [Range(0, 100)] [SerializeField] private float clean = 80f;
        [Range(0, 100)] [SerializeField] private float fun = 80f;

        public PetDefinition Definition => definition;
        public string PetName => petName;
        public RoomRoot CurrentRoom => currentRoom;

        public float Mood => (hunger + clean + fun) / 3f;

        public void BindDefinition(PetDefinition def)
        {
            definition = def;
            if (def != null && string.IsNullOrWhiteSpace(petName))
                petName = def.displayName;
        }

        public void AssignRoom(RoomRoot room)
        {
            currentRoom = room;
        }

        public void SetVisualRoots(Transform headRoot, Transform bodyRoot)
        {
            head = headRoot;
            body = bodyRoot;
        }
    }
}
