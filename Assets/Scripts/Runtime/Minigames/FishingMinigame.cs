using System;
using UnityEngine;
using PolyPets.Pets;

namespace PolyPets.Minigames
{
    /// <summary>
    /// Cat minigame — fishing QTE. Wait for BITE, press Space/click inside the window.
    /// </summary>
    public sealed class FishingMinigame : MonoBehaviour, IPetMinigame
    {
        [SerializeField] private int baseCoins = 12;
        [SerializeField] private int perfectBonus = 8;
        [SerializeField] private float windowSeconds = 0.55f;
        [SerializeField] private float roundSeconds = 9f;
        [SerializeField] private int rounds = 3;

        private MinigameHud _hud;
        private Action<MinigameResult> _onDone;
        private bool _playing;
        private float _elapsed;
        private float _biteAt;
        private bool _biting;
        private bool _roundResolved;
        private int _roundIndex;
        private int _coins;
        private int _hits;
        private int _perfects;

        public bool IsPlaying => _playing;

        public void BindHud(MinigameHud hud) => _hud = hud;

        public void Play(PetAgent pet, Action<MinigameResult> onDone)
        {
            _onDone = onDone;
            _playing = true;
            _coins = 0;
            _hits = 0;
            _perfects = 0;
            _roundIndex = 0;
            StartRound();
        }

        public void Abort()
        {
            if (!_playing) return;
            Finish();
        }

        private void StartRound()
        {
            _elapsed = 0f;
            _biting = false;
            _roundResolved = false;
            _biteAt = UnityEngine.Random.Range(1.2f, Mathf.Max(2f, roundSeconds - 1.6f));
            _hud?.Show($"Fishing ({_roundIndex + 1}/{rounds})\nWait for the bite…", $"Coins {_coins}");
        }

        private void Update()
        {
            if (!_playing || _roundResolved)
                return;

            _elapsed += Time.deltaTime;

            if (!_biting && _elapsed >= _biteAt)
            {
                _biting = true;
                _hud?.Show($"Fishing ({_roundIndex + 1}/{rounds})\n!!! BITE !!!\nSpace / Click NOW", $"Coins {_coins}");
            }

            bool pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);

            if (_biting && pressed)
            {
                float delta = _elapsed - _biteAt;
                bool perfect = delta <= windowSeconds * 0.4f;
                bool ok = delta <= windowSeconds;
                ResolveRound(ok, perfect);
                return;
            }

            if (_biting && _elapsed > _biteAt + windowSeconds)
            {
                ResolveRound(false, false);
                return;
            }

            if (_elapsed >= roundSeconds)
                ResolveRound(false, false);
        }

        private void ResolveRound(bool caught, bool perfect)
        {
            if (_roundResolved) return;
            _roundResolved = true;

            if (caught)
            {
                _hits++;
                int gain = baseCoins / rounds + (perfect ? perfectBonus / Mathf.Max(1, rounds - 1) : 0);
                gain = Mathf.Max(3, gain);
                if (perfect) _perfects++;
                _coins += gain;
                _hud?.Show(perfect ? "Perfect catch!" : "Caught one!", $"Coins {_coins}");
            }
            else
            {
                _coins += 1; // tiny pity so tutorial can't softlock
                _hud?.Show("It got away…", $"Coins {_coins}");
            }

            _roundIndex++;
            if (_roundIndex >= rounds)
                Invoke(nameof(Finish), 0.7f);
            else
                Invoke(nameof(StartRound), 0.7f);
        }

        private void Finish()
        {
            CancelInvoke();
            _playing = false;
            _hud?.Hide();
            float score = rounds <= 0 ? 0f : (_hits + _perfects * 0.25f) / rounds;
            _onDone?.Invoke(new MinigameResult("Fishing", _coins, score, completed: true));
        }
    }
}
