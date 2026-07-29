using UnityEngine;
using UnityEngine.UI;
using PolyPets.Needs;

namespace PolyPets.Pets
{
    /// <summary>
    /// Hold-click / drag on the pet to scrub dirt off via shader scrub mask.
    /// </summary>
    public sealed class PetCleanScrubber : MonoBehaviour
    {
        public static PetCleanScrubber Instance { get; private set; }

        [SerializeField] private Camera rayCamera;
        [SerializeField] private LayerMask petMask = ~0;
        [SerializeField] private float scrubRadiusUv = 0.09f;
        [SerializeField] private float cleanlinessPerSecond = 22f;
        [SerializeField] private Text statusHint;

        private PetAgent _target;
        private PetDirtVisual _dirt;
        private bool _scrubbing;
        private bool _sessionActive;
        private float _nextScrubSfx;

        public bool IsScrubMode { get; private set; }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BindHint(Text hint) => statusHint = hint;

        public void BeginClean(PetAgent pet)
        {
            if (pet == null)
                return;

            _target = pet;
            _dirt = pet.GetComponent<PetDirtVisual>() ?? pet.gameObject.AddComponent<PetDirtVisual>();
            _dirt.BindNeeds(pet.Needs);
            _dirt.CaptureRenderersFromHierarchy();
            _dirt.ClearScrubMask();
            _dirt.PushShaderState();
            IsScrubMode = true;
            _sessionActive = true;
            _nextScrubSfx = 0f;
            PolyPets.Audio.JuicySfx.PlayPop();
            if (statusHint != null)
                statusHint.text = "Scrub mode — click & drag on your pet to wipe dirt.";
        }

        public void CancelClean()
        {
            if (_sessionActive && _target != null && _dirt != null)
                FinishSession();
            else
                EndMode();
        }

        private void Update()
        {
            if (!IsScrubMode || _target == null)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                FinishSession();
                return;
            }

            if (Input.GetMouseButton(0))
            {
                if (UnityEngine.EventSystems.EventSystem.current != null
                    && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return;

                if (TryRaycastPet(out var hit))
                {
                    _scrubbing = true;
                    var uv = hit.textureCoord;
                    if (uv == Vector2.zero)
                        uv = new Vector2(0.5f, 0.5f);
                    _dirt.PaintScrub(uv, scrubRadiusUv);
                    _target.Needs?.ApplyScrub(cleanlinessPerSecond * Time.deltaTime);
                    if (Time.unscaledTime >= _nextScrubSfx)
                    {
                        PolyPets.Audio.JuicySfx.PlayScrubTick();
                        _nextScrubSfx = Time.unscaledTime + 0.09f;
                    }
                }
            }
            else if (_scrubbing && Input.GetMouseButtonUp(0))
            {
                _scrubbing = false;
            }

            // Auto-finish when mostly clean.
            if (_target.Needs != null && _target.Needs.Cleanliness >= 96f)
                FinishSession();
        }

        private bool TryRaycastPet(out RaycastHit hit)
        {
            hit = default;
            var cam = rayCamera != null ? rayCamera : Camera.main;
            if (cam == null || _target == null)
                return false;

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray, 50f, petMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform != null && hits[i].transform.IsChildOf(_target.transform))
                {
                    hit = hits[i];
                    return true;
                }
            }

            return false;
        }

        private void FinishSession()
        {
            if (_dirt != null && _target?.Needs != null)
            {
                float coverage = _dirt.MeasureScrubCoverage();
                _target.Needs.FinishCleaningSession(coverage);
                _dirt.ClearScrubMask();
                _dirt.PushShaderState();
                PolyPets.Audio.JuicySfx.PlayClean();
                if (coverage > 0.35f)
                    PolyPets.Audio.JuicySfx.PlayCoinDing();
                if (statusHint != null)
                {
                    statusHint.text = coverage > 0.15f
                        ? $"{_target.PetName} is fresher! Clean { _target.Needs.Cleanliness:0}"
                        : "A light wipe — keep scrubbing next time.";
                }
            }

            EndMode();
        }

        private void EndMode()
        {
            IsScrubMode = false;
            _sessionActive = false;
            _scrubbing = false;
            _target = null;
            _dirt = null;
        }
    }
}
