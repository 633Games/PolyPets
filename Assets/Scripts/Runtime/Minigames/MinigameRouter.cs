using System;
using UnityEngine;
using PolyPets.Economy;
using PolyPets.Pets;

namespace PolyPets.Minigames
{
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

    public interface IPetMinigame
    {
        bool IsPlaying { get; }
        void Play(PetAgent pet, Action<MinigameResult> onDone);
        void Abort();
    }

    /// <summary>
    /// Routes the active pet to its signature coin-earning minigame.
    /// Cat → Fishing QTE · Dog → Dig then Snap · Rabbit → Carrot Farm
    /// </summary>
    public sealed class MinigameRouter : MonoBehaviour
    {
        public static MinigameRouter Instance { get; private set; }

        [SerializeField] private PetAgent activePet;
        [SerializeField] private FishingMinigame fishing;
        [SerializeField] private DigSnapMinigame digSnap;
        [SerializeField] private CarrotFarmMinigame carrotFarm;
        [SerializeField] private MinigameHud minigameHud;

        public bool IsBusy =>
            (fishing != null && fishing.IsPlaying)
            || (digSnap != null && digSnap.IsPlaying)
            || (carrotFarm != null && carrotFarm.IsPlaying);

        public event Action<MinigameResult> MinigameFinished;

        private void Awake()
        {
            Instance = this;
            fishing ??= GetComponent<FishingMinigame>() ?? gameObject.AddComponent<FishingMinigame>();
            digSnap ??= GetComponent<DigSnapMinigame>() ?? gameObject.AddComponent<DigSnapMinigame>();
            carrotFarm ??= GetComponent<CarrotFarmMinigame>() ?? gameObject.AddComponent<CarrotFarmMinigame>();
            minigameHud ??= GetComponent<MinigameHud>() ?? gameObject.AddComponent<MinigameHud>();

            fishing.BindHud(minigameHud);
            digSnap.BindHud(minigameHud);
            carrotFarm.BindHud(minigameHud);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetActivePet(PetAgent pet) => activePet = pet;

        public void BindHud(MinigameHud hud)
        {
            minigameHud = hud;
            fishing?.BindHud(hud);
            digSnap?.BindHud(hud);
            carrotFarm?.BindHud(hud);
        }

        public void PlayActivePetMinigame()
        {
            if (IsBusy)
            {
                Debug.Log("[PolyPets] Minigame already running.");
                return;
            }

            if (activePet == null)
            {
                Debug.LogWarning("[PolyPets] No active pet for minigame.");
                return;
            }

            var id = activePet.Definition != null
                ? activePet.Definition.minigame
                : MinigameId.Fishing;

            switch (id)
            {
                case MinigameId.DigSnap:
                    digSnap.Play(activePet, OnFinished);
                    break;
                case MinigameId.CarrotFarm:
                    carrotFarm.Play(activePet, OnFinished);
                    break;
                default:
                    fishing.Play(activePet, OnFinished);
                    break;
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
}
