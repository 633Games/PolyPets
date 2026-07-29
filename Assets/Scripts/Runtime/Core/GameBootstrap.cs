using UnityEngine;
using PolyPets.Desktop;
using PolyPets.House;
using PolyPets.Camera;
using PolyPets.Rendering;
using PolyPets.Economy;
using PolyPets.Shop;
using PolyPets.Minigames;
using PolyPets.Pets;
using PolyPets.Tutorial;
using PolyPets.UI;

namespace PolyPets.Core
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private HouseController house;
        [SerializeField] private HouseCameraController houseCamera;
        [SerializeField] private DesktopWindowController desktopWindow;
        [SerializeField] private DayNightCycle dayNight;
        [SerializeField] private EconomyService economy;
        [SerializeField] private FoodInventory foodInventory;
        [SerializeField] private MinigameRouter minigameRouter;
        [SerializeField] private CareHudController careHud;
        [SerializeField] private StarterTutorial tutorial;

        private void Awake()
        {
            if (desktopWindow != null)
                desktopWindow.ApplyStartupWindowSettings();

            if (house != null)
                house.Initialize();

            // Keep scene camera framing unless HouseCameraController explicitly opts in.
            if (houseCamera != null && house != null && houseCamera.ReframeOnStart)
                houseCamera.FocusRoom(house.ActiveRoom);

            if (dayNight != null)
                dayNight.Apply(dayNight.TimeOfDay01);

            if (tutorial != null)
                tutorial.TutorialCompleted += OnTutorialCompleted;
            else if (minigameRouter != null && house?.ActiveRoom != null)
                minigameRouter.SetActivePet(house.ActiveRoom.Occupant);

            careHud?.RefreshAll();
        }

        private void OnDestroy()
        {
            if (tutorial != null)
                tutorial.TutorialCompleted -= OnTutorialCompleted;
        }

        private void OnTutorialCompleted(PetAgent pet)
        {
            minigameRouter?.SetActivePet(pet);
            careHud?.RefreshAll();
            Debug.Log($"[PolyPets] Tutorial done. Play {pet.Definition?.SignatureMinigameName} to earn coins, then buy food.");
        }
    }
}
