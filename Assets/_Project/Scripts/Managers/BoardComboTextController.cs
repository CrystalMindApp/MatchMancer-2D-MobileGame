using System.Collections;
using TMPro;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class BoardComboTextController : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField, Min(1f)] private float popScale = 1.16f;
        [SerializeField, Min(0f)] private float popDuration = 0.14f;
        [SerializeField, Min(0f)] private float fadeOutDelay = 0.35f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.2f;
        [SerializeField, Min(0f)] private float floatUpDistance = 8f;
        [SerializeField] private bool enableColorRamp = true;
        [SerializeField] private bool enableScaleRamp = true;
        [SerializeField] private Color baseComboColor = Color.white;
        [SerializeField] private Color maxComboColor = new Color(1f, 0.35f, 0.12f, 1f);
        [SerializeField, Min(1)] private int comboRampLimit = 3;
        [SerializeField, Min(0f)] private float baseComboScale = 1f;
        [SerializeField, Min(0f)] private float maxComboScale = 1.28f;

        [Header("References")]
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private SceneAudioLibrary sceneAudioLibrary;

        // Cache
        private RectTransform rectTransform;
        private Vector3 originalScale;
        private Vector2 originalAnchoredPosition;
        private Coroutine animationRoutine;

        // State
        private float currentRampScale = 1f;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            CacheReferences();
            ResetVisualState();
        }

        private void OnDisable()
        {
            StopRunningAnimation();
            ResetVisualState();
        }

        #endregion

        #region Public Methods

        public void ShowCombo(int comboCount)
        {
            if (comboText == null || canvasGroup == null)
            {
                return;
            }

            if (comboCount < 2)
            {
                HideImmediate();
                return;
            }

            StopRunningAnimation();
            comboText.text = $"COMBO x{comboCount}";
            ApplyComboRamp(comboCount);
            PlayComboSfx();
            animationRoutine = StartCoroutine(PopRoutine());
        }

        public void HideCombo()
        {
            if (canvasGroup == null)
            {
                return;
            }

            StopRunningAnimation();
            animationRoutine = StartCoroutine(FadeOutRoutine());
        }

        public void HideImmediate()
        {
            StopRunningAnimation();
            ResetVisualState();
        }

        #endregion

        #region Private Methods

        private void CacheReferences()
        {
            if (comboText == null)
            {
                comboText = GetComponentInChildren<TMP_Text>(true);
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
            originalScale = transform.localScale;

            if (rectTransform != null)
            {
                originalAnchoredPosition = rectTransform.anchoredPosition;
            }
        }

        private IEnumerator PopRoutine()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Vector3 rampScale = originalScale * currentRampScale;
            Vector3 peakScale = rampScale * popScale;
            float safeDuration = Mathf.Max(0f, popDuration);

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalAnchoredPosition;
            }

            if (safeDuration <= 0f)
            {
                transform.localScale = rampScale;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float time = Mathf.Clamp01(elapsed / safeDuration);
                float scaleTime = time < 0.5f ? time / 0.5f : (1f - time) / 0.5f;
                transform.localScale = Vector3.Lerp(rampScale, peakScale, Mathf.Clamp01(scaleTime));

                if (rectTransform != null && floatUpDistance > 0f)
                {
                    rectTransform.anchoredPosition = originalAnchoredPosition + Vector2.up * (floatUpDistance * time);
                }

                yield return null;
            }

            transform.localScale = rampScale;
            animationRoutine = null;
        }

        private void ApplyComboRamp(int comboCount)
        {
            int safeRampLimit = Mathf.Max(1, comboRampLimit);
            float rampTime = Mathf.Clamp01(Mathf.InverseLerp(1f, safeRampLimit, comboCount));

            if (comboText != null)
            {
                comboText.color = enableColorRamp
                    ? Color.Lerp(baseComboColor, maxComboColor, rampTime)
                    : baseComboColor;
            }

            currentRampScale = enableScaleRamp
                ? Mathf.Lerp(baseComboScale, maxComboScale, rampTime)
                : baseComboScale;
        }

        private IEnumerator FadeOutRoutine()
        {
            yield return new WaitForSecondsRealtime(fadeOutDelay);

            float safeDuration = Mathf.Max(0f, fadeOutDuration);
            float startAlpha = canvasGroup.alpha;

            if (safeDuration <= 0f)
            {
                ResetVisualState();
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float time = Mathf.Clamp01(elapsed / safeDuration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, time);
                yield return null;
            }

            ResetVisualState();
            animationRoutine = null;
        }

        private void StopRunningAnimation()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }
        }

        private void ResetVisualState()
        {
            transform.localScale = originalScale == Vector3.zero ? Vector3.one : originalScale;

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalAnchoredPosition;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (comboText != null)
            {
                comboText.color = baseComboColor;
            }

            currentRampScale = baseComboScale;
        }

        private void PlayComboSfx()
        {
            SceneAudioLibrary audioLibrary = sceneAudioLibrary != null ? sceneAudioLibrary : SceneAudioLibrary.Current;
            audioLibrary?.PlayCombo();
        }

        #endregion
    }
}
