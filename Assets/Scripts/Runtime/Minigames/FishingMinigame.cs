using System;
using UnityEngine;
using PolyPets.Pets;

namespace PolyPets.Minigames
{
    /// <summary>
    /// Cat fishing — baseline mix of Animal Crossing + Stardew Valley.
    ///
    /// AC (New Horizons): cast → nibbles (don't reel) → full bite/kerplunk → press immediately.
    /// Stardew: after hook, keep a green bar on the moving fish until catch meter fills
    /// (hold = bar up, release = bar falls).
    ///
    /// Refs: https://nookipedia.com/wiki/Fishing · https://stardewvalleywiki.com/Fishing
    /// </summary>
    public sealed class FishingMinigame : MonoBehaviour, IPetMinigame
    {
        private enum Phase
        {
            NibbleWait,
            BiteWindow,
            Reeling,
            BetweenRounds,
            Done,
        }

        [Header("AC-style bite")]
        [SerializeField] private int maxNibbles = 4;
        [SerializeField] private float nibbleGapMin = 0.55f;
        [SerializeField] private float nibbleGapMax = 1.1f;
        [SerializeField] private float biteWindowSeconds = 0.7f;

        [Header("Stardew-style reel")]
        [SerializeField] private float reelDuration = 4.5f;
        [SerializeField] private float barSpeed = 1.6f;
        [SerializeField] private float gravity = 1.9f;
        [SerializeField] private float fishWander = 1.3f;
        [SerializeField] private float catchFillRate = 0.35f;
        [SerializeField] private float catchDrainRate = 0.45f;

        [Header("Rewards")]
        [SerializeField] private int rounds = 2;
        [SerializeField] private int baseCoins = 10;
        [SerializeField] private int perfectBonus = 6;

        private MinigameHud _hud;
        private Action<MinigameResult> _onDone;
        private Phase _phase;
        private bool _playing;
        private int _round;
        private int _coins;
        private int _catches;

        // Nibble state
        private int _nibblesDone;
        private float _nextEventAt;
        private float _biteEndsAt;
        private bool _isRealBite;

        // Reel state
        private float _barY;      // 0-1
        private float _barVel;
        private float _fishY;     // 0-1
        private float _fishVel;
        private float _catchMeter; // 0-1
        private float _reelTimer;
        private bool _perfectHook;

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
            BeginCast();
        }

        public void Abort()
        {
            if (!_playing) return;
            Finish();
        }

        private void BeginCast()
        {
            _phase = Phase.NibbleWait;
            _nibblesDone = 0;
            _isRealBite = false;
            _perfectHook = false;
            _nextEventAt = Time.time + UnityEngine.Random.Range(0.8f, 1.4f);
            RefreshHud(
                $"Fishing ({_round + 1}/{rounds})\n" +
                "Line cast… wait for nibbles.\n" +
                "Don't press on ~nibble~ — only on !!BITE!!");
        }

        private void Update()
        {
            if (!_playing || _phase == Phase.BetweenRounds || _phase == Phase.Done)
                return;

            bool pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
            bool held = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);

            switch (_phase)
            {
                case Phase.NibbleWait:
                    UpdateNibbles(pressed);
                    break;
                case Phase.BiteWindow:
                    UpdateBiteWindow(pressed);
                    break;
                case Phase.Reeling:
                    UpdateReeling(held);
                    break;
            }
        }

        private void UpdateNibbles(bool pressed)
        {
            if (pressed)
            {
                // Early reel on nibble = fish spooked (AC rule).
                FailRound("Spooked it! (reeled on a nibble)");
                return;
            }

            if (Time.time < _nextEventAt)
            {
                RefreshHud(
                    $"Fishing ({_round + 1}/{rounds})\n" +
                    $"Waiting… nibbles {_nibblesDone}/{maxNibbles}\n" +
                    "Hold still.");
                return;
            }

            // After enough nibbles, next event is a guaranteed bite (AC: 5th approach).
            bool forceBite = _nibblesDone >= maxNibbles;
            _isRealBite = forceBite || UnityEngine.Random.value < 0.35f + _nibblesDone * 0.15f;

            if (_isRealBite)
            {
                _phase = Phase.BiteWindow;
                _biteEndsAt = Time.time + biteWindowSeconds;
                RefreshHud(
                    $"Fishing ({_round + 1}/{rounds})\n" +
                    "!!! BOBBER UNDER — BITE !!!\n" +
                    "Space / Click NOW");
            }
            else
            {
                _nibblesDone++;
                _nextEventAt = Time.time + UnityEngine.Random.Range(nibbleGapMin, nibbleGapMax);
                RefreshHud(
                    $"Fishing ({_round + 1}/{rounds})\n" +
                    $"~ nibble ~  ({_nibblesDone}/{maxNibbles})\n" +
                    "Don't reel yet…");
            }
        }

        private void UpdateBiteWindow(bool pressed)
        {
            if (pressed)
            {
                float remaining = _biteEndsAt - Time.time;
                _perfectHook = remaining > biteWindowSeconds * 0.55f;
                StartReel();
                return;
            }

            if (Time.time >= _biteEndsAt)
                FailRound("Too slow — it got away.");
            else
                RefreshHud(
                    $"Fishing ({_round + 1}/{rounds})\n" +
                    "!!! BITE !!!\n" +
                    $"Window {(_biteEndsAt - Time.time):0.00}s");
        }

        private void StartReel()
        {
            _phase = Phase.Reeling;
            _barY = 0.35f;
            _barVel = 0f;
            _fishY = 0.5f;
            _fishVel = UnityEngine.Random.Range(-0.4f, 0.4f);
            _catchMeter = 0.25f;
            _reelTimer = reelDuration;
            RefreshHud(BuildReelPrompt());
        }

        private void UpdateReeling(bool held)
        {
            _reelTimer -= Time.deltaTime;

            // Fish wanders (Stardew-like).
            _fishVel += UnityEngine.Random.Range(-fishWander, fishWander) * Time.deltaTime;
            _fishVel = Mathf.Clamp(_fishVel, -0.9f, 0.9f);
            _fishY = Mathf.Clamp01(_fishY + _fishVel * Time.deltaTime);
            if (_fishY <= 0f || _fishY >= 1f)
                _fishVel *= -0.7f;

            // Green bar: hold raises, release falls (Stardew).
            if (held)
                _barVel += barSpeed * Time.deltaTime;
            else
                _barVel -= gravity * Time.deltaTime;
            _barVel = Mathf.Clamp(_barVel, -1.6f, 1.6f);
            _barY = Mathf.Clamp01(_barY + _barVel * Time.deltaTime);
            if (_barY <= 0f || _barY >= 1f)
                _barVel *= -0.3f;

            float barHalf = 0.12f;
            bool onFish = Mathf.Abs(_barY - _fishY) <= barHalf;
            if (onFish)
                _catchMeter += catchFillRate * Time.deltaTime;
            else
                _catchMeter -= catchDrainRate * Time.deltaTime;
            _catchMeter = Mathf.Clamp01(_catchMeter);

            RefreshHud(BuildReelPrompt());

            if (_catchMeter >= 1f)
            {
                SucceedRound();
                return;
            }

            if (_catchMeter <= 0f || _reelTimer <= 0f)
                FailRound(_catchMeter <= 0f ? "Line snapped — lost it." : "Tired out — fish escaped.");
        }

        private string BuildReelPrompt()
        {
            return $"Fishing ({_round + 1}/{rounds}) — REEL\n" +
                   "Hold Space/Click = bar UP · release = fall\n" +
                   "Keep [BAR] on the fish ><\n\n" +
                   DrawMeter("FISH", _fishY, '><', '.') + "\n" +
                   DrawMeter("BAR ", _barY, '#', '-') + "\n" +
                   DrawMeter("CATCH", _catchMeter, '=', ' ');
        }

        private static string DrawMeter(string label, float t, char fill, char empty)
        {
            const int width = 18;
            int idx = Mathf.Clamp(Mathf.RoundToInt(t * (width - 1)), 0, width - 1);
            var chars = new char[width];
            for (int i = 0; i < width; i++)
                chars[i] = empty;
            chars[idx] = fill;
            return $"{label} |{new string(chars)}|";
        }

        private void SucceedRound()
        {
            _catches++;
            int gain = baseCoins + (_perfectHook ? perfectBonus : 0);
            _coins += gain;
            _phase = Phase.BetweenRounds;
            RefreshHud(_perfectHook
                ? $"Perfect hook! Caught one. +{gain}c"
                : $"Caught! +{gain}c");
            AdvanceOrFinish();
        }

        private void FailRound(string reason)
        {
            _coins += 1; // pity
            _phase = Phase.BetweenRounds;
            RefreshHud($"{reason}\n+1c pity");
            AdvanceOrFinish();
        }

        private void AdvanceOrFinish()
        {
            _round++;
            if (_round >= rounds)
                Invoke(nameof(Finish), 0.85f);
            else
                Invoke(nameof(BeginCast), 0.85f);
        }

        private void Finish()
        {
            CancelInvoke();
            _phase = Phase.Done;
            _playing = false;
            _hud?.Hide();
            float score = rounds <= 0 ? 0f : (float)_catches / rounds;
            _onDone?.Invoke(new MinigameResult("Fishing", _coins, score, completed: true));
        }

        private void RefreshHud(string prompt)
        {
            _hud?.Show(prompt, $"Caught {_catches}/{rounds} · Coins {_coins}");
        }
    }
}
