using System;
using System.Collections.Generic;
using UnityEngine;
using PolyPets.Pets;

namespace PolyPets.Minigames
{
    /// <summary>
    /// Dog minigame — dig the garden to find a snap target.
    /// Wrong holes fill back in. Dig the correct tile, then Snap (Space) to score.
    /// Controls: arrows/WASD move cursor · Space dig / snap · Esc finish early.
    /// </summary>
    public sealed class DigSnapMinigame : MonoBehaviour, IPetMinigame
    {
        [SerializeField] private int gridWidth = 4;
        [SerializeField] private int gridHeight = 3;
        [SerializeField] private int coinsPerSnap = 14;
        [SerializeField] private int snapsToWin = 3;
        [SerializeField] private float wrongFillDelay = 0.55f;

        private MinigameHud _hud;
        private Action<MinigameResult> _onDone;
        private bool _playing;
        private int _cursorX;
        private int _cursorY;
        private int _targetX;
        private int _targetY;
        private int _snaps;
        private int _coins;
        private bool _targetDug;
        private bool _filling;
        private readonly HashSet<Vector2Int> _openHoles = new();

        public bool IsPlaying => _playing;

        public void BindHud(MinigameHud hud) => _hud = hud;

        public void Play(PetAgent pet, Action<MinigameResult> onDone)
        {
            _onDone = onDone;
            _playing = true;
            _snaps = 0;
            _coins = 0;
            _cursorX = 0;
            _cursorY = 0;
            _openHoles.Clear();
            PlaceNewTarget();
            RefreshHud("Dig the garden! Find the buried snap toy.");
        }

        public void Abort()
        {
            if (!_playing) return;
            Finish();
        }

        private void PlaceNewTarget()
        {
            _targetDug = false;
            _filling = false;
            _openHoles.Clear();
            _targetX = UnityEngine.Random.Range(0, gridWidth);
            _targetY = UnityEngine.Random.Range(0, gridHeight);
        }

        private void Update()
        {
            if (!_playing || _filling)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Finish();
                return;
            }

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                _cursorX = Mathf.Max(0, _cursorX - 1);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                _cursorX = Mathf.Min(gridWidth - 1, _cursorX + 1);
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                _cursorY = Mathf.Min(gridHeight - 1, _cursorY + 1);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                _cursorY = Mathf.Max(0, _cursorY - 1);

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                if (_targetDug)
                    TrySnap();
                else
                    TryDig();
            }

            RefreshHud(null);
        }

        private void TryDig()
        {
            var cell = new Vector2Int(_cursorX, _cursorY);
            if (_openHoles.Contains(cell))
            {
                RefreshHud("Already dug here.");
                return;
            }

            if (_cursorX == _targetX && _cursorY == _targetY)
            {
                _openHoles.Add(cell);
                _targetDug = true;
                RefreshHud("Found it! Press Space to SNAP!");
                return;
            }

            // Wrong hole — show briefly then fill in.
            _openHoles.Add(cell);
            _filling = true;
            RefreshHud("Wrong hole — it fills back in…");
            Invoke(nameof(FillWrongHoles), wrongFillDelay);
        }

        private void FillWrongHoles()
        {
            _openHoles.Clear();
            _filling = false;
            RefreshHud("Garden patched. Keep digging!");
        }

        private void TrySnap()
        {
            if (!_targetDug)
                return;

            _snaps++;
            _coins += coinsPerSnap;
            RefreshHud($"SNAP! Got it ({_snaps}/{snapsToWin})");

            if (_snaps >= snapsToWin)
                Invoke(nameof(Finish), 0.65f);
            else
            {
                PlaceNewTarget();
                Invoke(nameof(PromptNext), 0.55f);
            }
        }

        private void PromptNext()
        {
            if (_playing)
                RefreshHud("New bury… dig again!");
        }

        private void RefreshHud(string banner)
        {
            if (_hud == null)
                return;

            var lines = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(banner))
                lines.AppendLine(banner);

            lines.AppendLine(_targetDug ? "SNAP ready — Space!" : "Move · Space to dig");
            lines.AppendLine(BuildGrid());
            _hud.Show(lines.ToString().TrimEnd(), $"Snaps {_snaps}/{snapsToWin} · Coins {_coins}");
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
                    bool isTargetOpen = open && x == _targetX && y == _targetY && _targetDug;

                    char c = '.';
                    if (isTargetOpen) c = '*';
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
            _playing = false;
            _hud?.Hide();
            float score = snapsToWin <= 0 ? 0f : (float)_snaps / snapsToWin;
            // Always grant earned coins; if they quit early with 0, tiny pity.
            int payout = _coins > 0 ? _coins : 2;
            _onDone?.Invoke(new MinigameResult("DigSnap", payout, score, completed: true));
        }
    }
}
