#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// One-click entry for a fresh clone: URP + full greybox house scene.
    /// Also nudges once on editor load if the starter scene is missing.
    /// </summary>
    public static class PolyPetsFirstRun
    {
        private const string PrefKey = "PolyPets.FirstRun.Dismissed";
        private const string ScenePath = "Assets/Scenes/House_LivingRoom.unity";

        [MenuItem("PolyPets/★ First-Time Setup (run this)", priority = -100)]
        public static void FirstTimeSetup()
        {
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
                "PolyPets Ready",
                "Setup complete.\n\n" +
                (feelReady
                    ? "Feel pack detected and wired to FEEL[Squash] tags.\n"
                    : "Tip: import More Mountains Feel, then re-run Feel → Upgrade Tags.\n") +
                "Idle coins drip onto the floor (max 10). Ambient loop is playing on Play.",
                "Nice");
        }

        [InitializeOnLoadMethod]
        private static void NudgeIfNeeded()
        {
            EditorApplication.delayCall += () =>
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
                    "Welcome to PolyPets",
                    "Fresh project detected — no starter house scene yet.\n\n" +
                    "Recommended first: import More Mountains Feel (Asset Store).\n\n" +
                    "Then OK to run First-Time Setup:\n" +
                    "• URP + greybox house + 4 rooms\n" +
                    "• Minigames, food/décor shops, clean scrub\n" +
                    "• Idle floor coins + ambient audio\n" +
                    "• Feel upgrade if the pack is present\n\n" +
                    "Or: PolyPets → ★ First-Time Setup",
                    "Run setup",
                    "Later");

                if (run)
                    FirstTimeSetup();
                else
                    EditorPrefs.SetBool(PrefKey, true);
            };
        }
    }
}
#endif
