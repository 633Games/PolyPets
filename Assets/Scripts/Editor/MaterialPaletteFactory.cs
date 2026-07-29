#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PolyPets.Rendering;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// Builds the locked 25-material v1 set under Assets/Materials/ + MaterialPalette_V1 asset.
    /// Menu: PolyPets → Materials → Rebuild Color Palette (25 mats)
    /// </summary>
    public static class MaterialPaletteFactory
    {
        private const string RootMenu = "PolyPets/Materials/";
        private const string Folder = MaterialPalette.MaterialsFolder;
        private const string PalettePath = MaterialPalette.AssetPath;

        private struct Spec
        {
            public string Name;
            public Color Color;
            public Color Shade;
            public float Outline;
            public float ShadeThreshold;
            public float Rim;
        }

        // Exactly 25 — keep new objects on this list only.
        private static readonly Spec[] Specs =
        {
            // House (9)
            S("Mat_Floor_WornWood", "72523A", "472E24", 0.008f),
            S("Mat_Wall_Peeling", "9E9480", "665C57", 0.006f),
            S("Mat_Trim_Dark", "403833", "1F1A1A", 0.010f),
            S("Mat_Prop_Dusty", "66615C", "383333", 0.010f),
            S("Mat_Accent_Lamp", "F2C773", "8C5933", 0.010f, rim: 0.35f),
            S("Mat_Rug_Charcoal", "2E2A28", "161412", 0.006f),
            S("Mat_Shadow_Blob", "1A1716", "0C0A0A", 0.000f, thresh: 0.6f, rim: 0f),
            S("Mat_Bowl_Ceramic", "D9D0C4", "7A7168", 0.010f),
            S("Mat_Metal_Dull", "8A8E94", "3E4248", 0.008f, thresh: 0.5f, rim: 0.3f),

            // Pets (6)
            S("Mat_Cat_Orange", "DB8C47", "734029", 0.014f),
            S("Mat_Cat_Dark", "2E2621", "14100F", 0.012f),
            S("Mat_Dog_Tan", "B88C59", "66401F", 0.014f),
            S("Mat_Dog_Brown", "40301F", "1A140C", 0.012f),
            S("Mat_Rabbit_Cream", "E6D1C7", "8C5966", 0.014f),
            S("Mat_Rabbit_Rose", "8C5966", "4D2E38", 0.012f),

            // Food & economy (4)
            S("Mat_Food_Budget", "A68F6A", "5C4A33", 0.010f),
            S("Mat_Food_Medium", "D98A4A", "7A4020", 0.010f),
            S("Mat_Food_Super", "E84B5A", "7A2030", 0.012f, rim: 0.28f),
            S("Mat_Coin_Gold", "E8C04A", "8C6A1A", 0.010f, rim: 0.4f),

            // Minigame / world (6)
            S("Mat_Plant_Leaf", "5A8F4A", "2E4D24", 0.010f),
            S("Mat_Carrot_Orange", "E87A2E", "8C3A12", 0.012f),
            S("Mat_Water_Pond", "4A7A8C", "243E4D", 0.008f, rim: 0.35f),
            S("Mat_Dirt_Garden", "5C4333", "2E211A", 0.008f),
            S("Mat_Fish_Silver", "C5D0D9", "5A6670", 0.010f, rim: 0.32f),
            S("Mat_Sky_Dusk", "6B5A7A", "2E2438", 0.000f, thresh: 0.55f, rim: 0.15f),
        };

        [MenuItem(RootMenu + "Rebuild Color Palette (25 mats)", priority = 0)]
        public static void RebuildMenu()
        {
            var palette = EnsurePalette(showDialog: true);
            Selection.activeObject = palette;
            EditorGUIUtility.PingObject(palette);
        }

        [MenuItem(RootMenu + "Select Material Palette", priority = 1)]
        public static void SelectPalette()
        {
            var palette = EnsurePalette(showDialog: false);
            Selection.activeObject = palette;
            EditorGUIUtility.PingObject(palette);
        }

        public static MaterialPalette EnsurePalette(bool showDialog = false)
        {
            EnsureFolders();
            if (Specs.Length != MaterialPalette.MaterialCount)
                Debug.LogError($"[PolyPets] Palette spec count {Specs.Length} != {MaterialPalette.MaterialCount}");

            var shader = Shader.Find("PolyPets/CelShade")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");

            var created = new Dictionary<string, Material>(Specs.Length);
            foreach (var spec in Specs)
            {
                created[spec.Name] = GetOrCreate(spec, shader);
            }

            var palette = AssetDatabase.LoadAssetAtPath<MaterialPalette>(PalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<MaterialPalette>();
                AssetDatabase.CreateAsset(palette, PalettePath);
            }

            palette.floorWornWood = created["Mat_Floor_WornWood"];
            palette.wallPeeling = created["Mat_Wall_Peeling"];
            palette.trimDark = created["Mat_Trim_Dark"];
            palette.propDusty = created["Mat_Prop_Dusty"];
            palette.accentLamp = created["Mat_Accent_Lamp"];
            palette.rugCharcoal = created["Mat_Rug_Charcoal"];
            palette.shadowBlob = created["Mat_Shadow_Blob"];
            palette.bowlCeramic = created["Mat_Bowl_Ceramic"];
            palette.metalDull = created["Mat_Metal_Dull"];
            palette.catOrange = created["Mat_Cat_Orange"];
            palette.catDark = created["Mat_Cat_Dark"];
            palette.dogTan = created["Mat_Dog_Tan"];
            palette.dogBrown = created["Mat_Dog_Brown"];
            palette.rabbitCream = created["Mat_Rabbit_Cream"];
            palette.rabbitRose = created["Mat_Rabbit_Rose"];
            palette.foodBudget = created["Mat_Food_Budget"];
            palette.foodMedium = created["Mat_Food_Medium"];
            palette.foodSuper = created["Mat_Food_Super"];
            palette.coinGold = created["Mat_Coin_Gold"];
            palette.plantLeaf = created["Mat_Plant_Leaf"];
            palette.carrotOrange = created["Mat_Carrot_Orange"];
            palette.waterPond = created["Mat_Water_Pond"];
            palette.dirtGarden = created["Mat_Dirt_Garden"];
            palette.fishSilver = created["Mat_Fish_Silver"];
            palette.skyDusk = created["Mat_Sky_Dusk"];

            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Material Palette",
                    $"Created/updated {Specs.Length} materials in {Folder}.\n\n" +
                    "Rule: new objects use only these mats.\n" +
                    "See docs/COLOR_PALETTE.md",
                    "Nice");
            }

            Debug.Log($"[PolyPets] Material palette ready ({Specs.Length} mats) → {Folder}");
            return palette;
        }

        public static Material Load(string materialName)
        {
            EnsurePalette(showDialog: false);
            return AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{materialName}.mat");
        }

        private static Spec S(string name, string hex, string shadeHex, float outline,
            float thresh = 0.45f, float rim = 0.22f)
        {
            return new Spec
            {
                Name = name,
                Color = Hex(hex),
                Shade = Hex(shadeHex),
                Outline = outline,
                ShadeThreshold = thresh,
                Rim = rim,
            };
        }

        private static Color Hex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString("#" + hex, out var c))
                c = Color.magenta;
            c.a = 1f;
            return c;
        }

        private static Material GetOrCreate(Spec spec, Shader shader)
        {
            var path = $"{Folder}/{spec.Name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                if (shader != null && existing.shader != shader)
                    existing.shader = shader;
                Apply(existing, spec);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var mat = new Material(shader) { name = spec.Name };
            Apply(mat, spec);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void Apply(Material mat, Spec spec)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", spec.Color);
            if (mat.HasProperty("_Color"))
                mat.color = spec.Color;
            if (mat.HasProperty("_ShadeColor"))
                mat.SetColor("_ShadeColor", spec.Shade);
            if (mat.HasProperty("_ShadeThreshold"))
                mat.SetFloat("_ShadeThreshold", spec.ShadeThreshold);
            if (mat.HasProperty("_ShadeSoftness"))
                mat.SetFloat("_ShadeSoftness", 0.05f);
            if (mat.HasProperty("_OutlineWidth"))
                mat.SetFloat("_OutlineWidth", spec.Outline);
            if (mat.HasProperty("_OutlineColor"))
                mat.SetColor("_OutlineColor", new Color(0.08f, 0.06f, 0.07f, 1f));
            if (mat.HasProperty("_RimStrength"))
                mat.SetFloat("_RimStrength", spec.Rim);
            if (mat.HasProperty("_RimColor"))
                mat.SetColor("_RimColor", new Color(0.75f, 0.82f, 0.9f, 1f));
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Rendering"))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Rendering");
        }
    }
}
#endif
