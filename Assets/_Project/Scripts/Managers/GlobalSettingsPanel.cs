using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrystalMind.MatchMancer
{
    public class GlobalSettingsPanel : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private bool persistAcrossScenes = true;
        [SerializeField] private string gameplaySceneName = "MainGame";
        [SerializeField] private string homeSceneName = "Home";

        [Header("References")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button settingsOpenButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button muteButton;
        [SerializeField] private Image muteIconImage;
        [SerializeField] private Sprite mutedSprite;
        [SerializeField] private Sprite unmutedSprite;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private UIPanelScaleTransition panelTransition;

        // Cache

        // State
        private bool hasLoggedMissingAudioManager;
        private bool isChangingVisibility;
        private bool isPanelOpen;
        private bool pausedBySettings;

        #endregion

        #region Properties

        public static GlobalSettingsPanel Instance { get; private set; }
        public bool IsOpen => isPanelOpen;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            RegisterButtonListeners();
            InitializeAudioControls();
            UpdateSceneAwareControls();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            Hide();
        }

        private void OnDestroy()
        {
            UnregisterButtonListeners();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            RestoreTimeScale();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Public Methods

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public void Toggle()
        {
            SetVisible(!IsOpen);
        }

        #endregion

        #region Private Methods

        private void RegisterButtonListeners()
        {
            if (settingsOpenButton != null)
            {
                settingsOpenButton.onClick.AddListener(HandleOpenClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (homeButton != null)
            {
                homeButton.onClick.AddListener(HandleHomeClicked);
            }

            if (muteButton != null)
            {
                muteButton.onClick.AddListener(HandleMuteClicked);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.AddListener(HandleBgmVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
            }
        }

        private void UnregisterButtonListeners()
        {
            if (settingsOpenButton != null)
            {
                settingsOpenButton.onClick.RemoveListener(HandleOpenClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }

            if (homeButton != null)
            {
                homeButton.onClick.RemoveListener(HandleHomeClicked);
            }

            if (muteButton != null)
            {
                muteButton.onClick.RemoveListener(HandleMuteClicked);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.RemoveListener(HandleBgmVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            }
        }

        private void InitializeAudioControls()
        {
            if (AudioManager.Instance == null)
            {
                WarnMissingAudioManager();
                return;
            }

            SetSliderValueWithoutNotify(masterVolumeSlider, AudioManager.Instance.GetMasterVolume());
            SetSliderValueWithoutNotify(bgmVolumeSlider, AudioManager.Instance.GetBgmVolume());
            SetSliderValueWithoutNotify(sfxVolumeSlider, AudioManager.Instance.GetSfxVolume());
            UpdateMuteIcon();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RestoreTimeScale();
            Hide();
            InitializeAudioControls();
            UpdateSceneAwareControls();
        }

        private void HandleOpenClicked()
        {
            Toggle();
        }

        private void HandleCloseClicked()
        {
            Hide();
        }

        private void HandleRestartClicked()
        {
            HideThen(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
        }

        private void HandleHomeClicked()
        {
            if (string.IsNullOrWhiteSpace(homeSceneName))
            {
                Debug.LogWarning("GlobalSettingsPanel: Home scene name is missing.");
                return;
            }

            HideThen(() =>
            {
                StageSession.ClearSelectedStage();
                SceneManager.LoadScene(homeSceneName);
            });
        }

        private void HandleMuteClicked()
        {
            if (AudioManager.Instance == null)
            {
                WarnMissingAudioManager();
                return;
            }

            AudioManager.Instance.ToggleMute();
            UpdateMuteIcon();
        }

        private void HandleMasterVolumeChanged(float value)
        {
            if (AudioManager.Instance == null)
            {
                WarnMissingAudioManager();
                return;
            }

            AudioManager.Instance.SetMasterVolume(value);
        }

        private void HandleBgmVolumeChanged(float value)
        {
            if (AudioManager.Instance == null)
            {
                WarnMissingAudioManager();
                return;
            }

            AudioManager.Instance.SetBgmVolume(value);
        }

        private void HandleSfxVolumeChanged(float value)
        {
            if (AudioManager.Instance == null)
            {
                WarnMissingAudioManager();
                return;
            }

            AudioManager.Instance.SetSfxVolume(value);
        }

        private void SetVisible(bool visible)
        {
            UpdateSceneAwareControls();

            if (visible)
            {
                ShowPanel();
                return;
            }

            HidePanel(null);
        }

        private void ShowPanel()
        {
            if (settingsPanel == null)
            {
                return;
            }

            if (isPanelOpen && !isChangingVisibility)
            {
                return;
            }

            isChangingVisibility = true;
            isPanelOpen = true;
            SetOpenButtonVisible(false);
            SetPanelControlsInteractable(false);
            settingsPanel.SetActive(true);
            SceneAudioLibrary.Current?.PlayPanelOpen();

            if (IsGameplayScene())
            {
                Time.timeScale = 0f;
                pausedBySettings = true;
            }

            if (panelTransition != null)
            {
                panelTransition.Show(() =>
                {
                    SetPanelControlsInteractable(true);
                    isChangingVisibility = false;
                });
                return;
            }

            settingsPanel.SetActive(true);
            SetPanelControlsInteractable(true);
            isChangingVisibility = false;
        }

        private void HidePanel(Action onComplete)
        {
            if (settingsPanel == null)
            {
                RestoreTimeScale();
                onComplete?.Invoke();
                return;
            }

            if (!isPanelOpen && !settingsPanel.activeSelf && !isChangingVisibility)
            {
                RestoreTimeScale();
                SetOpenButtonVisible(true);
                onComplete?.Invoke();
                return;
            }

            isChangingVisibility = true;
            isPanelOpen = false;
            SetOpenButtonVisible(true);
            SetPanelControlsInteractable(false);
            SceneAudioLibrary.Current?.PlayPanelClose();

            if (panelTransition != null)
            {
                panelTransition.Hide(() =>
                {
                    CompleteHidePanel();
                    onComplete?.Invoke();
                });
                return;
            }

            settingsPanel.SetActive(false);
            CompleteHidePanel();
            onComplete?.Invoke();
        }

        private void CompleteHidePanel()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            RestoreTimeScale();
            isChangingVisibility = false;
        }

        private void HideThen(Action onComplete)
        {
            HidePanel(onComplete);
        }

        private void SetOpenButtonVisible(bool visible)
        {
            if (settingsOpenButton != null)
            {
                settingsOpenButton.gameObject.SetActive(visible);
            }
        }

        private void SetPanelControlsInteractable(bool interactable)
        {
            if (closeButton != null)
            {
                closeButton.interactable = interactable;
            }

            if (restartButton != null)
            {
                restartButton.interactable = interactable;
            }

            if (homeButton != null)
            {
                homeButton.interactable = interactable;
            }

            if (muteButton != null)
            {
                muteButton.interactable = interactable;
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.interactable = interactable;
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.interactable = interactable;
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.interactable = interactable;
            }
        }

        private void SetSliderValueWithoutNotify(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            }
        }

        private void UpdateSceneAwareControls()
        {
            bool isGameplayScene = IsGameplayScene();

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(isGameplayScene);
            }

            if (homeButton != null)
            {
                homeButton.gameObject.SetActive(isGameplayScene);
            }
        }

        private bool IsGameplayScene()
        {
            return SceneManager.GetActiveScene().name == gameplaySceneName;
        }

        private void RestoreTimeScale()
        {
            if (pausedBySettings || Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }

            pausedBySettings = false;
        }

        private void UpdateMuteIcon()
        {
            if (muteIconImage == null || AudioManager.Instance == null)
            {
                return;
            }

            muteIconImage.sprite = AudioManager.Instance.IsMuted() ? mutedSprite : unmutedSprite;
        }

        private void WarnMissingAudioManager()
        {
            if (hasLoggedMissingAudioManager)
            {
                return;
            }

            hasLoggedMissingAudioManager = true;
            Debug.LogWarning("GlobalSettingsPanel: AudioManager is missing.");
        }

        #endregion
    }
}
