using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using CommonAPI.Phone;
using Reptile;

namespace GameOfRush.Phone
{
    public class AppGameOfRush : CustomApp
    {
        private GameObject _gameScreen;
        private RawImage _gridImage;
        private Texture2D _gridTex;

        private const int GridWidth = 192;
        private const int GridHeight = 192;
        private const int PixelSize = 15;

        private bool[,] _grid;
        private bool[,] _nextGrid;
        private float _updateTimer;
        private float _updateInterval = 0.1f;
        private System.Collections.Generic.HashSet<int> _history = new System.Collections.Generic.HashSet<int>();
        private const int MaxHistorySize = 50;

        public static void Initialize()
        {
            Sprite icon = null;
            string iconPath = GameOfRushPlugin.GetAppIconPath("AppIcon.png");
            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(iconPath);
                    var texture = new Texture2D(2, 2);
                    if (texture.LoadImage(bytes))
                    {
                        texture.wrapMode = TextureWrapMode.Clamp;
                        texture.filterMode = FilterMode.Bilinear;
                        texture.Apply();
                        icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    }
                }
                catch { }
            }
            if (icon != null) PhoneAPI.RegisterApp<AppGameOfRush>("GameOfRush", icon);
            else              PhoneAPI.RegisterApp<AppGameOfRush>("GameOfRush");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();
            CreateIconlessTitleBar("GameOfRush");
            ScrollView = PhoneScrollView.Create(this);
            GameOfRushPlayMode.OnExitPlayMode += HandleExitPlayMode;

            _grid = new bool[GridWidth, GridHeight];
            _nextGrid = new bool[GridWidth, GridHeight];

            ShowMainMenu();
        }

        public override void OnAppDisable()
        {
            base.OnAppDisable();
            if (!GameOfRushPlugin.RunInBackground.Value)
                StopGame();
        }

        private void Update()
        {
            if (!GameOfRushPlayMode.IsRunning) return;

            int speed = GameOfRushPlugin.GameSpeed?.Value ?? 1;
            if (speed < 0) speed = 1;

            if (speed == 0)
            {
                StepGameOfLife();
                if (_gridImage != null) RenderGrid();
                return;
            }

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= _updateInterval)
            {
                _updateTimer = 0f;
                for (int i = 0; i < speed; i++)
                {
                    StepGameOfLife();
                }
                if (_gridImage != null) RenderGrid();
            }
        }

        private void OnPlay()
        {
            RandomizeGrid();
            EnsureGameScreen();
            SetGameScreenVisible(true);
            GameOfRushPlayMode.Enter();
        }

        private void HandleExitPlayMode()
        {
            if (GameOfRushPlugin.RunInBackground.Value)
            {
                SetGameScreenVisible(false);
                ShowMainMenu();
            }
            else
            {
                StopGame();
                ShowMainMenu();
            }
        }

        private void StopGame()
        {
            SetGameScreenVisible(false);
        }

        private void ShowMainMenu()
        {
            SetGameScreenVisible(false);
            ScrollView.RemoveAllButtons();

            var playBtn = PhoneUIUtility.CreateSimpleButton("Play");
            playBtn.OnConfirm += OnPlay;
            ScrollView.AddButton(playBtn);

            var settingsBtn = PhoneUIUtility.CreateSimpleButton("Settings");
            settingsBtn.OnConfirm += ShowSettingsMenu;
            ScrollView.AddButton(settingsBtn);
        }

        private void ShowSettingsMenu()
        {
            ScrollView.RemoveAllButtons();

            var gameSpeedBtn = PhoneUIUtility.CreateSimpleButton(GetGameSpeedLabel());
            gameSpeedBtn.OnConfirm += () =>
            {
                int cur = GameOfRushPlugin.GameSpeed.Value;
                GameOfRushPlugin.GameSpeed.Value = cur == 1 ? 2 : cur == 2 ? 4 : cur == 4 ? 6 : 1;
                gameSpeedBtn.Label.text = GetGameSpeedLabel();
            };
            ScrollView.AddButton(gameSpeedBtn);

            var ColorOptionBtn = PhoneUIUtility.CreateSimpleButton(GetLcdOptionLabel());
            ColorOptionBtn.OnConfirm += () =>
            {
                GameOfRushPlugin.LcdOption.Value = (GameOfRushPlugin.LcdOption.Value + 1) % 18;
                ColorOptionBtn.Label.text = GetLcdOptionLabel();
                RenderGrid();
            };
            ScrollView.AddButton(ColorOptionBtn);

            var DebugOptionsBtn = PhoneUIUtility.CreateSimpleButton("Debug Options");
            DebugOptionsBtn.OnConfirm += ShowDebugOptionsMenu;
            ScrollView.AddButton(DebugOptionsBtn);

            var backBtn = PhoneUIUtility.CreateSimpleButton("Back");
            backBtn.OnConfirm += ShowMainMenu;
            ScrollView.AddButton(backBtn);
        }

        private void ShowDebugOptionsMenu()
        {
            ScrollView.RemoveAllButtons();

            var DebugspeedBtn = PhoneUIUtility.CreateSimpleButton(GetDebugGameSpeedLabel());
            DebugspeedBtn.OnConfirm += () =>
            {
                int cur = GameOfRushPlugin.GameSpeed.Value;
                GameOfRushPlugin.GameSpeed.Value = cur == 1 ? 2 : cur == 2 ? 4 : cur == 4 ? 6 : cur == 6 ? 0 : 1;
                DebugspeedBtn.Label.text = GetDebugGameSpeedLabel();
            };
            ScrollView.AddButton(DebugspeedBtn);

            var backBtn = PhoneUIUtility.CreateSimpleButton("Back");
            backBtn.OnConfirm += ShowSettingsMenu;
            ScrollView.AddButton(backBtn);
        }

        private void EnsureGameScreen()
        {
            if (_gameScreen != null) return;

            float displayW = GridWidth * PixelSize;
            float displayH = GridHeight * PixelSize;

            _gameScreen = new GameObject("GameOfRush_GameScreen", typeof(RectTransform));
            var rootRect = _gameScreen.GetComponent<RectTransform>();
            rootRect.SetParent(Content, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            var gridRect = gridGo.GetComponent<RectTransform>();
            gridRect.SetParent(rootRect, false);
            gridRect.anchorMin = gridRect.anchorMax = gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = Vector2.zero;
            gridRect.sizeDelta = new Vector2(displayW, displayH);

            _gridImage = gridGo.GetComponent<RawImage>();
            _gridImage.color = Color.white;
            _gridImage.raycastTarget = false;

            _gridTex = new Texture2D(GridWidth, GridHeight, TextureFormat.RGBA32, false);
            _gridTex.filterMode = FilterMode.Point;
            _gridTex.wrapMode = TextureWrapMode.Clamp;
            _gridImage.texture = _gridTex;

            _gameScreen.SetActive(false);
        }

        private void SetGameScreenVisible(bool visible)
        {
            if (_gameScreen != null) _gameScreen.SetActive(visible);
            if (ScrollView != null) ScrollView.gameObject.SetActive(!visible);
        }

        private void RandomizeGrid()
        {
            System.Random rand = new System.Random();
            for (int x = 0; x < GridWidth; x++)
                for (int y = 0; y < GridHeight; y++)
                    _grid[x, y] = rand.NextDouble() > 0.7;
            _history.Clear();
        }

        private int GetGridHash()
        {
            int hash = 0;
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    if (_grid[x, y])
                    {
                        hash = hash * 31 + x;
                        hash = hash * 31 + y;
                    }
                }
            }
            return hash;
        }

        private void StepGameOfLife()
        {
            bool anyChange = false;
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    int neighbors = CountNeighbors(x, y);
                    bool alive = _grid[x, y];
                    bool nextAlive;

                    if (alive && (neighbors < 2 || neighbors > 3))
                        nextAlive = false;
                    else if (!alive && neighbors == 3)
                        nextAlive = true;
                    else
                        nextAlive = alive;

                    if (nextAlive != alive) anyChange = true;
                    _nextGrid[x, y] = nextAlive;
                }
            }

            bool[,] temp = _grid;
            _grid = _nextGrid;
            _nextGrid = temp;

            if (anyChange)
            {
                int hash = GetGridHash();
                if (_history.Contains(hash))
                {
                    RandomizeGrid();
                    _history.Clear();
                }
                else
                {
                    _history.Add(hash);
                    if (_history.Count > MaxHistorySize)
                        _history.Clear();
                }
            }
            else
            {
                RandomizeGrid();
                _history.Clear();
            }
        }

        private int CountNeighbors(int x, int y)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = (x + dx + GridWidth) % GridWidth;
                    int ny = (y + dy + GridHeight) % GridHeight;
                    if (_grid[nx, ny]) count++;
                }
            }
            return count;
        }

        private void RenderGrid()
        {
            if (_gridTex == null) return;

            Color32[] pixels = new Color32[GridWidth * GridHeight];
            int lcdOption = GameOfRushPlugin.LcdOption?.Value ?? 0;

            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    pixels[(GridHeight - 1 - y) * GridWidth + x] = GetLcdPixelColor(_grid[x, y], lcdOption);
                }
            }

            _gridTex.SetPixels32(pixels);
            _gridTex.Apply(false, false);
        }

        private static Color32 GetLcdPixelColor(bool on, int option)
        {
            switch (option)
            {
                case 1:  return on ? new Color32(0,   0,   128, 255) : new Color32(216, 216, 192, 255);
                case 2:  return on ? new Color32(15,  56,  15,  255) : new Color32(139, 172, 15,  255);
                case 3:  return on ? new Color32(255, 255, 255, 255) : new Color32(0,   0,   0,   255);
                case 4:  return on ? new Color32(0,   100, 255, 255) : new Color32(0,   20,  40,  255);
                case 5:  return on ? new Color32(255, 0,   0,   255) : new Color32(40,  0,   0,   255);
                case 6:  return on ? new Color32(0,   255, 0,   255) : new Color32(0,   40,  0,   255);
                case 7:  return on ? new Color32(0,   255, 255, 255) : new Color32(0,   40,  40,  255);
                case 8:  return on ? new Color32(255, 255, 0,   255) : new Color32(40,  40,  0,   255);
                case 9:  return on ? new Color32(192, 192, 192, 255) : new Color32(32,  32,  32,  255);
                case 10: return on ? new Color32(255, 105, 180, 255) : new Color32(60,  20,  40,  255);
                case 11: return on ? new Color32(255, 165, 0,   255) : new Color32(50,  30,  0,   255);
                case 12: return on ? new Color32(148, 0,   211, 255) : new Color32(30,  0,   50,  255);
                case 13: return on ? new Color32(255, 192, 203, 255) : new Color32(50,  30,  35,  255);
                case 14: return on ? new Color32(220, 20,  60,  255) : new Color32(45,  5,   15,  255);
                case 15: return on ? new Color32(255, 228, 196, 255) : new Color32(50,  40,  35,  255);
                case 16: return on ? new Color32(139, 69,  19,  255) : new Color32(30,  15,  5,   255);
                case 17: return on ? new Color32(0,   255, 127, 255) : new Color32(0,   50,  25,  255);
                default: return on ? new Color32(0,   0,   0,   255) : new Color32(255, 255, 255, 255);
            }
        }

        private static string GetGameSpeedLabel() { int s = GameOfRushPlugin.GameSpeed.Value; return "Game Speed: " + (s >= 6 ? "6x" : s >= 4 ? "4x" : s >= 2 ? "2x" : "Normal"); }
        private static string GetDebugGameSpeedLabel() { int s = GameOfRushPlugin.GameSpeed.Value; return "Game Speed: " + (s == 0 ? "Unlimited" : s >= 6 ? "6x" : s >= 4 ? "4x" : s >= 2 ? "2x" : "Normal"); }
        private static string GetLcdOptionLabel() { string[] n = { "Mono", "Classic", "Green", "Inverted", "Blue", "Red", "Lime", "Cyan", "Yellow", "Grey", "HotPink", "Orange", "Purple", "Pink", "Crimson", "Bisque", "SaddleBrown", "SpringGreen" }; return "LCD: " + n[(GameOfRushPlugin.LcdOption?.Value ?? 0) % n.Length]; }
    }
}
