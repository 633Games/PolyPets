using UnityEngine;
using UnityEngine.UI;

namespace PolyPets.UI
{
    /// <summary>
    /// Minimal HUD wiring for the desktop companion chrome (uGUI Text for zero package deps).
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private Text coinText;
        [SerializeField] private Text roomText;
        [SerializeField] private Toggle alwaysOnTopToggle;

        private int _coins;

        public void SetCoins(int coins)
        {
            _coins = coins;
            if (coinText != null)
                coinText.text = $"{_coins}";
        }

        public void SetRoomName(string roomName)
        {
            if (roomText != null)
                roomText.text = roomName;
        }

        public void BindAlwaysOnTopToggle(Toggle toggle)
        {
            alwaysOnTopToggle = toggle;
        }
    }
}
