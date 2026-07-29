using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PolyPets.UI
{
    /// <summary>
    /// Shared UI fonts — Nunito (body) + Fredoka (titles). Vendored under Assets/Fonts/.
    /// </summary>
    public static class UiFonts
    {
        public const string BodyPath = "Assets/Fonts/Nunito/Nunito-Regular.ttf";
        public const string BodyBoldPath = "Assets/Fonts/Nunito/Nunito-Bold.ttf";
        public const string TitlePath = "Assets/Fonts/Fredoka/Fredoka-SemiBold.ttf";
        public const string TitleMediumPath = "Assets/Fonts/Fredoka/Fredoka-Medium.ttf";

        private static Font _body;
        private static Font _bodyBold;
        private static Font _title;
        private static Font _builtin;

        public static Font Body => _body != null ? _body : (_body = Load(BodyPath) ?? Builtin());
        public static Font BodyBold => _bodyBold != null ? _bodyBold : (_bodyBold = Load(BodyBoldPath) ?? Body);
        public static Font Title => _title != null ? _title : (_title = Load(TitlePath) ?? Load(TitleMediumPath) ?? BodyBold);

        public static void ApplyBody(Text text)
        {
            if (text == null) return;
            text.font = Body;
        }

        public static void ApplyTitle(Text text)
        {
            if (text == null) return;
            text.font = Title;
        }

        public static void ApplyAllUnder(Transform root, bool titlesByName = true)
        {
            if (root == null) return;
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                if (text == null) continue;
                string n = text.gameObject.name;
                bool title = titlesByName && (
                    n.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("StudioPresents", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("StudioLabel", System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (title)
                    ApplyTitle(text);
                else
                    ApplyBody(text);
            }
        }

        private static Font Load(string assetPath)
        {
#if UNITY_EDITOR
            var font = AssetDatabase.LoadAssetAtPath<Font>(assetPath);
            if (font != null)
                return font;
#endif
            return null;
        }

        private static Font Builtin()
        {
            if (_builtin != null)
                return _builtin;
            _builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                       ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _builtin;
        }

#if UNITY_EDITOR
        public static void EnsureImportSettings()
        {
            foreach (var path in new[] { BodyPath, BodyBoldPath, TitlePath, TitleMediumPath })
            {
                var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
                if (importer == null)
                    continue;
                bool dirty = false;
                if (importer.fontTextureCase != FontTextureCase.Dynamic)
                {
                    importer.fontTextureCase = FontTextureCase.Dynamic;
                    dirty = true;
                }

                if (importer.fontSize != 16)
                {
                    importer.fontSize = 16;
                    dirty = true;
                }

                if (dirty)
                    importer.SaveAndReimport();
            }

            _body = _bodyBold = _title = null;
        }
#endif
    }
}
