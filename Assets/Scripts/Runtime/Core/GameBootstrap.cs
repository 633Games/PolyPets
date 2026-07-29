using UnityEngine;
using PolyPets.Desktop;
using PolyPets.House;
using PolyPets.Camera;
using PolyPets.Rendering;
using PolyPets.Economy;
using PolyPets.Shop;
using PolyPets.Minigames;
using PolyPets.Pets;
using PolyPets.UI;

namespace PolyPets.Core
{
    /// <summary>
    /// Scene entry point. Kept intentionally thin for the vertical slice.
    /// </summary>
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

        private void Awake()
        {
            if (desktopWindow != null)
                desktopWindow.ApplyStartupWindowSettings();

            if (house != null)
                house.Initialize();

            if (houseCamera != null && house != null)
                houseCamera.FocusRoom(house.ActiveRoom);

            if (dayNight != null)
                dayNight.Apply(dayNight.TimeOfDay01);

            if (minigameRouter != null && house != null && house.ActiveRoom != null)
                minigameRouter.SetActivePet(house.ActiveRoom.Occupant);

            careHud?.RefreshAll();
        }
    }
}
