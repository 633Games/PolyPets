#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using PolyPets.Camera;
using PolyPets.Core;
using PolyPets.Desktop;
using PolyPets.Economy;
using PolyPets.Feel;
using PolyPets.House;
using PolyPets.Minigames;
using PolyPets.Needs;
using PolyPets.Pets;
using PolyPets.Rendering;
using PolyPets.Shop;
using PolyPets.Tutorial;
using PolyPets.UI;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// One-click scaffold for the PolyPets vertical slice on Unity 6.3 (URP):
    /// cel-shaded room, box-headed cat, framed camera, post-processing, day/night.
    /// Menu: PolyPets → Bootstrap Starter House Scene
    /// </summary>
    public static class PolyPetsSceneBootstrap
    {
        private const string RootMenu = "PolyPets/";
        private const string ScenePath = "Assets/Scenes/House_LivingRoom.unity";
        private const string PetDefPath = "Assets/ScriptableObjects/Pets/PetDefinition_Cat.asset";
        private const string MaterialsFolder = "Assets/Materials";
        private const string VolumeProfilePath = PostProcessFactory.DefaultProfilePath;

        private static readonly Vector3 RoomSize = new(6f, 3f, 6f);

        [MenuItem(RootMenu + "Bootstrap Starter House Scene", priority = 0)]
        public static void BootstrapStarterHouseScene()
        {
            EnsureFolders();
            PolyPetsUrpSetup.EnsureUrpPipelineAssets();
            var palette = MaterialPaletteFactory.EnsurePalette(showDialog: false);
            var spritePack = UiPrefabFactory.BuildUiPrefabKit(showDialog: false);
            VendorSpritePackApplier.ApplyVendorSprites(showDialog: false);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "House_LivingRoom";

            var materials = CreateMaterialKit(palette);
            var pets = PetCatalogFactory.EnsureStarterPets();
            var volumeProfile = CreateOrLoadVolumeProfile();
            var foods = FoodCatalogFactory.EnsureDefaultFoods();

            var systems = CreateRoot("=== SYSTEMS ===");
            var environment = CreateRoot("=== ENVIRONMENT ===");
            var characters = CreateRoot("=== CHARACTERS ===");
            var lighting = CreateRoot("=== LIGHTING ===");
            var cameras = CreateRoot("=== CAMERAS ===");
            var ui = CreateRoot("=== UI ===");

            var houseGo = CreateChild(environment, "House");
            var house = houseGo.AddComponent<HouseController>();

            var livingRoom = BuildLivingRoom(environment, materials);
            house.RegisterRoom(livingRoom);
            // Pet is spawned by the tutorial after name + species choice.

            var mainCamera = BuildCamera(cameras);
            var houseCam = mainCamera.gameObject.AddComponent<HouseCameraController>();
            PostProcessFactory.EnableCameraPostProcessing(mainCamera, hdr: true);

            var lights = BuildLighting(lighting);
            var volume = BuildGlobalVolume(lighting, volumeProfile);
            var dayNight = BuildDayNight(systems, lights, mainCamera, volume);

            var economy = systems.AddComponent<EconomyService>();
            var inventory = systems.AddComponent<FoodInventory>();
            inventory.SetCatalog(foods);
            WireFoodInventory(inventory, foods);
            var minigames = systems.AddComponent<MinigameRouter>();
            var minigameHud = systems.AddComponent<MinigameHud>();
            minigames.BindHud(minigameHud);

            var desktop = systems.AddComponent<DesktopWindowController>();
            var tutorial = systems.AddComponent<StarterTutorial>();
            var bootstrap = systems.AddComponent<GameBootstrap>();

            var hud = CozyHudBuilder.BuildHud(ui, livingRoom.DisplayName, dayNight, spritePack, economy, inventory, minigames, house);
            CozyHudBuilder.BuildMinigameOverlay(hud.canvas.transform, minigameHud);
            var shop = CozyHudBuilder.BuildFoodShopPanel(hud.canvas.transform, economy, inventory, house);
            // Persist shop + systems onto CareHud so Play mode keeps the Shop button wired.
            var careSo = new SerializedObject(hud.care);
            careSo.FindProperty("economy").objectReferenceValue = economy;
            careSo.FindProperty("inventory").objectReferenceValue = inventory;
            careSo.FindProperty("minigames").objectReferenceValue = minigames;
            careSo.FindProperty("house").objectReferenceValue = house;
            careSo.FindProperty("hud").objectReferenceValue = hud.hud;
            careSo.FindProperty("shopPanel").objectReferenceValue = shop;
            careSo.ApplyModifiedPropertiesWithoutUndo();
            hud.care.Bind(economy, inventory, minigames, house, hud.hud, shop);
            CozyHudBuilder.BuildTutorialPanel(hud.canvas.transform, tutorial, characters.transform, livingRoom, minigames, pets, materials.Palette);

            WireBootstrap(bootstrap, house, houseCam, desktop, dayNight, economy, inventory, minigames, hud.care, tutorial);
            WireHouseCamera(houseCam, mainCamera);
            WireHouseController(house, livingRoom);
            WireEconomy(economy, startingCoins: 0);

            houseCam.ApplyLens();
            houseCam.FocusRoom(livingRoom);
            dayNight.Apply(dayNight.TimeOfDay01);

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Selection.activeGameObject = tutorial.gameObject;
            EditorGUIUtility.PingObject(tutorial.gameObject);

            Debug.Log(
                "[PolyPets] Starter house scene ready.\n" +
                $"Saved to {ScenePath}\n" +
                "Tutorial: welcome → name → pick Cat/Dog/Rabbit → minigame earns coins → buy food.");

            EditorUtility.DisplayDialog(
                "PolyPets Bootstrap",
                "Starter house scene ready.\n\n" +
                "• Tutorial: welcome, name, Cat/Dog/Rabbit\n" +
                "• Minigames earn coins\n" +
                "• Shop: Budget / Medium / Super per species\n" +
                "• Click the bowl to feed (full pets wait)\n\n" +
                $"Scene: {ScenePath}",
                "Nice");
        }

        [MenuItem(RootMenu + "Select Starter Scene", priority = 1)]
        public static void SelectStarterScene()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "PolyPets",
                    "Starter scene not found. Run PolyPets → Bootstrap Starter House Scene first.",
                    "OK");
                return;
            }

            Selection.activeObject = sceneAsset;
            EditorGUIUtility.PingObject(sceneAsset);
        }

        [MenuItem(RootMenu + "Frame Camera On Active Room", priority = 20)]
        public static void FrameCameraOnActiveRoom()
        {
            var houseCam = FindFirst<HouseCameraController>();
            var house = FindFirst<HouseController>();
            if (houseCam == null || house == null || house.ActiveRoom == null)
            {
                EditorUtility.DisplayDialog("PolyPets", "Need HouseCameraController + HouseController with a room in the open scene.", "OK");
                return;
            }

            Undo.RecordObject(houseCam.transform, "Frame House Camera");
            if (houseCam.TargetCamera != null)
                Undo.RecordObject(houseCam.TargetCamera.transform, "Frame House Camera");

            houseCam.FocusRoom(house.ActiveRoom);
            EditorUtility.SetDirty(houseCam);
        }

        [MenuItem(RootMenu + "Rebuild Volume Profile", priority = 21)]
        public static void RebuildVolumeProfile()
        {
            EnsureFolders();
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (existing != null)
            {
                PostProcessFactory.PopulateProfile(existing);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(existing);
                Debug.Log($"[PolyPets] Rebuilt overrides on {VolumeProfilePath}");
                return;
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "PolyPets_VolumeProfile";
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            PostProcessFactory.PopulateProfile(profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(profile);
            Debug.Log($"[PolyPets] Created volume profile at {VolumeProfilePath}");
        }

        private static T FindFirst<T>() where T : Object
        {
            return Object.FindFirstObjectByType<T>();
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets", "Scenes");
            CreateFolder("Assets", "Materials");
            CreateFolder("Assets", "Shaders");
            CreateFolder("Assets", "ScriptableObjects");
            CreateFolder("Assets/ScriptableObjects", "Pets");
            CreateFolder("Assets/ScriptableObjects", "Rendering");
            CreateFolder("Assets", "Prefabs");
            CreateFolder("Assets/Prefabs", "Pets");
            CreateFolder("Assets/Prefabs", "Rooms");
            CreateFolder("Assets/Prefabs", "UI");
            CreateFolder("Assets", "Settings");
            AssetDatabase.Refresh();
        }

        private static void CreateFolder(string parent, string name)
        {
            var path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private struct MaterialKit
        {
            public Material Floor;
            public Material Wall;
            public Material Trim;
            public Material Prop;
            public Material CatPrimary;
            public Material CatSecondary;
            public Material Accent;
            public Material Rug;
            public Material Bowl;
            public MaterialPalette Palette;
        }

        private struct LightKit
        {
            public Light Sun;
            public Light Fill;
            public Light Lamp;
        }

        private static MaterialKit CreateMaterialKit(MaterialPalette palette)
        {
            // All house/pet mats come from the locked 25-material palette in Assets/Materials/.
            return new MaterialKit
            {
                Floor = palette.floorWornWood,
                Wall = palette.wallPeeling,
                Trim = palette.trimDark,
                Prop = palette.propDusty,
                CatPrimary = palette.catOrange,
                CatSecondary = palette.catDark,
                Accent = palette.accentLamp,
                Rug = palette.rugCharcoal,
                Bowl = palette.bowlCeramic,
                Palette = palette,
            };
        }

        private static Material GetOrCreateCelMaterial(string name, Color color, Color shade, float outline)
        {
            // Legacy helper — prefer MaterialPaletteFactory.EnsurePalette().
            return MaterialPaletteFactory.Load(name) ?? CreateLegacyCel(name, color, shade, outline);
        }

        private static Material CreateLegacyCel(string name, Color color, Color shade, float outline)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var shader = Shader.Find("PolyPets/CelShade")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");

            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                if (existing.shader != shader && shader != null)
                    existing.shader = shader;
                ApplyCelProperties(existing, color, shade, outline);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var mat = new Material(shader) { name = name };
            ApplyCelProperties(mat, color, shade, outline);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void ApplyCelProperties(Material mat, Color color, Color shade, float outline)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.color = color;
            if (mat.HasProperty("_ShadeColor"))
                mat.SetColor("_ShadeColor", shade);
            if (mat.HasProperty("_ShadeThreshold"))
                mat.SetFloat("_ShadeThreshold", 0.50f);
            if (mat.HasProperty("_ShadeSoftness"))
                mat.SetFloat("_ShadeSoftness", 0.02f);
            if (mat.HasProperty("_OutlineWidth"))
                mat.SetFloat("_OutlineWidth", Mathf.Max(outline * 1.45f, 0.012f));
            if (mat.HasProperty("_OutlineColor"))
                mat.SetColor("_OutlineColor", new Color(0.06f, 0.04f, 0.05f, 1f));
            if (mat.HasProperty("_ShadeColor"))
            {
                var s = shade;
                s.r *= 0.88f;
                s.g *= 0.88f;
                s.b *= 0.88f;
                mat.SetColor("_ShadeColor", s);
            }
            if (mat.HasProperty("_RimStrength"))
                mat.SetFloat("_RimStrength", 0.22f);
        }

        private static VolumeProfile CreateOrLoadVolumeProfile()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (existing != null)
                return existing;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "PolyPets_VolumeProfile";
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            PostProcessFactory.PopulateProfile(profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static PetDefinition CreateOrLoadCatDefinition(Material primary)
        {
            // Prefer shared catalog.
            return PetCatalogFactory.EnsureStarterPets().cat;
        }

        private static GameObject CreateRoot(string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go;
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static RoomRoot BuildLivingRoom(GameObject environmentRoot, MaterialKit mats)
        {
            var roomGo = CreateChild(environmentRoot, "Room_LivingRoom");
            roomGo.transform.position = Vector3.zero;

            var room = roomGo.AddComponent<RoomRoot>();
            room.Configure("living_room", "Living Room");

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(roomGo.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(RoomSize.x, 0.1f, RoomSize.z);
            ApplyMaterial(floor, mats.Floor);

            CreateWall(roomGo, "Wall_Back", new Vector3(0f, RoomSize.y * 0.5f, RoomSize.z * 0.5f),
                new Vector3(RoomSize.x, RoomSize.y, 0.12f), mats.Wall);
            CreateWall(roomGo, "Wall_Left", new Vector3(-RoomSize.x * 0.5f, RoomSize.y * 0.5f, 0f),
                new Vector3(0.12f, RoomSize.y, RoomSize.z), mats.Wall);
            CreateWall(roomGo, "Wall_Right", new Vector3(RoomSize.x * 0.5f, RoomSize.y * 0.5f, 0f),
                new Vector3(0.12f, RoomSize.y, RoomSize.z), mats.Wall);

            CreateWall(roomGo, "Trim_Back", new Vector3(0f, 0.1f, RoomSize.z * 0.5f - 0.02f),
                new Vector3(RoomSize.x - 0.2f, 0.2f, 0.08f), mats.Trim);

            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Prop_Crate";
            crate.transform.SetParent(roomGo.transform, false);
            crate.transform.localPosition = new Vector3(-1.6f, 0.35f, 1.2f);
            crate.transform.localScale = new Vector3(0.9f, 0.7f, 0.7f);
            crate.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            ApplyMaterial(crate, mats.Prop);

            var lampPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lampPole.name = "Prop_LampPole";
            lampPole.transform.SetParent(roomGo.transform, false);
            lampPole.transform.localPosition = new Vector3(1.8f, 0.7f, 1.5f);
            lampPole.transform.localScale = new Vector3(0.08f, 0.7f, 0.08f);
            ApplyMaterial(lampPole, mats.Trim);

            var lampShade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lampShade.name = "Prop_LampShade";
            lampShade.transform.SetParent(roomGo.transform, false);
            lampShade.transform.localPosition = new Vector3(1.8f, 1.45f, 1.5f);
            lampShade.transform.localScale = new Vector3(0.45f, 0.25f, 0.45f);
            ApplyMaterial(lampShade, mats.Accent);

            var rug = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rug.name = "Prop_Rug";
            rug.transform.SetParent(roomGo.transform, false);
            rug.transform.localPosition = new Vector3(0f, 0.01f, -0.3f);
            rug.transform.localScale = new Vector3(2.2f, 0.02f, 1.4f);
            ApplyMaterial(rug, mats.Rug != null ? mats.Rug : mats.Trim);

            var focus = CreateChild(roomGo, "FocusAnchor");
            focus.transform.localPosition = new Vector3(0f, 0.2f, 0.4f);

            var petAnchor = CreateChild(roomGo, "PetAnchor");
            petAnchor.transform.localPosition = new Vector3(0.15f, 0f, -0.2f);
            petAnchor.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);

            var so = new SerializedObject(room);
            so.FindProperty("focusAnchor").objectReferenceValue = focus.transform;
            so.FindProperty("petAnchor").objectReferenceValue = petAnchor.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            return room;
        }

        private static void CreateWall(GameObject parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent.transform, false);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = scale;
            ApplyMaterial(wall, mat);
        }

        private static void ApplyMaterial(GameObject go, Material mat)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && mat != null)
                renderer.sharedMaterial = mat;
        }

        private static PetAgent BuildBoxHeadCat(GameObject charactersRoot, PetDefinition def, MaterialKit mats)
        {
            var root = CreateChild(charactersRoot, "Pet_Cat_Mochi");
            root.transform.position = Vector3.zero;

            var agent = root.AddComponent<PetAgent>();
            agent.BindDefinition(def);

            var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "Shadow";
            Object.DestroyImmediate(shadow.GetComponent<Collider>());
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            shadow.transform.localScale = new Vector3(0.7f, 0.01f, 0.45f);
            ApplyMaterial(shadow, mats.Trim);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.4f, 0.85f);
            ApplyMaterial(body, mats.CatPrimary);

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head_Box";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.95f, 0.15f);
            head.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            ApplyMaterial(head, mats.CatPrimary);

            CreateCatPart(root, "Ear_L", new Vector3(-0.18f, 1.28f, 0.05f), new Vector3(0.14f, 0.18f, 0.1f), mats.CatSecondary);
            CreateCatPart(root, "Ear_R", new Vector3(0.18f, 1.28f, 0.05f), new Vector3(0.14f, 0.18f, 0.1f), mats.CatSecondary);
            CreateCatPart(root, "Eye_L", new Vector3(-0.12f, 0.98f, 0.4f), new Vector3(0.1f, 0.12f, 0.06f), mats.CatSecondary);
            CreateCatPart(root, "Eye_R", new Vector3(0.12f, 0.98f, 0.4f), new Vector3(0.1f, 0.12f, 0.06f), mats.CatSecondary);
            CreateCatPart(root, "Leg_FL", new Vector3(-0.16f, 0.16f, 0.25f), new Vector3(0.12f, 0.32f, 0.12f), mats.CatSecondary);
            CreateCatPart(root, "Leg_FR", new Vector3(0.16f, 0.16f, 0.25f), new Vector3(0.12f, 0.32f, 0.12f), mats.CatSecondary);
            CreateCatPart(root, "Leg_BL", new Vector3(-0.16f, 0.16f, -0.28f), new Vector3(0.12f, 0.32f, 0.12f), mats.CatSecondary);
            CreateCatPart(root, "Leg_BR", new Vector3(0.16f, 0.16f, -0.28f), new Vector3(0.12f, 0.32f, 0.12f), mats.CatSecondary);

            var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tail.name = "Tail";
            tail.transform.SetParent(root.transform, false);
            tail.transform.localPosition = new Vector3(0.28f, 0.55f, -0.5f);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            tail.transform.localScale = new Vector3(0.1f, 0.1f, 0.55f);
            ApplyMaterial(tail, mats.CatPrimary);

            agent.SetVisualRoots(head.transform, body.transform);

            var so = new SerializedObject(agent);
            so.FindProperty("petName").stringValue = "Mochi";
            so.FindProperty("definition").objectReferenceValue = def;
            so.FindProperty("head").objectReferenceValue = head.transform;
            so.FindProperty("body").objectReferenceValue = body.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory("Assets/Prefabs/Pets");
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Pets/Pet_Cat_Mochi.prefab");

            return agent;
        }

        private static void CreateCatPart(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent.transform, false);
            part.transform.localPosition = pos;
            part.transform.localScale = scale;
            ApplyMaterial(part, mat);
        }

        private static UnityEngine.Camera BuildCamera(GameObject camerasRoot)
        {
            var camGo = CreateChild(camerasRoot, "HouseCamera");
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.14f, 0.13f);
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            cam.allowMSAA = false;
            cam.allowHDR = true;
            camGo.AddComponent<AudioListener>();
            return cam;
        }

        private static LightKit BuildLighting(GameObject lightingRoot)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.32f, 0.28f);
            RenderSettings.fog = false;

            var key = CreateChild(lightingRoot, "Sun_KeyLight");
            var keyLight = key.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.92f, 0.82f);
            keyLight.intensity = 1.05f;
            keyLight.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

            var fill = CreateChild(lightingRoot, "FillLight");
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.55f, 0.6f, 0.7f);
            fillLight.intensity = 0.35f;
            fillLight.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(15f, 140f, 0f);

            var lamp = CreateChild(lightingRoot, "LampPoint");
            var lampLight = lamp.AddComponent<Light>();
            lampLight.type = LightType.Point;
            lampLight.color = new Color(1f, 0.8f, 0.5f);
            lampLight.intensity = 1.4f;
            lampLight.range = 4.5f;
            lampLight.shadows = LightShadows.None;
            lamp.transform.position = new Vector3(1.8f, 1.5f, 1.5f);

            return new LightKit { Sun = keyLight, Fill = fillLight, Lamp = lampLight };
        }

        private static Volume BuildGlobalVolume(GameObject lightingRoot, VolumeProfile profile)
        {
            var go = CreateChild(lightingRoot, "GlobalVolume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.profile = profile;
            return volume;
        }

        private static DayNightCycle BuildDayNight(GameObject systems, LightKit lights, UnityEngine.Camera cam, Volume volume)
        {
            var dayNight = systems.AddComponent<DayNightCycle>();
            var so = new SerializedObject(dayNight);
            so.FindProperty("sunLight").objectReferenceValue = lights.Sun;
            so.FindProperty("fillLight").objectReferenceValue = lights.Fill;
            so.FindProperty("lampLight").objectReferenceValue = lights.Lamp;
            so.FindProperty("targetCamera").objectReferenceValue = cam;
            so.FindProperty("globalVolume").objectReferenceValue = volume;
            so.FindProperty("dayLengthSeconds").floatValue = 480f;
            so.FindProperty("timeOfDay").floatValue = 0.35f;
            so.FindProperty("running").boolValue = true;
            so.FindProperty("editorPreview").boolValue = true;
            var interval = so.FindProperty("visualUpdateInterval");
            if (interval != null)
                interval.floatValue = 0.25f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Force default gradients via public API
            dayNight.SetTimeOfDay(0.35f);
            return dayNight;
        }

        private struct HudBundle
        {
            public HudController hud;
            public CareHudController care;
            public Canvas canvas;
        }

        private static HudBundle BuildHud(
            GameObject uiRoot,
            string roomName,
            DayNightCycle dayNight,
            UiSpritePack spritePack,
            EconomyService economy,
            FoodInventory inventory,
            MinigameRouter minigames,
            HouseController house)
        {
            var eventSystem = CreateChild(uiRoot, "EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            var canvasGo = CreateChild(uiRoot, "HUD_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480, 720);
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var hudRoot = canvasGo.AddComponent<UiHudRoot>();
            var hudRootSo = new SerializedObject(hudRoot);
            hudRootSo.FindProperty("spritePack").objectReferenceValue = spritePack;
            hudRootSo.FindProperty("buttonRoot").objectReferenceValue = canvasGo.transform;
            hudRootSo.ApplyModifiedPropertiesWithoutUndo();

            var hud = canvasGo.AddComponent<HudController>();
            var care = canvasGo.AddComponent<CareHudController>();

            var topPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Chrome/Bar_TopChrome.prefab");
            var bottomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Chrome/Bar_BottomActions.prefab");
            var wantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Chrome/Panel_WantPrompt.prefab");

            Text coinLabel = null;
            Text roomLabel = null;
            Text clockLabel = null;
            UiChromeButton feedBtn = null;
            UiChromeButton shopBtn = null;
            UiChromeButton playBtn = null;
            UiChromeButton minigameBtn = null;

            if (topPrefab != null)
            {
                var top = (GameObject)PrefabUtility.InstantiatePrefab(topPrefab);
                top.transform.SetParent(canvasGo.transform, false);
                var topRt = top.GetComponent<RectTransform>();
                topRt.anchorMin = new Vector2(0.5f, 1f);
                topRt.anchorMax = new Vector2(0.5f, 1f);
                topRt.pivot = new Vector2(0.5f, 1f);
                topRt.anchoredPosition = Vector2.zero;

                coinLabel = top.transform.Find("CoinText")?.GetComponent<Text>();
                roomLabel = top.transform.Find("RoomText")?.GetComponent<Text>();
                clockLabel = top.transform.Find("ClockText")?.GetComponent<Text>();
                if (roomLabel != null)
                    roomLabel.text = roomName;
            }
            else
            {
                var topBar = CreateUiPanel(canvasGo.transform, "TopBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -36f), new Vector2(480f, 72f), new Color(0.08f, 0.07f, 0.06f, 0.85f));
                coinLabel = CreateUiText(topBar.transform, "CoinText", "0",
                    new Vector2(0f, 0.65f), new Vector2(0f, 0.65f), new Vector2(70f, 0f), new Vector2(120f, 28f),
                    TextAnchor.MiddleLeft, 22);
                roomLabel = CreateUiText(topBar.transform, "RoomText", roomName,
                    new Vector2(1f, 0.65f), new Vector2(1f, 0.65f), new Vector2(-90f, 0f), new Vector2(160f, 28f),
                    TextAnchor.MiddleRight, 18);
                clockLabel = CreateUiText(topBar.transform, "ClockText", "08:24  Day",
                    new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(220f, 24f),
                    TextAnchor.MiddleCenter, 14);
            }

            // Needs strip under the top bar
            var needsBar = CreateUiPanel(canvasGo.transform, "NeedsBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -92f), new Vector2(460f, 44f), new Color(0.08f, 0.07f, 0.06f, 0.8f));
            var hungerLabel = CreateUiText(needsBar.transform, "HungerText", "Hunger 70",
                new Vector2(0.18f, 0.55f), new Vector2(0.18f, 0.55f), Vector2.zero, new Vector2(120f, 22f),
                TextAnchor.MiddleLeft, 14);
            var happyLabel = CreateUiText(needsBar.transform, "HappyText", "Happy 70",
                new Vector2(0.48f, 0.55f), new Vector2(0.48f, 0.55f), Vector2.zero, new Vector2(120f, 22f),
                TextAnchor.MiddleLeft, 14);
            var foodLabel = CreateUiText(needsBar.transform, "FoodStockText", "Food x0",
                new Vector2(0.78f, 0.55f), new Vector2(0.78f, 0.55f), Vector2.zero, new Vector2(100f, 22f),
                TextAnchor.MiddleLeft, 14);
            var statusLabel = CreateUiText(needsBar.transform, "StatusText", "Okay",
                new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(400f, 18f),
                TextAnchor.MiddleCenter, 12);

            if (bottomPrefab != null)
            {
                var bottom = (GameObject)PrefabUtility.InstantiatePrefab(bottomPrefab);
                bottom.transform.SetParent(canvasGo.transform, false);
                var bottomRt = bottom.GetComponent<RectTransform>();
                bottomRt.anchorMin = new Vector2(0.5f, 0f);
                bottomRt.anchorMax = new Vector2(0.5f, 0f);
                bottomRt.pivot = new Vector2(0.5f, 0f);
                bottomRt.anchoredPosition = Vector2.zero;

                foreach (var chrome in bottom.GetComponentsInChildren<UiChromeButton>(true))
                {
                    switch (chrome.ButtonId)
                    {
                        case UiButtonId.Feed: feedBtn = chrome; break;
                        case UiButtonId.Shop: shopBtn = chrome; break;
                        case UiButtonId.Play: playBtn = chrome; break;
                        case UiButtonId.Minigame: minigameBtn = chrome; break;
                    }
                }
            }

            if (wantPrefab != null)
            {
                var want = (GameObject)PrefabUtility.InstantiatePrefab(wantPrefab);
                want.transform.SetParent(canvasGo.transform, false);
                var wantRt = want.GetComponent<RectTransform>();
                wantRt.anchorMin = wantRt.anchorMax = new Vector2(0.5f, 0.22f);
                wantRt.anchoredPosition = Vector2.zero;
            }

            var so = new SerializedObject(hud);
            so.FindProperty("coinText").objectReferenceValue = coinLabel;
            so.FindProperty("roomText").objectReferenceValue = roomLabel;
            so.FindProperty("clockText").objectReferenceValue = clockLabel;
            so.FindProperty("dayNight").objectReferenceValue = dayNight;
            so.ApplyModifiedPropertiesWithoutUndo();

            care.BindMeters(hungerLabel, happyLabel, statusLabel, foodLabel);
            care.BindActionButtons(feedBtn, shopBtn, minigameBtn, playBtn);

            hud.SetCoins(economy != null ? economy.Coins : 0);
            hud.SetRoomName(roomName);
            hud.BindDayNight(dayNight);
            hudRoot.RefreshButtons();
            care.RefreshAll();

            return new HudBundle { hud = hud, care = care, canvas = canvas };
        }

        private static FoodShopPanel BuildFoodShopPanel(
            Transform canvas,
            EconomyService economy,
            FoodInventory inventory,
            HouseController house)
        {
            var shopGo = new GameObject("FoodShop", typeof(RectTransform));
            shopGo.transform.SetParent(canvas, false);
            var shopRt = shopGo.GetComponent<RectTransform>();
            shopRt.anchorMin = Vector2.zero;
            shopRt.anchorMax = Vector2.one;
            shopRt.offsetMin = Vector2.zero;
            shopRt.offsetMax = Vector2.zero;
            var shop = shopGo.AddComponent<FoodShopPanel>();

            var root = CreateUiPanel(shopGo.transform, "ShopPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400f, 420f), new Color(0.08f, 0.07f, 0.06f, 0.96f));
            root.SetActive(false);

            var title = CreateUiText(root.transform, "Title", "Food Shop",
                new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(360f, 32f),
                TextAnchor.MiddleCenter, 20);
            var body = CreateUiText(root.transform, "Body", "Budget / Medium / Super",
                new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(360f, 50f),
                TextAnchor.UpperCenter, 13);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;

            var buttonRoot = new GameObject("Rows", typeof(RectTransform));
            buttonRoot.transform.SetParent(root.transform, false);
            var brt = buttonRoot.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;

            var close = CreateSimpleButton(root.transform, "Close", "Close", new Vector2(0.5f, 0.08f), new Vector2(120f, 40f));
            shop.Bind(root, title, body, buttonRoot.transform, close, inventory, economy, house);
            return shop;
        }

        private static void BuildMinigameOverlay(Transform canvas, MinigameHud hud)
        {
            var root = CreateUiPanel(canvas, "MinigameOverlay", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(420f, 320f), new Color(0.05f, 0.04f, 0.04f, 0.92f));
            root.SetActive(false);

            var prompt = CreateUiText(root.transform, "Prompt", "Minigame",
                new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(390f, 220f),
                TextAnchor.UpperCenter, 15);
            prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
            prompt.verticalOverflow = VerticalWrapMode.Overflow;

            var score = CreateUiText(root.transform, "Score", "Coins 0",
                new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(360f, 28f),
                TextAnchor.MiddleCenter, 16);

            hud.Bind(prompt, score, root);
        }

        private static void BuildTutorialPanel(
            Transform canvas,
            StarterTutorial tutorial,
            Transform charactersRoot,
            RoomRoot room,
            MinigameRouter router,
            (PetDefinition cat, PetDefinition dog, PetDefinition rabbit) pets,
            MaterialKit materials)
        {
            var root = CreateUiPanel(canvas, "TutorialPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(420f, 460f), new Color(0.09f, 0.07f, 0.06f, 0.96f));

            var title = CreateUiText(root.transform, "Title", "Welcome to PolyPets",
                new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), Vector2.zero, new Vector2(380f, 40f),
                TextAnchor.MiddleCenter, 22);

            var body = CreateUiText(root.transform, "Body", "A cozy desktop home for box-headed pals.",
                new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(360f, 140f),
                TextAnchor.UpperCenter, 15);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;

            var inputGo = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputGo.transform.SetParent(root.transform, false);
            var inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = inputRt.anchorMax = new Vector2(0.5f, 0.42f);
            inputRt.sizeDelta = new Vector2(280f, 40f);
            inputGo.GetComponent<Image>().color = new Color(0.18f, 0.15f, 0.13f, 1f);
            var input = inputGo.GetComponent<InputField>();

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(inputGo.transform, false);
            StretchFull(placeholderGo.GetComponent<RectTransform>(), 8f);
            var placeholder = placeholderGo.GetComponent<Text>();
            placeholder.text = "Pet name…";
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                               ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            placeholder.fontSize = 16;

            var inputTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            inputTextGo.transform.SetParent(inputGo.transform, false);
            StretchFull(inputTextGo.GetComponent<RectTransform>(), 8f);
            var inputText = inputTextGo.GetComponent<Text>();
            inputText.font = placeholder.font;
            inputText.fontSize = 16;
            inputText.color = Color.white;
            inputText.supportRichText = false;

            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.text = "Mochi";
            inputGo.SetActive(false);

            var next = CreateSimpleButton(root.transform, "NextButton", "Let's go", new Vector2(0.5f, 0.18f), new Vector2(160f, 44f));
            var cat = CreateSimpleButton(root.transform, "Btn_Cat", "Cat\nFishing", new Vector2(0.2f, 0.22f), new Vector2(110f, 70f));
            var dog = CreateSimpleButton(root.transform, "Btn_Dog", "Dog\nDig + Snap", new Vector2(0.5f, 0.22f), new Vector2(110f, 70f));
            var rabbit = CreateSimpleButton(root.transform, "Btn_Rabbit", "Rabbit\nCarrots", new Vector2(0.8f, 0.22f), new Vector2(110f, 70f));
            cat.gameObject.SetActive(false);
            dog.gameObject.SetActive(false);
            rabbit.gameObject.SetActive(false);

            var nextLabel = next.GetComponentInChildren<Text>();

            tutorial.BindUi(root, title, body, input, next, nextLabel, cat, dog, rabbit);
            tutorial.BindWorld(
                charactersRoot,
                room,
                router,
                pets.cat,
                pets.dog,
                pets.rabbit,
                materials.Palette);

            root.transform.SetAsLastSibling();
        }

        private static void StretchFull(RectTransform rt, float pad)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        private static Button CreateSimpleButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.25f, 0.2f, 0.17f, 1f);
            var button = go.GetComponent<Button>();

            var text = CreateUiText(go.transform, "Label", label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, size - new Vector2(8f, 8f), TextAnchor.MiddleCenter, 14);
            text.raycastTarget = false;
            return button;
        }

        private static GameObject CreateUiPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateUiText(Transform parent, string name, string content, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, TextAnchor align, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var text = go.GetComponent<Text>();
            text.text = content;
            text.alignment = align;
            text.fontSize = fontSize;
            text.color = new Color(0.95f, 0.92f, 0.88f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void WireBootstrap(
            GameBootstrap bootstrap,
            HouseController house,
            HouseCameraController houseCam,
            DesktopWindowController desktop,
            DayNightCycle dayNight,
            EconomyService economy,
            FoodInventory inventory,
            MinigameRouter minigames,
            CareHudController care,
            StarterTutorial tutorial)
        {
            var so = new SerializedObject(bootstrap);
            so.FindProperty("house").objectReferenceValue = house;
            so.FindProperty("houseCamera").objectReferenceValue = houseCam;
            so.FindProperty("desktopWindow").objectReferenceValue = desktop;
            so.FindProperty("dayNight").objectReferenceValue = dayNight;
            so.FindProperty("economy").objectReferenceValue = economy;
            so.FindProperty("foodInventory").objectReferenceValue = inventory;
            so.FindProperty("minigameRouter").objectReferenceValue = minigames;
            so.FindProperty("careHud").objectReferenceValue = care;
            so.FindProperty("tutorial").objectReferenceValue = tutorial;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireEconomy(EconomyService economy, int startingCoins)
        {
            var so = new SerializedObject(economy);
            so.FindProperty("coins").intValue = startingCoins;
            so.FindProperty("startingCoins").intValue = startingCoins;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireFoodInventory(FoodInventory inventory, FoodItemDefinition[] foods)
        {
            var so = new SerializedObject(inventory);
            var catalog = so.FindProperty("catalog");
            catalog.arraySize = foods.Length;
            for (int i = 0; i < foods.Length; i++)
                catalog.GetArrayElementAtIndex(i).objectReferenceValue = foods[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireHouseCamera(HouseCameraController houseCam, UnityEngine.Camera cam)
        {
            var so = new SerializedObject(houseCam);
            so.FindProperty("targetCamera").objectReferenceValue = cam;
            so.FindProperty("fieldOfView").floatValue = 32f;
            so.FindProperty("viewOffset").vector3Value = new Vector3(4.2f, 3.4f, -4.2f);
            so.FindProperty("roomFocusOffset").vector3Value = new Vector3(0f, 1.1f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireHouseController(HouseController house, RoomRoot livingRoom)
        {
            var so = new SerializedObject(house);
            var rooms = so.FindProperty("rooms");
            rooms.arraySize = 1;
            rooms.GetArrayElementAtIndex(0).objectReferenceValue = livingRoom;
            so.FindProperty("activeRoomIndex").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
