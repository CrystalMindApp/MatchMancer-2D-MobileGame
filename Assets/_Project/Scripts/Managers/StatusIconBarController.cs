using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class StatusIconBarController : MonoBehaviour
    {
        #region Variables

        [Header("Player Status Icons")]
        [SerializeField] private StatusIconView statusIconPrefab;
        [SerializeField] private Transform iconContainer;
        [SerializeField] private Sprite poisonIcon;
        [SerializeField] private Sprite blindIcon;

        // State
        private StatusIconView poisonIconView;
        private StatusIconView blindIconView;

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

        public void SetBlind(bool active, int attemptsRemaining)
        {
            if (!active || attemptsRemaining <= 0)
            {
                HideBlindIcon();
                return;
            }

            if (!EnsureBlindIcon())
            {
                return;
            }

            blindIconView.gameObject.SetActive(true);
            blindIconView.Set(blindIcon, attemptsRemaining, 0);
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

        private bool EnsureBlindIcon()
        {
            if (blindIconView != null)
            {
                return true;
            }

            if (statusIconPrefab == null || blindIcon == null)
            {
                return false;
            }

            Transform parent = iconContainer != null ? iconContainer : transform;
            blindIconView = Instantiate(statusIconPrefab, parent);
            return blindIconView != null;
        }

        private void HidePoisonIcon()
        {
            if (poisonIconView != null)
            {
                poisonIconView.gameObject.SetActive(false);
            }
        }

        private void HideBlindIcon()
        {
            if (blindIconView != null)
            {
                blindIconView.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
