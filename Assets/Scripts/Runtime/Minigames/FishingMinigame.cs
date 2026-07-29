using System;
using UnityEngine;
using UnityEngine.UI;
using PolyPets.Pets;
using PolyPets.UI;

namespace PolyPets.Minigames
{
    /// <summary>
    /// Cat fishing: FISH cast → wait → shake + timing bar → CATCH.
    /// Good-zone press = catch; edge zones = chance catch.
    /// Icon states: idle (no line) → waiting (line down) → caught (fish on line).
    /// </summary>
    public sealed class FishingMinigame : MonoBehaviour, IPetMinigame
    {
        private enum Phase
        {
            Ready,
            Waiting,
            ShakeWindow,
            BetweenRounds,
            Done,
        }

        private enum RodVisual
        {
            Idle,
            Waiting,
            Caught,
        }

        [SerializeField] private float waitMin = 1.6f;
        [SerializeField] private float waitMax = 3.4f;
        [SerializeField] private float shakeWindowSeconds = 2.8f;
        [SerializeField] private float needleSpeed = 1.35f;
        [SerializeField] private float goodZoneHalfWidth = 0.11f;
        [Range(0f, 1f)]
        [SerializeField] private float edgeCatchChance = 0.32f;
        [SerializeField] private int rounds = 3;
        [SerializeField] private int catchCoins = 8;
        [SerializeField] private int missPity = 1;

        [SerializeField] private Sprite rodIdle;
        [SerializeField] private Sprite rodWaiting;
        [SerializeField] private Sprite rodCaught;

        private MinigameHud _hud;
        private Action<MinigameResult> _onDone;
        private Phase _phase;
        private bool _playing;
        private int _round;
        private int _coins;
        private int _catches;
        private float _shakeAt;
        private float _shakeEndsAt;
        private float _needleT;
        private int _needleDir = 1;

        private RectTransform _stage;
        private Image _rodImg;
        private RectTransform _rodRt;
        private Vector2 _rodRestPos;
        private GameObject _timingRoot;
        private RectTransform _needleRt;
        private RectTransform _goodZoneRt;
        private Text _statusLabel;
        private Button _fishBtn;
        private Button _catchBtn;
        private Text _fishLabel;
        private Text _catchLabel;
        private Sprite _roundSprite;
        private bool _built;

        public bool IsPlaying => _playing;

        public void BindHud(MinigameHud hud) => _hud = hud;

        public void Play(PetAgent pet, Action<MinigameResult> onDone)
        {
            CancelInvoke();
            _onDone = onDone;
            _playing = true;
            _coins = 0;
            _catches = 0;
            _round = 0;

            _hud?.Show("Fishing…", "✦ 0");
            EnsureStage();
            SetStageVisible(true);
            EnterReady();
        }

        public void Abort()
        {
            if (!_playing) return;
            Finish();
        }

        private void OnDisable()
        {
            if (_playing)
                Finish();
        }

        private void Update()
        {
            if (!_playing || _phase is Phase.BetweenRounds or Phase.Done)
                return;

            if (Input.GetKeyDown(KeyCode.F))
                OnFishPressed();
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Space))
                OnCatchPressed();

            switch (_phase)
            {
                case Phase.Waiting:
                    UpdateWaiting();
                    break;
                case Phase.ShakeWindow:
                    UpdateShakeWindow();
                    break;
            }

            AnimateRod();
        }

        private void EnterReady()
        {
            _phase = Phase.Ready;
            SetRodVisual(RodVisual.Idle);
            SetTimingVisible(false);
            RefreshHud("Tap FISH to cast.");
            SetButtons(fishOn: true, catchOn: false);
            SetStatus("Ready — tap FISH");
        }

        private void OnFishPressed()
        {
            if (!_playing || _phase != Phase.Ready)
                return;

            _phase = Phase.Waiting;
            _shakeAt = Time.time + UnityEngine.Random.Range(waitMin, waitMax);
            SetRodVisual(RodVisual.Waiting);
            SetTimingVisible(false);
            SetButtons(fishOn: false, catchOn: false);
            SetStatus("Line in the water…");
            RefreshHud("Wait for a bite — rod will shake.");
        }

        private void OnCatchPressed()
        {
            if (!_playing || _phase != Phase.ShakeWindow)
                return;

            ResolveCatch(NeedleInGoodZone());
        }

        private void UpdateWaiting()
        {
            if (Time.time < _shakeAt)
            {
                float left = _shakeAt - Time.time;
                SetStatus(left > 1.2f ? "Floating…" : "Something's biting…?");
                return;
            }

            BeginShakeWindow();
        }

        private void BeginShakeWindow()
        {
            _phase = Phase.ShakeWindow;
            _shakeEndsAt = Time.time + shakeWindowSeconds;
            _needleT = UnityEngine.Random.Range(0.05f, 0.95f);
            _needleDir = UnityEngine.Random.value < 0.5f ? -1 : 1;
            SetRodVisual(RodVisual.Waiting);
            SetTimingVisible(true);
            LayoutTimingBar();
            SetButtons(fishOn: false, catchOn: true);
            SetStatus("SHAKE! Hit the green zone!");
            RefreshHud("Rod shaking — CATCH in the green zone!");
        }

        private void UpdateShakeWindow()
        {
            if (Time.time >= _shakeEndsAt)
            {
                FailRound("Got away — try again.");
                return;
            }

            _needleT += _needleDir * needleSpeed * Time.deltaTime;
            if (_needleT >= 1f)
            {
                _needleT = 1f;
                _needleDir = -1;
            }
            else if (_needleT <= 0f)
            {
                _needleT = 0f;
                _needleDir = 1;
            }

            LayoutTimingBar();
            SetStatus($"CATCH!  {_shakeEndsAt - Time.time:0.0}s");
        }

        private bool NeedleInGoodZone()
        {
            float center = 0.5f;
            return Mathf.Abs(_needleT - center) <= goodZoneHalfWidth;
        }

        private void ResolveCatch(bool inGoodZone)
        {
            bool success = inGoodZone || UnityEngine.Random.value < edgeCatchChance;
            if (success)
            {
                if (!inGoodZone)
                    SetStatus("Lucky catch!");
                SucceedRound();
            }
            else
            {
                FailRound("Missed the zone — got away.");
            }
        }

        private void SucceedRound()
        {
            _catches++;
            _coins += catchCoins;
            _phase = Phase.BetweenRounds;
            SetRodVisual(RodVisual.Caught);
            SetTimingVisible(false);
            SetStatus($"Caught! +{catchCoins}✦");
            RefreshHud($"Nice catch! +{catchCoins}✦");
            SetButtons(false, false);
            AdvanceOrFinish();
        }

        private void FailRound(string reason)
        {
            _coins += missPity;
            _phase = Phase.BetweenRounds;
            SetRodVisual(RodVisual.Waiting);
            SetTimingVisible(false);
            SetStatus(reason);
            RefreshHud($"{reason}\n+{missPity}✦");
            SetButtons(false, false);
            AdvanceOrFinish();
        }

        private void AdvanceOrFinish()
        {
            _round++;
            if (_round >= rounds)
                Invoke(nameof(Finish), 0.95f);
            else
                Invoke(nameof(EnterReady), 0.95f);
        }

        private void Finish()
        {
            CancelInvoke();
            _phase = Phase.Done;
            _playing = false;
            SetTimingVisible(false);
            SetStageVisible(false);
            _hud?.Hide();
            float score = rounds <= 0 ? 0f : (float)_catches / rounds;
            _onDone?.Invoke(new MinigameResult("Fishing", _coins, score, completed: true));
        }

        private void RefreshHud(string prompt)
        {
            _hud?.Show(
                $"Fishing ({Mathf.Min(_round + 1, rounds)}/{rounds})\n{prompt}",
                $"Caught {_catches}/{rounds} · ✦ {_coins}");
        }

        private void SetButtons(bool fishOn, bool catchOn)
        {
            if (_fishBtn != null) _fishBtn.interactable = fishOn;
            if (_catchBtn != null) _catchBtn.interactable = catchOn;
            if (_fishLabel != null)
                _fishLabel.color = fishOn ? CozyUiTheme.Cocoa : CozyUiTheme.CocoaMuted;
            if (_catchLabel != null)
                _catchLabel.color = catchOn ? CozyUiTheme.Cocoa : CozyUiTheme.CocoaMuted;

            if (_catchBtn != null)
            {
                var img = _catchBtn.GetComponent<Image>();
                if (img != null)
                    img.color = (_phase == Phase.ShakeWindow && catchOn)
                        ? CozyUiTheme.Honey
                        : CozyUiTheme.ParchmentSolid;
            }
        }

        private void SetStatus(string msg)
        {
            if (_statusLabel != null)
                _statusLabel.text = msg;
        }

        private void SetRodVisual(RodVisual visual)
        {
            if (_rodImg == null)
                return;

            Sprite sprite = visual switch
            {
                RodVisual.Waiting => rodWaiting != null ? rodWaiting : rodIdle,
                RodVisual.Caught => rodCaught != null ? rodCaught : rodWaiting,
                _ => rodIdle,
            };
            if (sprite != null)
                _rodImg.sprite = sprite;
            _rodImg.color = CozyUiTheme.Cocoa;
        }

        private void AnimateRod()
        {
            if (_rodRt == null)
                return;

            if (_phase == Phase.ShakeWindow)
            {
                float s = Mathf.Sin(Time.time * 28f);
                _rodRt.localRotation = Quaternion.Euler(0f, 0f, s * 9f);
                _rodRt.anchoredPosition = _rodRestPos + new Vector2(s * 4f, 0f);
            }
            else
            {
                _rodRt.localRotation = Quaternion.identity;
                _rodRt.anchoredPosition = _rodRestPos;
            }
        }

        private void SetTimingVisible(bool visible)
        {
            if (_timingRoot != null)
                _timingRoot.SetActive(visible);
        }

        private void LayoutTimingBar()
        {
            if (_needleRt == null || _goodZoneRt == null)
                return;

            float barW = 320f;
            float zoneW = barW * goodZoneHalfWidth * 2f;
            _goodZoneRt.sizeDelta = new Vector2(zoneW, 28f);
            _goodZoneRt.anchoredPosition = Vector2.zero;

            float x = Mathf.Lerp(-barW * 0.5f, barW * 0.5f, _needleT);
            _needleRt.anchoredPosition = new Vector2(x, 0f);
        }

        private void SetStageVisible(bool visible)
        {
            if (_stage != null)
                _stage.gameObject.SetActive(visible);
        }

        private void EnsureStage()
        {
            if (_built && _stage != null)
            {
                WireButtons();
                return;
            }

            var root = ResolveOverlayRoot();
            if (root == null)
            {
                Debug.LogError("[Fishing] MinigameOverlay root missing — cannot build stage.");
                return;
            }

            var existing = root.Find("FishingStage");
            if (existing != null)
                Destroy(existing.gameObject);

            EnsureSprites();

            var stageGo = new GameObject("FishingStage", typeof(RectTransform));
            stageGo.transform.SetParent(root, false);
            stageGo.transform.SetSiblingIndex(1);
            _stage = stageGo.GetComponent<RectTransform>();
            Stretch(_stage, 8f);

            // Soft pond wash behind the icon
            var pond = MakeImage(_stage, "PondWash", new Color(0.45f, 0.62f, 0.72f, 0.55f));
            var pondRt = pond.rectTransform;
            pondRt.anchorMin = new Vector2(0.12f, 0.30f);
            pondRt.anchorMax = new Vector2(0.88f, 0.72f);
            pondRt.offsetMin = Vector2.zero;
            pondRt.offsetMax = Vector2.zero;
            if (_roundSprite != null)
            {
                pond.sprite = _roundSprite;
                pond.type = Image.Type.Sliced;
            }

            _rodImg = MakeImage(_stage, "RodIcon", CozyUiTheme.Cocoa);
            _rodRt = _rodImg.rectTransform;
            _rodRt.anchorMin = _rodRt.anchorMax = new Vector2(0.5f, 0.58f);
            _rodRt.sizeDelta = new Vector2(220f, 220f);
            _rodImg.preserveAspect = true;
            _rodImg.raycastTarget = false;
            _rodRestPos = _rodRt.anchoredPosition;
            SetRodVisual(RodVisual.Idle);

            BuildTimingBar(_stage);

            _statusLabel = MakeText(_stage, "Status", "Ready — tap FISH",
                new Vector2(0.5f, 0.22f), new Vector2(380f, 28f), 16, CozyUiTheme.Cocoa);

            _fishBtn = MakeActionButton(_stage, "Btn_Fish", "FISH", new Vector2(0.28f, 0.08f), out _fishLabel);
            _catchBtn = MakeActionButton(_stage, "Btn_Catch", "CATCH", new Vector2(0.72f, 0.08f), out _catchLabel);

            var prompt = root.Find("Prompt") as RectTransform;
            if (prompt != null)
            {
                prompt.anchorMin = prompt.anchorMax = new Vector2(0.5f, 0.92f);
                prompt.sizeDelta = new Vector2(400f, 44f);
                prompt.SetAsLastSibling();
            }

            var score = root.Find("Score") as RectTransform;
            if (score != null)
            {
                score.anchorMin = score.anchorMax = new Vector2(0.5f, 0.02f);
                score.sizeDelta = new Vector2(340f, 22f);
                score.SetAsLastSibling();
            }

            WireButtons();
            _built = true;
            Debug.Log("[Fishing] Stage built with 3-state rod icons + timing bar.");
        }

        private void BuildTimingBar(Transform parent)
        {
            _timingRoot = new GameObject("TimingBar", typeof(RectTransform));
            _timingRoot.transform.SetParent(parent, false);
            var rootRt = _timingRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.34f);
            rootRt.sizeDelta = new Vector2(340f, 40f);

            var track = MakeImage(_timingRoot.transform, "Track", CozyUiTheme.MeterTrack);
            var trackRt = track.rectTransform;
            trackRt.anchorMin = trackRt.anchorMax = new Vector2(0.5f, 0.5f);
            trackRt.sizeDelta = new Vector2(320f, 28f);
            if (_roundSprite != null)
            {
                track.sprite = _roundSprite;
                track.type = Image.Type.Sliced;
            }

            var good = MakeImage(_timingRoot.transform, "GoodZone", CozyUiTheme.Leaf);
            _goodZoneRt = good.rectTransform;
            _goodZoneRt.anchorMin = _goodZoneRt.anchorMax = new Vector2(0.5f, 0.5f);
            _goodZoneRt.sizeDelta = new Vector2(70f, 28f);
            if (_roundSprite != null)
            {
                good.sprite = _roundSprite;
                good.type = Image.Type.Sliced;
            }

            var needle = MakeImage(_timingRoot.transform, "Needle", CozyUiTheme.Cocoa);
            _needleRt = needle.rectTransform;
            _needleRt.anchorMin = _needleRt.anchorMax = new Vector2(0.5f, 0.5f);
            _needleRt.sizeDelta = new Vector2(8f, 36f);

            MakeText(_timingRoot.transform, "Hint", "green = sure catch · edges = lucky",
                new Vector2(0.5f, 0f), new Vector2(320f, 18f), 12, CozyUiTheme.CocoaSoft)
                .rectTransform.anchoredPosition = new Vector2(0f, -22f);

            _timingRoot.SetActive(false);
        }

        private Transform ResolveOverlayRoot()
        {
            if (_hud != null && _hud.RootTransform != null)
                return _hud.RootTransform;

            var canvas = GameObject.Find("HUD_Canvas");
            if (canvas != null)
            {
                var t = canvas.transform.Find("MinigameOverlay");
                if (t != null)
                    return t;
            }

            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t == null || t.name != "MinigameOverlay")
                    continue;
                if (!t.gameObject.scene.IsValid())
                    continue;
                return t;
            }

            return null;
        }

        private void WireButtons()
        {
            if (_fishBtn != null)
            {
                _fishBtn.onClick.RemoveListener(OnFishPressed);
                _fishBtn.onClick.AddListener(OnFishPressed);
            }

            if (_catchBtn != null)
            {
                _catchBtn.onClick.RemoveListener(OnCatchPressed);
                _catchBtn.onClick.AddListener(OnCatchPressed);
            }
        }

        private void EnsureSprites()
        {
            if (_roundSprite == null)
                _roundSprite = CozyUiTheme.CreateRoundedSprite(96, 22, Color.white, new Color(0.2f, 0.35f, 0.45f, 0.35f), 2);

            rodIdle ??= Resources.Load<Sprite>("Minigames/Fishing/rod_idle");
            rodWaiting ??= Resources.Load<Sprite>("Minigames/Fishing/rod_waiting");
            rodCaught ??= Resources.Load<Sprite>("Minigames/Fishing/rod_caught");

            if (rodIdle == null || rodWaiting == null || rodCaught == null)
                Debug.LogWarning("[Fishing] Missing rod state sprites under Resources/Minigames/Fishing/.");
        }

        private static Image MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Text MakeText(Transform parent, string name, string content, Vector2 anchor, Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = CozyUiTheme.UiFont;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private Button MakeActionButton(Transform parent, string name, string label, Vector2 anchor, out Text labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(140f, 52f);
            var img = go.GetComponent<Image>();
            img.sprite = _roundSprite;
            img.type = Image.Type.Sliced;
            img.color = CozyUiTheme.ParchmentSolid;
            img.raycastTarget = true;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.GetComponent<RectTransform>(), 4f);
            labelText = textGo.GetComponent<Text>();
            labelText.text = label;
            labelText.font = CozyUiTheme.UiFont;
            labelText.fontSize = 20;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = CozyUiTheme.Cocoa;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.raycastTarget = false;

            return go.GetComponent<Button>();
        }

        private static void Stretch(RectTransform rt, float pad)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        private void OnDestroy()
        {
            if (_roundSprite != null && _roundSprite.texture != null)
                Destroy(_roundSprite.texture);
        }
    }
}
