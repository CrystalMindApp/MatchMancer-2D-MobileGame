using System;
using TMPro;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class DamagePopupItem : MonoBehaviour
    {
        #region Variables

        [Header("References")]
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private CanvasGroup canvasGroup;

        // Cache
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private Camera worldCamera;
        private Camera uiCamera;
        private Transform followAnchor;
        private Action<DamagePopupItem> releaseCallback;

        // State
        private Vector2 screenOffset;
        private Vector2 floatOffset;
        private Vector3 baseScale = Vector3.one;
        private Vector3 punchScale = Vector3.one;
        private float duration;
        private float elapsed;
        private bool isPlaying;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            CacheReferences();
        }

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float time = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            UpdatePosition(time);
            UpdateVisual(time);

            if (time >= 1f)
            {
                Complete();
            }
        }

        #endregion

        #region Public Methods

        public void Play(
            string text,
            Transform anchor,
            Canvas canvas,
            Camera cameraForWorld,
            Vector2 startOffset,
            Vector2 targetFloatOffset,
            float popupDuration,
            Vector3 startScale,
            Vector3 peakScale,
            Color textColor,
            Action<DamagePopupItem> onComplete)
        {
            CacheReferences();

            followAnchor = anchor;
            parentCanvas = canvas;
            worldCamera = cameraForWorld;
            uiCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? parentCanvas.worldCamera
                : null;
            screenOffset = startOffset;
            floatOffset = targetFloatOffset;
            duration = Mathf.Max(0.01f, popupDuration);
            baseScale = startScale;
            punchScale = peakScale;
            releaseCallback = onComplete;
            elapsed = 0f;
            isPlaying = true;

            if (damageText != null)
            {
                damageText.text = text;
                damageText.color = textColor;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            transform.localScale = baseScale;
            gameObject.SetActive(true);
            UpdatePosition(0f);
        }

        public void Stop()
        {
            isPlaying = false;
            gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        private void CacheReferences()
        {
            if (damageText == null)
            {
                damageText = GetComponentInChildren<TMP_Text>(true);
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            rectTransform = transform as RectTransform;
        }

        private void UpdatePosition(float time)
        {
            if (rectTransform == null || followAnchor == null)
            {
                return;
            }

            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(worldCamera, followAnchor.position);
            Vector2 targetScreenPosition = screenPosition + screenOffset + Vector2.Lerp(Vector2.zero, floatOffset, time);

            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                RectTransform canvasRectTransform = parentCanvas.transform as RectTransform;

                if (canvasRectTransform != null &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, targetScreenPosition, uiCamera, out Vector2 localPoint))
                {
                    rectTransform.anchoredPosition = localPoint;
                }

                return;
            }

            rectTransform.position = targetScreenPosition;
        }

        private void UpdateVisual(float time)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - time;
            }

            float scaleTime = time < 0.35f ? time / 0.35f : 1f - ((time - 0.35f) / 0.65f);
            transform.localScale = Vector3.Lerp(baseScale, punchScale, Mathf.Clamp01(scaleTime));
        }

        private void Complete()
        {
            isPlaying = false;
            releaseCallback?.Invoke(this);
        }

        #endregion
    }
}
