using System;
using System.Collections;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class UIPanelScaleTransition : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField, Min(0f)] private float showDuration = 0.18f;
        [SerializeField, Min(0f)] private float hideDuration = 0.12f;
        [SerializeField, Min(0f)] private float showStartScale = 0.86f;
        [SerializeField, Min(0f)] private float showOvershootScale = 1.06f;
        [SerializeField, Min(0f)] private float showEndScale = 1f;
        [SerializeField, Min(0f)] private float hideEndScale = 0.9f;
        [SerializeField] private AnimationCurve showCurve;
        [SerializeField] private AnimationCurve hideCurve;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool setActiveOnShow = true;
        [SerializeField] private bool setInactiveOnHide = true;
        [SerializeField] private bool blockInteractionDuringTransition = true;

        [Header("References")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private CanvasGroup optionalCanvasGroup;

        // Cache
        private Coroutine transitionRoutine;
        private Vector3 originalScale = Vector3.one;

        // State
        private bool isVisible;

        #endregion

        #region Properties

        public bool IsTransitioning => transitionRoutine != null;
        public bool IsVisible => isVisible;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            CacheReferences();
            CaptureOriginalScale();
        }

        private void OnDisable()
        {
            StopTransitionRoutine();
            RestoreTransitionScale();
            SetInteraction(false);
        }

        #endregion

        #region Public Methods

        public void Show(Action onComplete = null)
        {
            CacheReferences();

            if (targetTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            StopTransitionRoutine();

            if (setActiveOnShow)
            {
                targetTransform.gameObject.SetActive(true);
            }

            transitionRoutine = StartCoroutine(ShowRoutine(onComplete));
        }

        public void Hide(Action onComplete = null)
        {
            CacheReferences();

            if (targetTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            StopTransitionRoutine();
            transitionRoutine = StartCoroutine(HideRoutine(onComplete));
        }

        public void ShowImmediate()
        {
            CacheReferences();
            StopTransitionRoutine();

            if (targetTransform == null)
            {
                return;
            }

            if (setActiveOnShow)
            {
                targetTransform.gameObject.SetActive(true);
            }

            targetTransform.localScale = originalScale * showEndScale;
            SetCanvasAlpha(1f);
            SetInteraction(true);
            isVisible = true;
        }

        public void HideImmediate()
        {
            CacheReferences();
            StopTransitionRoutine();

            if (targetTransform == null)
            {
                return;
            }

            targetTransform.localScale = originalScale * hideEndScale;
            SetCanvasAlpha(0f);
            SetInteraction(false);
            isVisible = false;

            if (setInactiveOnHide)
            {
                targetTransform.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Private Methods

        private void CacheReferences()
        {
            if (targetTransform == null)
            {
                targetTransform = transform;
            }
        }

        private void CaptureOriginalScale()
        {
            if (targetTransform != null)
            {
                originalScale = targetTransform.localScale;
            }
        }

        private IEnumerator ShowRoutine(Action onComplete)
        {
            SetInteraction(false);
            SetCanvasAlpha(1f);

            float safeDuration = Mathf.Max(0f, showDuration);
            targetTransform.localScale = originalScale * showStartScale;

            if (safeDuration <= 0f)
            {
                CompleteShow(onComplete);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += GetDeltaTime();
                float time = EvaluateCurve(showCurve, Mathf.Clamp01(elapsed / safeDuration));
                targetTransform.localScale = originalScale * EvaluateShowScale(time);
                yield return null;
            }

            CompleteShow(onComplete);
        }

        private IEnumerator HideRoutine(Action onComplete)
        {
            SetInteraction(false);

            float safeDuration = Mathf.Max(0f, hideDuration);
            Vector3 startScale = targetTransform.localScale;
            Vector3 endScale = originalScale * hideEndScale;

            if (safeDuration <= 0f)
            {
                CompleteHide(onComplete);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += GetDeltaTime();
                float time = EvaluateCurve(hideCurve, Mathf.Clamp01(elapsed / safeDuration));
                targetTransform.localScale = Vector3.LerpUnclamped(startScale, endScale, time);
                yield return null;
            }

            CompleteHide(onComplete);
        }

        private void CompleteShow(Action onComplete)
        {
            targetTransform.localScale = originalScale * showEndScale;
            SetCanvasAlpha(1f);
            SetInteraction(true);
            isVisible = true;
            transitionRoutine = null;
            onComplete?.Invoke();
        }

        private void CompleteHide(Action onComplete)
        {
            targetTransform.localScale = originalScale * hideEndScale;
            SetCanvasAlpha(0f);
            SetInteraction(false);
            isVisible = false;
            transitionRoutine = null;

            if (setInactiveOnHide)
            {
                targetTransform.gameObject.SetActive(false);
            }

            onComplete?.Invoke();
        }

        private float EvaluateShowScale(float time)
        {
            if (time < 0.65f)
            {
                float phase = Mathf.Clamp01(time / 0.65f);
                return Mathf.LerpUnclamped(showStartScale, showOvershootScale, phase);
            }

            float settlePhase = Mathf.Clamp01((time - 0.65f) / 0.35f);
            return Mathf.LerpUnclamped(showOvershootScale, showEndScale, settlePhase);
        }

        private float EvaluateCurve(AnimationCurve curve, float time)
        {
            if (curve == null || curve.length == 0)
            {
                return time;
            }

            return curve.Evaluate(time);
        }

        private float GetDeltaTime()
        {
            return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private void SetInteraction(bool interactable)
        {
            if (!blockInteractionDuringTransition || optionalCanvasGroup == null)
            {
                return;
            }

            optionalCanvasGroup.interactable = interactable;
            optionalCanvasGroup.blocksRaycasts = interactable;
        }

        private void SetCanvasAlpha(float alpha)
        {
            if (optionalCanvasGroup != null)
            {
                optionalCanvasGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        private void StopTransitionRoutine()
        {
            if (transitionRoutine == null)
            {
                return;
            }

            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        private void RestoreTransitionScale()
        {
            if (targetTransform != null)
            {
                targetTransform.localScale = originalScale * (isVisible ? showEndScale : hideEndScale);
            }
        }

        #endregion
    }
}
