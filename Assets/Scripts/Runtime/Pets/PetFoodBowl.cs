using UnityEngine;
using PolyPets.Feel;
using PolyPets.Needs;
using PolyPets.Pets;
using PolyPets.Shop;

namespace PolyPets.Pets
{
    /// <summary>
    /// Clickable food bowl beside a pet. Uses owned species food; refuses when the pet is full.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PetFoodBowl : MonoBehaviour
    {
        [SerializeField] private PetAgent pet;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private MeshRenderer bowlRenderer;
        [SerializeField] private Color emptyColor = new(0.45f, 0.4f, 0.35f);
        [SerializeField] private Color readyColor = new(0.75f, 0.55f, 0.3f);
        [SerializeField] private Color fullPetColor = new(0.35f, 0.45f, 0.55f);

        public PetAgent Pet => pet;

        public void Bind(PetAgent owner, FoodInventory inv = null)
        {
            pet = owner;
            inventory = inv != null ? inv : FoodInventory.Instance;
            RefreshVisual();
        }

        private void OnEnable()
        {
            if (inventory == null)
                inventory = FoodInventory.Instance;
            if (inventory != null)
                inventory.InventoryChanged += RefreshVisual;
            if (pet?.Needs != null)
                pet.Needs.NeedsChanged += OnNeedsChanged;
            RefreshVisual();
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.InventoryChanged -= RefreshVisual;
            if (pet?.Needs != null)
                pet.Needs.NeedsChanged -= OnNeedsChanged;
        }

        private void OnNeedsChanged(PetNeeds _) => RefreshVisual();

        private void OnMouseDown()
        {
            TryFeedFromBowl();
        }

        public bool TryFeedFromBowl()
        {
            if (pet == null)
            {
                Debug.LogWarning("[PolyPets] Bowl has no pet.");
                return false;
            }

            var needs = pet.Needs;
            if (needs == null)
                return false;

            if (needs.IsFull)
            {
                PolyPets.Audio.JuicySfx.PlayDeny();
                Debug.Log($"[PolyPets] {pet.PetName} is full — wait until hunger drops.");
                return false;
            }

            inventory ??= FoodInventory.Instance;
            if (inventory == null)
                return false;

            var species = pet.Definition != null ? pet.Definition.species : PetSpecies.Cat;
            if (!inventory.TryConsumeBestForSpecies(species, out var food))
            {
                PolyPets.Audio.JuicySfx.PlayDeny();
                Debug.Log($"[PolyPets] No {species} food in inventory. Open the shop!");
                return false;
            }

            if (!needs.TryFeed(food, out var reject))
            {
                // Refund if somehow rejected after consume (shouldn't happen often).
                if (reject == PetNeeds.FeedRejectReason.Full)
                    inventory.Add(food, 1);
                PolyPets.Audio.JuicySfx.PlayDeny();
                Debug.Log($"[PolyPets] {pet.PetName} won't eat right now ({reject}).");
                return false;
            }

            FeelTagBinder.EnsureTagOn(gameObject, FeelTagType.Punch, autoPlay: false)?.Play();
            PolyPets.Audio.JuicySfx.PlayFeed();
            PolyPets.Audio.JuicySfx.PlayPop();
            RefreshVisual();
            return true;
        }

        public void RefreshVisual()
        {
            if (bowlRenderer == null)
                bowlRenderer = GetComponent<MeshRenderer>();
            if (bowlRenderer == null)
                return;

            var species = pet?.Definition != null ? pet.Definition.species : PetSpecies.Cat;
            int stock = inventory != null ? inventory.CountForSpecies(species) : 0;
            bool full = pet?.Needs != null && pet.Needs.IsFull;

            Color c = emptyColor;
            if (full) c = fullPetColor;
            else if (stock > 0) c = readyColor;

            if (bowlRenderer.sharedMaterial != null && bowlRenderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                // Instance so we don't tint every bowl mat asset.
                var mat = bowlRenderer.material;
                mat.SetColor("_BaseColor", c);
            }
            else if (bowlRenderer.sharedMaterial != null)
            {
                var mat = bowlRenderer.material;
                mat.color = c;
            }
        }
    }
}
