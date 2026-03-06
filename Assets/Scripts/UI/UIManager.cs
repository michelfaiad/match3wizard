using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Match3Wizard
{
    /// <summary>
    /// Drives all UI: HUD mana bars, timer, result screen, gallery, spell targeting.
    /// Wire up all serialized references in the Inspector on the UIManager prefab.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        // ── HUD ───────────────────────────────────────────────────────────────
        [Header("HUD")]
        [SerializeField] private Slider[]   manaBars        = new Slider[4]; // Fire, Water, Air, Earth
        [SerializeField] private Button[]   elementButtons  = new Button[5]; // all 5 elements
        [SerializeField] private TMP_Text   timerText;
        [SerializeField] private TMP_Text[] crystalCounters = new TMP_Text[5];
        [SerializeField] private Slider     spiritProgressBar;
        [SerializeField] private TMP_Text   spiritProgressLabel;

        // ── Screens ───────────────────────────────────────────────────────────
        [Header("Screens")]
        [SerializeField] private GameObject screenMainMenu;
        [SerializeField] private GameObject screenGame;
        [SerializeField] private GameObject screenPause;
        [SerializeField] private GameObject screenResult;
        [SerializeField] private GameObject screenGallery;
        [SerializeField] private GameObject screenShop;

        // ── Result Screen ─────────────────────────────────────────────────────
        [Header("Result Screen")]
        [SerializeField] private TMP_Text resultManaText;
        [SerializeField] private TMP_Text resultCrystalText;

        // ── Spell targeting overlay ───────────────────────────────────────────
        [Header("Spell Targeting")]
        [SerializeField] private GameObject targetingOverlay;

        private Action<ElementType, int, int> _pendingTargetCallback;
        private ElementType                   _pendingSpellElement;

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Subscribe to systems
            if (ManaSystem.Instance != null)
                ManaSystem.Instance.OnManaChanged += OnManaChanged;

            if (CrystalSystem.Instance != null)
                CrystalSystem.Instance.OnSessionCrystalsChanged += OnCrystalsChanged;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += OnStateChanged;
                GameManager.Instance.OnTimerTick    += OnTimerTick;
                GameManager.Instance.OnMatchEnded   += ShowResultScreen;
            }

            // Wire element buttons
            for (int i = 0; i < elementButtons.Length; i++)
            {
                int idx = i;
                elementButtons[i]?.onClick.AddListener(() => OnElementButtonPressed((ElementType)idx));
            }

            ShowScreen(screenMainMenu);
        }

        private void OnDestroy()
        {
            if (ManaSystem.Instance != null)
                ManaSystem.Instance.OnManaChanged -= OnManaChanged;
        }

        // ── Screen management ─────────────────────────────────────────────────

        private void ShowScreen(GameObject screen)
        {
            screenMainMenu?.SetActive(screen == screenMainMenu);
            screenGame?.SetActive(screen == screenGame);
            screenPause?.SetActive(screen == screenPause);
            screenResult?.SetActive(screen == screenResult);
            screenGallery?.SetActive(screen == screenGallery);
            screenShop?.SetActive(screen == screenShop);
        }

        // ── State changes ─────────────────────────────────────────────────────

        private void OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing: ShowScreen(screenGame);  break;
                case GameState.Paused:  ShowScreen(screenPause); break;
                case GameState.MainMenu: ShowScreen(screenMainMenu); break;
            }
        }

        // ── Timer ─────────────────────────────────────────────────────────────

        private void OnTimerTick(float timeLeft)
        {
            if (timerText == null) return;
            int secs = Mathf.CeilToInt(timeLeft);
            timerText.text  = secs.ToString();
            timerText.color = timeLeft <= 10f ? Color.red : Color.white;
        }

        // ── Mana bars ─────────────────────────────────────────────────────────

        private void OnManaChanged(ElementType e, int newTotal)
        {
            if ((int)e < manaBars.Length && manaBars[(int)e] != null)
            {
                int maxMana = GameManager.Instance.Config.manaCap;
                manaBars[(int)e].value = (float)newTotal / Mathf.Max(1, maxMana);
            }

            // Update button interactability
            if ((int)e < elementButtons.Length && elementButtons[(int)e] != null)
                elementButtons[(int)e].interactable = ManaSystem.Instance.CanActivateSpell(e);
        }

        // ── Crystals ──────────────────────────────────────────────────────────

        private void OnCrystalsChanged(ElementType e, int newTotal)
        {
            int idx = (int)e;
            if (idx < crystalCounters.Length && crystalCounters[idx] != null)
                crystalCounters[idx].text = newTotal.ToString();

            UpdateSpiritProgress();
        }

        private void UpdateSpiritProgress()
        {
            if (spiritProgressBar == null) return;

            // Find the closest-to-completion unfree spirit
            var config = GameManager.Instance.Config;
            float best = 0f;
            string label = "";

            for (int i = 0; i < 5; i++)
            {
                var e = (ElementType)i;
                if (ProgressionSystem.Instance.IsSpiritFreed(e)) continue;

                int goal    = config.GetSpiritGoal(e);
                int current = CrystalSystem.Instance.GetTotalCrystals(e);
                float pct   = (float)current / goal;
                if (pct > best) { best = pct; label = e.DisplayName(); }
            }

            spiritProgressBar.value = best;
            if (spiritProgressLabel != null) spiritProgressLabel.text = label;
        }

        // ── Element button pressed ────────────────────────────────────────────

        private void OnElementButtonPressed(ElementType e)
        {
            AudioManager.Instance?.PlayButtonSFX();
            SpellSystem.Instance?.TryCastSpell(e);
        }

        // ── Spell Targeting ───────────────────────────────────────────────────

        /// <summary>
        /// Called by SpellSystem to request player to pick a cell target.
        /// When a cell is tapped, callback is fired with (element, col, row).
        /// </summary>
        public void BeginSpellTargeting(ElementType e, Action<ElementType, int, int> callback)
        {
            _pendingSpellElement  = e;
            _pendingTargetCallback = callback;
            if (targetingOverlay != null) targetingOverlay.SetActive(true);
        }

        /// <summary>Called by cell tap handler during targeting mode.</summary>
        public void OnCellTappedForTargeting(int col, int row)
        {
            if (_pendingTargetCallback == null) return;
            if (targetingOverlay != null) targetingOverlay.SetActive(false);
            _pendingTargetCallback.Invoke(_pendingSpellElement, col, row);
            _pendingTargetCallback = null;
        }

        // ── Result screen ─────────────────────────────────────────────────────

        private void ShowResultScreen()
        {
            ShowScreen(screenResult);
            AudioManager.Instance?.PlayResultJingle();

            if (resultManaText != null)
            {
                int total = 0;
                for (int i = 0; i < 4; i++)
                    total += ManaSystem.Instance?.GetMana((ElementType)i) ?? 0;
                resultManaText.text = $"Mana gerado: {total}";
            }

            if (resultCrystalText != null)
            {
                int total = 0;
                for (int i = 0; i < 5; i++)
                    total += CrystalSystem.Instance?.GetSessionCrystals((ElementType)i) ?? 0;
                resultCrystalText.text = $"Cristais obtidos: {total}";
            }
        }

        // ── Button handlers (wired via Inspector or here) ─────────────────────

        public void OnPlayButtonPressed()
        {
            AudioManager.Instance?.PlayButtonSFX();
            GameManager.Instance?.StartMatch();
        }

        public void OnPauseButtonPressed()
        {
            AudioManager.Instance?.PlayButtonSFX();
            GameManager.Instance?.PauseMatch();
        }

        public void OnResumeButtonPressed()
        {
            AudioManager.Instance?.PlayButtonSFX();
            GameManager.Instance?.ResumeMatch();
        }

        public void OnAbandonButtonPressed()
        {
            AudioManager.Instance?.PlayButtonSFX();
            GameManager.Instance?.AbandonMatch();
        }

        public void OnPlayAgainButtonPressed()
        {
            AudioManager.Instance?.PlayButtonSFX();
            GameManager.Instance?.StartMatch();
        }

        public void OnGalleryButtonPressed()
        {
            AudioManager.Instance?.PlayButtonSFX();
            ShowScreen(screenGallery);
        }

        public void OnShopButtonPressed()
        {
            AudioManager.Instance?.PlayButtonSFX();
            ShowScreen(screenShop);
        }

        public void OnBackToMenuButtonPressed()
        {
            AudioManager.Instance?.PlayButtonSFX();
            ShowScreen(screenMainMenu);
        }
    }
}
