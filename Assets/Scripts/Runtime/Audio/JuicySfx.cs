using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PolyPets.Audio
{
    public enum SfxId
    {
        UiClick,
        CoinSpawn,
        CoinDing,
        CoinCascade,
        Purchase,
        Deny,
        Feed,
        CleanSparkle,
        ScrubTick,
        LevelUp,
        MinigameWin,
        Hit,
        Miss,
        RoomWhoosh,
        Celebrate,
        Pop,
        ReelTick,
    }

    /// <summary>
    /// Dense payout juice — pachislot-style dings, cascades, and win stingers.
    /// Designed so every care/earn action sings like a Japanese medal game.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class JuicySfx : MonoBehaviour
    {
        public static JuicySfx Instance { get; private set; }

        [SerializeField] private float masterVolume = 0.85f;
        [SerializeField] private int poolSize = 12;
        [SerializeField] private bool muted;

        [Header("Clips (wired by bootstrap)")]
        [SerializeField] private AudioClip uiClick;
        [SerializeField] private AudioClip coinSpawn;
        [SerializeField] private AudioClip coinDing;
        [SerializeField] private AudioClip[] coinDingLadder;
        [SerializeField] private AudioClip coinCascade;
        [SerializeField] private AudioClip purchase;
        [SerializeField] private AudioClip deny;
        [SerializeField] private AudioClip feed;
        [SerializeField] private AudioClip cleanSparkle;
        [SerializeField] private AudioClip scrubTick;
        [SerializeField] private AudioClip levelUp;
        [SerializeField] private AudioClip minigameWin;
        [SerializeField] private AudioClip hit;
        [SerializeField] private AudioClip miss;
        [SerializeField] private AudioClip roomWhoosh;
        [SerializeField] private AudioClip celebrate;
        [SerializeField] private AudioClip pop;
        [SerializeField] private AudioClip reelTick;

        private readonly List<AudioSource> _pool = new();
        private int _poolIndex;
        private int _combo;
        private float _comboResetAt;
        private Coroutine _cascadeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                return;
            Instance = this;
            EnsurePool();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetMuted(bool value) => muted = value;
        public void SetMasterVolume(float v) => masterVolume = Mathf.Clamp01(v);

        public void BindClips(
            AudioClip uiClickClip,
            AudioClip coinSpawnClip,
            AudioClip coinDingClip,
            AudioClip[] coinLadder,
            AudioClip coinCascadeClip,
            AudioClip purchaseClip,
            AudioClip denyClip,
            AudioClip feedClip,
            AudioClip cleanSparkleClip,
            AudioClip scrubTickClip,
            AudioClip levelUpClip,
            AudioClip minigameWinClip,
            AudioClip hitClip,
            AudioClip missClip,
            AudioClip roomWhooshClip,
            AudioClip celebrateClip,
            AudioClip popClip,
            AudioClip reelTickClip)
        {
            uiClick = uiClickClip;
            coinSpawn = coinSpawnClip;
            coinDing = coinDingClip;
            coinDingLadder = coinLadder;
            coinCascade = coinCascadeClip;
            purchase = purchaseClip;
            deny = denyClip;
            feed = feedClip;
            cleanSparkle = cleanSparkleClip;
            scrubTick = scrubTickClip;
            levelUp = levelUpClip;
            minigameWin = minigameWinClip;
            hit = hitClip;
            miss = missClip;
            roomWhoosh = roomWhooshClip;
            celebrate = celebrateClip;
            pop = popClip;
            reelTick = reelTickClip;
        }

        public static void Play(SfxId id, float volumeScale = 1f, float pitch = 1f)
        {
            Instance?.PlayInternal(id, volumeScale, pitch);
        }

        public static void PlayUiClick() => Play(SfxId.UiClick, 0.75f, Random.Range(0.96f, 1.06f));
        public static void PlayCoinSpawn() => Play(SfxId.CoinSpawn, 0.55f, Random.Range(0.95f, 1.08f));
        public static void PlayPurchase() => Play(SfxId.Purchase, 0.9f);
        public static void PlayDeny() => Play(SfxId.Deny, 0.7f);
        public static void PlayFeed() => Play(SfxId.Feed, 0.85f, Random.Range(0.95f, 1.05f));
        public static void PlayClean() => Play(SfxId.CleanSparkle, 0.9f);
        public static void PlayScrubTick() => Play(SfxId.ScrubTick, 0.45f, Random.Range(0.9f, 1.15f));
        public static void PlayLevelUp() => Play(SfxId.LevelUp, 1f);
        public static void PlayMinigameWin() => Play(SfxId.MinigameWin, 1f);
        public static void PlayHit() => Play(SfxId.Hit, 0.85f, Random.Range(0.95f, 1.08f));
        public static void PlayMiss() => Play(SfxId.Miss, 0.65f);
        public static void PlayRoomWhoosh() => Play(SfxId.RoomWhoosh, 0.55f);
        public static void PlayCelebrate() => Play(SfxId.Celebrate, 1f);
        public static void PlayPop() => Play(SfxId.Pop, 0.7f, Random.Range(0.95f, 1.1f));
        public static void PlayReelTick() => Play(SfxId.ReelTick, 0.4f, Random.Range(0.9f, 1.2f));

        /// <summary>
        /// Rising-pitch medal ding — consecutive collects climb a ladder like a slot payout.
        /// </summary>
        public static void PlayCoinDing()
        {
            if (Instance == null)
                return;
            Instance.BumpCombo();
            int step = Mathf.Clamp(Instance._combo - 1, 0, 5);
            var ladder = Instance.coinDingLadder;
            if (ladder != null && step < ladder.Length && ladder[step] != null)
            {
                Instance.PlayClip(ladder[step], 0.9f, 1f);
                return;
            }

            float pitch = 1f + step * 0.07f;
            Instance.PlayInternal(SfxId.CoinDing, 0.9f, pitch);
        }

        /// <summary>
        /// Pachislot-style shower: cascade clip + rapid ladder dings for big payouts.
        /// </summary>
        public static void PlayCoinPayout(int amount)
        {
            if (Instance == null || amount <= 0)
                return;

            if (amount >= 8)
            {
                Instance.PlayInternal(SfxId.CoinCascade, 1f, 1f);
                Instance.StartCascadeDings(Mathf.Min(amount, 12));
            }
            else if (amount >= 3)
            {
                Instance.StartCascadeDings(amount);
            }
            else
            {
                PlayCoinDing();
                if (amount == 2)
                    Instance.DelayedDing(0.07f, 1);
            }
        }

        private void BumpCombo()
        {
            if (Time.unscaledTime > _comboResetAt)
                _combo = 0;
            _combo++;
            _comboResetAt = Time.unscaledTime + 0.85f;
        }

        private void StartCascadeDings(int count)
        {
            if (_cascadeRoutine != null)
                StopCoroutine(_cascadeRoutine);
            _cascadeRoutine = StartCoroutine(CascadeDings(count));
        }

        private void DelayedDing(float delay, int step)
        {
            StartCoroutine(DingAfter(delay, step));
        }

        private IEnumerator DingAfter(float delay, int step)
        {
            yield return new WaitForSecondsRealtime(delay);
            var ladder = coinDingLadder;
            if (ladder != null && step < ladder.Length && ladder[step] != null)
                PlayClip(ladder[step], 0.85f, 1f);
            else
                PlayInternal(SfxId.CoinDing, 0.85f, 1f + step * 0.07f);
        }

        private IEnumerator CascadeDings(int count)
        {
            for (int i = 0; i < count; i++)
            {
                int step = i % 6;
                var ladder = coinDingLadder;
                if (ladder != null && step < ladder.Length && ladder[step] != null)
                    PlayClip(ladder[step], 0.7f, 1f);
                else
                    PlayInternal(SfxId.CoinDing, 0.7f, 1f + step * 0.06f);
                yield return new WaitForSecondsRealtime(0.055f);
            }
            _cascadeRoutine = null;
        }

        private void PlayInternal(SfxId id, float volumeScale, float pitch)
        {
            var clip = Resolve(id);
            if (clip == null)
                return;
            PlayClip(clip, volumeScale, pitch);
        }

        private void PlayClip(AudioClip clip, float volumeScale, float pitch)
        {
            if (muted || clip == null || masterVolume <= 0.001f)
                return;

            EnsurePool();
            var src = _pool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _pool.Count;
            src.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            src.PlayOneShot(clip, Mathf.Clamp01(masterVolume * volumeScale));
        }

        private AudioClip Resolve(SfxId id) => id switch
        {
            SfxId.UiClick => uiClick,
            SfxId.CoinSpawn => coinSpawn,
            SfxId.CoinDing => coinDing,
            SfxId.CoinCascade => coinCascade,
            SfxId.Purchase => purchase,
            SfxId.Deny => deny,
            SfxId.Feed => feed,
            SfxId.CleanSparkle => cleanSparkle,
            SfxId.ScrubTick => scrubTick,
            SfxId.LevelUp => levelUp,
            SfxId.MinigameWin => minigameWin,
            SfxId.Hit => hit,
            SfxId.Miss => miss,
            SfxId.RoomWhoosh => roomWhoosh,
            SfxId.Celebrate => celebrate,
            SfxId.Pop => pop,
            SfxId.ReelTick => reelTick,
            _ => null,
        };

        private void EnsurePool()
        {
            while (_pool.Count < poolSize)
            {
                var go = new GameObject($"SfxVoice_{_pool.Count}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f;
                src.priority = 64;
                _pool.Add(src);
            }
        }
    }
}
