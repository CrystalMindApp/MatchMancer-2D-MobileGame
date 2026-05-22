using System.Collections;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class StageSelectPanel : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private bool enableStageClearTransition = true;
        [SerializeField, Min(0f)] private float transitionStartDelay = 0.15f;
        [SerializeField, Min(0f)] private float initialUnlockStartDelay = 0.6f;
        [SerializeField, Min(0f)] private float starRevealDelay = 0.22f;
        [SerializeField, Min(1f)] private float starPopScale = 1.25f;
        [SerializeField, Min(0f)] private float starPopDuration = 0.2f;
        [SerializeField, Min(0f)] private float postStarRevealDelay = 0.35f;
        [SerializeField, Min(0f)] private float lockShakeDuration = 1f;
        [SerializeField, Min(0f)] private float lockShakeStrength = 8f;
        [SerializeField, Min(0f)] private float postLockShakeDelay = 0.12f;
        [SerializeField, Min(0f)] private float postUnlockSpriteDelay = 0.25f;
        [SerializeField, Min(0f)] private float lockFadeDuration = 0.4f;
        [SerializeField, Min(0f)] private float postUnlockDelay = 0.15f;
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
                TryPlayInitialUnlockTransition();
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

        private void TryPlayInitialUnlockTransition()
        {
            if (!enableStageClearTransition)
            {
                return;
            }

            StageSelectButton stageButton = GetFirstUnshownUnlockedStageButton();
            if (stageButton == null)
            {
                return;
            }

            if (stageClearTransitionRoutine != null)
            {
                StopCoroutine(stageClearTransitionRoutine);
            }

            stageClearTransitionRoutine = StartCoroutine(InitialUnlockTransitionRoutine(stageButton));
        }

        private IEnumerator StageClearTransitionRoutine()
        {
            EnsureStageButtons();

            StageSelectButton clearedStageButton = GetStageButton(StageSession.PendingClearedStageIndex);
            StageSelectButton unlockedStageButton = GetStageButton(StageSession.PendingUnlockedStageIndex);

            SetStageButtonInputBlocked(blockInputDuringStageTransition);
            yield return new WaitForSecondsRealtime(transitionStartDelay);

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

                yield return new WaitForSecondsRealtime(postStarRevealDelay);
            }

            if (StageSession.PendingStageUnlockedNext && unlockedStageButton != null)
            {
                yield return StartCoroutine(unlockedStageButton.PlayUnlockTransition(
                    lockShakeDuration,
                    lockShakeStrength,
                    postLockShakeDelay,
                    postUnlockSpriteDelay,
                    lockFadeDuration));
                yield return new WaitForSecondsRealtime(postUnlockDelay);
            }

            StageSession.ClearPendingStageClearVisual();
            SetStageButtonInputBlocked(false);
            RefreshAllStageButtons();
            stageClearTransitionRoutine = null;
        }

        private IEnumerator InitialUnlockTransitionRoutine(StageSelectButton stageButton)
        {
            SetStageButtonInputBlocked(blockInputDuringStageTransition);
            stageButton.SetInputBlocked(true);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForSecondsRealtime(transitionStartDelay + initialUnlockStartDelay);
            yield return StartCoroutine(stageButton.PlayUnlockTransition(
                lockShakeDuration,
                lockShakeStrength,
                postLockShakeDelay,
                postUnlockSpriteDelay,
                lockFadeDuration));
            yield return new WaitForSecondsRealtime(postUnlockDelay);
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

        private StageSelectButton GetFirstUnshownUnlockedStageButton()
        {
            EnsureStageButtons();

            if (stageButtons == null)
            {
                return null;
            }

            StageSelectButton firstUnlockedButton = null;

            foreach (StageSelectButton stageButton in stageButtons)
            {
                if (stageButton == null ||
                    !stageButton.IsUnlocked ||
                    StageSession.HasShownUnlockVisual(stageButton.StageIndex))
                {
                    continue;
                }

                if (firstUnlockedButton == null || stageButton.StageIndex < firstUnlockedButton.StageIndex)
                {
                    firstUnlockedButton = stageButton;
                }
            }

            return firstUnlockedButton;
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
