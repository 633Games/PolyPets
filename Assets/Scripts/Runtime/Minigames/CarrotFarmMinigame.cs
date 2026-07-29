using System;
using System.Collections.Generic;
using UnityEngine;
using PolyPets.Pets;

namespace PolyPets.Minigames
{
    /// <summary>
    /// Rabbit minigame — run a field and collect carrots as they grow.
    /// WASD/Arrows move · walk onto grown carrots to collect · lasts a short timed round.
    /// </summary>
    public sealed class CarrotFarmMinigame : MonoBehaviour, IPetMinigame
    {
        [SerializeField] private int fieldWidth = 5;
        [SerializeField] private int fieldHeight = 4;
        [SerializeField] private float roundSeconds = 20f;
        [SerializeField] private float growSeconds = 2.4f;
        [SerializeField] private int coinsPerCarrot = 4;
        [SerializeField] private int maxCarrots = 6;

        private struct Plot
        {
            public float GrowTimer;
            public bool Grown;
        }

        private MinigameHud _hud;
        private Action<MinigameResult> _onDone;
        private bool _playing;
        private float _timeLeft;
        private int _px;
        private int _py;
        private int _collected;
        private int _coins;
        private Plot[,] _plots;
        private readonly List<Vector2Int> _active = new();

        public bool IsPlaying => _playing;

        public void BindHud(MinigameHud hud) => _hud = hud;

        public void Play(PetAgent pet, Action<MinigameResult> onDone)
        {
            _onDone = onDone;
            _playing = true;
            _timeLeft = roundSeconds;
            _px = fieldWidth / 2;
            _py = fieldHeight / 2;
            _collected = 0;
            _coins = 0;
            _plots = new Plot[fieldWidth, fieldHeight];
            _active.Clear();
            SeedInitialCarrots(3);
            RefreshHud("Carrots are growing — run and collect!");
        }

        public void Abort()
        {
            if (!_playing) return;
            Finish();
        }

        private void SeedInitialCarrots(int count)
        {
            for (int i = 0; i < count; i++)
                SpawnCarrot(startGrown: i == 0);
        }

        private void SpawnCarrot(bool startGrown = false)
        {
            if (_active.Count >= maxCarrots)
                return;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                int x = UnityEngine.Random.Range(0, fieldWidth);
                int y = UnityEngine.Random.Range(0, fieldHeight);
                if (x == _px && y == _py)
                    continue;
                if (_active.Contains(new Vector2Int(x, y)))
                    continue;

                _plots[x, y] = new Plot
                {
                    GrowTimer = startGrown ? 0f : growSeconds * UnityEngine.Random.Range(0.4f, 1f),
                    Grown = startGrown,
                };
                _active.Add(new Vector2Int(x, y));
                return;
            }
        }

        private void Update()
        {
            if (!_playing)
                return;

            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f)
            {
                Finish();
                return;
            }

            // Grow plots
            for (int i = 0; i < _active.Count; i++)
            {
                var p = _active[i];
                var plot = _plots[p.x, p.y];
                if (plot.Grown)
                    continue;
                plot.GrowTimer -= Time.deltaTime;
                if (plot.GrowTimer <= 0f)
                    plot.Grown = true;
                _plots[p.x, p.y] = plot;
            }

            // Chance to sprout new carrots
            if (_active.Count < maxCarrots && UnityEngine.Random.value < Time.deltaTime * 0.55f)
                SpawnCarrot();

            HandleMove();
            TryCollect();
            RefreshHud(null);
        }

        private void HandleMove()
        {
            int dx = 0, dy = 0;
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dx = -1;
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dx = 1;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dy = 1;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dy = -1;
            if (dx == 0 && dy == 0)
                return;

            _px = Mathf.Clamp(_px + dx, 0, fieldWidth - 1);
            _py = Mathf.Clamp(_py + dy, 0, fieldHeight - 1);
        }

        private void TryCollect()
        {
            var here = new Vector2Int(_px, _py);
            if (!_active.Contains(here))
                return;

            var plot = _plots[_px, _py];
            if (!plot.Grown)
                return;

            _collected++;
            _coins += coinsPerCarrot;
            _active.Remove(here);
            _plots[_px, _py] = default;
            SpawnCarrot();
        }

        private void RefreshHud(string banner)
        {
            if (_hud == null)
                return;

            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(banner))
                linesAppend(sb, banner);
            linesAppend(sb, $"Time {_timeLeft:0.0}s · WASD move · step on grown carrots (C)");
            sb.AppendLine(BuildField());
            _hud.Show(sb.ToString().TrimEnd(), $"Carrots {_collected} · Coins {_coins}");
        }

        private static void linesAppend(System.Text.StringBuilder sb, string line) => sb.AppendLine(line);

        private string BuildField()
        {
            var sb = new System.Text.StringBuilder();
            for (int y = fieldHeight - 1; y >= 0; y--)
            {
                for (int x = 0; x < fieldWidth; x++)
                {
                    bool rabbit = x == _px && y == _py;
                    bool active = _active.Contains(new Vector2Int(x, y));
                    bool grown = active && _plots[x, y].Grown;

                    char c = '.';
                    if (grown) c = 'C';
                    else if (active) c = 'c'; // sprouting
                    if (rabbit) c = 'R';

                    sb.Append(c);
                    sb.Append(' ');
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private void Finish()
        {
            _playing = false;
            _hud?.Hide();
            int payout = _coins > 0 ? _coins : 2;
            float score = Mathf.Clamp01(_collected / 8f);
            _onDone?.Invoke(new MinigameResult("CarrotFarm", payout, score, completed: true));
        }
    }
}
