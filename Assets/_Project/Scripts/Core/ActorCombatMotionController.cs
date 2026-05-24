using System.Collections;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class ActorCombatMotionController : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField, Min(0f)] private float attackNudgeDistance = 0.18f;
        [SerializeField, Min(0f)] private float recoilDistance = 0.12f;
        [SerializeField, Min(1f)] private float impactScale = 1.04f;
        [SerializeField, Min(0f)] private float moveOutDuration = 0.08f;
        [SerializeField, Min(0f)] private float returnDuration = 0.12f;
        [SerializeField] private bool useUnscaledTime;

        [Header("References")]
        [SerializeField] private Transform targetTransform;

        // Cache
        private Vector3 originalLocalPosition;
        private Vector3 originalLocalScale;
        private Coroutine motionRoutine;

        // State
        private bool hasCachedOriginalTransform;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            CacheReferences();
            CacheOriginalTransform();
        }

        private void OnDisable()
        {
            StopMotionRoutine();
            RestoreOriginalTransform();
        }

        #endregion

        #region Public Methods

        public void PlayAttackMotion(Vector3 targetWorldPosition)
        {
            Vector3 direction = GetLocalDirectionTo(targetWorldPosition);
            StartMotion(direction * attackNudgeDistance, originalLocalScale, false);
        }

        public void PlayHitMotion(Vector3 sourceWorldPosition)
        {
            Vector3 direction = -GetLocalDirectionTo(sourceWorldPosition);
            StartMotion(direction * recoilDistance, originalLocalScale * impactScale, true);
        }

        public void ResetMotion()
        {
            StopMotionRoutine();
            RestoreOriginalTransform();
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

        private void CacheOriginalTransform()
        {
            if (hasCachedOriginalTransform || targetTransform == null)
            {
                return;
            }

            originalLocalPosition = targetTransform.localPosition;
            originalLocalScale = targetTransform.localScale;
            hasCachedOriginalTransform = true;
        }

        private Vector3 GetLocalDirectionTo(Vector3 worldPosition)
        {
            if (targetTransform == null)
            {
                return Vector3.right;
            }

            Vector3 worldDirection = worldPosition - targetTransform.position;
            worldDirection.z = 0f;

            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                return Vector3.right;
            }

            Vector3 localDirection = targetTransform.parent != null
                ? targetTransform.parent.InverseTransformDirection(worldDirection.normalized)
                : worldDirection.normalized;
            localDirection.z = 0f;
            return localDirection.normalized;
        }

        private void StartMotion(Vector3 localOffset, Vector3 peakScale, bool scaleOnImpact)
        {
            CacheReferences();
            CacheOriginalTransform();

            if (targetTransform == null)
            {
                return;
            }

            StopMotionRoutine();
            motionRoutine = StartCoroutine(MotionRoutine(localOffset, peakScale, scaleOnImpact));
        }

        private IEnumerator MotionRoutine(Vector3 localOffset, Vector3 peakScale, bool scaleOnImpact)
        {
            Vector3 startPosition = targetTransform.localPosition;
            Vector3 startScale = targetTransform.localScale;
            Vector3 peakPosition = originalLocalPosition + localOffset;

            yield return StartCoroutine(InterpolateTransformRoutine(
                startPosition,
                peakPosition,
                startScale,
                scaleOnImpact ? peakScale : originalLocalScale,
                moveOutDuration));

            yield return StartCoroutine(InterpolateTransformRoutine(
                targetTransform.localPosition,
                originalLocalPosition,
                targetTransform.localScale,
                originalLocalScale,
                returnDuration));

            RestoreOriginalTransform();
            motionRoutine = null;
        }

        private IEnumerator InterpolateTransformRoutine(Vector3 fromPosition, Vector3 toPosition, Vector3 fromScale, Vector3 toScale, float duration)
        {
            float safeDuration = Mathf.Max(0f, duration);

            if (safeDuration <= 0f)
            {
                targetTransform.localPosition = toPosition;
                targetTransform.localScale = toScale;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float time = Mathf.Clamp01(elapsed / safeDuration);
                targetTransform.localPosition = Vector3.Lerp(fromPosition, toPosition, time);
                targetTransform.localScale = Vector3.Lerp(fromScale, toScale, time);
                yield return null;
            }

            targetTransform.localPosition = toPosition;
            targetTransform.localScale = toScale;
        }

        private void RestoreOriginalTransform()
        {
            if (targetTransform == null || !hasCachedOriginalTransform)
            {
                return;
            }

            targetTransform.localPosition = originalLocalPosition;
            targetTransform.localScale = originalLocalScale;
        }

        private void StopMotionRoutine()
        {
            if (motionRoutine == null)
            {
                return;
            }

            StopCoroutine(motionRoutine);
            motionRoutine = null;
        }

        #endregion
    }
}
