using System;
using UnityEngine;
using PolyPets.Economy;
using PolyPets.Pets;

namespace PolyPets.Minigames
{
    /// <summary>
    /// Shared minigame result. Coins are awarded only through this path.
    /// </summary>
    public readonly struct MinigameResult
    {
        public readonly string MinigameId;
        public readonly int CoinsEarned;
        public readonly float Score01;
        public readonly bool Completed;

        public MinigameResult(string id, int coins, float score01, bool completed)
        {
            MinigameId = id;
            CoinsEarned = coins;
            Score01 = Mathf.Clamp01(score01);
            Completed = completed;
        }
    }

    /// <summary>
    /// Routes minigame play and pays out coins. Placeholder fishing is included for the vertical slice.
    /// </summary>
    public sealed class MinigameRouter : MonoBehaviour
    {
        public static MinigameRouter Instance { get; private set; }

        [SerializeField] private PetAgent activePet;
        [SerializeField] private FishingMinigame fishing;

        public event Action<MinigameResult> MinigameFinished;

        private void Awake()
        {
            Instance = this;
            if (fishing == null)
                fishing = GetComponent<FishingMinigame>();
            if (fishing == null)
                fishing = gameObject.AddComponent<FishingMinigame>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetActivePet(PetAgent pet) => activePet = pet;

        public void PlayActivePetMinigame()
        {
            if (activePet == null)
            {
                Debug.LogWarning("[PolyPets] No active pet for minigame.");
                return;
            }

            string id = activePet.Definition != null
                ? activePet.Definition.signatureMinigame
                : "Fishing";

            if (string.Equals(id, "Fishing", StringComparison.OrdinalIgnoreCase))
                fishing.Play(activePet, OnFinished);
            else
            {
                // Fallback until other minigames exist.
                fishing.Play(activePet, OnFinished);
            }
        }

        private void OnFinished(MinigameResult result)
        {
            if (result.Completed && result.CoinsEarned > 0)
                EconomyService.Instance?.AddCoins(result.CoinsEarned, result.MinigameId);

            if (result.Completed)
                activePet?.Needs?.NotifyMinigameCompleted(result.Score01);

            MinigameFinished?.Invoke(result);
        }
    }

    /// <summary>
    /// Lightweight timing fishing loop (Space / click). Earns coins on catch — the only income source for now.
    /// </summary>
    public sealed class FishingMinigame : MonoBehaviour
    {
        [SerializeField] private int baseCoins = 12;
        [SerializeField] private int perfectBonus = 8;
        [SerializeField] private float windowSeconds = 0.55f;
        [SerializeField] private float roundSeconds = 8f;

        private PetAgent _pet;
        private Action<MinigameResult> _onDone;
        private bool _playing;
        private float _elapsed;
        private float _biteAt;
        private bool _biting;
        private bool _resolved;

        public bool IsPlaying => _playing;

        public void Play(PetAgent pet, Action<MinigameResult> onDone)
        {
            _pet = pet;
            _onDone = onDone;
            _playing = true;
            _resolved = false;
            _biting = false;
            _elapsed = 0f;
            _biteAt = UnityEngine.Random.Range(1.4f, Mathf.Max(2f, roundSeconds - 1.5f));
            Debug.Log("[PolyPets] Fishing started — wait for the bite, then press Space / click.");
        }

        private void Update()
        {
            if (!_playing)
                return;

            _elapsed += Time.deltaTime;

            if (!_biting && _elapsed >= _biteAt)
            {
                _biting = true;
                Debug.Log("[PolyPets] ! Bite ! — press Space / mouse now");
            }

            bool pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);

            if (_biting && !_resolved && pressed)
            {
                float delta = _elapsed - _biteAt;
                bool perfect = delta <= windowSeconds * 0.45f;
                bool ok = delta <= windowSeconds;
                Finish(ok, perfect);
                return;
            }

            if (_biting && _elapsed > _biteAt + windowSeconds)
            {
                Finish(false, false);
                return;
            }

            if (_elapsed >= roundSeconds)
                Finish(false, false);
        }

        private void Finish(bool caught, bool perfect)
        {
            if (_resolved)
                return;
            _resolved = true;
            _playing = false;

            int coins = 0;
            float score = 0f;
            if (caught)
            {
                coins = baseCoins + (perfect ? perfectBonus : 0);
                score = perfect ? 1f : 0.65f;
                Debug.Log(perfect
                    ? $"[PolyPets] Perfect catch! +{coins} coins"
                    : $"[PolyPets] Caught a fish. +{coins} coins");
            }
            else
            {
                // Soft fail still teaches the loop — tiny pity coins so they can eventually buy food.
                coins = 2;
                score = 0.15f;
                Debug.Log($"[PolyPets] Got away… +{coins} pity coins. Try again!");
            }

            _onDone?.Invoke(new MinigameResult("Fishing", coins, score, completed: true));
        }
    }
}
