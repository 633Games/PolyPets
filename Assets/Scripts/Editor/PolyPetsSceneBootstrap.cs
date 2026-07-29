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
using PolyPets.House;
using PolyPets.Pets;
using PolyPets.Rendering;
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

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "House_LivingRoom";

            var materials = CreateOrLoadMaterials();
            var catDef = CreateOrLoadCatDefinition(materials.CatPrimary);
            var volumeProfile = CreateOrLoadVolumeProfile();

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

            var cat = BuildBoxHeadCat(characters, catDef, materials);
            livingRoom.SetOccupant(cat);

            var mainCamera = BuildCamera(cameras);
            var houseCam = mainCamera.gameObject.AddComponent<HouseCameraController>();
            PostProcessFactory.EnableCameraPostProcessing(mainCamera, hdr: true);

            var lights = BuildLighting(lighting);
            var volume = BuildGlobalVolume(lighting, volumeProfile);
            var dayNight = BuildDayNight(systems, lights, mainCamera, volume);

            var desktop = systems.AddComponent<DesktopWindowController>();
            var bootstrap = systems.AddComponent<GameBootstrap>();

            var hud = BuildHud(ui, livingRoom.DisplayName, dayNight);

            WireBootstrap(bootstrap, house, houseCam, desktop, dayNight);
            WireHouseCamera(houseCam, mainCamera);
            WireHouseController(house, livingRoom);

            houseCam.ApplyLens();
            houseCam.FocusRoom(livingRoom);
            dayNight.Apply(dayNight.TimeOfDay01);

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Selection.activeGameObject = cat.gameObject;
            EditorGUIUtility.PingObject(cat.gameObject);

            Debug.Log(
                "[PolyPets] Starter house scene ready (Unity 6.3 / URP cel + day-night).\n" +
                $"Saved to {ScenePath}\n" +
                $"Volume profile: {VolumeProfilePath}");

            EditorUtility.DisplayDialog(
                "PolyPets Bootstrap",
                "Starter house scene created for Unity 6.3.\n\n" +
                "• Cel-shaded greybox room + cat\n" +
                "• URP post-processing (bloom / vignette / grade)\n" +
                "• Day/night cycle driving sun, lamp, ambient\n" +
                "• 3/4 house camera + desktop HUD clock\n\n" +
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
        }

        private struct LightKit
        {
            public Light Sun;
            public Light Fill;
            public Light Lamp;
        }

        private static MaterialKit CreateOrLoadMaterials()
        {
            return new MaterialKit
            {
                Floor = GetOrCreateCelMaterial("Mat_Floor_WornWood", new Color(0.45f, 0.32f, 0.22f), new Color(0.28f, 0.18f, 0.14f), outline: 0.008f),
                Wall = GetOrCreateCelMaterial("Mat_Wall_Peeling", new Color(0.62f, 0.58f, 0.5f), new Color(0.4f, 0.36f, 0.34f), outline: 0.006f),
                Trim = GetOrCreateCelMaterial("Mat_Trim_Dark", new Color(0.25f, 0.22f, 0.2f), new Color(0.12f, 0.1f, 0.1f), outline: 0.01f),
                Prop = GetOrCreateCelMaterial("Mat_Prop_Dusty", new Color(0.4f, 0.38f, 0.36f), new Color(0.22f, 0.2f, 0.2f), outline: 0.01f),
                CatPrimary = GetOrCreateCelMaterial("Mat_Cat_Orange", new Color(0.86f, 0.55f, 0.28f), new Color(0.45f, 0.25f, 0.16f), outline: 0.014f),
                CatSecondary = GetOrCreateCelMaterial("Mat_Cat_Dark", new Color(0.18f, 0.15f, 0.13f), new Color(0.08f, 0.06f, 0.06f), outline: 0.012f),
                Accent = GetOrCreateCelMaterial("Mat_Accent_Lamp", new Color(0.95f, 0.78f, 0.45f), new Color(0.55f, 0.35f, 0.2f), outline: 0.01f),
            };
        }

        private static Material GetOrCreateCelMaterial(string name, Color color, Color shade, float outline)
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
                mat.SetFloat("_ShadeThreshold", 0.45f);
            if (mat.HasProperty("_ShadeSoftness"))
                mat.SetFloat("_ShadeSoftness", 0.05f);
            if (mat.HasProperty("_OutlineWidth"))
                mat.SetFloat("_OutlineWidth", outline);
            if (mat.HasProperty("_OutlineColor"))
                mat.SetColor("_OutlineColor", new Color(0.08f, 0.06f, 0.07f, 1f));
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
            var existing = AssetDatabase.LoadAssetAtPath<PetDefinition>(PetDefPath);
            if (existing != null)
                return existing;

            var def = ScriptableObject.CreateInstance<PetDefinition>();
            def.petId = "cat";
            def.displayName = "Cat";
            def.personality = "Curious, lazy";
            def.signatureMinigame = "Fishing";
            def.baseCoinsPerSecond = 1.25f;
            def.primaryColor = primary != null && primary.HasProperty("_BaseColor")
                ? primary.GetColor("_BaseColor")
                : new Color(0.86f, 0.55f, 0.28f);
            def.secondaryColor = new Color(0.18f, 0.15f, 0.13f);

            AssetDatabase.CreateAsset(def, PetDefPath);
            return def;
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
            ApplyMaterial(rug, mats.Trim);

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
            so.ApplyModifiedPropertiesWithoutUndo();

            // Force default gradients via public API
            dayNight.SetTimeOfDay(0.35f);
            return dayNight;
        }

        private static HudController BuildHud(GameObject uiRoot, string roomName, DayNightCycle dayNight)
        {
            var eventSystem = CreateChild(uiRoot, "EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            var canvasGo = CreateChild(uiRoot, "HUD_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480, 720);
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var hud = canvasGo.AddComponent<HudController>();

            var topBar = CreateUiPanel(canvasGo.transform, "TopBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(480f, 72f), new Color(0.08f, 0.07f, 0.06f, 0.85f));

            var coinLabel = CreateUiText(topBar.transform, "CoinText", "12",
                new Vector2(0f, 0.65f), new Vector2(0f, 0.65f), new Vector2(70f, 0f), new Vector2(120f, 28f),
                TextAnchor.MiddleLeft, 22);

            var roomLabel = CreateUiText(topBar.transform, "RoomText", roomName,
                new Vector2(1f, 0.65f), new Vector2(1f, 0.65f), new Vector2(-90f, 0f), new Vector2(160f, 28f),
                TextAnchor.MiddleRight, 18);

            var clockLabel = CreateUiText(topBar.transform, "ClockText", "08:24  Day",
                new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(220f, 24f),
                TextAnchor.MiddleCenter, 14);

            var bottom = CreateUiPanel(canvasGo.transform, "BottomHint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 36f), new Vector2(420f, 40f), new Color(0.08f, 0.07f, 0.06f, 0.7f));
            CreateUiText(bottom.transform, "HintText", "Mochi wants to go fishing…",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 36f),
                TextAnchor.MiddleCenter, 16);

            var so = new SerializedObject(hud);
            so.FindProperty("coinText").objectReferenceValue = coinLabel;
            so.FindProperty("roomText").objectReferenceValue = roomLabel;
            so.FindProperty("clockText").objectReferenceValue = clockLabel;
            so.FindProperty("dayNight").objectReferenceValue = dayNight;
            so.ApplyModifiedPropertiesWithoutUndo();

            hud.SetCoins(12);
            hud.SetRoomName(roomName);
            hud.BindDayNight(dayNight);
            return hud;
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

        private static void WireBootstrap(GameBootstrap bootstrap, HouseController house,
            HouseCameraController houseCam, DesktopWindowController desktop, DayNightCycle dayNight)
        {
            var so = new SerializedObject(bootstrap);
            so.FindProperty("house").objectReferenceValue = house;
            so.FindProperty("houseCamera").objectReferenceValue = houseCam;
            so.FindProperty("desktopWindow").objectReferenceValue = desktop;
            so.FindProperty("dayNight").objectReferenceValue = dayNight;
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
