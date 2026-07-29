#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using PolyPets.Feel;

namespace PolyPets.EditorTools
{
    public static class FeelTagEditorTools
    {
        private const string RootMenu = "PolyPets/Feel/";

        [MenuItem(RootMenu + "Apply Tags In Open Scene", priority = 0)]
        public static void ApplyTagsInOpenScene()
        {
            int count = 0;
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in transforms)
            {
                if (!FeelTagNaming.TryParseObjectName(t.name, out var type))
                    continue;

                FeelTagBinder.EnsureTagOn(t.gameObject, type);
                count++;
                EditorUtility.SetDirty(t.gameObject);
            }

            Debug.Log($"[PolyPets] Applied Feel tags on {count} object(s).");
            EditorUtility.DisplayDialog("Feel Tags", $"Applied FEEL[*] behaviours on {count} object(s).", "OK");
        }

        [MenuItem(RootMenu + "Wrap Selection With FEEL[Squash]", priority = 1)]
        public static void WrapSelectionWithSquash()
        {
            var selected = Selection.activeTransform;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Feel", "Select a pet/root transform first.", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(selected.gameObject, "Wrap FEEL[Squash]");
            var container = FeelTagBinder.WrapChildrenWithFeelContainer(selected, FeelTagType.Squash, "Idle");
            Selection.activeTransform = container;
            EditorGUIUtility.PingObject(container);
        }

        [MenuItem(RootMenu + "Detect More Mountains Feel Package", priority = 20)]
        public static void DetectFeelPackage()
        {
            bool present = IsFeelPresent(out string detail);
            EditorUtility.DisplayDialog(
                "Feel Package",
                present
                    ? $"Feel / MMFeedbacks detected.\n\n{detail}\n\nYou can upgrade FEEL tags to MMF Players."
                    : "Feel not found in this project yet.\n\nImport Feel from the Package Manager (Asset Store) — see docs/FEEL_AND_UI.md.\nBuilt-in FEEL[Squash] idle still works.",
                "OK");
        }

        [MenuItem(RootMenu + "Upgrade Tags To MMF Players (requires Feel)", priority = 21)]
        public static void UpgradeTagsToMmfPlayersMenu()
        {
            UpgradeTagsToMmfPlayers(showDialog: true);
        }

        public static int UpgradeTagsToMmfPlayers(bool showDialog = true)
        {
            if (!IsFeelPresent(out _))
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Feel Required",
                        "Import More Mountains Feel first (Package Manager → My Assets → Feel).\n\nDocs: https://feel-docs.moremountains.com/how-to-install.html",
                        "OK");
                }

                return 0;
            }

            var mmfPlayerType = FindType("MoreMountains.Feedbacks.MMF_Player")
                                ?? FindType("MoreMountains.Feedbacks.MMFeedbacks");
            var squashType = FindType("MoreMountains.Feedbacks.MMF_SquashAndStretch");

            if (mmfPlayerType == null)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("Feel", "Could not resolve MMF_Player type.", "OK");
                return 0;
            }

            int upgraded = 0;
            var tags = UnityEngine.Object.FindObjectsByType<FeelTag>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var tag in tags)
            {
                if (tag.TagType != FeelTagType.Squash)
                    continue;

                // Remove lite idle so Feel owns the motion.
                var lite = tag.GetComponent<FeelIdleSquash>();
                if (lite != null)
                    Undo.DestroyObjectImmediate(lite);

                var player = tag.GetComponent(mmfPlayerType);
                if (player == null)
                    player = Undo.AddComponent(tag.gameObject, mmfPlayerType);

                TryConfigureMmfSquash(player, squashType, tag.EffectTarget);
                upgraded++;
                EditorUtility.SetDirty(tag.gameObject);
            }

            Debug.Log($"[PolyPets] Upgraded {upgraded} FEEL[Squash] tag(s) toward MMF Players. Tweak curves in the Feel inspector.");
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Feel Upgrade",
                    $"Prepared MMF Player on {upgraded} FEEL[Squash] object(s).\n\n" +
                    "Open each MMF Player and add/confirm Transform → SquashAndStretch:\n" +
                    "• Axis: YtoXZ\n• Timing: repeat forever (idle breathe)\n• Target: this FEEL[Squash] transform (scale 1,1,1)\n" +
                    "Auto Play on Start = on",
                    "OK");
            }

            return upgraded;
        }

        private static void TryConfigureMmfSquash(Component player, Type squashType, Transform target)
        {
            if (player == null)
                return;

            // Best-effort: set common serialized fields when present.
            var so = new SerializedObject(player);
            var autoPlay = so.FindProperty("AutoPlayOnStart") ?? so.FindProperty("m_AutoPlayOnStart");
            if (autoPlay != null)
                autoPlay.boolValue = true;

            // InitializationMode / CanPlayWhileAlreadyPlaying vary by Feel version — ignore if missing.
            so.ApplyModifiedPropertiesWithoutUndo();

            // Adding feedbacks via reflection is version-fragile; leave AddFeedback to the designer
            // with clear dialog instructions. If AddFeedback(Type) exists, try it.
            if (squashType == null)
                return;

            var add = player.GetType().GetMethod("AddFeedback", new[] { typeof(Type), typeof(bool) })
                      ?? player.GetType().GetMethod("AddFeedback", new[] { typeof(Type) });
            if (add == null)
                return;

            try
            {
                object feedback = add.GetParameters().Length == 2
                    ? add.Invoke(player, new object[] { squashType, true })
                    : add.Invoke(player, new object[] { squashType });

                if (feedback is UnityEngine.Object feedbackObj)
                {
                    var fso = new SerializedObject(feedbackObj);
                    var targetProp = fso.FindProperty("SquashAndStretchTarget")
                                     ?? fso.FindProperty("AnimateScaleTarget")
                                     ?? fso.FindProperty("m_SquashAndStretchTarget");
                    if (targetProp != null)
                        targetProp.objectReferenceValue = target;
                    var duration = fso.FindProperty("AnimateScaleDuration");
                    if (duration != null)
                        duration.floatValue = 1.6f;
                    fso.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PolyPets] Could not auto-add SquashAndStretch feedback: {ex.Message}");
            }
        }

        public static bool IsFeelPresent(out string detail)
        {
            var mmf = FindType("MoreMountains.Feedbacks.MMF_Player");
            var legacy = FindType("MoreMountains.Feedbacks.MMFeedbacks");
            var squash = FindType("MoreMountains.Feedbacks.MMF_SquashAndStretch");

            if (mmf == null && legacy == null)
            {
                detail = "No MoreMountains.Feedbacks types in loaded assemblies.";
                return false;
            }

            detail = $"MMF_Player: {(mmf != null ? mmf.Assembly.GetName().Name : "n/a")}\n" +
                     $"MMFeedbacks: {(legacy != null ? legacy.Assembly.GetName().Name : "n/a")}\n" +
                     $"MMF_SquashAndStretch: {(squash != null ? "yes" : "no")}";
            return true;
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = null;
                try { type = assembly.GetType(fullName, throwOnError: false); }
                catch { /* ignore dynamic assemblies */ }
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}
#endif
