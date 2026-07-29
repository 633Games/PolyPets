#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PolyPets.Rendering;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// Builds the locked 25-material v1 set under Assets/Materials/ + MaterialPalette_V1 asset,
    /// and wires albedo maps from Assets/Art/Textures (AmbientCG photo + procedural placeholders).
    /// Menu: PolyPets → Materials → Rebuild Color Palette (25 mats)
    /// </summary>
    public static class MaterialPaletteFactory
    {
        private const string RootMenu = "PolyPets/Materials/";
        private const string Folder = MaterialPalette.MaterialsFolder;
        private const string PalettePath = MaterialPalette.AssetPath;
        private const string TexPhoto = "Assets/Art/Textures/Photo";
        private const string TexProc = "Assets/Art/Textures/Procedural";

        private struct Spec
        {
            public string Name;
            public Color Color;
            public Color Shade;
            public float Outline;
            public float ShadeThreshold;
            public float Rim;
            public string TexturePath; // under Assets/
            public bool PhotoMap;     // photo maps: keep base color near white so albedo reads
        }

        // Exactly 25 — keep new objects on this list only.
        private static readonly Spec[] Specs =
        {
            // House (9)
            S("Mat_Floor_WornWood", "FFFFFF", "472E24", 0.008f, $"{TexPhoto}/Tex_Floor_Wood.png", photo: true),
            S("Mat_Wall_Peeling", "E8E0D4", "665C57", 0.006f, $"{TexPhoto}/Tex_Wall_Plaster.png", photo: true),
            S("Mat_Trim_Dark", "B0A8A0", "1F1A1A", 0.010f, $"{TexPhoto}/Tex_Trim_Stone.png", photo: true),
            S("Mat_Prop_Dusty", "E0D8D0", "383333", 0.010f, $"{TexPhoto}/Tex_Prop_Wood.png", photo: true),
            S("Mat_Accent_Lamp", "F2C773", "8C5933", 0.010f, $"{TexProc}/Tex_Lamp_Glow.png", rim: 0.35f),
            S("Mat_Rug_Charcoal", "C8C0B8", "161412", 0.006f, $"{TexPhoto}/Tex_Rug_Fabric.png", photo: true),
            S("Mat_Shadow_Blob", "1A1716", "0C0A0A", 0.000f, $"{TexProc}/Tex_Shadow.png", thresh: 0.6f, rim: 0f),
            S("Mat_Bowl_Ceramic", "D9D0C4", "7A7168", 0.010f, $"{TexProc}/Tex_Ceramic_Soft.png"),
            S("Mat_Metal_Dull", "FFFFFF", "3E4248", 0.008f, $"{TexPhoto}/Tex_Metal_Scratched.png", photo: true, thresh: 0.5f, rim: 0.3f),

            // Pets (6)
            S("Mat_Cat_Orange", "DB8C47", "734029", 0.014f, $"{TexProc}/Tex_Cat_Fur.png"),
            S("Mat_Cat_Dark", "2E2621", "14100F", 0.012f, $"{TexProc}/Tex_Cat_DarkFur.png"),
            S("Mat_Dog_Tan", "B88C59", "66401F", 0.014f, $"{TexProc}/Tex_Dog_Fur.png"),
            S("Mat_Dog_Brown", "40301F", "1A140C", 0.012f, $"{TexProc}/Tex_Dog_DarkFur.png"),
            S("Mat_Rabbit_Cream", "E6D1C7", "8C5966", 0.014f, $"{TexProc}/Tex_Rabbit_Fur.png"),
            S("Mat_Rabbit_Rose", "8C5966", "4D2E38", 0.012f, $"{TexProc}/Tex_Rabbit_Accent.png"),

            // Food & economy (4)
            S("Mat_Food_Budget", "A68F6A", "5C4A33", 0.010f, $"{TexProc}/Tex_Food_Budget.png"),
            S("Mat_Food_Medium", "D98A4A", "7A4020", 0.010f, $"{TexProc}/Tex_Food_Medium.png"),
            S("Mat_Food_Super", "E84B5A", "7A2030", 0.012f, $"{TexProc}/Tex_Food_Super.png", rim: 0.28f),
            S("Mat_Coin_Gold", "E8C04A", "8C6A1A", 0.010f, $"{TexProc}/Tex_Coin_Gold.png", rim: 0.4f),

            // Minigame / world (6)
            S("Mat_Plant_Leaf", "5A8F4A", "2E4D24", 0.010f, $"{TexProc}/Tex_Leaf_Soft.png"),
            S("Mat_Carrot_Orange", "E87A2E", "8C3A12", 0.012f, $"{TexProc}/Tex_Carrot.png"),
            S("Mat_Water_Pond", "4A7A8C", "243E4D", 0.008f, $"{TexProc}/Tex_Water_Pond.png", rim: 0.35f),
            S("Mat_Dirt_Garden", "FFFFFF", "2E211A", 0.008f, $"{TexPhoto}/Tex_Dirt_Ground.png", photo: true),
            S("Mat_Fish_Silver", "C5D0D9", "5A6670", 0.010f, $"{TexProc}/Tex_Fish_Scales.png", rim: 0.32f),
            S("Mat_Sky_Dusk", "FFFFFF", "2E2438", 0.000f, $"{TexProc}/Tex_Sky_Dusk.png", thresh: 0.55f, rim: 0.15f),
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

        [MenuItem(RootMenu + "Reimport Texture Placeholders", priority = 2)]
        public static void ReimportTexturesMenu()
        {
            EnsureTextureImports();
            EnsurePalette(showDialog: false);
            EditorUtility.DisplayDialog(
                "Textures",
                "Reimported Art/Textures and re-wired material albedos.\n\n" +
                "Photo = AmbientCG CC0 · Procedural = generated placeholders.",
                "OK");
        }

        public static MaterialPalette EnsurePalette(bool showDialog = false)
        {
            EnsureFolders();
            EnsureTextureImports();

            if (Specs.Length != MaterialPalette.MaterialCount)
                Debug.LogError($"[PolyPets] Palette spec count {Specs.Length} != {MaterialPalette.MaterialCount}");

            var shader = Shader.Find("PolyPets/CelShade")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");

            var created = new Dictionary<string, Material>(Specs.Length);
            foreach (var spec in Specs)
                created[spec.Name] = GetOrCreate(spec, shader);

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
                    $"Created/updated {Specs.Length} materials in {Folder}.\n" +
                    "Albedo maps wired from Assets/Art/Textures.\n\n" +
                    "See docs/COLOR_PALETTE.md",
                    "Nice");
            }

            Debug.Log($"[PolyPets] Material palette ready ({Specs.Length} mats + textures) → {Folder}");
            return palette;
        }

        public static Material Load(string materialName)
        {
            EnsurePalette(showDialog: false);
            return AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{materialName}.mat");
        }

        private static Spec S(
            string name,
            string hex,
            string shadeHex,
            float outline,
            string texturePath,
            float thresh = 0.45f,
            float rim = 0.22f,
            bool photo = false)
        {
            return new Spec
            {
                Name = name,
                Color = Hex(hex),
                Shade = Hex(shadeHex),
                Outline = outline,
                ShadeThreshold = thresh,
                Rim = rim,
                TexturePath = texturePath,
                PhotoMap = photo,
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

            var tex = LoadAlbedo(spec.TexturePath);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
            }
        }

        private static Texture2D LoadAlbedo(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
                return null;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static void EnsureTextureImports()
        {
            AssetDatabase.Refresh();
            foreach (var folder in new[] { TexPhoto, TexProc })
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    continue;

                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null)
                        continue;

                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Default)
                    {
                        importer.textureType = TextureImporterType.Default;
                        changed = true;
                    }

                    if (importer.textureShape != TextureImporterShape.Texture2D)
                    {
                        importer.textureShape = TextureImporterShape.Texture2D;
                        changed = true;
                    }

                    if (!importer.sRGBTexture)
                    {
                        importer.sRGBTexture = true;
                        changed = true;
                    }

                    if (!importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = true;
                        changed = true;
                    }

                    if (importer.wrapMode != TextureWrapMode.Repeat)
                    {
                        importer.wrapMode = TextureWrapMode.Repeat;
                        changed = true;
                    }

                    if (importer.filterMode != FilterMode.Bilinear)
                    {
                        importer.filterMode = FilterMode.Bilinear;
                        changed = true;
                    }

                    // Photo maps stay sharper; procedural can be a bit softer.
                    var maxSize = path.Contains("/Photo/") ? 512 : 256;
                    if (importer.maxTextureSize != maxSize)
                    {
                        importer.maxTextureSize = maxSize;
                        changed = true;
                    }

                    if (changed)
                        importer.SaveAndReimport();
                }
            }
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/Art"))
                AssetDatabase.CreateFolder("Assets", "Art");
            if (!AssetDatabase.IsValidFolder("Assets/Art/Textures"))
                AssetDatabase.CreateFolder("Assets/Art", "Textures");
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Rendering"))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Rendering");
        }
    }
}
#endif
