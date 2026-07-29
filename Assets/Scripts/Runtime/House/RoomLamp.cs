using UnityEngine;

namespace PolyPets.House
{
    /// <summary>
    /// Marks a warm room lamp so DayNightCycle can drive intensity on every active room.
    /// </summary>
    public sealed class RoomLamp : MonoBehaviour
    {
        [SerializeField] private Light lampLight;

        public Light LampLight => lampLight != null ? lampLight : GetComponent<Light>();

        public void Bind(Light light) => lampLight = light;

        private void Awake()
        {
            if (lampLight == null)
                lampLight = GetComponent<Light>();
        }
    }
}
