#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PolyPets.Core;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// One-click entry for a fresh clone: URP + full house scene + 633 Games branding.
    /// Also nudges once on editor load if the starter scene is missing.
    /// </summary>
    public static class PolyPetsFirstRun
    {
        private const string PrefKey = "PolyPets.FirstRun.Dismissed";
        private const string ScenePath = "Assets/Scenes/House_LivingRoom.unity";

        [MenuItem("PolyPets/★ First-Time Setup (run this)", priority = -100)]
        public static void FirstTimeSetup()
        {
            EnsureInputHandlingBoth();
            StudioBrandingSetup.ApplyPlayerBranding();

            // Prefer importing More Mountains Feel BEFORE this step so upgrade can run.
            bool feelReady = FeelTagEditorTools.IsFeelPresent(out var feelDetail);

            PolyPetsSceneBootstrap.BootstrapStarterHouseScene();
            VendorSpritePackApplier.ApplyVendorSprites(showDialog: false);
            MaterialPaletteFactory.EnsurePalette(showDialog: false);

            if (feelReady)
            {
                FeelTagEditorTools.UpgradeTagsToMmfPlayers(showDialog: false);
                Debug.Log("[PolyPets] Feel detected — upgraded FEEL[Squash] tags toward MMF Players.\n" + feelDetail);
            }
            else
            {
                Debug.Log(
                    "[PolyPets] Feel Asset Store pack not detected yet. Built-in FEEL[Squash] idle is active.\n" +
                    "Import Feel, then run PolyPets → Feel → Upgrade Tags To MMF Players.");
            }

            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (scene != null)
            {
                EditorSceneManager.OpenScene(ScenePath);
                Selection.activeObject = scene;
                EditorGUIUtility.PingObject(scene);
            }

            EditorPrefs.SetBool(PrefKey, true);

            EditorUtility.DisplayDialog(
                $"{StudioBrand.ProductName} — {StudioBrand.StudioName}",
                "Setup complete.\n\n" +
                (feelReady
                    ? "Feel pack detected and wired to FEEL[Squash] tags.\n"
                    : "Tip: import More Mountains Feel, then re-run Feel → Upgrade Tags.\n") +
                "Idle coins drip onto the floor (max 10). Ambient + juicy SFX on Play.\n\n" +
                StudioBrand.CopyrightLine,
                "Let's play");
        }

        /// <summary>
        /// Batchmode entry: Unity -batchmode -projectPath . -executeMethod PolyPets.EditorTools.PolyPetsFirstRun.FirstTimeSetupBatch -quit
        /// </summary>
        public static void FirstTimeSetupBatch()
        {
            EnsureInputHandlingBoth();
            StudioBrandingSetup.ApplyPlayerBranding();
            PolyPetsSceneBootstrap.BootstrapStarterHouseScene();
            VendorSpritePackApplier.ApplyVendorSprites(showDialog: false);
            MaterialPaletteFactory.EnsurePalette(showDialog: false);
            EditorPrefs.SetBool(PrefKey, true);
            Debug.Log("[PolyPets] FirstTimeSetupBatch finished.");
        }

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            // Fix Input System prompt as soon as scripts compile — before First-Time Setup.
            EditorApplication.delayCall += EnsureInputHandlingBoth;
            EditorApplication.delayCall += NudgeIfNeeded;
        }

        private static void EnsureInputHandlingBoth()
        {
            // 0 = Input Manager, 1 = Input System Package, 2 = Both
            try
            {
                PlayerSettings.SetPropertyInt("activeInputHandler", 2);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PolyPets] Could not set Active Input Handling to Both: {ex.Message}");
            }
        }

        private static void NudgeIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (EditorPrefs.GetBool(PrefKey, false))
                return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorPrefs.SetBool(PrefKey, true);
                return;
            }

            bool run = EditorUtility.DisplayDialog(
                $"{StudioBrand.StudioName} · {StudioBrand.ProductName}",
                "Fresh project detected — no starter house scene yet.\n\n" +
                "Recommended first: import More Mountains Feel (Asset Store).\n\n" +
                "Then OK to run First-Time Setup:\n" +
                "• URP + dressed house (4 rooms)\n" +
                "• Minigames, shops, clean, idle coins, juicy SFX\n" +
                "• 633 Games player branding\n" +
                "• Feel upgrade if the pack is present\n\n" +
                "Or: PolyPets → ★ First-Time Setup",
                "Run setup",
                "Later");

            if (run)
                FirstTimeSetup();
            else
                EditorPrefs.SetBool(PrefKey, true);
        }
    }
}
#endif
