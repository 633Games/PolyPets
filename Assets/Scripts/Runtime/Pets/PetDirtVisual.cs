using UnityEngine;
using PolyPets.Needs;

namespace PolyPets.Pets
{
    /// <summary>
    /// Instances pet materials and drives cel-shader dirt + scrub mask from PetNeeds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PetDirtVisual : MonoBehaviour
    {
        [SerializeField] private PetNeeds needs;
        [SerializeField] private Texture2D dirtMap;
        [SerializeField] private Color dirtColor = new(0.22f, 0.16f, 0.12f, 1f);
        [SerializeField] private float dirtStrength = 0.88f;
        [SerializeField] private float dirtTiling = 2.5f;
        [SerializeField] private int scrubResolution = 128;

        private Material[] _instances;
        private Renderer[] _renderers;
        private Texture2D _scrubMask;
        private Color32[] _scrubPixels;
        private bool _scrubDirty;
        private float _scrubGlow;

        public Texture2D ScrubMask => _scrubMask;

        private void Awake()
        {
            needs ??= GetComponent<PetNeeds>();
            EnsureInstances();
            EnsureScrubMask();
            if (needs != null)
                needs.NeedsChanged += OnNeedsChanged;
            PushShaderState();
        }

        private void OnDestroy()
        {
            if (needs != null)
                needs.NeedsChanged -= OnNeedsChanged;
            DestroyInstances();
        }

        private void LateUpdate()
        {
            if (_scrubDirty)
            {
                _scrubMask.Apply(false);
                _scrubDirty = false;
            }

            if (_scrubGlow > 0f)
            {
                _scrubGlow = Mathf.MoveTowards(_scrubGlow, 0f, Time.deltaTime * 1.5f);
                SetFloatAll("_ScrubGlow", _scrubGlow);
            }
        }

        public void BindNeeds(PetNeeds petNeeds)
        {
            if (needs != null)
                needs.NeedsChanged -= OnNeedsChanged;
            needs = petNeeds;
            if (needs != null)
                needs.NeedsChanged += OnNeedsChanged;
            PushShaderState();
        }

        public void CaptureRenderersFromHierarchy()
        {
            DestroyInstances();
            EnsureInstances();
            PushShaderState();
        }

        private void OnNeedsChanged(PetNeeds _) => PushShaderState();

        private void EnsureInstances()
        {
            if (_instances != null)
                return;

            _renderers = GetComponentsInChildren<Renderer>(true);
            var list = new System.Collections.Generic.List<Material>();
            for (int i = 0; i < _renderers.Length; i++)
            {
                var shared = _renderers[i].sharedMaterials;
                var instanced = new Material[shared.Length];
                for (int m = 0; m < shared.Length; m++)
                {
                    if (shared[m] == null)
                        continue;
                    // Skip bowl / shadow-only props under pet that aren't cel body if named FoodBowl/Shadow.
                    var goName = _renderers[i].gameObject.name;
                    if (goName is "FoodBowl" or "Shadow")
                    {
                        instanced[m] = shared[m];
                        continue;
                    }

                    var mat = new Material(shared[m]) { name = shared[m].name + " (DirtInstance)" };
                    if (dirtMap != null && mat.HasProperty("_DirtMap"))
                        mat.SetTexture("_DirtMap", dirtMap);
                    if (mat.HasProperty("_DirtColor"))
                        mat.SetColor("_DirtColor", dirtColor);
                    if (mat.HasProperty("_DirtStrength"))
                        mat.SetFloat("_DirtStrength", dirtStrength);
                    if (mat.HasProperty("_DirtTiling"))
                        mat.SetFloat("_DirtTiling", dirtTiling);
                    instanced[m] = mat;
                    list.Add(mat);
                }

                _renderers[i].materials = instanced;
            }

            _instances = list.ToArray();
        }

        private void DestroyInstances()
        {
            if (_instances != null)
            {
                for (int i = 0; i < _instances.Length; i++)
                {
                    if (_instances[i] != null)
                        Destroy(_instances[i]);
                }
            }

            _instances = null;
            if (_scrubMask != null)
            {
                Destroy(_scrubMask);
                _scrubMask = null;
            }
        }

        private void EnsureScrubMask()
        {
            if (_scrubMask != null)
                return;

            _scrubMask = new Texture2D(scrubResolution, scrubResolution, TextureFormat.RGBA32, false)
            {
                name = "PetScrubMask",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            _scrubPixels = new Color32[scrubResolution * scrubResolution];
            ClearScrubMask();
            AssignScrubMaskToMaterials();
        }

        public void ClearScrubMask()
        {
            EnsureScrubMask();
            var black = new Color32(0, 0, 0, 255);
            for (int i = 0; i < _scrubPixels.Length; i++)
                _scrubPixels[i] = black;
            _scrubMask.SetPixels32(_scrubPixels);
            _scrubMask.Apply(false);
            _scrubDirty = false;
        }

        private void AssignScrubMaskToMaterials()
        {
            if (_instances == null || _scrubMask == null)
                return;
            for (int i = 0; i < _instances.Length; i++)
            {
                if (_instances[i] != null && _instances[i].HasProperty("_ScrubMask"))
                    _instances[i].SetTexture("_ScrubMask", _scrubMask);
            }
        }

        public void PushShaderState()
        {
            EnsureInstances();
            EnsureScrubMask();
            float dirt = needs != null ? needs.DirtAmount01 : 0f;
            SetFloatAll("_DirtAmount", dirt);
            AssignScrubMaskToMaterials();
        }

        public void PaintScrub(Vector2 uv, float radiusUv = 0.08f, byte strength = 255)
        {
            EnsureScrubMask();
            int res = scrubResolution;
            int cx = Mathf.Clamp(Mathf.RoundToInt(uv.x * (res - 1)), 0, res - 1);
            int cy = Mathf.Clamp(Mathf.RoundToInt(uv.y * (res - 1)), 0, res - 1);
            int r = Mathf.Max(1, Mathf.RoundToInt(radiusUv * res));
            int r2 = r * r;

            for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
            {
                if (x * x + y * y > r2)
                    continue;
                int px = cx + x;
                int py = cy + y;
                if (px < 0 || py < 0 || px >= res || py >= res)
                    continue;
                int idx = py * res + px;
                var c = _scrubPixels[idx];
                c.r = (byte)Mathf.Max(c.r, strength);
                c.g = c.r;
                c.b = c.r;
                _scrubPixels[idx] = c;
            }

            _scrubMask.SetPixels32(_scrubPixels);
            _scrubDirty = true;
            _scrubGlow = 0.45f;
            SetFloatAll("_ScrubGlow", _scrubGlow);
        }

        public float MeasureScrubCoverage()
        {
            if (_scrubPixels == null)
                return 0f;
            int lit = 0;
            for (int i = 0; i < _scrubPixels.Length; i++)
            {
                if (_scrubPixels[i].r > 40)
                    lit++;
            }

            return (float)lit / _scrubPixels.Length;
        }

        private void SetFloatAll(string prop, float value)
        {
            if (_instances == null)
                return;
            for (int i = 0; i < _instances.Length; i++)
            {
                if (_instances[i] != null && _instances[i].HasProperty(prop))
                    _instances[i].SetFloat(prop, value);
            }
        }

        public void ConfigureDirtMap(Texture2D map)
        {
            dirtMap = map;
            if (_instances == null)
                return;
            for (int i = 0; i < _instances.Length; i++)
            {
                if (_instances[i] != null && _instances[i].HasProperty("_DirtMap") && map != null)
                    _instances[i].SetTexture("_DirtMap", map);
            }
        }
    }
}
