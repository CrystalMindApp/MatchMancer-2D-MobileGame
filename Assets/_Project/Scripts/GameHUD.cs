using System.Collections;
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
    [SerializeField] private TMP_Text heroSpeedText;
    [SerializeField] private TMP_Text enemySpeedText;
    [SerializeField] private TMP_Text heroAttackText;
    [SerializeField] private TMP_Text heroHealText;
    [SerializeField] private TMP_Text heroCritChanceText;
    [SerializeField] private TMP_Text heroCritMultiplierText;
    [SerializeField] private TMP_Text heroActiveSkillText;
    [SerializeField] private TMP_Text heroPassiveSkillText;
    [SerializeField] private TMP_Text enemyAttackText;
    [SerializeField] private TMP_Text enemyHealText;
    [SerializeField] private TMP_Text enemyCritChanceText;
    [SerializeField] private TMP_Text enemyCritMultiplierText;
    [SerializeField] private TMP_Text enemyDisruptChanceText;
    [SerializeField] private TMP_Text enemyDisruptDescriptionText;
    [SerializeField] private TMP_Text enemyPassiveSkillText;
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
    [SerializeField] private Image playerDelayedHealthFillImage;
    [SerializeField] private Image enemyHealthFillImage;
    [SerializeField] private Image enemyDelayedHealthFillImage;
    [SerializeField] private Image passiveChargeFillImage;
    [SerializeField] private Image activeSkillBlockerImage;
    [SerializeField] private Button activeSkillButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button backToHomeButton;
    [SerializeField] private SceneAudioLibrary sceneAudioLibrary;
    [SerializeField] private UIPanelScaleTransition resultPanelTransition;
    [SerializeField] private EnemyIntentUIController enemyIntentUIController;
    [SerializeField] private StatusIconBarController playerStatusIconBar;
    [SerializeField] private EnemyTraitIconController enemyTraitIconController;
    [SerializeField] private CinemachineTinyImpulse resultTinyImpulse;

    [Header("HP Bar Timing")]
    [SerializeField, Min(0f)] private float delayedHpFollowDelay = 0.25f;
    [SerializeField, Min(0f)] private float delayedHpLerpSpeed = 6f;

    [Header("Phase Text")]
    [SerializeField] private string matchingPhaseLabel = "MATCHING PHASE";
    [SerializeField] private string combatPhaseLabel = "COMBAT PHASE";
    [SerializeField, Min(1f)] private float phaseTextPopScale = 1.12f;
    [SerializeField, Min(0f)] private float phaseTextPopDuration = 0.12f;

    [Header("Skill Button Readability")]
    [SerializeField, Range(0.5f, 1f)] private float disabledSkillScale = 0.92f;
    [SerializeField, Min(0f)] private float skillScaleTransitionDuration = 0.12f;
    [SerializeField, Min(1f)] private float readyPunchScale = 1.08f;

    [Header("Result Reward Animation")]
    [SerializeField] private RectTransform[] resultStarRectTransforms;
    [SerializeField] private RectTransform resultButtonGroup;
    [SerializeField, Min(0f)] private float resultRewardStartDelay = 0.15f;
    [SerializeField, Min(0f)] private float starRevealDelay = 0.18f;
    [SerializeField, Min(0f)] private float starDropDuration = 0.28f;
    [SerializeField] private float starStartRotation = -12f;
    [SerializeField, Min(0f)] private float starStartScale = 1.35f;
    [SerializeField, Min(0f)] private float starImpactScale = 1.18f;
    [SerializeField, Min(0f)] private float starImpactDuration = 0.1f;
    [SerializeField] private AnimationCurve starDropCurve;
    [SerializeField] private AnimationCurve starImpactCurve;
    [SerializeField, Min(0f)] private float buttonDrawerRevealDelay = 0.08f;
    [SerializeField, Min(0f)] private float buttonDrawerSlideDuration = 0.25f;
    [SerializeField] private Vector2 buttonDrawerHiddenOffset = new Vector2(0f, 80f);
    [SerializeField] private AnimationCurve buttonDrawerCurve;
    [SerializeField, Min(0f)] private float starImpactShakeForce;

    // State
    private bool hasSearchedSceneAudioLibrary;
    private Coroutine playerDelayedHpRoutine;
    private Coroutine enemyDelayedHpRoutine;
    private Coroutine resultRewardRoutine;
    private Coroutine phaseTextPopRoutine;
    private Coroutine skillButtonScaleRoutine;
    private float playerDelayedHpTarget = -1f;
    private float enemyDelayedHpTarget = -1f;
    private Vector2 buttonGroupShownPosition;
    private Vector3 phaseTextBaseScale = Vector3.one;
    private Vector3 skillButtonBaseScale = Vector3.one;
    private string currentPhaseLabel;
    private bool hasButtonGroupShownPosition;
    private bool hasPhaseTextBaseScale;
    private bool hasSkillButtonBaseScale;
    private bool hasSkillButtonState;
    private bool wasActiveSkillReady;
    private Vector3[] starShownScales;
    private Quaternion[] starShownRotations;
    private bool hasCachedResultStarState;

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
        SetResultButtonsInteractable(false);
        PrepareResultRewardVisuals(isWin ? stars : 0);
        PlayResultSfx(isWin);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultPanelTransition != null)
        {
            resultPanelTransition.Show(() => StartResultRewardSequence(isWin, stars));
            return;
        }

        StartResultRewardSequence(isWin, stars);
    }

    public void HideResult()
    {
        SetResultButtonsInteractable(false);
        StopResultRewardSequence();

        if (resultPanelTransition != null)
        {
            resultPanelTransition.HideImmediate();
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        SetResultStars(0);
        RestoreResultButtonGroup();
    }

    public void UpdatePlayerHealth(int currentHp, int maxHp)
    {
        int safeMaxHp = Mathf.Max(0, maxHp);
        int safeCurrentHp = safeMaxHp > 0 ? Mathf.Clamp(currentHp, 0, safeMaxHp) : 0;
        float fillAmount = safeMaxHp > 0 ? (float)safeCurrentHp / safeMaxHp : 0f;
        SetText(playerHpText, $"{safeCurrentHp}/{safeMaxHp}");
        SetImageFill(playerHealthFillImage, fillAmount);
        UpdatePlayerDelayedHealthFill(fillAmount);
    }

    public void UpdateEnemyHealth(int currentHp, int maxHp)
    {
        int safeMaxHp = Mathf.Max(0, maxHp);
        int safeCurrentHp = safeMaxHp > 0 ? Mathf.Clamp(currentHp, 0, safeMaxHp) : 0;
        float fillAmount = safeMaxHp > 0 ? (float)safeCurrentHp / safeMaxHp : 0f;
        SetText(enemyHpText, $"{safeCurrentHp}/{safeMaxHp}");
        SetImageFill(enemyHealthFillImage, fillAmount);
        UpdateEnemyDelayedHealthFill(fillAmount);
    }

    public void UpdatePassiveCharge(int currentValue, int requiredValue)
    {
        int safeRequiredValue = Mathf.Max(0, requiredValue);
        int safeCurrentValue = safeRequiredValue > 0 ? Mathf.Clamp(currentValue, 0, safeRequiredValue) : 0;
        SetText(passiveStackText, $"{safeCurrentValue}/{safeRequiredValue}");
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
        RefreshSpeedTexts();
        RefreshStatTexts();
        SetText(currentStageText, gameManager.CurrentStageText);
        SetText(currentRoundText, gameManager.CurrentRoundText);
        UpdatePassiveCharge(gameManager.PurplePassiveStack, gameManager.PassiveStackThreshold);
        RefreshPlayerStatusIcons();
        RefreshEnemyTraitIcon();
        SetPhaseText(gameManager.IsMatchingPhase ? matchingPhaseLabel : combatPhaseLabel);
        enemyIntentUIController?.UpdateIntent(
            gameManager.EnemySkillTurnsRemaining,
            gameManager.EnemySkillCooldownTurns,
            gameManager.IsEnemySkillReady,
            gameManager.EnemyIntentFill01);
        RefreshActiveSkillBlocker();
    }

    private void RefreshSpeedTexts()
    {
        bool hasSplitSpeedText = heroSpeedText != null || enemySpeedText != null;

        SetText(heroSpeedText, gameManager.HeroSpeedText);
        SetText(enemySpeedText, gameManager.EnemySpeedText);

        if (speedText == null)
        {
            return;
        }

        SetText(speedText, hasSplitSpeedText ? string.Empty : gameManager.SpeedInfoText);
    }

    private void RefreshStatTexts()
    {
        SetText(heroAttackText, gameManager.HeroAttackStatText);
        SetText(heroHealText, gameManager.HeroHealStatText);
        SetText(heroCritChanceText, gameManager.HeroCritChanceText);
        SetText(heroCritMultiplierText, gameManager.HeroCritMultiplierText);
        SetText(heroActiveSkillText, gameManager.HeroActiveSkillText);
        SetText(heroPassiveSkillText, gameManager.HeroPassiveSkillText);
        SetText(enemyAttackText, gameManager.EnemyAttackStatText);
        SetText(enemyHealText, gameManager.EnemyHealStatText);
        SetText(enemyCritChanceText, gameManager.EnemyCritChanceText);
        SetText(enemyCritMultiplierText, gameManager.EnemyCritMultiplierText);
        SetText(enemyDisruptChanceText, gameManager.EnemyDisruptChanceText);
        SetText(enemyDisruptDescriptionText, gameManager.EnemyDisruptDescriptionText);
        SetOptionalText(enemyPassiveSkillText, gameManager.EnemyPassiveSkillText);
    }

    private void RefreshPlayerStatusIcons()
    {
        playerStatusIconBar?.SetPoison(
            gameManager.PlayerHasPoison,
            gameManager.PlayerPoisonTurnsRemaining,
            gameManager.PlayerPoisonDamagePerTurn);
        playerStatusIconBar?.SetBlind(
            gameManager.PlayerHasBlind,
            gameManager.PlayerBlindAttemptsRemaining);
    }

    private void RefreshEnemyTraitIcon()
    {
        enemyTraitIconController?.SetCurseTrait(
            gameManager.EnemyHasTileCurseTrait,
            gameManager.EnemyTileCurseTraitEffect);
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

        RefreshSkillButtonScale(gameManager.CanUseActiveSkillNow);
    }

    private void RefreshSkillButtonScale(bool isReady)
    {
        if (activeSkillButton == null)
        {
            wasActiveSkillReady = isReady;
            return;
        }

        if (!hasSkillButtonBaseScale)
        {
            skillButtonBaseScale = activeSkillButton.transform.localScale;
            hasSkillButtonBaseScale = true;
        }

        if (!hasSkillButtonState)
        {
            hasSkillButtonState = true;
            wasActiveSkillReady = isReady;
            Vector3 targetScale = isReady ? skillButtonBaseScale : skillButtonBaseScale * disabledSkillScale;
            activeSkillButton.transform.localScale = targetScale;
            return;
        }

        if (wasActiveSkillReady == isReady)
        {
            return;
        }

        bool shouldPunch = isReady && !wasActiveSkillReady;
        wasActiveSkillReady = isReady;

        if (skillButtonScaleRoutine != null)
        {
            StopCoroutine(skillButtonScaleRoutine);
        }

        skillButtonScaleRoutine = StartCoroutine(SkillButtonScaleRoutine(isReady, shouldPunch));
    }

    private IEnumerator SkillButtonScaleRoutine(bool isReady, bool shouldPunch)
    {
        Transform targetTransform = activeSkillButton.transform;
        Vector3 startScale = targetTransform.localScale;
        Vector3 targetScale = isReady ? skillButtonBaseScale : skillButtonBaseScale * disabledSkillScale;
        float safeDuration = Mathf.Max(0f, skillScaleTransitionDuration);

        if (safeDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float time = Mathf.Clamp01(elapsed / safeDuration);
                targetTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, time);
                yield return null;
            }
        }

        targetTransform.localScale = targetScale;

        if (shouldPunch && readyPunchScale > 1f && safeDuration > 0f)
        {
            float elapsed = 0f;
            Vector3 punchScale = skillButtonBaseScale * readyPunchScale;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float time = Mathf.Clamp01(elapsed / safeDuration);
                float punch = Mathf.Sin(time * Mathf.PI);
                targetTransform.localScale = Vector3.LerpUnclamped(skillButtonBaseScale, punchScale, punch);
                yield return null;
            }

            targetTransform.localScale = skillButtonBaseScale;
        }

        skillButtonScaleRoutine = null;
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
        SetResultButtonsInteractable(false);
        gameManager?.RetryCurrentStage();
    }

    private void HandleBackToHomeClicked()
    {
        PlayButtonClickSfx();
        SetResultButtonsInteractable(false);
        gameManager?.BackToHome();
    }

    private void SetPhaseText(string label)
    {
        if (stateText == null)
        {
            return;
        }

        string safeLabel = string.IsNullOrWhiteSpace(label) ? string.Empty : label;

        if (currentPhaseLabel == safeLabel)
        {
            return;
        }

        currentPhaseLabel = safeLabel;
        SetText(stateText, safeLabel);
        PlayPhaseTextPop();
    }

    private void PlayPhaseTextPop()
    {
        if (stateText == null || phaseTextPopDuration <= 0f)
        {
            return;
        }

        if (!hasPhaseTextBaseScale)
        {
            phaseTextBaseScale = stateText.transform.localScale;
            hasPhaseTextBaseScale = true;
        }

        if (phaseTextPopRoutine != null)
        {
            StopCoroutine(phaseTextPopRoutine);
            stateText.transform.localScale = phaseTextBaseScale;
        }

        phaseTextPopRoutine = StartCoroutine(PhaseTextPopRoutine());
    }

    private IEnumerator PhaseTextPopRoutine()
    {
        Transform targetTransform = stateText.transform;
        Vector3 targetScale = phaseTextBaseScale * phaseTextPopScale;
        float elapsed = 0f;

        while (elapsed < phaseTextPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float time = Mathf.Clamp01(elapsed / phaseTextPopDuration);
            float punch = Mathf.Sin(time * Mathf.PI);
            targetTransform.localScale = Vector3.LerpUnclamped(phaseTextBaseScale, targetScale, punch);
            yield return null;
        }

        targetTransform.localScale = phaseTextBaseScale;
        phaseTextPopRoutine = null;
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

    private void PrepareResultRewardVisuals(int stars)
    {
        StopResultRewardSequence();
        CacheResultRewardState();

        int safeStars = Mathf.Clamp(stars, 0, 3);
        int starCount = GetResultStarCount();

        for (int i = 0; i < starCount; i++)
        {
            RectTransform starTransform = GetResultStarTransform(i);
            bool shouldReveal = i < safeStars;

            if (starTransform != null)
            {
                if (shouldReveal)
                {
                    SetResultStarActive(i, true);
                    RestoreResultStar(i);
                    starTransform.localScale = GetStarShownScale(i, starTransform) * starStartScale;
                    starTransform.localRotation = Quaternion.Euler(0f, 0f, starStartRotation);
                    SetResultStarAlpha(starTransform, 0f);
                    continue;
                }

                RestoreResultStar(i);
                SetResultStarAlpha(starTransform, 1f);
            }

            SetResultStarActive(i, shouldReveal);
        }

        HideResultButtonGroupForDrawer();
    }

    private void StartResultRewardSequence(bool isWin, int stars)
    {
        StopResultRewardSequence();
        resultRewardRoutine = StartCoroutine(ResultRewardSequenceRoutine(isWin, stars));
    }

    private void StopResultRewardSequence()
    {
        if (resultRewardRoutine == null)
        {
            return;
        }

        StopCoroutine(resultRewardRoutine);
        resultRewardRoutine = null;
    }

    private IEnumerator ResultRewardSequenceRoutine(bool isWin, int stars)
    {
        if (resultRewardStartDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(resultRewardStartDelay);
        }

        if (isWin)
        {
            yield return StartCoroutine(PlayStarRewardSequenceRoutine(stars));
        }

        if (buttonDrawerRevealDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(buttonDrawerRevealDelay);
        }

        yield return StartCoroutine(RevealResultButtonGroupRoutine());
        SetResultButtonsInteractable(true);
        resultRewardRoutine = null;
    }

    private IEnumerator PlayStarRewardSequenceRoutine(int stars)
    {
        int safeStars = Mathf.Clamp(stars, 0, 3);

        if (!HasAnyResultStarReference())
        {
            SetResultStars(safeStars);
            yield break;
        }

        for (int i = 0; i < safeStars; i++)
        {
            RectTransform starTransform = GetResultStarTransform(i);

            if (starTransform == null)
            {
                SetResultStarActive(i, true);
                continue;
            }

            yield return StartCoroutine(PlaySingleStarStampRoutine(i, starTransform));

            if (starRevealDelay > 0f && i < safeStars - 1)
            {
                yield return new WaitForSecondsRealtime(starRevealDelay);
            }
        }
    }

    private IEnumerator PlaySingleStarStampRoutine(int index, RectTransform starTransform)
    {
        if (starTransform == null)
        {
            yield break;
        }

        SetResultStarActive(index, true);
        SetResultStarAlpha(starTransform, 0f);

        Vector3 shownScale = GetStarShownScale(index, starTransform);
        Quaternion shownRotation = GetStarShownRotation(index, starTransform);
        Vector3 startScale = shownScale * starStartScale;
        Vector3 impactScale = shownScale * Mathf.Max(1f, starImpactScale);
        Quaternion startRotation = Quaternion.Euler(0f, 0f, starStartRotation);

        float safeDropDuration = Mathf.Max(0f, starDropDuration);
        float elapsed = 0f;

        if (safeDropDuration <= 0f)
        {
            SetResultStarAlpha(starTransform, 1f);
            starTransform.localScale = impactScale;
            starTransform.localRotation = shownRotation;
        }
        else
        {
            while (elapsed < safeDropDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float time = Mathf.Clamp01(elapsed / safeDropDuration);
                float curvedTime = EvaluateCurve(starDropCurve, time);
                SetResultStarAlpha(starTransform, curvedTime);
                starTransform.localScale = Vector3.LerpUnclamped(startScale, impactScale, curvedTime);
                starTransform.localRotation = Quaternion.LerpUnclamped(startRotation, shownRotation, curvedTime);
                yield return null;
            }
        }

        SetResultStarAlpha(starTransform, 1f);
        starTransform.localScale = impactScale;
        starTransform.localRotation = shownRotation;
        resultTinyImpulse?.Shake(starImpactShakeForce);

        if (starImpactDuration > 0f && starImpactScale > 0f)
        {
            yield return StartCoroutine(PlayStarImpactRoutine(starTransform, shownScale));
        }
    }

    private IEnumerator PlayStarImpactRoutine(RectTransform starTransform, Vector3 shownScale)
    {
        float elapsed = 0f;

        while (elapsed < starImpactDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float time = Mathf.Clamp01(elapsed / starImpactDuration);
            float curvedTime = EvaluateCurve(starImpactCurve, time);
            float punch = Mathf.Sin(curvedTime * Mathf.PI);
            starTransform.localScale = Vector3.LerpUnclamped(shownScale, shownScale * starImpactScale, punch);
            yield return null;
        }

        starTransform.localScale = shownScale;
    }

    private IEnumerator RevealResultButtonGroupRoutine()
    {
        if (resultButtonGroup == null)
        {
            SetResultButtonsInteractable(true);
            yield break;
        }

        CacheResultRewardState();

        Vector2 shownPosition = buttonGroupShownPosition;
        Vector2 hiddenPosition = shownPosition + buttonDrawerHiddenOffset;
        float safeDuration = Mathf.Max(0f, buttonDrawerSlideDuration);
        resultButtonGroup.anchoredPosition = hiddenPosition;
        resultButtonGroup.gameObject.SetActive(true);

        if (safeDuration <= 0f)
        {
            resultButtonGroup.anchoredPosition = shownPosition;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float time = Mathf.Clamp01(elapsed / safeDuration);
            float curvedTime = EvaluateCurve(buttonDrawerCurve, time);
            resultButtonGroup.anchoredPosition = Vector2.LerpUnclamped(hiddenPosition, shownPosition, curvedTime);
            yield return null;
        }

        resultButtonGroup.anchoredPosition = shownPosition;
    }

    private void HideResultButtonGroupForDrawer()
    {
        if (resultButtonGroup == null)
        {
            return;
        }

        CacheResultRewardState();
        resultButtonGroup.anchoredPosition = buttonGroupShownPosition + buttonDrawerHiddenOffset;
        resultButtonGroup.gameObject.SetActive(false);
    }

    private void RestoreResultButtonGroup()
    {
        if (resultButtonGroup == null || !hasButtonGroupShownPosition)
        {
            return;
        }

        resultButtonGroup.anchoredPosition = buttonGroupShownPosition;
    }

    private void CacheResultRewardState()
    {
        if (resultButtonGroup != null && !hasButtonGroupShownPosition)
        {
            buttonGroupShownPosition = resultButtonGroup.anchoredPosition;
            hasButtonGroupShownPosition = true;
        }

        if (hasCachedResultStarState)
        {
            return;
        }

        int starCount = GetResultStarCount();
        starShownScales = new Vector3[starCount];
        starShownRotations = new Quaternion[starCount];

        for (int i = 0; i < starCount; i++)
        {
            RectTransform starTransform = GetResultStarTransform(i);
            if (starTransform == null)
            {
                continue;
            }

            starShownScales[i] = starTransform.localScale;
            starShownRotations[i] = starTransform.localRotation;
        }

        hasCachedResultStarState = true;
    }

    private RectTransform GetResultStarTransform(int index)
    {
        if (resultStarRectTransforms != null &&
            index >= 0 &&
            index < resultStarRectTransforms.Length &&
            resultStarRectTransforms[index] != null)
        {
            return resultStarRectTransforms[index];
        }

        if (resultStarObjects != null &&
            index >= 0 &&
            index < resultStarObjects.Length &&
            resultStarObjects[index] != null)
        {
            return resultStarObjects[index].GetComponent<RectTransform>();
        }

        return null;
    }

    private int GetResultStarCount()
    {
        int objectCount = resultStarObjects != null ? resultStarObjects.Length : 0;
        int rectCount = resultStarRectTransforms != null ? resultStarRectTransforms.Length : 0;
        return Mathf.Max(objectCount, rectCount);
    }

    private bool HasAnyResultStarReference()
    {
        return GetResultStarCount() > 0;
    }

    private void SetResultStarActive(int index, bool active)
    {
        if (resultStarObjects != null && index >= 0 && index < resultStarObjects.Length && resultStarObjects[index] != null)
        {
            resultStarObjects[index].SetActive(active);
            return;
        }

        RectTransform starTransform = GetResultStarTransform(index);
        if (starTransform != null)
        {
            starTransform.gameObject.SetActive(active);
        }
    }

    private void RestoreResultStar(int index)
    {
        RectTransform starTransform = GetResultStarTransform(index);
        if (starTransform == null || !hasCachedResultStarState || index >= starShownScales.Length)
        {
            return;
        }

        starTransform.localScale = starShownScales[index];
        starTransform.localRotation = starShownRotations[index];
    }

    private Vector3 GetStarShownScale(int index, RectTransform fallbackTransform)
    {
        return hasCachedResultStarState && index < starShownScales.Length
            ? starShownScales[index]
            : fallbackTransform.localScale;
    }

    private Quaternion GetStarShownRotation(int index, RectTransform fallbackTransform)
    {
        return hasCachedResultStarState && index < starShownRotations.Length
            ? starShownRotations[index]
            : fallbackTransform.localRotation;
    }

    private void SetResultStarAlpha(RectTransform starTransform, float alpha)
    {
        if (starTransform == null)
        {
            return;
        }

        Graphic[] graphics = starTransform.GetComponentsInChildren<Graphic>(true);
        float safeAlpha = Mathf.Clamp01(alpha);

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] == null)
            {
                continue;
            }

            Color color = graphics[i].color;
            color.a = safeAlpha;
            graphics[i].color = color;
        }
    }

    private float EvaluateCurve(AnimationCurve curve, float time)
    {
        return curve != null && curve.length > 0 ? curve.Evaluate(time) : time;
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

    private void UpdatePlayerDelayedHealthFill(float targetFill)
    {
        if (playerDelayedHealthFillImage == null)
        {
            return;
        }

        float safeTarget = Mathf.Clamp01(targetFill);

        if (playerDelayedHpTarget < 0f)
        {
            playerDelayedHpTarget = safeTarget;
            playerDelayedHealthFillImage.fillAmount = safeTarget;
            return;
        }

        if (Mathf.Approximately(playerDelayedHpTarget, safeTarget))
        {
            return;
        }

        playerDelayedHpTarget = safeTarget;

        if (playerDelayedHpRoutine != null)
        {
            StopCoroutine(playerDelayedHpRoutine);
        }

        playerDelayedHpRoutine = StartCoroutine(DelayedHealthFillRoutine(playerDelayedHealthFillImage, safeTarget, true));
    }

    private void UpdateEnemyDelayedHealthFill(float targetFill)
    {
        if (enemyDelayedHealthFillImage == null)
        {
            return;
        }

        float safeTarget = Mathf.Clamp01(targetFill);

        if (enemyDelayedHpTarget < 0f)
        {
            enemyDelayedHpTarget = safeTarget;
            enemyDelayedHealthFillImage.fillAmount = safeTarget;
            return;
        }

        if (Mathf.Approximately(enemyDelayedHpTarget, safeTarget))
        {
            return;
        }

        enemyDelayedHpTarget = safeTarget;

        if (enemyDelayedHpRoutine != null)
        {
            StopCoroutine(enemyDelayedHpRoutine);
        }

        enemyDelayedHpRoutine = StartCoroutine(DelayedHealthFillRoutine(enemyDelayedHealthFillImage, safeTarget, false));
    }

    private IEnumerator DelayedHealthFillRoutine(Image image, float targetFill, bool isPlayer)
    {
        if (delayedHpFollowDelay > 0f)
        {
            yield return new WaitForSeconds(delayedHpFollowDelay);
        }

        while (image != null && !Mathf.Approximately(image.fillAmount, targetFill))
        {
            if (delayedHpLerpSpeed <= 0f)
            {
                image.fillAmount = targetFill;
                break;
            }

            image.fillAmount = Mathf.MoveTowards(image.fillAmount, targetFill, delayedHpLerpSpeed * Time.deltaTime);
            yield return null;
        }

        if (image != null)
        {
            image.fillAmount = targetFill;
        }

        if (isPlayer)
        {
            playerDelayedHpRoutine = null;
            yield break;
        }

        enemyDelayedHpRoutine = null;
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

    private void SetOptionalText(TMP_Text text, string value)
    {
        if (text != null && !string.IsNullOrWhiteSpace(value))
        {
            text.text = value;
        }
    }

    #endregion
}
