using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrystalMind.MatchMancer
{
    public class StatusIconView : MonoBehaviour
    {
        #region Variables

        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text remainingTurnsText;
        [SerializeField] private TMP_Text amountText;

        #endregion

        #region Public Methods

        public void Set(Sprite icon, int turnsRemaining, int amount)
        {
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }

            if (remainingTurnsText != null)
            {
                remainingTurnsText.gameObject.SetActive(true);
                remainingTurnsText.text = Mathf.Max(0, turnsRemaining).ToString();
            }

            if (amountText == null)
            {
                return;
            }

            bool hasAmount = amount > 0;
            amountText.gameObject.SetActive(hasAmount);
            amountText.text = hasAmount ? amount.ToString() : string.Empty;
        }

        public void SetIconOnly(Sprite icon)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (remainingTurnsText != null)
            {
                remainingTurnsText.gameObject.SetActive(false);
                remainingTurnsText.text = string.Empty;
            }

            if (amountText != null)
            {
                amountText.gameObject.SetActive(false);
                amountText.text = string.Empty;
            }
        }

        #endregion
    }
}
