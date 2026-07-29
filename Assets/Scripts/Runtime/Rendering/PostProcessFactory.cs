using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PolyPets.Rendering
{
    /// <summary>
    /// Factory helpers for the desktop companion look: soft bloom, vignette, cel-friendly grading.
    /// </summary>
    public static class PostProcessFactory
    {
        public const string DefaultProfilePath = "Assets/Settings/PolyPets_VolumeProfile.asset";

        public static void PopulateProfile(VolumeProfile profile)
        {
            if (profile == null)
                return;

            ClearOverrides(profile);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.45f);
            bloom.scatter.Override(0.65f);
            bloom.tint.Override(new Color(1f, 0.85f, 0.65f));

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.55f);
            vignette.color.Override(new Color(0.08f, 0.05f, 0.06f));

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.4f);
            color.contrast.Override(18f);
            color.saturation.Override(12f);
            color.colorFilter.Override(new Color(1f, 0.97f, 0.94f));

            var whiteBalance = profile.Add<WhiteBalance>(true);
            whiteBalance.temperature.Override(8f);
            whiteBalance.tint.Override(2f);

            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.Override(TonemappingMode.Neutral);

            var liftGammaGain = profile.Add<LiftGammaGain>(true);
            liftGammaGain.gamma.Override(new Vector4(1f, 1f, 1f, -0.02f));
            liftGammaGain.gain.Override(new Vector4(1f, 1f, 1f, 0.05f));
        }

        public static VolumeProfile CreateDefaultProfile()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "PolyPets_VolumeProfile";
            PopulateProfile(profile);
            return profile;
        }

        public static void EnableCameraPostProcessing(Camera camera, bool hdr = true)
        {
            if (camera == null)
                return;

            camera.allowHDR = hdr;
            camera.allowMSAA = false;

            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.renderType = CameraRenderType.Base;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            data.antialiasingQuality = AntialiasingQuality.Medium;
        }

        private static void ClearOverrides(VolumeProfile profile)
        {
            if (profile.components == null)
                return;

            for (int i = profile.components.Count - 1; i >= 0; i--)
            {
                var component = profile.components[i];
                profile.Remove(component.GetType());
            }
        }
    }
}
