using UnityEngine;
using PolyPets.Desktop;
using PolyPets.House;
using PolyPets.Camera;
using PolyPets.Rendering;

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
        }
    }
}
