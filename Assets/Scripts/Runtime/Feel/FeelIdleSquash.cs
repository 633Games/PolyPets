using UnityEngine;

namespace PolyPets.Feel
{
    /// <summary>
    /// Built-in looping squash &amp; stretch idle. Matches Feel's recommended hierarchy:
    /// Character (1,1,1) → FEEL[Squash] container (1,1,1, this component) → Model.
    /// Docs: https://feel-docs.moremountains.com/list_mmfeedbacks.html (SquashAndStretch note)
    /// When More Mountains Feel is imported, the editor upgrader can swap this for an MMF Player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FeelIdleSquash : MonoBehaviour
    {
        public enum AxisMode
        {
            YtoXZ = 0,
            XtoYZ = 1,
            ZtoXY = 2,
        }

        [SerializeField] private AxisMode axis = AxisMode.YtoXZ;
        [SerializeField] private float period = 1.6f;
        [SerializeField] private float intensity = 0.08f;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool useUnscaledTime;

        private Vector3 _baseScale = Vector3.one;
        private float _time;
        private bool _playing;

        private void Awake()
        {
            _baseScale = transform.localScale;
            if (_baseScale == Vector3.zero)
                _baseScale = Vector3.one;
        }

        private void OnEnable()
        {
            if (playOnEnable)
                Play();
        }

        private void OnDisable() => Stop();

        public void Play()
        {
            _playing = true;
            _time = 0f;
        }

        public void Stop()
        {
            _playing = false;
            transform.localScale = _baseScale;
        }

        public void Configure(float newPeriod, float newIntensity, bool unscaled = false)
        {
            period = Mathf.Max(0.05f, newPeriod);
            intensity = Mathf.Max(0f, newIntensity);
            useUnscaledTime = unscaled;
        }

        private void LateUpdate()
        {
            if (!_playing || period <= 0.0001f)
                return;

            _time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float wave = Mathf.Sin((_time / period) * Mathf.PI * 2f);
            float primary = 1f + wave * intensity;
            // Conserve volume-ish: secondary axes compensate (Feel SquashAndStretch idea).
            float secondary = 1f - wave * intensity * 0.5f;

            Vector3 scale = axis switch
            {
                AxisMode.XtoYZ => new Vector3(_baseScale.x * primary, _baseScale.y * secondary, _baseScale.z * secondary),
                AxisMode.ZtoXY => new Vector3(_baseScale.x * secondary, _baseScale.y * secondary, _baseScale.z * primary),
                _ => new Vector3(_baseScale.x * secondary, _baseScale.y * primary, _baseScale.z * secondary),
            };

            transform.localScale = scale;
        }
    }
}
