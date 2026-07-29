using UnityEngine;

namespace PolyPets.Feel
{
    /// <summary>
    /// Marks a transform as a Feel juice target. Prefer naming the object <c>FEEL[Squash]</c>;
    /// this component is added automatically by the binder / bootstrap.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FeelTag : MonoBehaviour
    {
        [SerializeField] private FeelTagType tagType = FeelTagType.Squash;
        [Tooltip("When true, Play() runs on Enable (good for looping idles).")]
        [SerializeField] private bool autoPlay = true;
        [SerializeField] private Transform effectTarget;

        public FeelTagType TagType => tagType;
        public bool AutoPlay => autoPlay;
        public Transform EffectTarget => effectTarget != null ? effectTarget : transform;

        public void Configure(FeelTagType type, bool playOnEnable = true, Transform target = null)
        {
            tagType = type;
            autoPlay = playOnEnable;
            effectTarget = target != null ? target : transform;
        }

        private void Reset()
        {
            if (FeelTagNaming.TryParseObjectName(gameObject.name, out var parsed))
                tagType = parsed;
            effectTarget = transform;
        }

        private void OnEnable()
        {
            if (autoPlay)
                FeelTagBinder.ApplyAndPlay(this);
            else
                FeelTagBinder.Apply(this);
        }

        public void Play() => FeelTagBinder.Play(this);
        public void Stop() => FeelTagBinder.Stop(this);
    }
}
