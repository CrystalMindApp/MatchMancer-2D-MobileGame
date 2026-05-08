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
    [SerializeField] private TMP_Text passiveStackText;
    [SerializeField] private TMP_Text playerSkillText;
    [SerializeField] private TMP_Text enemySkillText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Image activeSkillBlockerImage;
    [SerializeField] private Button activeSkillButton;
    [SerializeField] private Button restartButton;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
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

    #endregion

    #region Private Methods

    private void Refresh()
    {
        if (gameManager == null)
        {
            return;
        }

        SetText(playerHpText, $"Player HP: {gameManager.PlayerCurrentHp} / {gameManager.PlayerMaxHp}");
        SetText(enemyHpText, $"Enemy HP: {gameManager.EnemyCurrentHp} / {gameManager.EnemyMaxHp}");
        SetText(speedText, gameManager.SpeedInfoText);
        SetText(passiveStackText, $"Passive: {gameManager.PurplePassiveStack}");
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

    private void OnRestartClicked()
    {
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
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
