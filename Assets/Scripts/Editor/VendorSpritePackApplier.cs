#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PolyPets.UI;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// Imports vendored Kenney / game-icons.net PNGs as Sprites and wires UiSpritePack_Default.
    /// </summary>
    public static class VendorSpritePackApplier
    {
        private const string PackPath = "Assets/ScriptableObjects/UI/UiSpritePack_Default.asset";
        private const string VendorRoot = "Assets/Art/Vendor";
        private const string GiButtons = VendorRoot + "/GameIconsNet/Buttons";
        private const string KenneyGrey = VendorRoot + "/Kenney/UIPack/Grey";
        private const string KenneyYellow = VendorRoot + "/Kenney/UIPack/Yellow";
        private const string KenneyIcons = VendorRoot + "/Kenney/GameIcons/White2x";

        private static readonly Dictionary<UiButtonId, string> ButtonIconFiles = new()
        {
            { UiButtonId.Shop, "shop.png" },
            { UiButtonId.Back, "back.png" },
            { UiButtonId.Close, "close.png" },
            { UiButtonId.Home, "home.png" },
            { UiButtonId.Adopt, "adopt.png" },
            { UiButtonId.Feed, "feed.png" },
            { UiButtonId.Play, "play.png" },
            { UiButtonId.Clean, "clean.png" },
            { UiButtonId.Settings, "settings.png" },
            { UiButtonId.CollectAll, "coins.png" },
            { UiButtonId.PrevRoom, "prev.png" },
            { UiButtonId.NextRoom, "next.png" },
            { UiButtonId.AcceptWant, "accept.png" },
            { UiButtonId.SnoozeWant, "snooze.png" },
            { UiButtonId.AlwaysOnTop, "pin.png" },
            { UiButtonId.PauseTime, "pause.png" },
            { UiButtonId.Inventory, "inventory.png" },
            { UiButtonId.Renovate, "renovate.png" },
            { UiButtonId.Minigame, "minigame.png" },
        };

        [MenuItem("PolyPets/UI/Apply Vendor Sprite Pack (v1)", priority = 10)]
        public static void ApplyMenu()
        {
            int n = ApplyVendorSprites(showDialog: true);
            Debug.Log($"[PolyPets] Vendor sprite pack applied ({n} button icons).");
        }

        public static int ApplyVendorSprites(bool showDialog = false)
        {
            if (!AssetDatabase.IsValidFolder(VendorRoot))
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Vendor sprites",
                        "Assets/Art/Vendor is missing. Pull the latest branch that includes sprite packs.",
                        "OK");
                }

                return 0;
            }

            AssetDatabase.Refresh();
            ForceSpriteImportUnder(VendorRoot);

            var pack = AssetDatabase.LoadAssetAtPath<UiSpritePack>(PackPath);
            if (pack == null)
                pack = UiPrefabFactory.BuildUiPrefabKit(showDialog: false);

            pack.panelBackground = LoadSprite($"{KenneyGrey}/button_rectangle_depth_gradient.png")
                                   ?? LoadSprite($"{KenneyGrey}/button_rectangle_flat.png");
            pack.buttonBackground = LoadSprite($"{KenneyYellow}/button_rectangle_depth_gradient.png")
                                    ?? LoadSprite($"{KenneyGrey}/button_rectangle_depth_gradient.png");
            pack.buttonBackgroundPressed = LoadSprite($"{KenneyYellow}/button_rectangle_depth_flat.png")
                                           ?? LoadSprite($"{KenneyGrey}/button_rectangle_depth_flat.png");
            pack.buttonBackgroundDisabled = LoadSprite($"{KenneyGrey}/button_rectangle_flat.png");

            // Warm companion chrome (matches greybox living room).
            pack.labelColor = new Color(0.95f, 0.92f, 0.88f, 1f);
            pack.accentColor = new Color(0.95f, 0.82f, 0.45f, 1f);
            pack.dangerColor = new Color(0.85f, 0.32f, 0.28f, 1f);

            var entries = pack.buttons;
            if (entries == null || entries.Length == 0)
                entries = new UiButtonSpriteEntry[ButtonIconFiles.Count];

            var byId = new Dictionary<UiButtonId, UiButtonSpriteEntry>();
            for (int i = 0; i < entries.Length; i++)
                byId[entries[i].id] = entries[i];

            int applied = 0;
            foreach (var pair in ButtonIconFiles)
            {
                var icon = LoadSprite($"{GiButtons}/{pair.Value}");
                if (icon == null)
                    icon = FallbackKenneyIcon(pair.Key);
                if (icon == null)
                    continue;

                byId.TryGetValue(pair.Key, out var entry);
                entry.id = pair.Key;
                entry.icon = icon;
                if (string.IsNullOrWhiteSpace(entry.label))
                    entry.label = pair.Key.ToString();
                byId[pair.Key] = entry;
                applied++;
            }

            var list = new List<UiButtonSpriteEntry>(byId.Values);
            list.Sort((a, b) => a.id.CompareTo(b.id));
            pack.buttons = list.ToArray();

            EditorUtility.SetDirty(pack);
            AssetDatabase.SaveAssets();

            // Refresh any open HUD chrome.
            var hudRoots = Object.FindObjectsByType<UiHudRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < hudRoots.Length; i++)
            {
                hudRoots[i].SetSpritePack(pack);
                EditorUtility.SetDirty(hudRoots[i]);
            }

            if (showDialog)
            {
                Selection.activeObject = pack;
                EditorGUIUtility.PingObject(pack);
                EditorUtility.DisplayDialog(
                    "Vendor Sprite Pack",
                    $"Applied {applied} button icons + Kenney chrome.\n\n" +
                    "Pack: UiSpritePack_Default\n" +
                    "See docs/SPRITE_PACKS.md for mapping + licenses.",
                    "Nice");
            }

            return applied;
        }

        private static Sprite FallbackKenneyIcon(UiButtonId id)
        {
            string name = id switch
            {
                UiButtonId.Shop => "shoppingCart.png",
                UiButtonId.Home => "home.png",
                UiButtonId.Settings => "gear.png",
                UiButtonId.Back => "return.png",
                UiButtonId.Close => "cross.png",
                UiButtonId.AcceptWant => "checkmark.png",
                UiButtonId.PrevRoom => "previous.png",
                UiButtonId.NextRoom => "next.png",
                UiButtonId.PauseTime => "pause.png",
                UiButtonId.Play => "right.png",
                UiButtonId.Minigame => "gamepad.png",
                UiButtonId.CollectAll => "star.png",
                UiButtonId.Inventory => "shoppingBasket.png",
                _ => null,
            };
            return name == null ? null : LoadSprite($"{KenneyIcons}/{name}");
        }

        private static Sprite LoadSprite(string assetPath)
        {
            if (!File.Exists(assetPath))
                return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void ForceSpriteImportUnder(string folder)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            var dirty = false;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    changed = true;
                }

                // 9-slice friendly for Kenney UI rectangles.
                if (path.Contains("/UIPack/") && path.Contains("button_rectangle"))
                {
                    var border = new Vector4(12, 12, 12, 12);
                    if (importer.spriteBorder != border)
                    {
                        importer.spriteBorder = border;
                        changed = true;
                    }
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    dirty = true;
                }
            }

            if (dirty)
                AssetDatabase.Refresh();
        }
    }
}
#endif
