using UnityEngine;

namespace PolyPets.Audio
{
    /// <summary>
    /// Loops cozy background ambience. Assign Amb_CozyHouse_CC0 (or SoftPad) in the inspector / bootstrap.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AmbientAudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip primaryLoop;
        [SerializeField] private AudioClip softPadLoop;
        [SerializeField] [Range(0f, 1f)] private float primaryVolume = 0.28f;
        [SerializeField] [Range(0f, 1f)] private float padVolume = 0.12f;
        [SerializeField] private bool playOnStart = true;

        private AudioSource _primary;
        private AudioSource _pad;

        private void Awake()
        {
            _primary = GetComponent<AudioSource>();
            _primary.playOnAwake = false;
            _primary.loop = true;
            _primary.spatialBlend = 0f;

            var padGo = new GameObject("SoftPadSource");
            padGo.transform.SetParent(transform, false);
            _pad = padGo.AddComponent<AudioSource>();
            _pad.playOnAwake = false;
            _pad.loop = true;
            _pad.spatialBlend = 0f;
        }

        private void Start()
        {
            if (playOnStart)
                Play();
        }

        public void BindClips(AudioClip houseAmbience, AudioClip softPad = null)
        {
            primaryLoop = houseAmbience;
            softPadLoop = softPad;
        }

        public void Play()
        {
            if (_primary == null)
                return;

            if (primaryLoop != null)
            {
                _primary.clip = primaryLoop;
                _primary.volume = primaryVolume;
                if (!_primary.isPlaying)
                    _primary.Play();
            }

            if (_pad != null && softPadLoop != null)
            {
                _pad.clip = softPadLoop;
                _pad.volume = padVolume;
                if (!_pad.isPlaying)
                    _pad.Play();
            }
        }

        public void Stop()
        {
            _primary?.Stop();
            _pad?.Stop();
        }

        public void SetMuted(bool muted)
        {
            if (_primary != null)
                _primary.mute = muted;
            if (_pad != null)
                _pad.mute = muted;
        }
    }
}
