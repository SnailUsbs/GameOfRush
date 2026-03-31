using Reptile;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfRush
{
    public class GameOfRushPlayMode : MonoBehaviour
    {
        public static bool IsActive { get; private set; }
        public static bool IsRunning { get; private set; }
        public static GameOfRushPlayMode Instance { get; private set; }
        public static System.Action OnExitPlayMode;

        private GameObject _hintCanvas;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void EnsureHintCreated()
        {
            if (_hintCanvas != null) return;

            var uiManager = Core.Instance?.UIManager?.transform;
            if (uiManager == null) return;

            _hintCanvas = new GameObject("GameOfRush_Hint");
            _hintCanvas.transform.SetParent(uiManager, false);

            var canvas = _hintCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            _hintCanvas.AddComponent<CanvasScaler>();
            _hintCanvas.AddComponent<GraphicRaycaster>();

            var hintGo = new GameObject("HintLabel");
            hintGo.transform.SetParent(_hintCanvas.transform, false);

            var hintText = hintGo.AddComponent<TMPro.TextMeshProUGUI>();
            hintText.text = "-Dance Button To Exit Play Mode";
            hintText.fontSize = 22;
            hintText.color = Color.green;
            hintText.alignment = TMPro.TextAlignmentOptions.TopRight;
            hintText.enableWordWrapping = true;

            var hintRect = hintGo.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(1f, 1f);
            hintRect.anchorMax = new Vector2(1f, 1f);
            hintRect.pivot = new Vector2(1f, 1f);
            hintRect.sizeDelta = new Vector2(400f, 80f);
            hintRect.anchoredPosition = new Vector2(-16f, -10f);

            _hintCanvas.SetActive(false);
        }

        public static void Enter()
        {
            IsRunning = true;
            IsActive = true;
            Instance?.EnsureHintCreated();
            if (Instance?._hintCanvas != null)
                Instance._hintCanvas.SetActive(true);
        }

        public static void Exit(bool toBackground = false)
        {
            if (!IsActive) return;
            IsActive = false;
            if (Instance?._hintCanvas != null)
                Instance._hintCanvas.SetActive(false);
            if (!toBackground)
                IsRunning = false;
            OnExitPlayMode?.Invoke();
        }

        private void Update()
        {
            if (!IsActive) return;

            var player = WorldHandler.instance?.GetCurrentPlayer();
            if (player == null) return;
            if (player.danceButtonNew)
                Exit(GameOfRushPlugin.RunInBackground?.Value ?? false);
        }

        public static void SetGameScreenVisible(bool visible)
        {
        }
    }
}
