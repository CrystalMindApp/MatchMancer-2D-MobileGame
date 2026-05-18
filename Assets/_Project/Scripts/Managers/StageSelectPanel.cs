using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class StageSelectPanel : MonoBehaviour
    {
        #region Variables

        [Header("References")]
        [SerializeField] private StageSelectButton[] stageButtons;

        // Cache

        // State

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            RefreshAllStageButtons();
        }

        #endregion

        #region Public Methods

        public void RefreshAllStageButtons()
        {
            EnsureStageButtons();

            if (stageButtons == null)
            {
                return;
            }

            foreach (StageSelectButton stageButton in stageButtons)
            {
                if (stageButton != null)
                {
                    stageButton.RefreshVisualState();
                }
            }
        }

        #endregion

        #region Private Methods

        private void EnsureStageButtons()
        {
            if (stageButtons != null && stageButtons.Length > 0)
            {
                return;
            }

            stageButtons = GetComponentsInChildren<StageSelectButton>(true);
        }

        #endregion
    }
}
