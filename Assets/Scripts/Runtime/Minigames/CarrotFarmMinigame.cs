using System;
using System.Collections.Generic;
using UnityEngine;
using PolyPets.Pets;

namespace PolyPets.Minigames
{
    /// <summary>
    /// Rabbit Carrot Farm — baseline: Farm Rush / Carrot Farmer / Happy Harvest style.
    ///
    /// Farm Rush: keep moving, harvest ripe produce as it appears.
    /// Carrot Farmer / Happy Harvest: plant/grow stages, interact when ready, timed pressure.
    ///
    /// Loop: plots advance seed → sprout → ready → (overripe spoil).
    /// Run with WASD, step onto READY carrots to harvest. Spoiled plots reset.
    /// </summary>
    public sealed class CarrotFarmMinigame : MonoBehaviour, IPetMinigame
    {
        private enum Stage
        {
            Empty = 0,
            Seed = 1,
            Sprout = 2,
            Ready = 3,
            Spoiled = 4,
        }

        private struct Plot
        {
            public Stage Stage;
            public float Timer;
        }

        [SerializeField] private int fieldWidth = 5;
        [SerializeField] private int fieldHeight = 4;
        [SerializeField] private float roundSeconds = 22f;
        [SerializeField] private float seedSeconds = 1.2f;
        [SerializeField] private float sproutSeconds = 1.6f;
        [SerializeField] private float readySeconds = 2.8f; // time before spoil
        [SerializeField] private float spoilClearSeconds = 0.8f;
        [SerializeField] private int coinsPerCarrot = 4;
        [SerializeField] private int bonusStreakCoins = 2;
        [SerializeField] private int maxActivePlots = 7;

        private MinigameHud _hud;
        private Action<MinigameResult> _onDone;
        private bool _playing;
        private float _timeLeft;
        private int _px;
        private int _py;
        private int _collected;
        private int _spoiled;
        private int _coins;
        private int _streak;
        private Plot[,] _plots;

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
            _spoiled = 0;
            _coins = 0;
            _streak = 0;
            _plots = new Plot[fieldWidth, fieldHeight];
            for (int i = 0; i < 3; i++)
                PlantRandom(startReady: i == 0);
            RefreshHud("Carrots growing — harvest READY ones (C) before they spoil!");
        }

        public void Abort()
        {
            if (!_playing) return;
            Finish();
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

            TickGrowth();
            MaybePlant();
            HandleMove();
            TryHarvest();
            RefreshHud(null);
        }

        private void TickGrowth()
        {
            for (int x = 0; x < fieldWidth; x++)
            for (int y = 0; y < fieldHeight; y++)
            {
                var plot = _plots[x, y];
                if (plot.Stage == Stage.Empty)
                    continue;

                plot.Timer -= Time.deltaTime;
                if (plot.Timer > 0f)
                {
                    _plots[x, y] = plot;
                    continue;
                }

                switch (plot.Stage)
                {
                    case Stage.Seed:
                        plot.Stage = Stage.Sprout;
                        plot.Timer = sproutSeconds;
                        break;
                    case Stage.Sprout:
                        plot.Stage = Stage.Ready;
                        plot.Timer = readySeconds;
                        break;
                    case Stage.Ready:
                        plot.Stage = Stage.Spoiled;
                        plot.Timer = spoilClearSeconds;
                        _spoiled++;
                        _streak = 0;
                        break;
                    case Stage.Spoiled:
                        plot.Stage = Stage.Empty;
                        plot.Timer = 0f;
                        break;
                }

                _plots[x, y] = plot;
            }
        }

        private void MaybePlant()
        {
            if (CountActive() >= maxActivePlots)
                return;
            if (UnityEngine.Random.value < Time.deltaTime * 0.7f)
                PlantRandom(startReady: false);
        }

        private int CountActive()
        {
            int n = 0;
            for (int x = 0; x < fieldWidth; x++)
            for (int y = 0; y < fieldHeight; y++)
            {
                if (_plots[x, y].Stage != Stage.Empty)
                    n++;
            }

            return n;
        }

        private void PlantRandom(bool startReady)
        {
            for (int attempt = 0; attempt < 24; attempt++)
            {
                int x = UnityEngine.Random.Range(0, fieldWidth);
                int y = UnityEngine.Random.Range(0, fieldHeight);
                if (x == _px && y == _py)
                    continue;
                if (_plots[x, y].Stage != Stage.Empty)
                    continue;

                if (startReady)
                {
                    _plots[x, y] = new Plot { Stage = Stage.Ready, Timer = readySeconds };
                }
                else
                {
                    _plots[x, y] = new Plot { Stage = Stage.Seed, Timer = seedSeconds * UnityEngine.Random.Range(0.7f, 1.1f) };
                }

                return;
            }
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

        private void TryHarvest()
        {
            var plot = _plots[_px, _py];
            if (plot.Stage != Stage.Ready)
                return;

            _collected++;
            _streak++;
            int gain = coinsPerCarrot + (_streak >= 3 ? bonusStreakCoins : 0);
            _coins += gain;
            _plots[_px, _py] = default;
            PlantRandom(startReady: false);
        }

        private void RefreshHud(string banner)
        {
            if (_hud == null)
                return;

            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(banner))
                sb.AppendLine(banner);
            sb.AppendLine($"Time {_timeLeft:0.0}s · WASD move · step on READY (C)");
            sb.AppendLine("Legend: R you · . empty · , seed · c sprout · C ready · x spoil");
            sb.AppendLine(BuildField());
            string streak = _streak >= 3 ? " · STREAK!" : "";
            _hud.Show(sb.ToString().TrimEnd(), $"Carrots {_collected} · Spoiled {_spoiled} · Coins {_coins}{streak}");
        }

        private string BuildField()
        {
            var sb = new System.Text.StringBuilder();
            for (int y = fieldHeight - 1; y >= 0; y--)
            {
                for (int x = 0; x < fieldWidth; x++)
                {
                    if (x == _px && y == _py)
                    {
                        sb.Append("R ");
                        continue;
                    }

                    char c = _plots[x, y].Stage switch
                    {
                        Stage.Seed => ',',
                        Stage.Sprout => 'c',
                        Stage.Ready => 'C',
                        Stage.Spoiled => 'x',
                        _ => '.',
                    };
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
            float score = Mathf.Clamp01(_collected / 10f);
            _onDone?.Invoke(new MinigameResult("CarrotFarm", payout, score, completed: true));
        }
    }
}
