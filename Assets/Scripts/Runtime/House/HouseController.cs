using System.Collections.Generic;
using UnityEngine;

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

            activeRoomIndex = Mathf.Clamp(index, 0, rooms.Count - 1);

            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i] != null)
                    rooms[i].gameObject.SetActive(i == activeRoomIndex);
            }
        }
    }
}
