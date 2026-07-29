using UnityEngine;
using PolyPets.Economy;
using PolyPets.Feel;
using PolyPets.House;
using PolyPets.Pets;

namespace PolyPets.Economy
{
    /// <summary>
    /// World pickup coin on the floor. Rotates; click to collect into the wallet.
    /// </summary>
    public sealed class IdleFloorCoin : MonoBehaviour
    {
        [SerializeField] private float spinDegreesPerSecond = 110f;
        [SerializeField] private float bobAmplitude = 0.04f;
        [SerializeField] private float bobSpeed = 2.2f;
        [SerializeField] private int coinValue = 1;

        private Vector3 _basePos;
        private float _phase;
        private IdleCoinSpawner _spawner;
        private bool _collected;

        public void Init(IdleCoinSpawner spawner, int value, Material mat)
        {
            _spawner = spawner;
            coinValue = Mathf.Max(1, value);
            _basePos = transform.localPosition;
            _phase = Random.Range(0f, Mathf.PI * 2f);
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null && mat != null)
                renderer.sharedMaterial = mat;
            FeelTagBinder.EnsureTagOn(gameObject, FeelTagType.Pop, autoPlay: true);
        }

        private void Update()
        {
            transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, SpaceSpace.World);
            float y = Mathf.Sin((Time.time + _phase) * bobSpeed) * bobAmplitude;
            transform.localPosition = _basePos + new Vector3(0f, y, 0f);
        }

        private void OnMouseDown() => TryCollect();

        public bool TryCollect()
        {
            if (_collected)
                return false;
            _collected = true;

            EconomyService.Instance?.AddCoins(coinValue, "idle floor coin");
            var pet = _spawner != null ? _spawner.ActivePet : null;
            pet?.GetComponent<PetProgression>()?.AddXp(2f, "idle coin");

            FeelTagBinder.EnsureTagOn(gameObject, FeelTagType.Punch, autoPlay: false)?.Play();
            FeelBridge.TryPlayFeedback(gameObject, "Collect");
            PolyPets.Audio.JuicySfx.PlayCoinDing();
            PolyPets.Audio.JuicySfx.PlayPop();

            _spawner?.NotifyCollected(this);
            Destroy(gameObject);
            return true;
        }
    }

    /// <summary>
    /// Idle coin drip into the active room. Caps at 10 floor coins; no more spawn until collected.
    /// </summary>
    public sealed class IdleCoinSpawner : MonoBehaviour
    {
        public static IdleCoinSpawner Instance { get; private set; }

        [SerializeField] private HouseController house;
        [SerializeField] private Material coinMaterial;
        [SerializeField] private int maxCoinsInRoom = 10;
        [SerializeField] private float baseSecondsBetweenDrops = 9f;
        [SerializeField] private float minSecondsBetweenDrops = 3.5f;

        private readonly System.Collections.Generic.List<IdleFloorCoin> _active = new();
        private float _timer;

        public PetAgent ActivePet => house != null ? house.ActiveRoom?.Occupant : null;
        public int ActiveCount => _active.Count;

        private void Awake()
        {
            Instance = this;
            _timer = baseSecondsBetweenDrops * 0.5f;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Bind(HouseController houseController, Material mat)
        {
            house = houseController;
            coinMaterial = mat;
        }

        private void Update()
        {
            // Don't drip during scrub/minigame clutter? Allow always for AFK glance.
            if (house?.ActiveRoom == null || ActivePet == null)
                return;

            // Cap: no more drops while 10 are on the floor.
            PruneNulls();
            if (_active.Count >= maxCoinsInRoom)
                return;

            float interval = ComputeInterval();
            _timer -= Time.deltaTime;
            if (_timer > 0f)
                return;

            _timer = interval;
            SpawnOne();
        }

        private float ComputeInterval()
        {
            float interval = baseSecondsBetweenDrops;
            var progression = ActivePet != null ? ActivePet.GetComponent<PetProgression>() : null;
            if (progression != null)
                interval /= Mathf.Max(0.35f, progression.IdleDropRateMultiplier);

            var needs = ActivePet != null ? ActivePet.Needs : null;
            if (needs != null)
            {
                // Happier / cleaner pets drip a bit faster; sad/dirty slower.
                float mood = (needs.Happiness + needs.Cleanliness) * 0.005f; // 0-1
                interval *= Mathf.Lerp(1.35f, 0.75f, mood);
            }

            var buffs = HouseBuffs.Instance;
            if (buffs != null)
                interval /= Mathf.Lerp(1f, 1.25f, (buffs.CoinEarnMultiplier - 1f));

            return Mathf.Max(minSecondsBetweenDrops, interval);
        }

        private void SpawnOne()
        {
            var room = house.ActiveRoom;
            if (room == null)
                return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "IdleCoin";
            go.transform.SetParent(room.transform, false);
            float x = Random.Range(-1.6f, 1.6f);
            float z = Random.Range(-1.4f, 1.2f);
            go.transform.localPosition = new Vector3(x, 0.12f, z);
            go.transform.localScale = new Vector3(0.28f, 0.04f, 0.28f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var coin = go.AddComponent<IdleFloorCoin>();
            int value = 1;
            var progression = ActivePet != null ? ActivePet.GetComponent<PetProgression>() : null;
            if (progression != null && progression.Level >= 5)
                value = Random.value < 0.2f ? 2 : 1;
            coin.Init(this, value, coinMaterial);
            _active.Add(coin);
            FeelBridge.TryPlayFeedback(go, "CoinSpawn");
            PolyPets.Audio.JuicySfx.PlayCoinSpawn();
        }

        public void NotifyCollected(IdleFloorCoin coin)
        {
            _active.Remove(coin);
        }

        private void PruneNulls()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] == null)
                    _active.RemoveAt(i);
            }
        }

        /// <summary>When switching rooms, clear tracked list for coins that belonged to old room parenting.</summary>
        public void OnRoomChanged()
        {
            PruneNulls();
            // Coins stay in their room GameObject; only count those in the active room.
            var room = house?.ActiveRoom;
            _active.Clear();
            if (room == null)
                return;
            foreach (var coin in room.GetComponentsInChildren<IdleFloorCoin>(true))
                _active.Add(coin);
        }
    }
}
