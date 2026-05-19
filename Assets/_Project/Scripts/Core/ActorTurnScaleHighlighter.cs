using System.Collections;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class ActorTurnScaleHighlighter : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField, Min(1f)] private float activeTurnScale = 1.08f;
        [SerializeField, Min(0f)] private float activeTurnScaleDuration = 0.12f;
        [SerializeField, Min(0f)] private float activeTurnReturnDuration = 0.12f;
        [SerializeField] private bool useUnscaledTime;

        [Header("References")]
        [SerializeField] private Transform targetTransform;

        // Cache
        private Vector3 originalScale = Vector3.one;
        private Coroutine scaleRoutine;

        // State
        private bool hasCachedScale;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            CacheReferences();
            CacheOriginalScale();
        }

        private void OnDisable()
        {
            StopScaleRoutine();
            RestoreOriginalScale();
        }

        #endregion

        #region Public Methods

        public void Highlight()
        {
            CacheReferences();
            CacheOriginalScale();
            StartScaleRoutine(originalScale * activeTurnScale, activeTurnScaleDuration);
        }

        public void ClearHighlight()
        {
            CacheReferences();
            CacheOriginalScale();
            StartScaleRoutine(originalScale, activeTurnReturnDuration);
        }

        public void ResetHighlight()
        {
            StopScaleRoutine();
            RestoreOriginalScale();
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

        private void CacheOriginalScale()
        {
            if (hasCachedScale || targetTransform == null)
            {
                return;
            }

            originalScale = targetTransform.localScale;
            hasCachedScale = true;
        }

        private void StartScaleRoutine(Vector3 targetScale, float duration)
        {
            if (targetTransform == null)
            {
                return;
            }

            StopScaleRoutine();

            if (duration <= 0f)
            {
                targetTransform.localScale = targetScale;
                return;
            }

            scaleRoutine = StartCoroutine(ScaleRoutine(targetScale, duration));
        }

        private IEnumerator ScaleRoutine(Vector3 targetScale, float duration)
        {
            Vector3 startScale = targetTransform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float time = Mathf.Clamp01(elapsed / duration);
                targetTransform.localScale = Vector3.Lerp(startScale, targetScale, time);
                yield return null;
            }

            targetTransform.localScale = targetScale;
            scaleRoutine = null;
        }

        private void RestoreOriginalScale()
        {
            if (targetTransform != null && hasCachedScale)
            {
                targetTransform.localScale = originalScale;
            }
        }

        private void StopScaleRoutine()
        {
            if (scaleRoutine == null)
            {
                return;
            }

            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        #endregion
    }
}
