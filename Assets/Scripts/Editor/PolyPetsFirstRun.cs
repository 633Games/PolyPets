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
            PolyPetsSceneBootstrap.BootstrapStarterHouseScene();

            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (scene != null)
            {
                EditorSceneManager.OpenScene(ScenePath);
                Selection.activeObject = scene;
                EditorGUIUtility.PingObject(scene);
            }

            EditorPrefs.SetBool(PrefKey, true);
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
                    "Click OK to run First-Time Setup:\n" +
                    "• URP pipeline\n" +
                    "• Cel-shaded living room greybox\n" +
                    "• Tutorial + Cat/Dog/Rabbit minigames\n" +
                    "• Food shop + bowls\n\n" +
                    "You can also use: PolyPets → ★ First-Time Setup",
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
