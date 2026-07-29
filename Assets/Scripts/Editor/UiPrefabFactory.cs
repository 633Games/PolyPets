#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using PolyPets.Feel;
using PolyPets.UI;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// Builds the reusable UI prefab kit + default sprite pack (chrome sprites + vendor icons).
    /// Swap sprites on the UiSpritePack asset — prefabs pick them up automatically.
    /// </summary>
    public static class UiPrefabFactory
    {
        private const string RootMenu = "PolyPets/UI/";
        private const string PackPath = "Assets/ScriptableObjects/UI/UiSpritePack_Default.asset";
        private const string PrefabFolder = "Assets/Prefabs/UI/Buttons";
        private const string ChromeFolder = "Assets/Art/UI/Chrome";

        private static readonly UiButtonId[] AllButtons =
        {
            UiButtonId.Shop,
            UiButtonId.Back,
            UiButtonId.Close,
            UiButtonId.Home,
            UiButtonId.Adopt,
            UiButtonId.Feed,
            UiButtonId.Play,
            UiButtonId.Clean,
            UiButtonId.Settings,
            UiButtonId.CollectAll,
            UiButtonId.PrevRoom,
            UiButtonId.NextRoom,
            UiButtonId.AcceptWant,
            UiButtonId.SnoozeWant,
            UiButtonId.AlwaysOnTop,
            UiButtonId.PauseTime,
            UiButtonId.Inventory,
            UiButtonId.Renovate,
            UiButtonId.Minigame,
        };

        [MenuItem(RootMenu + "Build UI Prefab Kit + Sprite Pack", priority = 0)]
        public static void BuildUiPrefabKitMenu()
        {
            BuildUiPrefabKit(showDialog: true);
        }

        public static UiSpritePack BuildUiPrefabKit(bool showDialog = false)
        {
            EnsureFolders();
            var pack = CreateOrUpdateSpritePack();
            int built = 0;

            foreach (var id in AllButtons)
            {
                CreateButtonPrefab(id, pack);
                built++;
            }

            CreatePanelPrefab(pack);
            CreateWantPromptPrefab(pack);
            CreateBottomBarPrefab(pack);
            CreateTopBarPrefab(pack);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorGUIUtility.PingObject(pack);
                Debug.Log($"[PolyPets] Built {built} button prefabs + chrome panels.\nPack: {PackPath}\nFolder: {PrefabFolder}");
                EditorUtility.DisplayDialog(
                    "UI Prefab Kit",
                    $"Created/updated {built} button prefabs.\n\n" +
                    "1. Open UiSpritePack_Default\n" +
                    "2. Drop your sprite pack icons into each slot\n" +
                    "3. Prefabs refresh icons/labels from the pack\n\n" +
                    $"Folder: {PrefabFolder}",
                    "Nice");
            }

            return pack;
        }

        [MenuItem(RootMenu + "Select Sprite Pack", priority = 1)]
        public static void SelectSpritePack()
        {
            var pack = AssetDatabase.LoadAssetAtPath<UiSpritePack>(PackPath);
            if (pack == null)
            {
                BuildUiPrefabKit(showDialog: true);
                pack = AssetDatabase.LoadAssetAtPath<UiSpritePack>(PackPath);
            }

            Selection.activeObject = pack;
            EditorGUIUtility.PingObject(pack);
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets", "Art");
            CreateFolder("Assets/Art", "UI");
            CreateFolder("Assets/Art/UI", "Chrome");
            CreateFolder("Assets/Art/UI", "Brand");
            CreateFolder("Assets", "ScriptableObjects");
            CreateFolder("Assets/ScriptableObjects", "UI");
            CreateFolder("Assets", "Prefabs");
            CreateFolder("Assets/Prefabs", "UI");
            CreateFolder("Assets/Prefabs/UI", "Buttons");
            CreateFolder("Assets/Prefabs/UI", "Chrome");
        }

        private static void CreateFolder(string parent, string name)
        {
            var path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static UiSpritePack CreateOrUpdateSpritePack()
        {
            var pack = AssetDatabase.LoadAssetAtPath<UiSpritePack>(PackPath);
            if (pack == null)
            {
                pack = ScriptableObject.CreateInstance<UiSpritePack>();
                AssetDatabase.CreateAsset(pack, PackPath);
            }

            pack.panelBackground = GetOrCreateChromeSprite("spr_panel", new Color(0.12f, 0.1f, 0.09f, 0.92f), 64, 64);
            pack.buttonBackground = GetOrCreateChromeSprite("spr_btn", new Color(0.22f, 0.18f, 0.16f, 1f), 64, 64);
            pack.buttonBackgroundPressed = GetOrCreateChromeSprite("spr_btn_pressed", new Color(0.16f, 0.13f, 0.12f, 1f), 64, 64);
            pack.buttonBackgroundDisabled = GetOrCreateChromeSprite("spr_btn_disabled", new Color(0.2f, 0.2f, 0.2f, 0.5f), 64, 64);

            // Ensure entries exist for every button id with a chrome icon.
            var entries = new UiButtonSpriteEntry[AllButtons.Length];
            for (int i = 0; i < AllButtons.Length; i++)
            {
                var id = AllButtons[i];
                string label = pack.GetLabel(id);
                if (pack.TryGet(id, out var existing) && !string.IsNullOrWhiteSpace(existing.label))
                    label = existing.label;

                entries[i] = new UiButtonSpriteEntry
                {
                    id = id,
                    label = string.IsNullOrWhiteSpace(label) ? id.ToString() : label,
                    icon = GetOrCreateChromeSprite($"spr_icon_{id}", AccentFor(id), 64, 64),
                };
            }

            pack.buttons = entries;
            EditorUtility.SetDirty(pack);
            return pack;
        }

        private static Color AccentFor(UiButtonId id) => id switch
        {
            UiButtonId.Close => new Color(0.75f, 0.3f, 0.28f),
            UiButtonId.AcceptWant => new Color(0.35f, 0.7f, 0.4f),
            UiButtonId.SnoozeWant => new Color(0.55f, 0.5f, 0.45f),
            UiButtonId.Shop => new Color(0.92f, 0.7f, 0.3f),
            UiButtonId.Home => new Color(0.55f, 0.75f, 0.9f),
            _ => new Color(0.9f, 0.6f, 0.35f),
        };

        private static Sprite GetOrCreateChromeSprite(string name, Color color, int w, int h)
        {
            var path = $"{ChromeFolder}/{name}.png";
            if (File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (existing != null)
                    return existing;
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            // Simple border
            for (int x = 0; x < w; x++)
            {
                pixels[x] = Color.white;
                pixels[(h - 1) * w + x] = Color.white;
            }

            for (int y = 0; y < h; y++)
            {
                pixels[y * w] = Color.white;
                pixels[y * w + (w - 1)] = Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void CreateButtonPrefab(UiButtonId id, UiSpritePack pack)
        {
            var path = $"{PrefabFolder}/Btn_{id}.prefab";
            var root = new GameObject($"Btn_{id}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UiChromeButton));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120f, 48f);

            var bg = root.GetComponent<Image>();
            bg.sprite = pack.buttonBackground;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            var button = root.GetComponent<Button>();
            button.targetGraphic = bg;

            // Icon
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(root.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(24f, 0f);
            iconRt.sizeDelta = new Vector2(28f, 28f);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = pack.GetIcon(id);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // Label
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(root.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = new Vector2(44f, 4f);
            labelRt.offsetMax = new Vector2(-8f, -4f);
            var label = labelGo.GetComponent<Text>();
            label.text = pack.GetLabel(id);
            label.alignment = TextAnchor.MiddleLeft;
            label.fontSize = 16;
            label.color = pack.labelColor;
            label.raycastTarget = false;
            label.font = UiFonts.Body;

            var chrome = root.GetComponent<UiChromeButton>();
            var so = new SerializedObject(chrome);
            so.FindProperty("buttonId").enumValueIndex = (int)id;
            so.FindProperty("spritePack").objectReferenceValue = pack;
            so.FindProperty("background").objectReferenceValue = bg;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("useDangerColor").boolValue = id == UiButtonId.Close;
            so.FindProperty("applyFeelPop").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            chrome.ApplyPack();
            FeelTagBinder.EnsureTagOn(root, FeelTagType.Punch, autoPlay: false);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreatePanelPrefab(UiSpritePack pack)
        {
            var path = "Assets/Prefabs/UI/Chrome/Panel_Modal.prefab";
            var root = new GameObject("Panel_Modal", typeof(RectTransform), typeof(Image));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 420f);
            var img = root.GetComponent<Image>();
            img.sprite = pack.panelBackground;
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.96f);

            // Title
            var titleGo = CreateText(root.transform, "Title", "Title", new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(320f, 36f), 22, TextAnchor.MiddleCenter, pack.labelColor);

            // Close button instance (nested reference via instantiate prefab if exists)
            var closePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Btn_Close.prefab");
            if (closePrefab != null)
            {
                var close = (GameObject)PrefabUtility.InstantiatePrefab(closePrefab);
                close.transform.SetParent(root.transform, false);
                var closeRt = close.GetComponent<RectTransform>();
                closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 1f);
                closeRt.anchoredPosition = new Vector2(-28f, -28f);
                closeRt.sizeDelta = new Vector2(56f, 40f);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            _ = titleGo;
        }

        private static void CreateWantPromptPrefab(UiSpritePack pack)
        {
            var path = "Assets/Prefabs/UI/Chrome/Panel_WantPrompt.prefab";
            var root = new GameObject("Panel_WantPrompt", typeof(RectTransform), typeof(Image));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 140f);
            var img = root.GetComponent<Image>();
            img.sprite = pack.panelBackground;
            img.type = Image.Type.Sliced;

            CreateText(root.transform, "WantText", "Mochi wants to go fishing…", new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(360f, 40f), 16, TextAnchor.MiddleCenter, pack.labelColor);

            PlaceButtonPrefab(root.transform, UiButtonId.AcceptWant, new Vector2(0.32f, 0.25f), new Vector2(140f, 44f));
            PlaceButtonPrefab(root.transform, UiButtonId.SnoozeWant, new Vector2(0.68f, 0.25f), new Vector2(140f, 44f));

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreateBottomBarPrefab(UiSpritePack pack)
        {
            var path = "Assets/Prefabs/UI/Chrome/Bar_BottomActions.prefab";
            var root = new GameObject("Bar_BottomActions", typeof(RectTransform), typeof(Image), typeof(UiHudRoot));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(480f, 72f);
            var img = root.GetComponent<Image>();
            img.sprite = pack.panelBackground;
            img.color = new Color(1f, 1f, 1f, 0.85f);

            float[] xs = { 0.10f, 0.28f, 0.46f, 0.64f, 0.82f };
            UiButtonId[] ids = { UiButtonId.Feed, UiButtonId.Play, UiButtonId.Clean, UiButtonId.Shop, UiButtonId.Home };
            for (int i = 0; i < ids.Length; i++)
                PlaceButtonPrefab(root.transform, ids[i], new Vector2(xs[i], 0.5f), new Vector2(84f, 48f));

            var hud = root.GetComponent<UiHudRoot>();
            var so = new SerializedObject(hud);
            so.FindProperty("spritePack").objectReferenceValue = pack;
            so.FindProperty("buttonRoot").objectReferenceValue = root.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreateTopBarPrefab(UiSpritePack pack)
        {
            var path = "Assets/Prefabs/UI/Chrome/Bar_TopChrome.prefab";
            var root = new GameObject("Bar_TopChrome", typeof(RectTransform), typeof(Image), typeof(UiHudRoot));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(480f, 64f);
            root.GetComponent<Image>().sprite = pack.panelBackground;

            CreateText(root.transform, "CoinText", "12", new Vector2(0.12f, 0.55f), Vector2.zero, new Vector2(80f, 28f), 20, TextAnchor.MiddleLeft, pack.labelColor);
            CreateText(root.transform, "RoomText", "Living Room", new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(160f, 28f), 16, TextAnchor.MiddleCenter, pack.labelColor);
            CreateText(root.transform, "ClockText", "08:24  Day", new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(160f, 22f), 13, TextAnchor.MiddleCenter, pack.labelColor);

            PlaceButtonPrefab(root.transform, UiButtonId.Settings, new Vector2(0.82f, 0.5f), new Vector2(64f, 40f));
            PlaceButtonPrefab(root.transform, UiButtonId.AlwaysOnTop, new Vector2(0.94f, 0.5f), new Vector2(48f, 40f));
            PlaceButtonPrefab(root.transform, UiButtonId.PrevRoom, new Vector2(0.28f, 0.5f), new Vector2(48f, 40f));
            PlaceButtonPrefab(root.transform, UiButtonId.NextRoom, new Vector2(0.72f, 0.5f), new Vector2(48f, 40f));

            var hud = root.GetComponent<UiHudRoot>();
            var so = new SerializedObject(hud);
            so.FindProperty("spritePack").objectReferenceValue = pack;
            so.FindProperty("buttonRoot").objectReferenceValue = root.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void PlaceButtonPrefab(Transform parent, UiButtonId id, Vector2 anchor, Vector2 size)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Btn_{id}.prefab");
            GameObject instance;
            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(parent, false);
            }
            else
            {
                // Fallback inline if order means prefab not ready
                instance = new GameObject($"Btn_{id}", typeof(RectTransform), typeof(Image), typeof(Button));
                instance.transform.SetParent(parent, false);
            }

            var rt = instance.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }

        private static Text CreateText(Transform parent, string name, string content, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = color;
            text.raycastTarget = false;
            text.font = UiFonts.Body;
            return text;
        }
    }
}
#endif
