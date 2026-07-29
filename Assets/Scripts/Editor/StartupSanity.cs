#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using PolyPets.Audio;
using PolyPets.Core;
using PolyPets.UI;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// Post-setup checklist so First-Time Setup lands a playable, stable scene.
    /// </summary>
    public static class StartupSanity
    {
        private const string ScenePath = "Assets/Scenes/House_LivingRoom.unity";

        public static string ValidateSceneReady()
        {
            var sb = new StringBuilder();
            int ok = 0;
            int warn = 0;

            void Pass(string msg) { ok++; sb.AppendLine("✓ " + msg); }
            void Warn(string msg) { warn++; sb.AppendLine("! " + msg); }

            if (File.Exists(ScenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                Pass("Starter scene saved");
            else
                Warn("Starter scene missing — re-run setup");

            if (AssetDatabase.LoadAssetAtPath<Font>(UiFonts.BodyPath) != null)
                Pass("Nunito body font");
            else
                Warn("Nunito font missing under Assets/Fonts/Nunito/");

            if (AssetDatabase.LoadAssetAtPath<Font>(UiFonts.TitlePath) != null)
                Pass("Fredoka title font");
            else
                Warn("Fredoka font missing under Assets/Fonts/Fredoka/");

            if (AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ambient/Amb_CozyHouse_CC0.ogg") != null)
                Pass("Ambient audio");
            else
                Warn("Ambient clip missing");

            if (AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sfx/Sfx_CoinDing.ogg") != null)
                Pass("Juicy SFX bank");
            else
                Warn("SFX bank missing");

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(StudioBrand.BrandMarkAssetPath) != null)
                Pass("633 Games brand mark");
            else
                Warn("Brand mark missing");

            if (AssetDatabase.IsValidFolder("Assets/Prefabs/UI/Chrome"))
                Pass("UI chrome prefabs folder");
            else
                Warn("UI chrome prefabs not built");

            if (AssetDatabase.IsValidFolder("Assets/Materials"))
                Pass("Material palette folder");
            else
                Warn("Materials folder missing");

            sb.Insert(0, $"Sanity {ok} ok · {warn} warn\n");
            return sb.ToString().TrimEnd();
        }

        [MenuItem("PolyPets/★ Startup Sanity Check", priority = -90)]
        public static void MenuValidate()
        {
            var report = ValidateSceneReady();
            EditorUtility.DisplayDialog("Startup Sanity", report, "OK");
            Debug.Log("[PolyPets] " + report);
        }
    }
}
#endif
