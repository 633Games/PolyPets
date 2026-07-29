using System.Collections.Generic;
using UnityEngine;
using PolyPets.Pets;

namespace PolyPets.House
{
    public sealed class HouseController : MonoBehaviour
    {
        [SerializeField] private List<RoomRoot> rooms = new();
        [SerializeField] private int activeRoomIndex;

        public RoomRoot ActiveRoom =>
            rooms != null && rooms.Count > 0
                ? rooms[Mathf.Clamp(activeRoomIndex, 0, rooms.Count - 1)]
                : null;

        public IReadOnlyList<RoomRoot> Rooms => rooms;
        public int ActiveRoomIndex => activeRoomIndex;

        public void Initialize()
        {
            if (rooms == null)
                rooms = new List<RoomRoot>();

            if (rooms.Count == 0)
                rooms.AddRange(GetComponentsInChildren<RoomRoot>(true));

            SetActiveRoom(activeRoomIndex);
        }

        public void RegisterRoom(RoomRoot room)
        {
            if (room == null)
                return;

            if (rooms == null)
                rooms = new List<RoomRoot>();

            if (!rooms.Contains(room))
                rooms.Add(room);
        }

        public void SetActiveRoom(int index)
        {
            if (rooms == null || rooms.Count == 0)
                return;

            var previous = ActiveRoom;
            PetAgent pet = previous != null ? previous.Occupant : null;

            activeRoomIndex = Mathf.Clamp(index, 0, rooms.Count - 1);

            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i] != null)
                    rooms[i].gameObject.SetActive(i == activeRoomIndex);
            }

            if (pet != null && ActiveRoom != null && ActiveRoom != previous)
            {
                if (previous != null)
                    previous.ClearOccupantKeepWorld(pet);
                ActiveRoom.SetOccupant(pet);
            }

            HouseBuffs.Instance?.Recalculate();
            Economy.IdleCoinSpawner.Instance?.OnRoomChanged();
            if (Application.isPlaying)
                PolyPets.Audio.JuicySfx.PlayRoomWhoosh();
        }

        public void NextRoom()
        {
            if (rooms == null || rooms.Count == 0)
                return;
            SetActiveRoom((activeRoomIndex + 1) % rooms.Count);
        }

        public void PrevRoom()
        {
            if (rooms == null || rooms.Count == 0)
                return;
            SetActiveRoom((activeRoomIndex - 1 + rooms.Count) % rooms.Count);
        }
    }
}
