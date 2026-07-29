using UnityEngine;
using PolyPets.House;
using PolyPets.Needs;
using PolyPets.Shop;

namespace PolyPets.Pets
{
    /// <summary>
    /// Runtime pet instance. Visuals are cel-shaded box pals (swap mesh later if desired).
    /// </summary>
    public sealed class PetAgent : MonoBehaviour
    {
        [SerializeField] private PetDefinition definition;
        [SerializeField] private string petName = "Mochi";
        [SerializeField] private RoomRoot currentRoom;
        [SerializeField] private Transform head;
        [SerializeField] private Transform body;
        [SerializeField] private PetNeeds needs;

        public PetDefinition Definition => definition;
        public string PetName => petName;
        public RoomRoot CurrentRoom => currentRoom;
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

        public void SetPetName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
                petName = name.Trim();
            gameObject.name = $"Pet_{definition?.species ?? PetSpecies.Cat}_{petName}";
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

        public bool TryFeedFromInventory()
        {
            var inv = FoodInventory.Instance;
            if (inv == null || Needs == null)
                return false;

            if (Needs.IsFull)
                return false;

            var species = definition != null ? definition.species : PetSpecies.Cat;
            if (!inv.TryConsumeBestForSpecies(species, out var food))
                return false;

            return Needs.TryFeed(food);
        }

        public PetFoodBowl EnsureFoodBowl(Material bowlMat = null)
        {
            var existing = GetComponentInChildren<PetFoodBowl>(true);
            if (existing != null)
            {
                existing.Bind(this);
                return existing;
            }

            var bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "FoodBowl";
            bowl.transform.SetParent(transform, false);
            bowl.transform.localPosition = new Vector3(0.55f, 0.08f, 0.15f);
            bowl.transform.localScale = new Vector3(0.35f, 0.08f, 0.35f);
            if (bowlMat != null)
            {
                var renderer = bowl.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.sharedMaterial = bowlMat;
            }

            var click = bowl.AddComponent<PetFoodBowl>();
            click.Bind(this);
            return click;
        }
    }
}
