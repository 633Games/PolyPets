#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PolyPets.Camera;
using PolyPets.Core;
using PolyPets.Economy;
using PolyPets.House;
using PolyPets.Minigames;
using PolyPets.Pets;
using PolyPets.Rendering;
using PolyPets.Shop;
using PolyPets.Tutorial;
using PolyPets.UI;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// Builds / rebuilds the soft companion HUD without touching the 3D house or camera.
    /// </summary>
    public static class CozyHudBuilder
    {
        private const string PanelSpritePath = "Assets/Art/UI/Cozy/spr_panel_round.png";
        private const string ChipSpritePath = "Assets/Art/UI/Cozy/spr_chip_round.png";
        private const string ButtonSpritePath = "Assets/Art/UI/Cozy/spr_btn_round.png";
        private const string PillSpritePath = "Assets/Art/UI/Cozy/spr_pill_round.png";
        private const string CircleFillPath = "Assets/Art/UI/Cozy/spr_circle_fill.png";
        private const string CircleTrackPath = "Assets/Art/UI/Cozy/spr_circle_track.png";
        private const string BowlIconPath = "Assets/Art/UI/Cozy/spr_icon_bowl.png";
        private const string MoodIconPath = "Assets/Art/UI/Cozy/spr_icon_mood.png";

        public struct HudBundle
        {
            public HudController hud;
            public CareHudController care;
            public Canvas canvas;
        }

        private const string LivingRoomScenePath = "Assets/Scenes/House_LivingRoom.unity";

        [MenuItem("PolyPets/UI/Apply Cozy Companion HUD", priority = -50)]
        public static void ApplyCozyHudMenu()
        {
            if (!ApplyCozyHudToOpenScene())
            {
                EditorUtility.DisplayDialog(
                    "Cozy HUD",
                    "Open House_LivingRoom (or any scene with === SYSTEMS === / === UI ===) first.",
                    "OK");
                return;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog(
                "Cozy HUD",
                "Soft companion chrome applied.\n\n" +
                "• Parchment chips + tummy/mood meters\n" +
                "• Floating Feed / Play / Shop dock\n" +
                "• Cozy shop + tutorial panels\n\n" +
                "Camera / house left alone.",
                "Nice");
        }

        /// <summary>No dialogs — safe for Cursor bridge / automation.</summary>
        [MenuItem("PolyPets/UI/Apply Cozy Companion HUD (Silent)", priority = -49)]
        public static void ApplyCozyHudSilentMenu()
        {
            if (!ApplyCozyHudToOpenScene())
            {
                Debug.LogWarning("[PolyPets] Silent cozy HUD apply failed (missing scene roots).");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PolyPets] Cozy HUD applied (silent) and scene saved.");
        }

        /// <summary>
        /// Batchmode-safe entry: open Living Room if needed, rebuild cozy HUD, save scene.
        /// No DisplayDialog — safe for -executeMethod / -batchmode.
        /// Usage: Unity.exe -batchmode -quit -projectPath ... -executeMethod PolyPets.EditorTools.CozyHudBuilder.ApplyCozyHudBatch
        /// </summary>
        public static void ApplyCozyHudBatch()
        {
            try
            {
                Debug.Log("[PolyPets] ApplyCozyHudBatch: starting…");

                var scene = EditorSceneManager.GetActiveScene();
                bool needOpen = !scene.IsValid()
                    || string.IsNullOrEmpty(scene.path)
                    || !scene.path.Replace('\\', '/').EndsWith("House_LivingRoom.unity");

                if (needOpen)
                {
                    string absScene = Path.GetFullPath(LivingRoomScenePath);
                    if (!File.Exists(LivingRoomScenePath) && !File.Exists(absScene))
                    {
                        // Also try via dataPath (cwd-independent).
                        absScene = Path.Combine(Application.dataPath, "Scenes", "House_LivingRoom.unity");
                    }

                    if (!File.Exists(LivingRoomScenePath) && !File.Exists(absScene))
                    {
                        Debug.LogError($"[PolyPets] ApplyCozyHudBatch FAILED: scene missing at {LivingRoomScenePath}");
                        EditorApplication.Exit(1);
                        return;
                    }

                    Debug.Log($"[PolyPets] Opening {LivingRoomScenePath}");
                    scene = EditorSceneManager.OpenScene(LivingRoomScenePath, OpenSceneMode.Single);
                }

                if (!ApplyCozyHudToOpenScene())
                {
                    Debug.LogError(
                        "[PolyPets] ApplyCozyHudBatch FAILED: ApplyCozyHudToOpenScene returned false " +
                        "(need === SYSTEMS ===, === UI ===, EconomyService, HouseController).");
                    EditorApplication.Exit(1);
                    return;
                }

                var active = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(active);
                bool saved = EditorSceneManager.SaveScene(active);
                AssetDatabase.SaveAssets();

                if (!saved)
                {
                    Debug.LogError("[PolyPets] ApplyCozyHudBatch FAILED: SaveScene returned false.");
                    EditorApplication.Exit(1);
                    return;
                }

                Debug.Log(
                    "[PolyPets] ApplyCozyHudBatch SUCCESS: cozy HUD + FoodShopPanel applied and scene saved → " +
                    active.path);
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PolyPets] ApplyCozyHudBatch EXCEPTION: {ex}");
                EditorApplication.Exit(1);
            }
        }

        public static bool ApplyCozyHudToOpenScene()
        {
            var systems = GameObject.Find("=== SYSTEMS ===");
            var uiRoot = GameObject.Find("=== UI ===");
            if (systems == null || uiRoot == null)
            {
                Debug.LogWarning("[PolyPets] Cozy HUD: missing === SYSTEMS === or === UI === in open scene.");
                return false;
            }

            var bootstrap = systems.GetComponent<GameBootstrap>();
            var dayNight = systems.GetComponent<DayNightCycle>();
            var economy = systems.GetComponent<EconomyService>();
            var inventory = systems.GetComponent<FoodInventory>();
            var minigames = systems.GetComponent<MinigameRouter>();
            var minigameHud = systems.GetComponent<MinigameHud>();
            var tutorial = systems.GetComponent<StarterTutorial>();
            var house = Object.FindFirstObjectByType<HouseController>();
            var characters = GameObject.Find("=== CHARACTERS ===");

            if (economy == null || house == null)
            {
                Debug.LogWarning("[PolyPets] Cozy HUD: missing EconomyService or HouseController.");
                return false;
            }

            EnsureCozySpritesOnDisk();
            var pack = UiPrefabFactory.BuildUiPrefabKit(showDialog: false);
            VendorSpritePackApplier.ApplyVendorSprites(showDialog: false);
            // Keep Kenney icons/labels, drop loud yellow chrome so dock stays readable
            if (pack != null)
            {
                pack.buttonBackground = null;
                pack.buttonBackgroundPressed = null;
                pack.buttonBackgroundDisabled = null;
                pack.labelColor = CozyUiTheme.Cocoa;
                pack.accentColor = CozyUiTheme.Cocoa;
                EditorUtility.SetDirty(pack);
            }

            // Clear old UI children but keep the root.
            for (int i = uiRoot.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(uiRoot.transform.GetChild(i).gameObject);

            string roomName = house.ActiveRoom != null ? house.ActiveRoom.DisplayName : "Living Room";
            var hud = BuildHud(uiRoot, roomName, dayNight, pack, economy, inventory, minigames, house);
            if (minigameHud != null)
                BuildMinigameOverlay(hud.canvas.transform, minigameHud);

            var shop = BuildFoodShopPanel(hud.canvas.transform, economy, inventory, house);
            PersistCareBindings(hud.care, economy, inventory, minigames, house, hud.hud, shop);

            if (tutorial != null && characters != null && house.ActiveRoom != null)
            {
                var pets = PetCatalogFactory.EnsureStarterPets();
                var palette = MaterialPaletteFactory.EnsurePalette(showDialog: false);
                BuildTutorialPanel(
                    hud.canvas.transform,
                    tutorial,
                    characters.transform,
                    house.ActiveRoom,
                    minigames,
                    pets,
                    palette);
            }

            if (bootstrap != null)
            {
                var so = new SerializedObject(bootstrap);
                so.FindProperty("careHud").objectReferenceValue = hud.care;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            hud.care.RefreshAll();
            if (!Application.isBatchMode)
                Selection.activeGameObject = hud.canvas.gameObject;

            Debug.Log(
                $"[PolyPets] Cozy HUD applied: ActionDock + FoodShopPanel wired " +
                $"(shop={(shop != null)}, care={hud.care != null}).");
            return true;
        }

        public static HudBundle BuildHud(
            GameObject uiRoot,
            string roomName,
            DayNightCycle dayNight,
            UiSpritePack spritePack,
            EconomyService economy,
            FoodInventory inventory,
            MinigameRouter minigames,
            HouseController house)
        {
            EnsureCozySpritesOnDisk();

            var eventSystem = CreateChild(uiRoot, "EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            var canvasGo = CreateChild(uiRoot, "HUD_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // balanced for portrait desk window
            canvasGo.AddComponent<GraphicRaycaster>();

            var hudRoot = canvasGo.AddComponent<UiHudRoot>();
            var hudRootSo = new SerializedObject(hudRoot);
            hudRootSo.FindProperty("spritePack").objectReferenceValue = spritePack;
            hudRootSo.FindProperty("buttonRoot").objectReferenceValue = canvasGo.transform;
            hudRootSo.ApplyModifiedPropertiesWithoutUndo();

            var hud = canvasGo.AddComponent<HudController>();
            var care = canvasGo.AddComponent<CareHudController>();

            Sprite panel = LoadSprite(PanelSpritePath);
            Sprite pill = LoadSprite(PillSpritePath);
            Sprite circleFill = LoadSprite(CircleFillPath);
            Sprite circleTrack = LoadSprite(CircleTrackPath);
            Sprite bowlIcon = LoadSprite(BowlIconPath);
            Sprite moodIcon = LoadSprite(MoodIconPath);

            // Top-left coin chip — roomy for fat-finger desk play
            var coinChip = CreatePanel(canvasGo.transform, "CoinChip", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -14f), new Vector2(132f, 48f), panel, CozyUiTheme.Parchment);
            coinChip.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
            var coinLabel = CreateText(coinChip.transform, "CoinText", "✦ 0",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 20, CozyUiTheme.Cocoa);
            Stretch(coinLabel.rectTransform, 10f);

            // Top-right room / clock chip
            var roomChip = CreatePanel(canvasGo.transform, "RoomChip", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-14f, -14f), new Vector2(188f, 56f), panel, CozyUiTheme.Parchment);
            roomChip.GetComponent<RectTransform>().pivot = new Vector2(1f, 1f);
            var roomLabel = CreateText(roomChip.transform, "RoomText", roomName,
                new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(168f, 24f),
                TextAnchor.MiddleCenter, 16, CozyUiTheme.Cocoa);
            var clockLabel = CreateText(roomChip.transform, "ClockText", "08:24 · daylight",
                new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(168f, 20f),
                TextAnchor.MiddleCenter, 13, CozyUiTheme.CocoaMuted);

            // Needs card: stock → meters → status, evenly spaced
            var needsCard = CreatePanel(canvasGo.transform, "NeedsCard", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -78f), new Vector2(248f, 128f), panel, CozyUiTheme.CreamChip);
            needsCard.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            var foodLabel = CreateText(needsCard.transform, "FoodStockText", "Treats ×0",
                new Vector2(0.5f, 0.90f), new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(220f, 20f),
                TextAnchor.MiddleCenter, 13, CozyUiTheme.CocoaMuted);
            var hungerMeter = BuildRadialMeter(
                needsCard.transform, "HungerMeter", CozyUiTheme.Honey,
                new Vector2(0.30f, 0.48f), circleTrack, circleFill, bowlIcon);
            var happyMeter = BuildRadialMeter(
                needsCard.transform, "HappyMeter", CozyUiTheme.Blush,
                new Vector2(0.70f, 0.48f), circleTrack, circleFill, moodIcon);
            var statusLabel = CreateText(needsCard.transform, "StatusText", "Settling in…",
                new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(220f, 22f),
                TextAnchor.MiddleCenter, 14, CozyUiTheme.CocoaSoft);

            // Floating action dock — larger hit targets, even spacing
            var dock = CreatePanel(canvasGo.transform, "ActionDock", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 22f), new Vector2(392f, 110f), panel, CozyUiTheme.Parchment);
            dock.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);

            var feedBtn = CreateDockButton(dock.transform, UiButtonId.Feed, spritePack, pill, new Vector2(0.18f, 0.52f));
            var playBtn = CreateDockButton(dock.transform, UiButtonId.Play, spritePack, pill, new Vector2(0.50f, 0.52f));
            var shopBtn = CreateDockButton(dock.transform, UiButtonId.Shop, spritePack, pill, new Vector2(0.82f, 0.52f));

            var so = new SerializedObject(hud);
            so.FindProperty("coinText").objectReferenceValue = coinLabel;
            so.FindProperty("roomText").objectReferenceValue = roomLabel;
            so.FindProperty("clockText").objectReferenceValue = clockLabel;
            so.FindProperty("dayNight").objectReferenceValue = dayNight;
            so.ApplyModifiedPropertiesWithoutUndo();

            care.BindMeters(null, null, statusLabel, foodLabel);
            care.BindNeedMeters(hungerMeter, happyMeter);
            care.BindActionButtons(feedBtn, shopBtn, null, playBtn);
            PersistDockButtons(care, feedBtn, shopBtn, playBtn);
            PersistNeedMeters(care, hungerMeter, happyMeter, statusLabel, foodLabel);

            hud.SetCoins(economy != null ? economy.Coins : 0);
            hud.SetRoomName(roomName);
            hud.BindDayNight(dayNight);
            hudRoot.RefreshButtons();
            care.RefreshAll();

            return new HudBundle { hud = hud, care = care, canvas = canvas };
        }

        public static FoodShopPanel BuildFoodShopPanel(
            Transform canvas,
            EconomyService economy,
            FoodInventory inventory,
            HouseController house)
        {
            EnsureCozySpritesOnDisk();
            Sprite panelSprite = LoadSprite(PanelSpritePath);

            var shopGo = new GameObject("FoodShop", typeof(RectTransform));
            shopGo.transform.SetParent(canvas, false);
            Stretch(shopGo.GetComponent<RectTransform>(), 0f);
            var shop = shopGo.AddComponent<FoodShopPanel>();

            var dim = CreatePanel(shopGo.transform, "Dimmer", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, null, CozyUiTheme.OverlayDim);
            Stretch(dim.GetComponent<RectTransform>(), 0f);
            dim.GetComponent<Image>().raycastTarget = true;
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;

            var panel = CreatePanel(shopGo.transform, "ShopPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400f, 460f), panelSprite, CozyUiTheme.ParchmentSolid);

            var title = CreateText(panel.transform, "Title", "Little pantry",
                new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(340f, 34f),
                TextAnchor.MiddleCenter, 24, CozyUiTheme.Cocoa);
            var body = CreateText(panel.transform, "Body", "Pick a treat",
                new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(340f, 48f),
                TextAnchor.UpperCenter, 15, CozyUiTheme.CocoaSoft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;

            var buttonRoot = new GameObject("Rows", typeof(RectTransform));
            buttonRoot.transform.SetParent(panel.transform, false);
            var rowsRt = buttonRoot.GetComponent<RectTransform>();
            rowsRt.anchorMin = new Vector2(0f, 0.18f);
            rowsRt.anchorMax = new Vector2(1f, 0.78f);
            rowsRt.offsetMin = Vector2.zero;
            rowsRt.offsetMax = Vector2.zero;

            var close = CreateSoftButton(panel.transform, "Close", "All set", new Vector2(0.5f, 0.08f), new Vector2(160f, 48f));
            shop.Bind(shopGo, title, body, buttonRoot.transform, close, inventory, economy, house, dimBtn);
            var shopSo = new SerializedObject(shop);
            shopSo.FindProperty("root").objectReferenceValue = shopGo;
            shopSo.FindProperty("titleText").objectReferenceValue = title;
            shopSo.FindProperty("bodyText").objectReferenceValue = body;
            shopSo.FindProperty("buttonRoot").objectReferenceValue = buttonRoot.transform;
            shopSo.FindProperty("closeButton").objectReferenceValue = close;
            shopSo.FindProperty("dimmerButton").objectReferenceValue = dimBtn;
            shopSo.FindProperty("inventory").objectReferenceValue = inventory;
            shopSo.FindProperty("economy").objectReferenceValue = economy;
            shopSo.FindProperty("house").objectReferenceValue = house;
            shopSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(shop);
            shopGo.SetActive(false);

            return shop;
        }

        private static void PersistCareBindings(
            CareHudController care,
            EconomyService economy,
            FoodInventory inventory,
            MinigameRouter minigames,
            HouseController house,
            HudController hud,
            FoodShopPanel shop)
        {
            care.Bind(economy, inventory, minigames, house, hud, shop);
            var so = new SerializedObject(care);
            so.FindProperty("economy").objectReferenceValue = economy;
            so.FindProperty("inventory").objectReferenceValue = inventory;
            so.FindProperty("minigames").objectReferenceValue = minigames;
            so.FindProperty("house").objectReferenceValue = house;
            so.FindProperty("hud").objectReferenceValue = hud;
            so.FindProperty("shopPanel").objectReferenceValue = shop;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(care);
        }

        private static void PersistDockButtons(
            CareHudController care,
            UiChromeButton feed,
            UiChromeButton shop,
            UiChromeButton play)
        {
            var so = new SerializedObject(care);
            so.FindProperty("feedButton").objectReferenceValue = feed;
            so.FindProperty("shopButton").objectReferenceValue = shop;
            so.FindProperty("playButton").objectReferenceValue = play;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(care);
        }

        private static void PersistNeedMeters(CareHudController care, NeedMeterView hunger, NeedMeterView happy, Text status, Text food)
        {
            care.BindMeters(null, null, status, food);
            care.BindNeedMeters(hunger, happy);
            var so = new SerializedObject(care);
            so.FindProperty("hungerMeter").objectReferenceValue = hunger;
            so.FindProperty("happinessMeter").objectReferenceValue = happy;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.FindProperty("foodStockText").objectReferenceValue = food;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(care);
        }

        public static void BuildMinigameOverlay(Transform canvas, MinigameHud hud)
        {
            EnsureCozySpritesOnDisk();
            Sprite panel = LoadSprite(PanelSpritePath);

            var root = CreatePanel(canvas, "MinigameOverlay", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(440f, 520f), panel, CozyUiTheme.ParchmentSolid);
            root.SetActive(false);

            var prompt = CreateText(root.transform, "Prompt", "Let's play",
                new Vector2(0.5f, 0.94f), new Vector2(0.5f, 0.94f), Vector2.zero, new Vector2(400f, 40f),
                TextAnchor.MiddleCenter, 16, CozyUiTheme.Cocoa);
            prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
            prompt.verticalOverflow = VerticalWrapMode.Overflow;

            var score = CreateText(root.transform, "Score", "✦ 0",
                new Vector2(0.5f, 0.02f), new Vector2(0.5f, 0.02f), Vector2.zero, new Vector2(340f, 22f),
                TextAnchor.MiddleCenter, 15, CozyUiTheme.Amber);

            hud.Bind(prompt, score, root);
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("promptText").objectReferenceValue = prompt;
            hudSo.FindProperty("scoreText").objectReferenceValue = score;
            hudSo.FindProperty("root").objectReferenceValue = root;
            hudSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
        }

        public static void BuildTutorialPanel(
            Transform canvas,
            StarterTutorial tutorial,
            Transform charactersRoot,
            RoomRoot room,
            MinigameRouter router,
            (PetDefinition cat, PetDefinition dog, PetDefinition rabbit) pets,
            MaterialPalette palette)
        {
            EnsureCozySpritesOnDisk();
            Sprite panel = LoadSprite(PanelSpritePath);
            Sprite pill = LoadSprite(PillSpritePath);

            var root = CreatePanel(canvas, "TutorialPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400f, 420f), panel, CozyUiTheme.ParchmentSolid);

            var title = CreateText(root.transform, "Title", "Welcome home",
                new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(340f, 40f),
                TextAnchor.MiddleCenter, 26, CozyUiTheme.Cocoa);

            var body = CreateText(root.transform, "Body",
                "A tiny desk window for a box-headed pal.\nName them, pick a friend, then keep them cozy.",
                new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(340f, 110f),
                TextAnchor.UpperCenter, 16, CozyUiTheme.CocoaSoft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;

            var inputGo = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputGo.transform.SetParent(root.transform, false);
            var inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = inputRt.anchorMax = new Vector2(0.5f, 0.44f);
            inputRt.sizeDelta = new Vector2(280f, 44f);
            var inputImg = inputGo.GetComponent<Image>();
            inputImg.sprite = pill;
            inputImg.type = Image.Type.Sliced;
            inputImg.color = CozyUiTheme.InputWell;
            var input = inputGo.GetComponent<InputField>();

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(inputGo.transform, false);
            Stretch(placeholderGo.GetComponent<RectTransform>(), 12f);
            var placeholder = placeholderGo.GetComponent<Text>();
            placeholder.text = "What should we call them?";
            placeholder.color = CozyUiTheme.CocoaMuted;
            placeholder.font = CozyUiTheme.UiFont;
            placeholder.fontSize = 15;

            var inputTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            inputTextGo.transform.SetParent(inputGo.transform, false);
            Stretch(inputTextGo.GetComponent<RectTransform>(), 12f);
            var inputText = inputTextGo.GetComponent<Text>();
            inputText.font = CozyUiTheme.UiFont;
            inputText.fontSize = 16;
            inputText.color = CozyUiTheme.Cocoa;
            inputText.supportRichText = false;

            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.text = "Mochi";
            inputGo.SetActive(false);

            var next = CreateSoftButton(root.transform, "NextButton", "Let's settle in", new Vector2(0.5f, 0.16f), new Vector2(200f, 48f));
            var cat = CreateSoftButton(root.transform, "Btn_Cat", "Cat\nFishing", new Vector2(0.2f, 0.32f), new Vector2(120f, 84f));
            var dog = CreateSoftButton(root.transform, "Btn_Dog", "Dog\nDig + Snap", new Vector2(0.5f, 0.32f), new Vector2(120f, 84f));
            var rabbit = CreateSoftButton(root.transform, "Btn_Rabbit", "Rabbit\nCarrots", new Vector2(0.8f, 0.32f), new Vector2(120f, 84f));
            cat.gameObject.SetActive(false);
            dog.gameObject.SetActive(false);
            rabbit.gameObject.SetActive(false);

            var nextLabel = next.GetComponentInChildren<Text>();
            var needs = canvas.Find("NeedsCard")?.gameObject;
            var dock = canvas.Find("ActionDock")?.gameObject;
            tutorial.BindUi(root, title, body, input, next, nextLabel, cat, dog, rabbit, needs, dock);
            tutorial.BindWorld(charactersRoot, room, router, pets.cat, pets.dog, pets.rabbit, palette);
            root.transform.SetAsLastSibling();
        }

        private static NeedMeterView BuildRadialMeter(
            Transform parent,
            string name,
            Color fillColor,
            Vector2 anchor,
            Sprite trackSprite,
            Sprite fillSprite,
            Sprite iconSprite)
        {
            const float pieSize = 76f;

            var go = new GameObject(name, typeof(RectTransform), typeof(NeedMeterView));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(pieSize, pieSize);

            var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(go.transform, false);
            Stretch(track.GetComponent<RectTransform>(), 0f);
            var trackImg = track.GetComponent<Image>();
            trackImg.sprite = trackSprite;
            trackImg.type = Image.Type.Simple;
            trackImg.preserveAspect = true;
            trackImg.color = CozyUiTheme.MeterTrack;
            trackImg.raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(go.transform, false);
            Stretch(fill.GetComponent<RectTransform>(), 3f);
            var fillImg = fill.GetComponent<Image>();
            fillImg.sprite = fillSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Radial360;
            fillImg.fillOrigin = (int)Image.Origin360.Top;
            fillImg.fillClockwise = true;
            fillImg.preserveAspect = true;
            fillImg.color = fillColor;
            fillImg.fillAmount = 0.7f;
            fillImg.raycastTarget = false;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(go.transform, false);
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(32f, 32f);
            var iconImg = icon.GetComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.type = Image.Type.Simple;
            iconImg.preserveAspect = true;
            iconImg.color = CozyUiTheme.CocoaSoft;
            iconImg.raycastTarget = false;

            var meter = go.GetComponent<NeedMeterView>();
            meter.Bind(trackImg, fillImg, iconImg, fillColor);
            meter.Set(70f);
            return meter;
        }

        private static UiChromeButton CreateDockButton(Transform parent, UiButtonId id, UiSpritePack pack, Sprite pill, Vector2 anchor)
        {
            var go = new GameObject($"Btn_{id}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UiChromeButton));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(112f, 84f);

            var bg = go.GetComponent<Image>();
            bg.sprite = pill;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.66f);
            iconRt.sizeDelta = new Vector2(36f, 36f);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = pack != null ? pack.GetIcon(id) : null;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = CozyUiTheme.Cocoa;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.06f, 0.04f);
            labelRt.anchorMax = new Vector2(0.94f, 0.42f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = pack != null ? pack.GetLabel(id) : id.ToString();
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.color = CozyUiTheme.Cocoa;
            label.font = CozyUiTheme.UiFont;
            label.raycastTarget = false;

            var chrome = go.GetComponent<UiChromeButton>();
            var so = new SerializedObject(chrome);
            so.FindProperty("buttonId").enumValueIndex = (int)id;
            so.FindProperty("spritePack").objectReferenceValue = pack;
            so.FindProperty("background").objectReferenceValue = bg;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("applyFeelPop").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            chrome.ApplyPack();
            // High-contrast cozy dock: solid parchment + cocoa ink (ignore loud vendor chrome)
            bg.sprite = pill;
            bg.type = Image.Type.Sliced;
            bg.color = CozyUiTheme.ParchmentSolid;
            if (label != null)
            {
                label.color = CozyUiTheme.Cocoa;
                label.fontSize = 18;
                label.fontStyle = FontStyle.Bold;
            }
            if (icon != null)
                icon.color = CozyUiTheme.Cocoa;

            return chrome;
        }

        private static Button CreateSoftButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size)
        {
            EnsureCozySpritesOnDisk();
            Sprite pill = LoadSprite(PillSpritePath);

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = pill;
            img.type = Image.Type.Sliced;
            img.color = CozyUiTheme.CreamChip;
            img.raycastTarget = true;

            var text = CreateText(go.transform, "Label", label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, size - new Vector2(12f, 10f), TextAnchor.MiddleCenter, 16, CozyUiTheme.Cocoa);
            text.raycastTarget = false;
            return go.GetComponent<Button>();
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 aMin, Vector2 aMax,
            Vector2 pos, Vector2 size, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            img.color = color;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string content, Vector2 aMin, Vector2 aMax,
            Vector2 pos, Vector2 size, TextAnchor align, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.text = content;
            text.alignment = align;
            text.fontSize = fontSize;
            text.color = color;
            text.font = CozyUiTheme.UiFont;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void Stretch(RectTransform rt, float pad)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        public static void EnsureCozySpritesOnDisk()
        {
            CreateFolder("Assets", "Art");
            CreateFolder("Assets/Art", "UI");
            CreateFolder("Assets/Art/UI", "Cozy");

            // Larger source + soft borders so 9-slice stays friendly when stretched
            WriteRoundedPng(PanelSpritePath, 128, 24, CozyUiTheme.ParchmentSolid, CozyUiTheme.BorderSoft, 2, force: true);
            WriteRoundedPng(ChipSpritePath, 96, 20, Color.white, CozyUiTheme.BorderSoft, 2, force: true);
            WriteRoundedPng(ButtonSpritePath, 96, 20, Color.white, new Color(0.82f, 0.70f, 0.52f, 0.55f), 2, force: true);
            WriteRoundedPng(PillSpritePath, 128, 28, Color.white, new Color(0.84f, 0.72f, 0.56f, 0.50f), 2, force: true);

            WriteGeneratedSprite(CircleFillPath, () => CozyUiTheme.CreateCircleSprite(128, Color.white), sliced: false, force: true);
            WriteGeneratedSprite(CircleTrackPath, () => CozyUiTheme.CreateCircleSprite(128, CozyUiTheme.MeterTrack), sliced: false, force: true);
            WriteGeneratedSprite(BowlIconPath, () => CozyUiTheme.CreateBowlIcon(128, Color.white), sliced: false, force: true);
            WriteGeneratedSprite(MoodIconPath, () => CozyUiTheme.CreateMoodIcon(128, Color.white), sliced: false, force: true);
        }

        private static void WriteRoundedPng(string path, int size, int radius, Color fill, Color border, int borderWidth, bool force = false)
        {
            string absPath = ToAbsPath(path);
            if (!force && File.Exists(absPath) && AssetDatabase.LoadAssetAtPath<Sprite>(path) != null)
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(absPath)!);

            var sprite = CozyUiTheme.CreateRoundedSprite(size, radius, fill, border, borderWidth);
            File.WriteAllBytes(absPath, sprite.texture.EncodeToPNG());
            Object.DestroyImmediate(sprite.texture);
            ImportSprite(path, sliced: true, border: radius);
        }

        private static void WriteGeneratedSprite(string path, System.Func<Sprite> factory, bool sliced, bool force = false)
        {
            string absPath = ToAbsPath(path);
            if (!force && File.Exists(absPath) && AssetDatabase.LoadAssetAtPath<Sprite>(path) != null)
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(absPath)!);
            var sprite = factory();
            File.WriteAllBytes(absPath, sprite.texture.EncodeToPNG());
            Object.DestroyImmediate(sprite.texture);
            ImportSprite(path, sliced, border: 0);
        }

        private static string ToAbsPath(string path) =>
            path.StartsWith("Assets/", System.StringComparison.Ordinal)
                ? Path.Combine(Application.dataPath, path.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar))
                : path;

        private static void ImportSprite(string path, bool sliced, int border)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.spriteBorder = sliced
                ? new Vector4(border, border, border, border)
                : Vector4.zero;
            importer.SaveAndReimport();
        }

        private static void CreateFolder(string parent, string name)
        {
            var path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif


