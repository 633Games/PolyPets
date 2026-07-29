using UnityEngine;

namespace PolyPets.Feel
{
    /// <summary>Soft looping wobble (position + slight roll). Feel analogue: Transform/Wiggle.</summary>
    [DisallowMultipleComponent]
    public sealed class FeelIdleWobble : MonoBehaviour
    {
        [SerializeField] private Vector3 positionAmplitude = new(0.03f, 0.02f, 0f);
        [SerializeField] private Vector3 rotationAmplitude = new(0f, 0f, 4f);
        [SerializeField] private float period = 2.2f;
        [SerializeField] private bool playOnEnable = true;

        private Vector3 _basePos;
        private Quaternion _baseRot;
        private float _time;
        private bool _playing;

        private void Awake()
        {
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
        }

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        private void OnDisable() => Stop();

        public void Play() { _playing = true; _time = 0f; }

        public void Stop()
        {
            _playing = false;
            transform.localPosition = _basePos;
            transform.localRotation = _baseRot;
        }

        private void LateUpdate()
        {
            if (!_playing) return;
            _time += Time.deltaTime;
            float t = _time / Mathf.Max(0.05f, period) * Mathf.PI * 2f;
            transform.localPosition = _basePos + new Vector3(
                Mathf.Sin(t) * positionAmplitude.x,
                Mathf.Sin(t * 1.3f) * positionAmplitude.y,
                Mathf.Cos(t * 0.7f) * positionAmplitude.z);
            transform.localRotation = _baseRot * Quaternion.Euler(
                Mathf.Sin(t * 0.8f) * rotationAmplitude.x,
                Mathf.Cos(t) * rotationAmplitude.y,
                Mathf.Sin(t * 1.1f) * rotationAmplitude.z);
        }
    }

    /// <summary>Vertical bounce idle.</summary>
    [DisallowMultipleComponent]
    public sealed class FeelIdleBounce : MonoBehaviour
    {
        [SerializeField] private float height = 0.08f;
        [SerializeField] private float period = 0.9f;
        [SerializeField] private bool playOnEnable = true;

        private Vector3 _basePos;
        private float _time;
        private bool _playing;

        private void Awake() => _basePos = transform.localPosition;
        private void OnEnable() { if (playOnEnable) Play(); }
        private void OnDisable() => Stop();
        public void Play() { _playing = true; _time = 0f; }
        public void Stop() { _playing = false; transform.localPosition = _basePos; }

        private void LateUpdate()
        {
            if (!_playing) return;
            _time += Time.deltaTime;
            float y = Mathf.Abs(Mathf.Sin(_time / Mathf.Max(0.05f, period) * Mathf.PI)) * height;
            transform.localPosition = _basePos + Vector3.up * y;
        }
    }

    /// <summary>One-shot or looping punch scale.</summary>
    [DisallowMultipleComponent]
    public sealed class FeelPunchScale : MonoBehaviour
    {
        [SerializeField] private float punch = 0.12f;
        [SerializeField] private float duration = 0.25f;
        [SerializeField] private bool playOnEnable;
        [SerializeField] private bool loop;

        private Vector3 _baseScale;
        private float _time;
        private bool _playing;

        private void Awake() => _baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        private void OnEnable() { if (playOnEnable) Play(); }
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

        private void LateUpdate()
        {
            if (!_playing) return;
            _time += Time.deltaTime;
            float u = Mathf.Clamp01(_time / Mathf.Max(0.01f, duration));
            float wave = Mathf.Sin(u * Mathf.PI);
            transform.localScale = _baseScale * (1f + wave * punch);
            if (u >= 1f)
            {
                if (loop) _time = 0f;
                else Stop();
            }
        }
    }

    /// <summary>Light positional shake.</summary>
    [DisallowMultipleComponent]
    public sealed class FeelShake : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.04f;
        [SerializeField] private float frequency = 18f;
        [SerializeField] private bool playOnEnable;
        [SerializeField] private bool loop = true;
        [SerializeField] private float duration = 0.35f;

        private Vector3 _basePos;
        private float _time;
        private bool _playing;

        private void Awake() => _basePos = transform.localPosition;
        private void OnEnable() { if (playOnEnable) Play(); }
        private void OnDisable() => Stop();
        public void Play() { _playing = true; _time = 0f; }
        public void Stop() { _playing = false; transform.localPosition = _basePos; }

        private void LateUpdate()
        {
            if (!_playing) return;
            _time += Time.deltaTime;
            float damp = loop ? 1f : 1f - Mathf.Clamp01(_time / Mathf.Max(0.01f, duration));
            float t = _time * frequency;
            transform.localPosition = _basePos + new Vector3(
                Mathf.Sin(t * 1.7f), Mathf.Cos(t * 1.3f), 0f) * (amplitude * damp);
            if (!loop && _time >= duration) Stop();
        }
    }

    /// <summary>Pop-in scale from zero-ish.</summary>
    [DisallowMultipleComponent]
    public sealed class FeelPopIn : MonoBehaviour
    {
        [SerializeField] private float duration = 0.35f;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 _baseScale;
        private float _time;
        private bool _playing;

        private void Awake() => _baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        private void OnEnable() { if (playOnEnable) Play(); }
        public void Play() { _playing = true; _time = 0f; transform.localScale = Vector3.zero; }
        public void Stop() { _playing = false; transform.localScale = _baseScale; }

        private void LateUpdate()
        {
            if (!_playing) return;
            _time += Time.deltaTime;
            float u = Mathf.Clamp01(_time / Mathf.Max(0.01f, duration));
            float s = curve != null && curve.length > 0 ? curve.Evaluate(u) : u;
            // Overshoot pop
            float pop = s + Mathf.Sin(s * Mathf.PI) * 0.08f;
            transform.localScale = _baseScale * pop;
            if (u >= 1f) Stop();
        }
    }
}
