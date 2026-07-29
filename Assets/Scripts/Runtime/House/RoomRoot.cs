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
            if (slot == null || def == null)
                return;

            GameObject go;
            // Multi-part décor reads better than a single primitive blob.
            if (def.decorationId == "plant_pot")
                go = SpawnPlantDecor(slot, mat);
            else if (def.decorationId == "cozy_lamp")
                go = SpawnLampDecor(slot, mat);
            else
            {
                go = GameObject.CreatePrimitive(def.primitive);
                go.name = $"Decor_{def.decorationId}";
                go.transform.SetParent(slot, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = def.localScale;
                var col = go.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);
                ApplyDecorMat(go, mat, def.tint);
            }

            _spawnedDecor.Add(go);
        }

        private static GameObject SpawnPlantDecor(Transform slot, Material leafMat)
        {
            var root = new GameObject("Decor_plant_pot");
            root.transform.SetParent(slot, false);
            var pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.name = "Pot";
            pot.transform.SetParent(root.transform, false);
            pot.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            pot.transform.localScale = new Vector3(0.32f, 0.2f, 0.32f);
            Destroy(pot.GetComponent<Collider>());
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.name = "Leaf";
            leaf.transform.SetParent(root.transform, false);
            leaf.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            leaf.transform.localScale = new Vector3(0.4f, 0.38f, 0.4f);
            Destroy(leaf.GetComponent<Collider>());
            ApplyDecorMat(leaf, leafMat, new Color(0.4f, 0.65f, 0.35f));
            return root;
        }

        private static GameObject SpawnLampDecor(Transform slot, Material shadeMat)
        {
            var root = new GameObject("Decor_cozy_lamp");
            root.transform.SetParent(slot, false);
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(root.transform, false);
            pole.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            pole.transform.localScale = new Vector3(0.07f, 0.35f, 0.07f);
            Destroy(pole.GetComponent<Collider>());
            var shade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shade.name = "Shade";
            shade.transform.SetParent(root.transform, false);
            shade.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            shade.transform.localScale = new Vector3(0.4f, 0.25f, 0.4f);
            Destroy(shade.GetComponent<Collider>());
            ApplyDecorMat(shade, shadeMat, new Color(0.95f, 0.78f, 0.45f));
            return root;
        }

        private static void ApplyDecorMat(GameObject go, Material mat, Color tint)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;
            if (mat != null)
                renderer.sharedMaterial = mat;
            else if (renderer.sharedMaterial != null)
            {
                var inst = new Material(renderer.sharedMaterial);
                if (inst.HasProperty("_BaseColor"))
                    inst.SetColor("_BaseColor", tint);
                else
                    inst.color = tint;
                renderer.sharedMaterial = inst;
            }
        }

        public int FreeSlotCount =>
            decorationSlots == null ? 0 : Mathf.Max(0, decorationSlots.Length - placed.Count);
    }
}
