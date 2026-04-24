using System;
using RecoilArena.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace RecoilArena.UI
{
    public class GameUIController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject upgradePanel;

        [Header("Main Menu")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;

        [Header("Pause")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseMainMenuButton;

        [Header("Game Over")]
        [SerializeField] private Text gameOverSummaryText;
        [SerializeField] private Button gameOverRestartButton;
        [SerializeField] private Button gameOverMainMenuButton;

        [Header("HUD")]
        [SerializeField] private Text hpText;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text waveText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text statusBannerText;
        [SerializeField] private Image hitMarkerImage;

        [Header("Upgrades")]
        [SerializeField] private Text upgradeTitleText;
        [SerializeField] private Button[] upgradeButtons;
        [SerializeField] private Text[] upgradeButtonTexts;

        private UpgradeChoice[] _currentChoices;
        private float _hitMarkerTimer;

        public event Action StartClicked;
        public event Action QuitClicked;
        public event Action ResumeClicked;
        public event Action RestartClicked;
        public event Action MainMenuClicked;
        public event Action<UpgradeChoice> UpgradeSelected;

        private void Awake()
        {
            HookButtons();
            SetHitMarkerAlpha(0f);
            ClearStatusBanner();
        }

        private void Update()
        {
            if (_hitMarkerTimer > 0f)
            {
                _hitMarkerTimer -= Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(_hitMarkerTimer / 0.15f);
                SetHitMarkerAlpha(alpha);
            }
            else if (hitMarkerImage != null && hitMarkerImage.color.a > 0f)
            {
                SetHitMarkerAlpha(0f);
            }
        }

        public void ShowMainMenu()
        {
            SetPanel(mainMenuPanel, true);
            SetPanel(hudPanel, true);
            SetPanel(pausePanel, false);
            SetPanel(gameOverPanel, false);
            SetPanel(upgradePanel, false);
            SetHudVisibility(false);
            ClearStatusBanner();
        }

        public void ShowGameplayHud()
        {
            SetPanel(mainMenuPanel, false);
            SetPanel(hudPanel, true);
            SetPanel(pausePanel, false);
            SetPanel(gameOverPanel, false);
            SetPanel(upgradePanel, false);
            SetHudVisibility(true);
            ClearStatusBanner();
        }

        public void ShowPause()
        {
            SetPanel(pausePanel, true);
            SetPanel(mainMenuPanel, false);
            SetPanel(gameOverPanel, false);
            SetPanel(upgradePanel, false);
            SetHudVisibility(true);
            ShowStatusBanner("PAUSED");
        }

        public void ShowGameOver(int finalScore, int reachedWave)
        {
            SetPanel(gameOverPanel, true);
            SetPanel(pausePanel, false);
            SetPanel(mainMenuPanel, false);
            SetPanel(upgradePanel, false);
            SetHudVisibility(true);
            ShowStatusBanner("SYSTEM FAILURE");

            if (gameOverSummaryText != null)
            {
                gameOverSummaryText.text = $"Wave {reachedWave}  |  Score {finalScore}";
            }
        }

        public void ShowWaveBreak(UpgradeChoice[] choices, int wave)
        {
            _currentChoices = choices;
            SetPanel(upgradePanel, true);
            SetPanel(mainMenuPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(gameOverPanel, false);
            SetHudVisibility(true);

            if (upgradeTitleText != null)
            {
                upgradeTitleText.text = $"Wave {wave} Cleared - Choose Upgrade";
            }

            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                bool active = i < choices.Length;
                if (upgradeButtons[i] != null)
                {
                    upgradeButtons[i].gameObject.SetActive(active);
                }

                if (active && i < upgradeButtonTexts.Length && upgradeButtonTexts[i] != null)
                {
                    UpgradeChoice choice = choices[i];
                    upgradeButtonTexts[i].text = $"{choice.Title}\n<size=22>{choice.Description}</size>";
                }
            }

            ShowStatusBanner("RECALIBRATION WINDOW");
        }

        public void HideUpgradePanel()
        {
            SetPanel(upgradePanel, false);
            ClearStatusBanner();
        }

        public void SetHP(float current, float max)
        {
            if (hpText != null)
            {
                hpText.text = $"HP {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            }
        }

        public void SetAmmo(int clip, int reserve)
        {
            if (ammoText != null)
            {
                ammoText.text = $"AMMO {clip}/{reserve}";
            }
        }

        public void SetWave(int wave)
        {
            if (waveText != null)
            {
                waveText.text = $"WAVE {wave}";
            }
        }

        public void SetScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE {score}";
            }
        }

        public void ShowHitMarker(bool weakSpot)
        {
            if (hitMarkerImage == null)
            {
                return;
            }

            hitMarkerImage.color = weakSpot ? new Color(1f, 0.3f, 0.24f, 1f) : new Color(1f, 1f, 1f, 1f);
            _hitMarkerTimer = weakSpot ? 0.2f : 0.15f;
            SetHitMarkerAlpha(1f);
        }

        public void ShowStatusBanner(string message)
        {
            if (statusBannerText != null)
            {
                statusBannerText.text = message;
                statusBannerText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
            }
        }

        public void ClearStatusBanner()
        {
            ShowStatusBanner(string.Empty);
        }

        private void HookButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(() => StartClicked?.Invoke());
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(() => QuitClicked?.Invoke());
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(() => ResumeClicked?.Invoke());
            }

            if (pauseRestartButton != null)
            {
                pauseRestartButton.onClick.AddListener(() => RestartClicked?.Invoke());
            }

            if (pauseMainMenuButton != null)
            {
                pauseMainMenuButton.onClick.AddListener(() => MainMenuClicked?.Invoke());
            }

            if (gameOverRestartButton != null)
            {
                gameOverRestartButton.onClick.AddListener(() => RestartClicked?.Invoke());
            }

            if (gameOverMainMenuButton != null)
            {
                gameOverMainMenuButton.onClick.AddListener(() => MainMenuClicked?.Invoke());
            }

            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                int capture = i;
                if (upgradeButtons[capture] != null)
                {
                    upgradeButtons[capture].onClick.AddListener(() => OnUpgradeButtonClicked(capture));
                }
            }
        }

        private void OnUpgradeButtonClicked(int index)
        {
            if (_currentChoices == null || index < 0 || index >= _currentChoices.Length)
            {
                return;
            }

            UpgradeSelected?.Invoke(_currentChoices[index]);
        }

        private void SetPanel(GameObject panel, bool visible)
        {
            if (panel != null)
            {
                panel.SetActive(visible);
            }
        }

        private void SetHudVisibility(bool visible)
        {
            if (hpText != null) hpText.gameObject.SetActive(visible);
            if (ammoText != null) ammoText.gameObject.SetActive(visible);
            if (waveText != null) waveText.gameObject.SetActive(visible);
            if (scoreText != null) scoreText.gameObject.SetActive(visible);
            if (hitMarkerImage != null) hitMarkerImage.gameObject.SetActive(visible);
        }

        private void SetHitMarkerAlpha(float alpha)
        {
            if (hitMarkerImage == null)
            {
                return;
            }

            Color c = hitMarkerImage.color;
            c.a = alpha;
            hitMarkerImage.color = c;
        }
    }
}
