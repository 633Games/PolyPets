using System;
using System.Collections.Generic;
using UnityEngine;
using PolyPets.Pets;

namespace PolyPets.Minigames
{
    /// <summary>
    /// Dog Dig + Snap — baseline: backyard dig-for-treasure + whack-a-mole / prairie-dog snap.
    ///
    /// Dig treasure games: probe garden tiles for buried loot; empty holes aren't rewarding.
    /// Whack-a-mole / pesky moles: target pops from a hole — hit it in a short reaction window.
    ///
    /// Our twist (design): wrong digs <b>fill back in</b>. Correct dig reveals a toy → SNAP window.
    /// If you miss the snap, it ducks under and the hole fills — dig again.
    /// </summary>
    public sealed class DigSnapMinigame : MonoBehaviour, IPetMinigame
    {
        private enum Phase
        {
            Digging,
            SnapWindow,
            Filling,
            Between,
            Done,
        }

        [SerializeField] private int gridWidth = 4;
        [SerializeField] private int gridHeight = 3;
        [SerializeField] private int snapsToWin = 3;
        [SerializeField] private int coinsPerSnap = 14;
        [SerializeField] private float wrongFillDelay = 0.65f;
        [SerializeField] private float snapWindowSeconds = 0.9f;
        [SerializeField] private float missFillDelay = 0.55f;

        private MinigameHud _hud;
        private Action<MinigameResult> _onDone;
        private Phase _phase;
        private bool _playing;
        private int _cursorX;
        private int _cursorY;
        private int _targetX;
        private int _targetY;
        private int _snaps;
        private int _coins;
        private float _snapEndsAt;
        private readonly HashSet<Vector2Int> _openHoles = new();
        private string _banner = "";

        public bool IsPlaying => _playing;

        public void BindHud(MinigameHud hud) => _hud = hud;

        public void Play(PetAgent pet, Action<MinigameResult> onDone)
        {
            CancelInvoke();
            _onDone = onDone;
            _playing = true;
            _snaps = 0;
            _coins = 0;
            _cursorX = gridWidth / 2;
            _cursorY = gridHeight / 2;
            _banner = "Dig for the buried toy — wrong holes fill in!";
            PlaceNewTarget();
            _phase = Phase.Digging;
            RefreshHud();
        }

        public void Abort()
        {
            if (!_playing) return;
            Finish();
        }

        private void PlaceNewTarget()
        {
            _openHoles.Clear();
            _targetX = UnityEngine.Random.Range(0, gridWidth);
            _targetY = UnityEngine.Random.Range(0, gridHeight);
        }

        private void Update()
        {
            if (!_playing || _phase == Phase.Filling || _phase == Phase.Between || _phase == Phase.Done)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Finish();
                return;
            }

            if (_phase == Phase.Digging)
            {
                HandleMove();
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    TryDig();
                RefreshHud();
            }
            else if (_phase == Phase.SnapWindow)
            {
                // Cursor locked on the pop hole during snap (mole is here).
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    DoSnap(success: true);
                    return;
                }

                if (Time.time >= _snapEndsAt)
                {
                    DoSnap(success: false);
                    return;
                }

                RefreshHud();
            }
        }

        private void HandleMove()
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                _cursorX = Mathf.Max(0, _cursorX - 1);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                _cursorX = Mathf.Min(gridWidth - 1, _cursorX + 1);
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                _cursorY = Mathf.Min(gridHeight - 1, _cursorY + 1);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                _cursorY = Mathf.Max(0, _cursorY - 1);
        }

        private void TryDig()
        {
            var cell = new Vector2Int(_cursorX, _cursorY);
            if (_openHoles.Contains(cell))
            {
                _banner = "Already dug — pick another tile.";
                return;
            }

            if (_cursorX == _targetX && _cursorY == _targetY)
            {
                _openHoles.Add(cell);
                _phase = Phase.SnapWindow;
                _snapEndsAt = Time.time + snapWindowSeconds;
                _banner = "Toy popped up! SNAP (Space) before it ducks!";
                RefreshHud();
                return;
            }

            // Empty dig — show hole then fill (treasure-hunt miss).
            _openHoles.Add(cell);
            _phase = Phase.Filling;
            _banner = "Nothing… hole fills back in.";
            RefreshHud();
            Invoke(nameof(AfterWrongFill), wrongFillDelay);
        }

        private void AfterWrongFill()
        {
            _openHoles.Clear();
            _phase = Phase.Digging;
            _banner = "Garden patched. Keep digging!";
            RefreshHud();
        }

        private void DoSnap(bool success)
        {
            if (success)
            {
                _snaps++;
                _coins += coinsPerSnap;
                _banner = $"SNAP! ({_snaps}/{snapsToWin}) +{coinsPerSnap}c";
                _phase = Phase.Between;
                PolyPets.Audio.JuicySfx.PlayHit();
                PolyPets.Audio.JuicySfx.PlayCoinDing();
                RefreshHud();

                if (_snaps >= snapsToWin)
                    Invoke(nameof(Finish), 0.7f);
                else
                    Invoke(nameof(NextTarget), 0.7f);
            }
            else
            {
                _coins += 1;
                _banner = "Too slow — it ducked under. Hole fills…";
                _phase = Phase.Filling;
                PolyPets.Audio.JuicySfx.PlayMiss();
                RefreshHud();
                Invoke(nameof(AfterMissFill), missFillDelay);
            }
        }

        private void AfterMissFill()
        {
            _openHoles.Clear();
            // Same bury site moves (toy relocates).
            PlaceNewTarget();
            _phase = Phase.Digging;
            _banner = "It re-buried somewhere else. Dig!";
            RefreshHud();
        }

        private void NextTarget()
        {
            PlaceNewTarget();
            _phase = Phase.Digging;
            _banner = "New bury… dig again!";
            RefreshHud();
        }

        private void RefreshHud()
        {
            if (_hud == null)
                return;

            string prompt =
                $"{_banner}\n" +
                (_phase == Phase.SnapWindow
                    ? $"SNAP NOW — {Mathf.Max(0f, _snapEndsAt - Time.time):0.00}s\n"
                    : "WASD move · Space dig\n") +
                "Legend: + cursor · o dug · * pop · . soil\n" +
                BuildGrid();

            _hud.Show(prompt.TrimEnd(), $"Snaps {_snaps}/{snapsToWin} · Coins {_coins}");
        }

        private string BuildGrid()
        {
            var sb = new System.Text.StringBuilder();
            for (int y = gridHeight - 1; y >= 0; y--)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    bool cursor = x == _cursorX && y == _cursorY;
                    bool open = _openHoles.Contains(new Vector2Int(x, y));
                    bool pop = open && x == _targetX && y == _targetY && _phase == Phase.SnapWindow;

                    char c = '.';
                    if (pop) c = '*';
                    else if (open) c = 'o';
                    if (cursor) c = c == '.' ? '+' : char.ToUpperInvariant(c);

                    sb.Append(c);
                    sb.Append(' ');
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private void Finish()
        {
            CancelInvoke();
            _phase = Phase.Done;
            _playing = false;
            _hud?.Hide();
            float score = snapsToWin <= 0 ? 0f : (float)_snaps / snapsToWin;
            int payout = _coins > 0 ? _coins : 2;
            _onDone?.Invoke(new MinigameResult("DigSnap", payout, score, completed: true));
        }
    }
}
