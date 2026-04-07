using UnityEngine;

namespace DotWars.Core
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        Victory,
        Defeat
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private GameConfig _gameConfig;

        public GameConfig Config => _gameConfig;
        public GameState State { get; private set; } = GameState.Menu;

        private int _timeScaleIndex;
        private readonly float[] _timeScales = { 1f, 2f, 4f };

        public float CurrentTimeScale => _timeScales[_timeScaleIndex];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _gameConfig = Resources.Load<GameConfig>("GameConfig");
            if (_gameConfig == null)
                Debug.LogError("[GameManager] GameConfig not found in Resources!");
        }

        public void SetState(GameState newState)
        {
            if (State == newState) return;

            var oldState = State;
            State = newState;

            Time.timeScale = newState == GameState.Playing ? _timeScales[_timeScaleIndex] : 0f;

            EventBus.OnGameStateChanged?.Invoke(oldState, newState);
        }

        public void CycleTimeScale()
        {
            _timeScaleIndex = (_timeScaleIndex + 1) % _timeScales.Length;

            if (State == GameState.Playing)
                Time.timeScale = _timeScales[_timeScaleIndex];
        }

        public void StartGame()
        {
            _timeScaleIndex = 0;
            SetState(GameState.Playing);
        }

        public void PauseGame()
        {
            if (State == GameState.Playing)
                SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (State == GameState.Paused)
                SetState(GameState.Playing);
        }
    }
}
