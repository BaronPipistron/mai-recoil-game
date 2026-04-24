using System.Collections;
using RecoilArena.Core;
using RecoilArena.Player;
using RecoilArena.UI;
using RecoilArena.Upgrades;
using RecoilArena.Waves;
using RecoilArena.Weapons;
using UnityEngine;

namespace RecoilArena.Core
{
    public class GameSession : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerCameraEffects cameraEffects;
        [SerializeField] private ShotgunWeapon shotgun;
        [SerializeField] private WaveDirector waveDirector;
        [SerializeField] private UpgradeSystem upgradeSystem;
        [SerializeField] private GameUIController gameUI;
        [SerializeField] private Transform playerSpawnPoint;

        [Header("Flow")]
        [SerializeField] private float nextWaveDelay = 2f;
        [SerializeField] private float postUpgradeAmmoGrant = 4f;

        private float _baseMaxHealth;
        private float _lastKnownHealth;
        private Coroutine _statusRoutine;

        public GameState CurrentState { get; private set; }

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }

            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>();
            }

            if (shotgun == null)
            {
                shotgun = FindFirstObjectByType<ShotgunWeapon>();
            }

            if (cameraEffects == null)
            {
                cameraEffects = FindFirstObjectByType<PlayerCameraEffects>();
            }

            if (waveDirector == null)
            {
                waveDirector = FindFirstObjectByType<WaveDirector>();
            }

            if (upgradeSystem == null)
            {
                upgradeSystem = FindFirstObjectByType<UpgradeSystem>();
            }

            if (gameUI == null)
            {
                gameUI = FindFirstObjectByType<GameUIController>();
            }

            _baseMaxHealth = playerHealth.MaxHealth;
            _lastKnownHealth = playerHealth.CurrentHealth;

            BindEvents();
            waveDirector.Setup(playerController.transform, playerHealth);
        }

        private void Start()
        {
            EnterMainMenu();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (CurrentState == GameState.Playing)
                {
                    PauseGame();
                }
                else if (CurrentState == GameState.Paused)
                {
                    ResumeGame();
                }
            }
        }

        private void BindEvents()
        {
            gameUI.StartClicked += StartRun;
            gameUI.QuitClicked += QuitGame;
            gameUI.ResumeClicked += ResumeGame;
            gameUI.RestartClicked += RestartRun;
            gameUI.MainMenuClicked += EnterMainMenu;
            gameUI.UpgradeSelected += OnUpgradeSelected;

            playerHealth.HealthChanged += OnPlayerHealthChanged;
            playerHealth.Died += OnPlayerDied;

            shotgun.AmmoChanged += gameUI.SetAmmo;
            shotgun.ShotFeedback += OnShotFeedback;

            waveDirector.WaveStarted += OnWaveStarted;
            waveDirector.WaveCleared += OnWaveCleared;
            waveDirector.ScoreChanged += gameUI.SetScore;
        }

        private void UnbindEvents()
        {
            if (gameUI != null)
            {
                gameUI.StartClicked -= StartRun;
                gameUI.QuitClicked -= QuitGame;
                gameUI.ResumeClicked -= ResumeGame;
                gameUI.RestartClicked -= RestartRun;
                gameUI.MainMenuClicked -= EnterMainMenu;
                gameUI.UpgradeSelected -= OnUpgradeSelected;
            }

            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= OnPlayerHealthChanged;
                playerHealth.Died -= OnPlayerDied;
            }

            if (shotgun != null)
            {
                shotgun.AmmoChanged -= gameUI.SetAmmo;
                shotgun.ShotFeedback -= OnShotFeedback;
            }

            if (waveDirector != null)
            {
                waveDirector.WaveStarted -= OnWaveStarted;
                waveDirector.WaveCleared -= OnWaveCleared;
                waveDirector.ScoreChanged -= gameUI.SetScore;
            }
        }

        private void StartRun()
        {
            Time.timeScale = 1f;
            upgradeSystem.ResetRun();

            playerController.ResetRuntimeTuning();
            playerController.WarpTo(playerSpawnPoint.position);
            playerController.transform.rotation = Quaternion.Euler(0f, playerSpawnPoint.eulerAngles.y, 0f);

            playerHealth.ResetHealth(_baseMaxHealth);
            _lastKnownHealth = playerHealth.CurrentHealth;

            shotgun.ResetRuntimeTuning();
            shotgun.RefillAllAmmo();

            waveDirector.StartRun();
            gameUI.ShowGameplayHud();
            gameUI.SetWave(0);
            SetControlState(true);
            SetState(GameState.Playing);
        }

        private void RestartRun()
        {
            StartRun();
        }

        private void EnterMainMenu()
        {
            Time.timeScale = 1f;
            waveDirector.StopRun();
            SetControlState(false);
            SetState(GameState.MainMenu);
            gameUI.ShowMainMenu();
            gameUI.SetWave(0);
            gameUI.SetScore(0);
            gameUI.SetAmmo(0, 0);
            gameUI.SetHP(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        private void PauseGame()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            Time.timeScale = 0f;
            SetControlState(false);
            SetState(GameState.Paused);
            gameUI.ShowPause();
        }

        private void ResumeGame()
        {
            if (CurrentState != GameState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            SetControlState(true);
            SetState(GameState.Playing);
            gameUI.ShowGameplayHud();
        }

        private void OnWaveStarted(int wave)
        {
            gameUI.SetWave(wave);
            if (CurrentState != GameState.GameOver && CurrentState != GameState.MainMenu)
            {
                SetState(GameState.Playing);
                SetControlState(true);
                gameUI.ShowGameplayHud();
                ShowTemporaryStatus($"WAVE {wave}", 1.35f);
            }
        }

        private void OnWaveCleared(int wave)
        {
            if (CurrentState == GameState.GameOver || CurrentState == GameState.MainMenu)
            {
                return;
            }

            SetState(GameState.WaveBreak);
            SetControlState(false);
            shotgun.AddAmmo(Mathf.CeilToInt(postUpgradeAmmoGrant + wave * 0.35f));

            UpgradeChoice[] choices = upgradeSystem.BuildChoices(3);
            gameUI.ShowWaveBreak(choices, wave);
        }

        private void OnUpgradeSelected(UpgradeChoice choice)
        {
            if (CurrentState != GameState.WaveBreak)
            {
                return;
            }

            upgradeSystem.ApplyChoice(choice, playerController, playerHealth, shotgun);
            gameUI.HideUpgradePanel();
            SetControlState(true);
            SetState(GameState.Playing);
            ShowTemporaryStatus("NEXT WAVE INBOUND", 1.1f);
            waveDirector.RequestNextWave(nextWaveDelay);
        }

        private void OnPlayerHealthChanged(float current, float max)
        {
            gameUI.SetHP(current, max);

            if (cameraEffects != null && current < _lastKnownHealth - 0.01f)
            {
                cameraEffects.PlayDamageShake(0.95f);
            }

            _lastKnownHealth = current;
        }

        private void OnShotFeedback(bool anyHit, bool weakSpot)
        {
            if (anyHit)
            {
                gameUI.ShowHitMarker(weakSpot);
            }
        }

        private void OnPlayerDied()
        {
            if (CurrentState == GameState.GameOver)
            {
                return;
            }

            Time.timeScale = 0f;
            SetControlState(false);
            SetState(GameState.GameOver);
            gameUI.ShowGameOver(waveDirector.Score, waveDirector.CurrentWave);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetControlState(bool enabled)
        {
            playerController.SetControlEnabled(enabled);
            shotgun.SetControlEnabled(enabled);
        }

        private void SetState(GameState newState)
        {
            CurrentState = newState;
        }

        private void ShowTemporaryStatus(string message, float duration)
        {
            if (_statusRoutine != null)
            {
                StopCoroutine(_statusRoutine);
            }

            _statusRoutine = StartCoroutine(StatusRoutine(message, duration));
        }

        private IEnumerator StatusRoutine(string message, float duration)
        {
            gameUI.ShowStatusBanner(message);
            yield return new WaitForSeconds(duration);

            if (CurrentState == GameState.Playing)
            {
                gameUI.ClearStatusBanner();
            }

            _statusRoutine = null;
        }
    }
}
