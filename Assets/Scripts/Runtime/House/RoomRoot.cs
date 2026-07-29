using UnityEngine;
using PolyPets.Pets;

namespace PolyPets.House
{
    public sealed class RoomRoot : MonoBehaviour
    {
        [SerializeField] private string roomId = "living_room";
        [SerializeField] private string displayName = "Living Room";
        [SerializeField] private Transform focusAnchor;
        [SerializeField] private Transform petAnchor;
        [SerializeField] private PetAgent occupant;

        public string RoomId => roomId;
        public string DisplayName => displayName;
        public PetAgent Occupant => occupant;

        public Vector3 FocusPoint => focusAnchor != null ? focusAnchor.position : transform.position;
        public Transform PetAnchor => petAnchor != null ? petAnchor : transform;

        public void Configure(string id, string name)
        {
            roomId = id;
            displayName = name;
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
    }
}
