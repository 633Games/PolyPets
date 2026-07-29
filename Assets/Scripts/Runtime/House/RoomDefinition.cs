using UnityEngine;

namespace PolyPets.House
{
    [CreateAssetMenu(menuName = "PolyPets/Room Definition", fileName = "RoomDefinition")]
    public sealed class RoomDefinition : ScriptableObject
    {
        public string roomId = "living_room";
        public string displayName = "Living Room";
        public int unlockCost = 0;
        public int petSlots = 1;
        [TextArea] public string blurb = "A rundown lounge with one lonely lamp.";
    }
}
