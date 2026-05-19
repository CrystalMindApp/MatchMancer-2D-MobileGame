using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CrystalMind.MatchMancer
{
    public class UIPulseHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        #region Variables

        [Header("Settings")]
        [SerializeField, Min(1f)] private float hoverPulseScale = 1.06f;
        [SerializeField, Min(0f)] private float pulseSpeed = 4f;
        [SerializeField, Min(0f)] private float returnSpeed = 10f;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool enableClickPop = true;
        [SerializeField, Range(0.8f, 1.2f)] private float clickScale = 0.95f;
        [SerializeField, Min(0f)] private float clickPopDuration = 0.08f;

        [Header("References")]
        [SerializeField] private RectTransform targetTransform;

        // Cache
        private Vector3 originalScale;
        private Coroutine clickRoutine;

        // State
        private bool isHovered;
        private bool isPointerDown;
        private float pulseTimer;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            CacheTargetTransform();
            CacheOriginalScale();
        }

        private void OnEnable()
        {
            CacheTargetTransform();
            CacheOriginalScale();
            pulseTimer = 0f;
        }

        private void Update()
        {
            UpdatePulse();
        }

        private void OnDisable()
        {
            RestoreOriginalScale();
        }

        private void OnDestroy()
        {
            RestoreOriginalScale();
        }

        #endregion

        #region Public Methods

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            isPointerDown = false;
            pulseTimer = 0f;

            StopClickRoutine();

            Transform target = GetTargetTransform();
            if (target != null)
            {
                target.localScale = originalScale;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            isPointerDown = false;
            pulseTimer = 0f;
            StopClickRoutine();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!enableClickPop)
            {
                return;
            }

            isPointerDown = true;
            StartClickPop(originalScale * clickScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;
        }

        #endregion

        #region Private Methods

        private void UpdatePulse()
        {
            Transform target = GetTargetTransform();
            if (target == null || isPointerDown)
            {
                return;
            }

            float deltaTime = GetDeltaTime();

            if (isHovered)
            {
                pulseTimer += deltaTime * pulseSpeed;
                float pulse = (1f - Mathf.Cos(pulseTimer)) * 0.5f;
                Vector3 targetScale = Vector3.Lerp(originalScale, originalScale * hoverPulseScale, pulse);
                target.localScale = targetScale;
                return;
            }

            target.localScale = Vector3.Lerp(target.localScale, originalScale, deltaTime * returnSpeed);
        }

        private void StartClickPop(Vector3 targetScale)
        {
            StopClickRoutine();

            clickRoutine = StartCoroutine(ClickPopRoutine(targetScale));
        }

        private IEnumerator ClickPopRoutine(Vector3 targetScale)
        {
            Transform target = GetTargetTransform();
            if (target == null)
            {
                yield break;
            }

            float safeDuration = Mathf.Max(0f, clickPopDuration);

            if (safeDuration <= 0f)
            {
                target.localScale = targetScale;
                yield break;
            }

            Vector3 startScale = target.localScale;
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += GetDeltaTime();
                float time = Mathf.Clamp01(elapsed / safeDuration);
                target.localScale = Vector3.Lerp(startScale, targetScale, time);
                yield return null;
            }

            clickRoutine = null;
        }

        private void CacheTargetTransform()
        {
            if (targetTransform == null)
            {
                targetTransform = transform as RectTransform;
            }
        }

        private void CacheOriginalScale()
        {
            Transform target = GetTargetTransform();
            if (target != null)
            {
                originalScale = target.localScale;
            }
        }

        private void StopClickRoutine()
        {
            if (clickRoutine != null)
            {
                StopCoroutine(clickRoutine);
                clickRoutine = null;
            }
        }

        private Transform GetTargetTransform()
        {
            return targetTransform != null ? targetTransform : transform;
        }

        private float GetDeltaTime()
        {
            return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private void RestoreOriginalScale()
        {
            StopClickRoutine();

            Transform target = GetTargetTransform();
            if (target != null)
            {
                target.localScale = originalScale;
            }

            isHovered = false;
            isPointerDown = false;
            pulseTimer = 0f;
        }

        #endregion
    }
}
