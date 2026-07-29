using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace PolyPets.Rendering
{
    /// <summary>
    /// Drives sun / fill / lamp / ambient / post exposure across a looping day.
    /// Tuned for a cozy cel-shaded desktop house (not a full outdoor sim).
    /// Visuals update on a throttle — applying every frame made the companion feel laggy.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class DayNightCycle : MonoBehaviour
    {
        public enum Phase
        {
            Night,
            Dawn,
            Day,
            Dusk
        }

        [Header("Time")]
        [Tooltip("Real seconds for a full 24h loop. Desktop companion default ~8 minutes.")]
        [SerializeField] private float dayLengthSeconds = 480f;
        [Range(0f, 1f)]
        [SerializeField] private float timeOfDay = 0.35f;
        [SerializeField] private bool running = true;
        [SerializeField] private bool editorPreview = true;
        [Tooltip("How often lighting/post is refreshed while playing. Lower = smoother day cycle, higher = cheaper.")]
        [SerializeField] private float visualUpdateInterval = 0.25f;

        [Header("Lights")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Light fillLight;
        [SerializeField] private Light lampLight;
        [SerializeField] private Vector3 sunOrbitAxis = new(1f, 0.15f, 0f);

        [Header("Curves (0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset)")]
        [SerializeField] private AnimationCurve sunIntensity = AnimationCurve.EaseInOut(0f, 0.05f, 0.5f, 1.15f);
        [SerializeField] private AnimationCurve fillIntensity = AnimationCurve.Linear(0f, 0.12f, 1f, 0.12f);
        [SerializeField] private AnimationCurve lampIntensity = AnimationCurve.EaseInOut(0f, 2.2f, 0.5f, 0.35f);
        [SerializeField] private Gradient sunColor;
        [SerializeField] private Gradient ambientColor;
        [SerializeField] private Gradient cameraClearColor;
        [SerializeField] private AnimationCurve postExposure = AnimationCurve.Linear(0f, 0.15f, 0.5f, 0.55f);
        [SerializeField] private AnimationCurve whiteBalanceTemp = AnimationCurve.Linear(0f, -8f, 0.5f, 12f);

        [Header("Outputs")]
        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private Volume globalVolume;
        [FormerlySerializedAs("onPhaseChanged")]
        [SerializeField] private bool debugLogPhase;

        private Phase _phase = Phase.Day;
        private ColorAdjustmentsDriver _colorDriver;
        private float _visualTimer;
        private float _lastAppliedTime = -999f;
        private bool _sunWasEnabled = true;

        public float TimeOfDay01 => timeOfDay;
        public float DayLengthSeconds => dayLengthSeconds;
        public Phase CurrentPhase => _phase;
        public bool IsRunning => running;

        public event Action<Phase> PhaseChanged;

        private void Reset()
        {
            BuildDefaultGradients();
        }

        private void Awake()
        {
            EnsureDefaults();
            _colorDriver = ColorAdjustmentsDriver.FromVolume(globalVolume);
            SoftenSunShadows();
            Apply(timeOfDay, force: true);
        }

        private void Update()
        {
            // Edit-mode scrubbing is OnValidate-only — Update every frame in the editor was a big hitch.
            if (!Application.isPlaying)
                return;

            if (running && dayLengthSeconds > 0.01f)
            {
                timeOfDay += Time.deltaTime / dayLengthSeconds;
                if (timeOfDay >= 1f)
                    timeOfDay -= Mathf.Floor(timeOfDay);
            }

            float interval = Mathf.Max(0.05f, visualUpdateInterval);
            _visualTimer += Time.deltaTime;
            if (_visualTimer < interval && Mathf.Abs(timeOfDay - _lastAppliedTime) < 0.002f)
                return;

            _visualTimer = 0f;
            Apply(timeOfDay);
        }

        public void SetRunning(bool value) => running = value;

        public void SetTimeOfDay(float value01)
        {
            EnsureDefaults();
            timeOfDay = Mathf.Repeat(value01, 1f);
            Apply(timeOfDay, force: true);
        }

        public void SkipTo(Phase phase)
        {
            SetTimeOfDay(phase switch
            {
                Phase.Night => 0.0f,
                Phase.Dawn => 0.22f,
                Phase.Day => 0.45f,
                Phase.Dusk => 0.72f,
                _ => 0.45f
            });
        }

        public void Apply(float t) => Apply(t, force: false);

        private void Apply(float t, bool force)
        {
            EnsureDefaults();
            t = Mathf.Repeat(t, 1f);

            if (!force && Mathf.Abs(t - _lastAppliedTime) < 0.0005f)
                return;
            _lastAppliedTime = t;

            // Sun orbits: midnight below horizon, noon overhead-ish for the room.
            float sunAngle = t * 360f - 90f;
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunOrbitAxis.normalized * sunAngle);
                sunLight.intensity = EvaluateOrDefault(sunIntensity, t, 1f);
                sunLight.color = sunColor != null ? sunColor.Evaluate(t) : Color.white;
                bool on = sunLight.intensity > 0.01f;
                if (on != _sunWasEnabled)
                {
                    sunLight.enabled = on;
                    _sunWasEnabled = on;
                }
            }

            if (fillLight != null)
            {
                fillLight.intensity = EvaluateOrDefault(fillIntensity, t, 0.3f);
                fillLight.color = Color.Lerp(
                    new Color(0.35f, 0.4f, 0.55f),
                    new Color(0.7f, 0.75f, 0.85f),
                    DayFactor(t));
            }

            if (lampLight != null)
            {
                lampLight.intensity = EvaluateOrDefault(lampIntensity, t, 1f);
                lampLight.color = Color.Lerp(
                    new Color(1f, 0.72f, 0.4f),
                    new Color(1f, 0.85f, 0.65f),
                    DayFactor(t));
            }

            if (ambientColor != null)
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = ambientColor.Evaluate(t);
            }

            if (targetCamera != null && cameraClearColor != null)
                targetCamera.backgroundColor = cameraClearColor.Evaluate(t);

            _colorDriver?.Apply(
                EvaluateOrDefault(postExposure, t, 0.4f),
                EvaluateOrDefault(whiteBalanceTemp, t, 0f));

            var next = EvaluatePhase(t);
            if (next != _phase)
            {
                _phase = next;
                if (debugLogPhase)
                    Debug.Log($"[PolyPets] Day/Night → {_phase} (t={t:0.00})");
                PhaseChanged?.Invoke(_phase);
            }
        }

        private void SoftenSunShadows()
        {
            if (sunLight == null)
                return;
            // Keep soft shadows in Play; hard shadows + a moving sun is what felt expensive.
            if (sunLight.shadows == LightShadows.None)
                sunLight.shadows = LightShadows.Soft;
            else if (sunLight.shadows == LightShadows.Hard)
                sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = Mathf.Clamp(sunLight.shadowStrength <= 0.01f ? 0.65f : sunLight.shadowStrength, 0.35f, 0.85f);
        }

        public static Phase EvaluatePhase(float t)
        {
            t = Mathf.Repeat(t, 1f);
            if (t < 0.18f || t >= 0.82f) return Phase.Night;
            if (t < 0.30f) return Phase.Dawn;
            if (t < 0.68f) return Phase.Day;
            return Phase.Dusk;
        }

        private void EnsureDefaults()
        {
            if (sunColor == null || sunColor.colorKeys == null || sunColor.colorKeys.Length == 0
                || ambientColor == null || cameraClearColor == null
                || sunIntensity == null || sunIntensity.length == 0)
            {
                BuildDefaultGradients();
            }
        }

        private static float DayFactor(float t)
        {
            return Mathf.Clamp01(1f - Mathf.Abs(t - 0.5f) * 2f);
        }

        private static float EvaluateOrDefault(AnimationCurve curve, float t, float fallback)
        {
            return curve != null && curve.length > 0 ? curve.Evaluate(t) : fallback;
        }

        private void BuildDefaultGradients()
        {
            sunColor = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.25f, 0.3f, 0.55f), 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.35f), 0.22f),
                    new GradientColorKey(new Color(1f, 0.96f, 0.88f), 0.5f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.3f), 0.74f),
                    new GradientColorKey(new Color(0.25f, 0.3f, 0.55f), 1f),
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                }
            };

            ambientColor = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.12f, 0.13f, 0.22f), 0f),
                    new GradientColorKey(new Color(0.45f, 0.35f, 0.32f), 0.22f),
                    new GradientColorKey(new Color(0.42f, 0.4f, 0.38f), 0.5f),
                    new GradientColorKey(new Color(0.4f, 0.28f, 0.28f), 0.74f),
                    new GradientColorKey(new Color(0.12f, 0.13f, 0.22f), 1f),
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                }
            };

            cameraClearColor = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.06f, 0.07f, 0.12f), 0f),
                    new GradientColorKey(new Color(0.35f, 0.28f, 0.3f), 0.22f),
                    new GradientColorKey(new Color(0.22f, 0.2f, 0.18f), 0.5f),
                    new GradientColorKey(new Color(0.28f, 0.14f, 0.16f), 0.74f),
                    new GradientColorKey(new Color(0.06f, 0.07f, 0.12f), 1f),
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                }
            };

            sunIntensity = new AnimationCurve(
                new Keyframe(0f, 0.04f),
                new Keyframe(0.18f, 0.15f),
                new Keyframe(0.28f, 0.85f),
                new Keyframe(0.5f, 1.2f),
                new Keyframe(0.72f, 0.7f),
                new Keyframe(0.82f, 0.12f),
                new Keyframe(1f, 0.04f));

            fillIntensity = new AnimationCurve(
                new Keyframe(0f, 0.18f),
                new Keyframe(0.5f, 0.4f),
                new Keyframe(1f, 0.18f));

            lampIntensity = new AnimationCurve(
                new Keyframe(0f, 2.4f),
                new Keyframe(0.25f, 1.1f),
                new Keyframe(0.5f, 0.35f),
                new Keyframe(0.75f, 1.4f),
                new Keyframe(1f, 2.4f));

            postExposure = new AnimationCurve(
                new Keyframe(0f, 0.05f),
                new Keyframe(0.5f, 0.55f),
                new Keyframe(1f, 0.05f));

            whiteBalanceTemp = new AnimationCurve(
                new Keyframe(0f, -12f),
                new Keyframe(0.25f, 8f),
                new Keyframe(0.5f, 14f),
                new Keyframe(0.75f, 5f),
                new Keyframe(1f, -12f));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureDefaults();
            visualUpdateInterval = Mathf.Max(0.05f, visualUpdateInterval);

            if (!Application.isPlaying && editorPreview)
            {
                _colorDriver = ColorAdjustmentsDriver.FromVolume(globalVolume);
                Apply(timeOfDay, force: true);
            }
        }
#endif
    }

    /// <summary>
    /// Thin wrapper so DayNightCycle can drive URP Color Adjustments / White Balance without
    /// hard-serializing override refs that break when the volume profile is rebuilt.
    /// </summary>
    internal sealed class ColorAdjustmentsDriver
    {
        private readonly VolumeProfile _profile;
        private UnityEngine.Rendering.Universal.ColorAdjustments _color;
        private UnityEngine.Rendering.Universal.WhiteBalance _whiteBalance;
        private bool _resolved;

        private ColorAdjustmentsDriver(VolumeProfile profile)
        {
            _profile = profile;
        }

        public static ColorAdjustmentsDriver FromVolume(Volume volume)
        {
            if (volume == null || volume.profile == null)
                return null;
            return new ColorAdjustmentsDriver(volume.profile);
        }

        public void Apply(float exposure, float temperature)
        {
            if (_profile == null)
                return;

            if (!_resolved)
            {
                _profile.TryGet(out _color);
                _profile.TryGet(out _whiteBalance);
                _resolved = true;
            }

            if (_color != null)
            {
                _color.active = true;
                _color.postExposure.overrideState = true;
                _color.postExposure.value = exposure;
            }

            if (_whiteBalance != null)
            {
                _whiteBalance.active = true;
                _whiteBalance.temperature.overrideState = true;
                _whiteBalance.temperature.value = temperature;
            }
        }
    }
}
