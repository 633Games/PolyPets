#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PolyPets.Shop;

namespace PolyPets.EditorTools
{
    public static class DecorationCatalogFactory
    {
        private const string Folder = "Assets/ScriptableObjects/Decorations";

        public static DecorationDefinition[] EnsureDefaultDecorations()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Decorations");

            var list = new[]
            {
                Make("plant_pot", "Plant Pot", "Softens the mood.", 16,
                    new[] { "living_room", "bedroom", "garden" }, DecorationSlotKind.FloorProp,
                    1.8f, 0.94f, 0.08f, PrimitiveType.Cylinder, new Vector3(0.35f, 0.4f, 0.35f),
                    new Color(0.4f, 0.65f, 0.35f), "Mat_Plant_Leaf"),
                Make("cozy_lamp", "Cozy Lamp", "Warm light, happier evenings.", 22,
                    new[] { "living_room", "bedroom" }, DecorationSlotKind.Corner,
                    2.2f, 0.9f, 0.12f, PrimitiveType.Cube, new Vector3(0.35f, 0.55f, 0.35f),
                    new Color(0.95f, 0.78f, 0.45f), "Mat_Accent_Lamp"),
                Make("plush_rug", "Plush Rug", "Comfy paws.", 20,
                    new[] { "living_room", "bedroom" }, DecorationSlotKind.Centerpiece,
                    1.5f, 0.93f, 0.06f, PrimitiveType.Cube, new Vector3(1.2f, 0.04f, 0.8f),
                    new Color(0.35f, 0.28f, 0.25f), "Mat_Rug_Charcoal"),
                Make("kitchen_shelf", "Snack Shelf", "Treats within reach.", 24,
                    new[] { "kitchen" }, DecorationSlotKind.WallHang,
                    1.2f, 0.95f, 0.15f, PrimitiveType.Cube, new Vector3(0.9f, 0.15f, 0.3f),
                    new Color(0.55f, 0.4f, 0.28f), "Mat_Prop_Dusty"),
                Make("fruit_bowl", "Fruit Bowl", "Looks tasty — mood up.", 18,
                    new[] { "kitchen", "living_room" }, DecorationSlotKind.FloorProp,
                    2.0f, 0.92f, 0.1f, PrimitiveType.Sphere, new Vector3(0.4f, 0.25f, 0.4f),
                    new Color(0.85f, 0.45f, 0.3f), "Mat_Food_Medium"),
                Make("garden_bed", "Garden Bed", "Dirt & sprouts — dig energy!", 26,
                    new[] { "garden" }, DecorationSlotKind.Centerpiece,
                    1.6f, 0.94f, 0.18f, PrimitiveType.Cube, new Vector3(1.4f, 0.25f, 0.7f),
                    new Color(0.4f, 0.28f, 0.18f), "Mat_Dirt_Garden"),
                Make("carrot_crate", "Carrot Crate", "Rabbit paradise.", 28,
                    new[] { "garden", "kitchen" }, DecorationSlotKind.FloorProp,
                    1.4f, 0.93f, 0.2f, PrimitiveType.Cube, new Vector3(0.55f, 0.4f, 0.45f),
                    new Color(0.9f, 0.5f, 0.2f), "Mat_Carrot_Orange"),
                Make("star_poster", "Star Poster", "Dream big — earn more.", 30,
                    new[] { "bedroom", "living_room" }, DecorationSlotKind.WallHang,
                    1.0f, 0.96f, 0.22f, PrimitiveType.Cube, new Vector3(0.7f, 0.5f, 0.05f),
                    new Color(0.85f, 0.75f, 0.35f), "Mat_Coin_Gold"),
                Make("pond_basin", "Pond Basin", "Ripple of luck for fishing.", 32,
                    new[] { "garden", "living_room" }, DecorationSlotKind.FloorProp,
                    1.3f, 0.94f, 0.25f, PrimitiveType.Cylinder, new Vector3(0.7f, 0.12f, 0.7f),
                    new Color(0.35f, 0.55f, 0.65f), "Mat_Water_Pond"),
            };

            AssetDatabase.SaveAssets();
            return list;
        }

        private static DecorationDefinition Make(
            string id,
            string display,
            string blurb,
            int price,
            string[] rooms,
            DecorationSlotKind kind,
            float happyPassive,
            float decayMul,
            float coinBonus,
            PrimitiveType prim,
            Vector3 scale,
            Color tint,
            string matName)
        {
            var path = $"{Folder}/Decoration_{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DecorationDefinition>(path);
            var def = existing != null ? existing : ScriptableObject.CreateInstance<DecorationDefinition>();
            def.decorationId = id;
            def.displayName = display;
            def.blurb = blurb;
            def.priceCoins = price;
            def.allowedRoomIds = rooms;
            def.slotKind = kind;
            def.happinessPassivePerMinute = happyPassive;
            def.happinessDecayMultiplier = decayMul;
            def.coinEarnBonus = coinBonus;
            def.primitive = prim;
            def.localScale = scale;
            def.tint = tint;
            def.materialName = matName;
            if (existing == null)
                AssetDatabase.CreateAsset(def, path);
            else
                EditorUtility.SetDirty(def);
            return def;
        }
    }
}
#endif
