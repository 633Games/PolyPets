using UnityEngine;
using PolyPets.House;

namespace PolyPets.Shop
{
    public enum DecorationSlotKind
    {
        FloorProp = 0,
        WallHang = 1,
        Corner = 2,
        Centerpiece = 3,
    }

    /// <summary>
    /// Buyable room decoration. Buffs happiness comfort and minigame coin earnings.
    /// </summary>
    [CreateAssetMenu(menuName = "PolyPets/Decoration", fileName = "Decoration_")]
    public sealed class DecorationDefinition : ScriptableObject
    {
        public string decorationId = "plant_pot";
        public string displayName = "Plant Pot";
        [TextArea] public string blurb = "A little green friend for the room.";
        public int priceCoins = 18;
        [Tooltip("Which rooms can place this. Empty = any room.")]
        public string[] allowedRoomIds;
        public DecorationSlotKind slotKind = DecorationSlotKind.FloorProp;

        [Header("Stat effects (while placed)")]
        [Tooltip("Added to house happiness passive gain per minute.")]
        public float happinessPassivePerMinute = 1.5f;
        [Tooltip("Multiplies happiness decay (0.85 = 15% slower decay).")]
        [Range(0.5f, 1f)] public float happinessDecayMultiplier = 0.92f;
        [Tooltip("Added to minigame coin multiplier (0.1 = +10% coins).")]
        public float coinEarnBonus = 0.1f;

        [Header("Greybox visual")]
        public PrimitiveType primitive = PrimitiveType.Cube;
        public Vector3 localScale = new(0.45f, 0.55f, 0.45f);
        public Color tint = new(0.45f, 0.7f, 0.4f, 1f);
        public string materialName = "Mat_Plant_Leaf";

        public bool AllowedInRoom(string roomId)
        {
            if (allowedRoomIds == null || allowedRoomIds.Length == 0)
                return true;
            for (int i = 0; i < allowedRoomIds.Length; i++)
            {
                if (allowedRoomIds[i] == roomId)
                    return true;
            }

            return false;
        }
    }
}
