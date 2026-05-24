using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrystalMind.MatchMancer
{
    public class EnemyIntentUIController : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private string readyText = "Ready";
        [SerializeField] private string cooldownPrefix = "Enemy Skill";
        [SerializeField] private Color readyColor = new Color(1f, 0.35f, 0.2f, 1f);
        [SerializeField] private Color cooldownColor = Color.white;

        [Header("References")]
        [SerializeField] private TMP_Text intentText;
        [SerializeField] private Image fillImage;
        [SerializeField] private GameObject readyGlow;
        [SerializeField] private GameObject[] readinessDots;

        // Cache

        // State

        #endregion

        #region Public Methods

        public void UpdateIntent(int turnsRemaining, int cooldownTurns, bool isReady, float fillAmount)
        {
            int safeTurnsRemaining = Mathf.Max(0, turnsRemaining);
            int safeCooldownTurns = Mathf.Max(0, cooldownTurns);

            if (intentText != null)
            {
                intentText.text = isReady
                    ? $"{cooldownPrefix}: {readyText}"
                    : $"{cooldownPrefix}: {safeTurnsRemaining}";
                intentText.color = isReady ? readyColor : cooldownColor;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.Clamp01(fillAmount);
            }

            if (readyGlow != null)
            {
                readyGlow.SetActive(isReady);
            }

            UpdateDots(safeTurnsRemaining, safeCooldownTurns, isReady);
        }

        #endregion

        #region Private Methods

        private void UpdateDots(int turnsRemaining, int cooldownTurns, bool isReady)
        {
            if (readinessDots == null)
            {
                return;
            }

            int safeCooldownTurns = Mathf.Max(1, cooldownTurns);
            int activeDots = isReady
                ? readinessDots.Length
                : Mathf.RoundToInt((1f - Mathf.Clamp01((float)turnsRemaining / safeCooldownTurns)) * readinessDots.Length);

            for (int i = 0; i < readinessDots.Length; i++)
            {
                if (readinessDots[i] != null)
                {
                    readinessDots[i].SetActive(i < activeDots);
                }
            }
        }

        #endregion
    }
}
