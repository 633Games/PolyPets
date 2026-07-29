using UnityEngine;
using UnityEngine.UI;
using PolyPets.Rendering;

namespace PolyPets.UI
{
    /// <summary>
    /// Minimal HUD wiring for the desktop companion chrome (uGUI Text for zero package deps).
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private Text coinText;
        [SerializeField] private Text roomText;
        [SerializeField] private Text clockText;
        [SerializeField] private Toggle alwaysOnTopToggle;
        [SerializeField] private DayNightCycle dayNight;

        private int _coins;

        private void Update()
        {
            if (clockText == null || dayNight == null)
                return;

            clockText.text = FormatClock(dayNight.TimeOfDay01, dayNight.CurrentPhase);
        }

        public void SetCoins(int coins)
        {
            bool changed = coins != _coins;
            _coins = coins;
            if (coinText != null)
                coinText.text = $"★ {_coins}";
            if (changed && Application.isPlaying && coinText != null)
                Feel.FeelTagBinder.EnsureTagOn(coinText.gameObject, Feel.FeelTagType.Punch, autoPlay: false)?.Play();
        }

        public void SetRoomName(string roomName)
        {
            if (roomText != null)
                roomText.text = roomName;
        }

        public void BindDayNight(DayNightCycle cycle)
        {
            dayNight = cycle;
        }

        public void BindAlwaysOnTopToggle(Toggle toggle)
        {
            alwaysOnTopToggle = toggle;
        }

        private static string FormatClock(float t, DayNightCycle.Phase phase)
        {
            float hours = t * 24f;
            int h = Mathf.FloorToInt(hours) % 24;
            int m = Mathf.FloorToInt((hours - Mathf.Floor(hours)) * 60f);
            string phaseLabel = phase switch
            {
                DayNightCycle.Phase.Dawn => "Dawn",
                DayNightCycle.Phase.Day => "Day",
                DayNightCycle.Phase.Dusk => "Dusk",
                _ => "Night"
            };
            return $"{h:00}:{m:00}  {phaseLabel}";
        }
    }
}
