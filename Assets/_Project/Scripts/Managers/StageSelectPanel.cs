using System.Collections;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class StageSelectPanel : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private bool enableStageClearTransition = true;
        [SerializeField, Min(0f)] private float starRevealDelay = 0.18f;
        [SerializeField, Min(1f)] private float starPopScale = 1.25f;
        [SerializeField, Min(0f)] private float starPopDuration = 0.18f;
        [SerializeField, Min(0f)] private float lockFadeDuration = 0.25f;
        [SerializeField] private bool blockInputDuringStageTransition = true;

        [Header("References")]
        [SerializeField] private StageSelectButton[] stageButtons;

        // Cache
        private Coroutine stageClearTransitionRoutine;

        // State

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            RefreshAllStageButtons();
            TryPlayPendingStageClearTransition();
        }

        private void OnDisable()
        {
            if (stageClearTransitionRoutine != null)
            {
                StopCoroutine(stageClearTransitionRoutine);
                stageClearTransitionRoutine = null;
            }

            SetStageButtonInputBlocked(false);
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

        private void TryPlayPendingStageClearTransition()
        {
            if (!enableStageClearTransition || !StageSession.HasPendingStageClearVisual)
            {
                return;
            }

            if (!StageSession.PendingStageStarsImproved && !StageSession.PendingStageUnlockedNext)
            {
                StageSession.ClearPendingStageClearVisual();
                return;
            }

            if (stageClearTransitionRoutine != null)
            {
                StopCoroutine(stageClearTransitionRoutine);
            }

            stageClearTransitionRoutine = StartCoroutine(StageClearTransitionRoutine());
        }

        private IEnumerator StageClearTransitionRoutine()
        {
            EnsureStageButtons();

            StageSelectButton clearedStageButton = GetStageButton(StageSession.PendingClearedStageIndex);
            StageSelectButton unlockedStageButton = GetStageButton(StageSession.PendingUnlockedStageIndex);

            SetStageButtonInputBlocked(blockInputDuringStageTransition);

            if (StageSession.PendingStageUnlockedNext && unlockedStageButton != null)
            {
                unlockedStageButton.SetInputBlocked(true);
                unlockedStageButton.SetLockedVisual(true, 1f);
            }

            if (StageSession.PendingStageStarsImproved && clearedStageButton != null)
            {
                yield return StartCoroutine(clearedStageButton.PlayStarRevealTransition(
                    StageSession.PendingPreviousStars,
                    StageSession.PendingEarnedStars,
                    starRevealDelay,
                    starPopScale,
                    starPopDuration));
            }

            if (StageSession.PendingStageUnlockedNext && unlockedStageButton != null)
            {
                yield return StartCoroutine(unlockedStageButton.PlayUnlockTransition(lockFadeDuration));
            }

            StageSession.ClearPendingStageClearVisual();
            SetStageButtonInputBlocked(false);
            RefreshAllStageButtons();
            stageClearTransitionRoutine = null;
        }

        private StageSelectButton GetStageButton(int stageIndex)
        {
            if (stageButtons == null)
            {
                return null;
            }

            foreach (StageSelectButton stageButton in stageButtons)
            {
                if (stageButton != null && stageButton.StageIndex == stageIndex)
                {
                    return stageButton;
                }
            }

            return null;
        }

        private void SetStageButtonInputBlocked(bool blocked)
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
                    stageButton.SetInputBlocked(blocked);
                }
            }
        }

        #endregion
    }
}
