using UnityEngine;
using PolyPets.Needs;
using PolyPets.Shop;

namespace PolyPets.Pets
{
    /// <summary>
    /// Runtime pet instance. Visuals can be greybox primitives or a swapped mesh later.
    /// </summary>
    public sealed class PetAgent : MonoBehaviour
    {
        [SerializeField] private PetDefinition definition;
        [SerializeField] private string petName = "Mochi";
        [SerializeField] private House.RoomRoot currentRoom;
        [SerializeField] private Transform head;
        [SerializeField] private Transform body;
        [SerializeField] private PetNeeds needs;

        public PetDefinition Definition => definition;
        public string PetName => petName;
        public House.RoomRoot CurrentRoom => currentRoom;
        public PetNeeds Needs => needs != null ? needs : needs = GetComponent<PetNeeds>();

        private void Awake()
        {
            if (needs == null)
                needs = GetComponent<PetNeeds>();
            if (needs == null)
                needs = gameObject.AddComponent<PetNeeds>();
        }

        public void BindDefinition(PetDefinition def)
        {
            definition = def;
            if (def != null && string.IsNullOrWhiteSpace(petName))
                petName = def.displayName;
        }

        public void AssignRoom(House.RoomRoot room)
        {
            currentRoom = room;
        }

        public void SetVisualRoots(Transform headRoot, Transform bodyRoot)
        {
            head = headRoot;
            body = bodyRoot;
        }

        public bool TryFeedFromInventory()
        {
            var inv = FoodInventory.Instance;
            if (inv == null || Needs == null)
                return false;

            if (!inv.TryConsumeBestAvailable(out var food))
                return false;

            return Needs.TryFeed(food);
        }
    }
}
