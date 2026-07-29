#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// Ensures Unity 6.3 URP pipeline assets exist and are assigned for cel + post-processing.
    /// </summary>
    public static class PolyPetsUrpSetup
    {
        private const string RendererPath = "Assets/Settings/PolyPets_URP_Renderer.asset";
        private const string PipelinePath = "Assets/Settings/PolyPets_URP.asset";

        [MenuItem("PolyPets/Ensure URP Pipeline Assets", priority = 40)]
        public static void EnsureUrpPipelineAssetsMenu()
        {
            EnsureUrpPipelineAssets(showDialog: true);
        }

        public static void EnsureUrpPipelineAssets(bool showDialog = false)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            // Desktop companion: HDR for bloom, soft shadows for cel bands, modest additional lights (lamp).
            var so = new SerializedObject(pipeline);
            SetBool(so, "m_SupportsHDR", true);
            SetFloat(so, "m_RenderScale", 1f);
            SetInt(so, "m_MainLightShadowmapResolution", 2048);
            SetInt(so, "m_AdditionalLightsRenderingMode", 1); // PerPixel
            SetInt(so, "m_AdditionalLightsShadowResolutionTier", 1);
            so.ApplyModifiedPropertiesWithoutUndo();

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();

            Debug.Log($"[PolyPets] URP assigned.\nRenderer: {RendererPath}\nPipeline: {PipelinePath}");
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "PolyPets URP",
                    "Universal Render Pipeline assets are ready and assigned.\n\n" +
                    "HDR on · additional lights on (for the lamp) · soft shadows enabled.",
                    "OK");
            }
        }

        private static void SetBool(SerializedObject so, string name, bool value)
        {
            var prop = so.FindProperty(name);
            if (prop != null)
                prop.boolValue = value;
        }

        private static void SetFloat(SerializedObject so, string name, float value)
        {
            var prop = so.FindProperty(name);
            if (prop != null)
                prop.floatValue = value;
        }

        private static void SetInt(SerializedObject so, string name, int value)
        {
            var prop = so.FindProperty(name);
            if (prop != null)
                prop.intValue = value;
        }
    }
}
#endif
