using CrystalMind.MatchMancer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    #region Variables

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_Text playerHpText;
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text currentStageText;
    [SerializeField] private TMP_Text currentRoundText;
    [SerializeField] private TMP_Text passiveStackText;
    [SerializeField] private TMP_Text playerSkillText;
    [SerializeField] private TMP_Text enemySkillText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultDescriptionText;
    [SerializeField] private GameObject[] resultStarObjects;
    [SerializeField] private Image playerHealthFillImage;
    [SerializeField] private Image enemyHealthFillImage;
    [SerializeField] private Image passiveChargeFillImage;
    [SerializeField] private Image activeSkillBlockerImage;
    [SerializeField] private Button activeSkillButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button backToHomeButton;
    [SerializeField] private SceneAudioLibrary sceneAudioLibrary;
    [SerializeField] private UIPanelScaleTransition resultPanelTransition;

    // State
    private bool hasSearchedSceneAudioLibrary;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(HandleRestartClicked);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(HandleRetryClicked);
        }

        if (backToHomeButton != null)
        {
            backToHomeButton.onClick.AddListener(HandleBackToHomeClicked);
        }

        HideResult();
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(HandleRestartClicked);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(HandleRetryClicked);
        }

        if (backToHomeButton != null)
        {
            backToHomeButton.onClick.RemoveListener(HandleBackToHomeClicked);
        }

    }

    private void Update()
    {
        Refresh();
    }

    #endregion

    #region Public Methods

    public void ShowPlayerSkillText(string message)
    {
        SetText(playerSkillText, message);
    }

    public void ClearPlayerSkillText()
    {
        SetText(playerSkillText, string.Empty);
    }

    public void ShowEnemySkillText(string message)
    {
        SetText(enemySkillText, message);
    }

    public void ClearEnemySkillText()
    {
        SetText(enemySkillText, string.Empty);
    }

    public void ShowResult(bool isWin, string stageName, int stars)
    {
        SetText(resultTitleText, isWin ? "Stage Clear" : "Defeated");
        SetText(resultDescriptionText, GetResultDescription(isWin, stageName));
        SetResultStars(isWin ? stars : 0);
        SetResultButtonsInteractable(false);
        PlayResultSfx(isWin);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultPanelTransition != null)
        {
            resultPanelTransition.Show(() => SetResultButtonsInteractable(true));
            return;
        }

        SetResultButtonsInteractable(true);
    }

    public void HideResult()
    {
        SetResultButtonsInteractable(false);

        if (resultPanelTransition != null)
        {
            resultPanelTransition.HideImmediate();
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        SetResultStars(0);
    }

    public void UpdatePlayerHealth(int currentHp, int maxHp)
    {
        int safeMaxHp = Mathf.Max(0, maxHp);
        int safeCurrentHp = safeMaxHp > 0 ? Mathf.Clamp(currentHp, 0, safeMaxHp) : 0;
        SetText(playerHpText, $"Player HP: {safeCurrentHp} / {safeMaxHp}");
        SetImageFill(playerHealthFillImage, safeMaxHp > 0 ? (float)safeCurrentHp / safeMaxHp : 0f);
    }

    public void UpdateEnemyHealth(int currentHp, int maxHp)
    {
        int safeMaxHp = Mathf.Max(0, maxHp);
        int safeCurrentHp = safeMaxHp > 0 ? Mathf.Clamp(currentHp, 0, safeMaxHp) : 0;
        SetText(enemyHpText, $"Enemy HP: {safeCurrentHp} / {safeMaxHp}");
        SetImageFill(enemyHealthFillImage, safeMaxHp > 0 ? (float)safeCurrentHp / safeMaxHp : 0f);
    }

    public void UpdatePassiveCharge(int currentValue, int requiredValue)
    {
        int safeRequiredValue = Mathf.Max(0, requiredValue);
        int safeCurrentValue = safeRequiredValue > 0 ? Mathf.Clamp(currentValue, 0, safeRequiredValue) : 0;
        SetText(passiveStackText, $"Passive: {safeCurrentValue} / {safeRequiredValue}");
        SetImageFill(passiveChargeFillImage, safeRequiredValue > 0 ? (float)safeCurrentValue / safeRequiredValue : 0f);
    }

    #endregion

    #region Private Methods

    private void Refresh()
    {
        if (gameManager == null)
        {
            return;
        }

        UpdatePlayerHealth(gameManager.PlayerCurrentHp, gameManager.PlayerMaxHp);
        UpdateEnemyHealth(gameManager.EnemyCurrentHp, gameManager.EnemyMaxHp);
        SetText(speedText, gameManager.SpeedInfoText);
        SetText(currentStageText, gameManager.CurrentStageText);
        SetText(currentRoundText, gameManager.CurrentRoundText);
        UpdatePassiveCharge(gameManager.PurplePassiveStack, gameManager.PassiveStackThreshold);
        SetText(stateText, gameManager.TurnStatusText);
        RefreshActiveSkillBlocker();
    }

    private void RefreshActiveSkillBlocker()
    {
        float maxGauge = Mathf.Max(1, gameManager.MaxSkillGauge);
        float skillPercent = Mathf.Clamp01(gameManager.CurrentSkillGauge / maxGauge);

        if (activeSkillBlockerImage != null)
        {
            activeSkillBlockerImage.fillAmount = 1f - skillPercent;
        }

        if (activeSkillButton != null)
        {
            activeSkillButton.interactable = gameManager.CanUseActiveSkillNow;
        }
    }

    private void HandleRestartClicked()
    {
        PlayButtonClickSfx();

        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
    }

    private void HandleRetryClicked()
    {
        PlayButtonClickSfx();

        HideResultThen(() => gameManager?.RetryCurrentStage());
    }

    private void HandleBackToHomeClicked()
    {
        PlayButtonClickSfx();

        HideResultThen(() => gameManager?.BackToHome());
    }

    private void HideResultThen(System.Action onComplete)
    {
        SetResultButtonsInteractable(false);

        if (resultPanelTransition != null)
        {
            resultPanelTransition.Hide(() =>
            {
                if (resultPanel != null)
                {
                    resultPanel.SetActive(false);
                }

                onComplete?.Invoke();
            });
            return;
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        onComplete?.Invoke();
    }

    private string GetResultDescription(bool isWin, string stageName)
    {
        if (!isWin)
        {
            return "Try again";
        }

        return string.IsNullOrWhiteSpace(stageName) ? "Battle complete" : stageName;
    }

    private void SetResultStars(int stars)
    {
        if (resultStarObjects == null)
        {
            return;
        }

        int safeStars = Mathf.Clamp(stars, 0, 3);

        for (int i = 0; i < resultStarObjects.Length; i++)
        {
            if (resultStarObjects[i] != null)
            {
                resultStarObjects[i].SetActive(safeStars >= i + 1);
            }
        }
    }

    private void SetResultButtonsInteractable(bool interactable)
    {
        if (retryButton != null)
        {
            retryButton.interactable = interactable;
        }

        if (backToHomeButton != null)
        {
            backToHomeButton.interactable = interactable;
        }
    }

    private void SetImageFill(Image image, float fillAmount)
    {
        if (image != null)
        {
            image.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }

    private void PlayButtonClickSfx()
    {
        SceneAudioLibrary audioLibrary = GetSceneAudioLibrary();

        if (audioLibrary == null)
        {
            return;
        }

        audioLibrary.PlayButtonClick();
    }

    private void PlayResultSfx(bool isWin)
    {
        SceneAudioLibrary audioLibrary = GetSceneAudioLibrary();

        if (audioLibrary == null)
        {
            return;
        }

        audioLibrary.PlayResult(isWin);
    }

    private SceneAudioLibrary GetSceneAudioLibrary()
    {
        if (sceneAudioLibrary != null)
        {
            return sceneAudioLibrary;
        }

        if (SceneAudioLibrary.Current != null)
        {
            sceneAudioLibrary = SceneAudioLibrary.Current;
            return sceneAudioLibrary;
        }

        if (hasSearchedSceneAudioLibrary)
        {
            return null;
        }

        hasSearchedSceneAudioLibrary = true;
        sceneAudioLibrary = FindFirstObjectByType<SceneAudioLibrary>();
        return sceneAudioLibrary;
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    #endregion
}
