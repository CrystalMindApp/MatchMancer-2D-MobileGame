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

        #endregion
    }
}
