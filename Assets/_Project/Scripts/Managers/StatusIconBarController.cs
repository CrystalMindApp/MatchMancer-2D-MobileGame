using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class StatusIconBarController : MonoBehaviour
    {
        #region Variables

        [Header("Poison Status")]
        [SerializeField] private StatusIconView statusIconPrefab;
        [SerializeField] private Transform iconContainer;
        [SerializeField] private Sprite poisonIcon;

        // State
        private StatusIconView poisonIconView;

        #endregion

        #region Public Methods

        public void SetPoison(bool active, int turnsRemaining, int damagePerTurn)
        {
            if (!active || turnsRemaining <= 0)
            {
                HidePoisonIcon();
                return;
            }

            if (!EnsurePoisonIcon())
            {
                return;
            }

            poisonIconView.gameObject.SetActive(true);
            poisonIconView.Set(poisonIcon, turnsRemaining, damagePerTurn);
        }

        #endregion

        #region Private Methods

        private bool EnsurePoisonIcon()
        {
            if (poisonIconView != null)
            {
                return true;
            }

            if (statusIconPrefab == null || poisonIcon == null)
            {
                return false;
            }

            Transform parent = iconContainer != null ? iconContainer : transform;
            poisonIconView = Instantiate(statusIconPrefab, parent);
            return poisonIconView != null;
        }

        private void HidePoisonIcon()
        {
            if (poisonIconView != null)
            {
                poisonIconView.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
