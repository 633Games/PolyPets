using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace PolyPets.Rendering
{
    /// <summary>
    /// Drives sun / fill / lamp / ambient / post exposure across a looping day.
    /// Tuned for a cozy cel-shaded desktop house (not a full outdoor sim).
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
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Volume globalVolume;
        [FormerlySerializedAs("onPhaseChanged")]
        [SerializeField] private bool debugLogPhase;

        private Phase _phase = Phase.Day;
        private ColorAdjustmentsDriver _colorDriver;
        private PolyPets.House.RoomLamp[] _roomLamps;
        private float _roomLampRefreshAt;

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
            Apply(timeOfDay);
        }

        private void Update()
        {
            if (!Application.isPlaying && !editorPreview)
                return;

            if (running && Application.isPlaying && dayLengthSeconds > 0.01f)
            {
                timeOfDay += Time.deltaTime / dayLengthSeconds;
                if (timeOfDay >= 1f)
                    timeOfDay -= Mathf.Floor(timeOfDay);
            }

            Apply(timeOfDay);
        }

        public void SetRunning(bool value) => running = value;

        public void SetTimeOfDay(float value01)
        {
            EnsureDefaults();
            timeOfDay = Mathf.Repeat(value01, 1f);
            Apply(timeOfDay);
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

        public void Apply(float t)
        {
            EnsureDefaults();
            t = Mathf.Repeat(t, 1f);

            // Sun orbits: midnight below horizon, noon overhead-ish for the room.
            float sunAngle = t * 360f - 90f;
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunOrbitAxis.normalized * sunAngle);
                sunLight.intensity = EvaluateOrDefault(sunIntensity, t, 1f);
                sunLight.color = sunColor != null ? sunColor.Evaluate(t) : Color.white;
                sunLight.enabled = sunLight.intensity > 0.01f;
            }

            if (fillLight != null)
            {
                fillLight.intensity = EvaluateOrDefault(fillIntensity, t, 0.3f);
                fillLight.color = Color.Lerp(
                    new Color(0.35f, 0.4f, 0.55f),
                    new Color(0.7f, 0.75f, 0.85f),
                    DayFactor(t));
            }

            float lampI = EvaluateOrDefault(lampIntensity, t, 1f);
            var lampColor = Color.Lerp(
                new Color(1f, 0.72f, 0.4f),
                new Color(1f, 0.85f, 0.65f),
                DayFactor(t));

            if (lampLight != null)
            {
                lampLight.intensity = lampI;
                lampLight.color = lampColor;
            }

            if (_roomLamps == null || Time.unscaledTime >= _roomLampRefreshAt)
            {
                _roomLamps = Object.FindObjectsByType<PolyPets.House.RoomLamp>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                _roomLampRefreshAt = Time.unscaledTime + 1.5f;
            }

            for (int i = 0; i < _roomLamps.Length; i++)
            {
                var rl = _roomLamps[i]?.LampLight;
                if (rl == null || rl == lampLight)
                    continue;
                rl.intensity = lampI;
                rl.color = lampColor;
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
            // 1 around noon, 0 at midnight.
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

            if (!Application.isPlaying)
            {
                _colorDriver = ColorAdjustmentsDriver.FromVolume(globalVolume);
                Apply(timeOfDay);
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

            if (_profile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments color))
            {
                color.active = true;
                color.postExposure.overrideState = true;
                color.postExposure.value = exposure;
            }

            if (_profile.TryGet(out UnityEngine.Rendering.Universal.WhiteBalance whiteBalance))
            {
                whiteBalance.active = true;
                whiteBalance.temperature.overrideState = true;
                whiteBalance.temperature.value = temperature;
            }
        }
    }
}
