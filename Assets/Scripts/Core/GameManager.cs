using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Match3Wizard
{
    public enum GameState { MainMenu, Playing, Paused, Result }

    /// <summary>
    /// Central singleton. Owns the match timer and game state machine.
    /// All other singletons register/deregister themselves via their Awake/OnDestroy.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [SerializeField] private GameConfig config;

        // ── State ─────────────────────────────────────────────────────────────
        public GameState State { get; private set; } = GameState.MainMenu;
        public float     TimeLeft { get; private set; }
        public bool      IsTimerRunning => State == GameState.Playing;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<GameState> OnStateChanged;
        public event Action<float>     OnTimerTick;      // fires every frame with TimeLeft
        public event Action            OnMatchEnded;

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SaveSystem.Load();
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            TimeLeft -= Time.deltaTime;
            OnTimerTick?.Invoke(TimeLeft);

            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                EndMatch();
            }
        }

        private void OnApplicationQuit() => SaveSystem.Save();

        // ── Public API ────────────────────────────────────────────────────────

        public GameConfig Config => config;

        public void StartMatch()
        {
            TimeLeft = config.matchDuration;
            SetState(GameState.Playing);
            BoardManager.Instance?.InitBoard();
            ManaSystem.Instance?.ResetSession();
            CrystalSystem.Instance?.ResetSession();
        }

        public void PauseMatch()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
        }

        public void ResumeMatch()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void AbandonMatch()
        {
            EndMatch();
            LoadScene("MainMenu");
        }

        public void EndMatch()
        {
            SetState(GameState.Result);

            // Persist crystals + mana earned this session
            CrystalSystem.Instance?.FlushSessionToSave();
            ManaSystem.Instance?.FlushSessionToSave();
            ProgressionSystem.Instance?.CheckSpiritUnlocks();
            AchievementSystem.Instance?.CheckEndOfMatch();
            SaveSystem.Save();

            OnMatchEnded?.Invoke();
        }

        public void LoadScene(string sceneName) =>
            SceneManager.LoadScene(sceneName);

        // ── Private ───────────────────────────────────────────────────────────

        private void SetState(GameState next)
        {
            State = next;
            OnStateChanged?.Invoke(next);
        }
    }
}
