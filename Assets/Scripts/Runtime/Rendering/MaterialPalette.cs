using UnityEngine;
using PolyPets.Pets;

namespace PolyPets.Rendering
{
    /// <summary>
    /// Locked v1 material set — keep all room / prop / pet / food art on these 25 mats in Assets/Materials/.
    /// </summary>
    [CreateAssetMenu(menuName = "PolyPets/Material Palette", fileName = "MaterialPalette_V1")]
    public sealed class MaterialPalette : ScriptableObject
    {
        public const string AssetPath = "Assets/ScriptableObjects/Rendering/MaterialPalette_V1.asset";
        public const string MaterialsFolder = "Assets/Materials";
        public const int MaterialCount = 25;

        [Header("House")]
        public Material floorWornWood;
        public Material wallPeeling;
        public Material trimDark;
        public Material propDusty;
        public Material accentLamp;
        public Material rugCharcoal;
        public Material shadowBlob;
        public Material bowlCeramic;
        public Material metalDull;

        [Header("Pets")]
        public Material catOrange;
        public Material catDark;
        public Material dogTan;
        public Material dogBrown;
        public Material rabbitCream;
        public Material rabbitRose;

        [Header("Food & economy")]
        public Material foodBudget;
        public Material foodMedium;
        public Material foodSuper;
        public Material coinGold;

        [Header("Minigame / world accents")]
        public Material plantLeaf;
        public Material carrotOrange;
        public Material waterPond;
        public Material dirtGarden;
        public Material fishSilver;
        public Material skyDusk;

        public Material Get(string materialName)
        {
            return materialName switch
            {
                "Mat_Floor_WornWood" => floorWornWood,
                "Mat_Wall_Peeling" => wallPeeling,
                "Mat_Trim_Dark" => trimDark,
                "Mat_Prop_Dusty" => propDusty,
                "Mat_Accent_Lamp" => accentLamp,
                "Mat_Rug_Charcoal" => rugCharcoal,
                "Mat_Shadow_Blob" => shadowBlob,
                "Mat_Bowl_Ceramic" => bowlCeramic,
                "Mat_Metal_Dull" => metalDull,
                "Mat_Cat_Orange" => catOrange,
                "Mat_Cat_Dark" => catDark,
                "Mat_Dog_Tan" => dogTan,
                "Mat_Dog_Brown" => dogBrown,
                "Mat_Rabbit_Cream" => rabbitCream,
                "Mat_Rabbit_Rose" => rabbitRose,
                "Mat_Food_Budget" => foodBudget,
                "Mat_Food_Medium" => foodMedium,
                "Mat_Food_Super" => foodSuper,
                "Mat_Coin_Gold" => coinGold,
                "Mat_Plant_Leaf" => plantLeaf,
                "Mat_Carrot_Orange" => carrotOrange,
                "Mat_Water_Pond" => waterPond,
                "Mat_Dirt_Garden" => dirtGarden,
                "Mat_Fish_Silver" => fishSilver,
                "Mat_Sky_Dusk" => skyDusk,
                _ => null,
            };
        }

        public void GetPetPair(PetSpecies species, out Material primary, out Material secondary)
        {
            switch (species)
            {
                case PetSpecies.Dog:
                    primary = dogTan;
                    secondary = dogBrown;
                    break;
                case PetSpecies.Rabbit:
                    primary = rabbitCream;
                    secondary = rabbitRose;
                    break;
                default:
                    primary = catOrange;
                    secondary = catDark;
                    break;
            }
        }
    }
}
