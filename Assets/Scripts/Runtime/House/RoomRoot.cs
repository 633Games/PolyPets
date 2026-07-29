using System.Collections.Generic;
using UnityEngine;
using PolyPets.Pets;
using PolyPets.Shop;

namespace PolyPets.House
{
    public sealed class RoomRoot : MonoBehaviour
    {
        [SerializeField] private string roomId = "living_room";
        [SerializeField] private string displayName = "Living Room";
        [SerializeField] private Transform focusAnchor;
        [SerializeField] private Transform petAnchor;
        [SerializeField] private Transform[] decorationSlots = System.Array.Empty<Transform>();
        [SerializeField] private PetAgent occupant;
        [SerializeField] private List<DecorationDefinition> placed = new();

        private readonly List<GameObject> _spawnedDecor = new();

        public string RoomId => roomId;
        public string DisplayName => displayName;
        public PetAgent Occupant => occupant;
        public IReadOnlyList<DecorationDefinition> PlacedDecorations => placed;

        public Vector3 FocusPoint => focusAnchor != null ? focusAnchor.position : transform.position;
        public Transform PetAnchor => petAnchor != null ? petAnchor : transform;

        public void Configure(string id, string name)
        {
            roomId = id;
            displayName = name;
        }

        public void SetAnchors(Transform focus, Transform pet, Transform[] slots)
        {
            focusAnchor = focus;
            petAnchor = pet;
            decorationSlots = slots ?? System.Array.Empty<Transform>();
        }

        public void SetOccupant(PetAgent pet)
        {
            occupant = pet;
            if (pet == null)
                return;

            pet.transform.SetParent(PetAnchor, false);
            pet.transform.localPosition = Vector3.zero;
            pet.transform.localRotation = Quaternion.identity;
            pet.AssignRoom(this);
        }

        public void ClearOccupantKeepWorld(PetAgent expected)
        {
            if (occupant == expected)
                occupant = null;
        }

        public bool TryPlaceDecoration(DecorationDefinition def, Material mat = null)
        {
            if (def == null || !def.AllowedInRoom(roomId))
                return false;
            if (decorationSlots == null || placed.Count >= decorationSlots.Length)
                return false;

            int slotIndex = placed.Count;
            placed.Add(def);
            SpawnVisual(def, decorationSlots[slotIndex], mat);
            HouseBuffs.Instance?.Recalculate();
            return true;
        }

        private void SpawnVisual(DecorationDefinition def, Transform slot, Material mat)
        {
            if (slot == null)
                return;
            var go = GameObject.CreatePrimitive(def.primitive);
            go.name = $"Decor_{def.decorationId}";
            go.transform.SetParent(slot, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = def.localScale;
            var col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (mat != null)
                    renderer.sharedMaterial = mat;
                else if (renderer.sharedMaterial != null)
                {
                    var inst = new Material(renderer.sharedMaterial);
                    if (inst.HasProperty("_BaseColor"))
                        inst.SetColor("_BaseColor", def.tint);
                    else
                        inst.color = def.tint;
                    renderer.sharedMaterial = inst;
                }
            }

            _spawnedDecor.Add(go);
        }

        public int FreeSlotCount =>
            decorationSlots == null ? 0 : Mathf.Max(0, decorationSlots.Length - placed.Count);
    }
}
