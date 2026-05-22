using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrystalMind.MatchMancer
{
    public class StageSelectButton : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField, Min(0)] private int stageIndex;
        [SerializeField] private string mainGameSceneName = "MainGame";

        [Header("References")]
        [SerializeField] private StageDefinition stageDefinition;
        [SerializeField] private Button button;
        [SerializeField] private SceneAudioLibrary sceneAudioLibrary;

        [Header("Stage Clear Visuals")]
        [SerializeField] private GameObject[] starObjects;
        [SerializeField] private Image lockImage;
        [SerializeField] private CanvasGroup lockCanvasGroup;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite unlockedSprite;

        // Cache

        // State
        private bool hasSearchedSceneAudioLibrary;
        private bool inputBlocked;
        private bool isUnlockAnimating;

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            RefreshVisualState();
        }

        #endregion

        #region Properties

        public bool IsUnlocked => StageProgression.IsStageUnlocked(stageIndex);
        public int Stars => StageProgression.GetStageStars(stageIndex);
        public int StageIndex => stageIndex;

        #endregion

        #region Public Methods

        public void SelectStageAndLoad()
        {
            PlayButtonClickSfx();

            if (inputBlocked)
            {
                return;
            }

            if (!IsUnlocked)
            {
                Debug.Log($"Stage locked: {stageIndex}");
                return;
            }

            if (stageDefinition == null)
            {
                Debug.LogWarning("StageSelectButton: No StageDefinition assigned.");
                return;
            }

            if (string.IsNullOrWhiteSpace(mainGameSceneName))
            {
                Debug.LogWarning("StageSelectButton: Main game scene name is missing.");
                return;
            }

            StageSession.SelectedStage = stageDefinition;
            StageSession.SelectedStageIndex = stageIndex;
            SceneManager.LoadScene(mainGameSceneName);
        }

        public void RefreshVisualState()
        {
            if (isUnlockAnimating)
            {
                return;
            }

            bool isUnlocked = GetDisplayUnlockedState();
            int stars = GetDisplayStars();
            bool showLockedPresentation = ShouldShowLockedPresentation(isUnlocked);

            if (button != null)
            {
                button.interactable = isUnlocked && !inputBlocked && !showLockedPresentation;
            }

            SetLockPresentation(showLockedPresentation, showLockedPresentation ? lockedSprite : unlockedSprite, 1f);
            SetStarsVisual(stars);
        }

        public void SetInputBlocked(bool blocked)
        {
            inputBlocked = blocked;
            RefreshVisualState();
        }

        public void SetStarsVisual(int stars)
        {
            if (starObjects == null)
            {
                return;
            }

            int safeStars = Mathf.Clamp(stars, 0, starObjects.Length);

            for (int i = 0; i < starObjects.Length; i++)
            {
                if (starObjects[i] != null)
                {
                    starObjects[i].SetActive(safeStars >= i + 1);
                    starObjects[i].transform.localScale = Vector3.one;
                }
            }
        }

        public void SetLockedVisual(bool locked, float alpha = 1f)
        {
            SetLockPresentation(locked, locked ? lockedSprite : unlockedSprite, alpha);
        }

        public IEnumerator PlayStarRevealTransition(int previousStars, int earnedStars, float revealDelay, float popScale, float popDuration)
        {
            SetStarsVisual(previousStars);

            int targetStars = Mathf.Clamp(earnedStars, 0, starObjects != null ? starObjects.Length : 0);
            int startStars = Mathf.Clamp(previousStars, 0, targetStars);

            for (int i = startStars; i < targetStars; i++)
            {
                if (starObjects == null || i >= starObjects.Length || starObjects[i] == null)
                {
                    continue;
                }

                yield return new WaitForSecondsRealtime(Mathf.Max(0f, revealDelay));
                yield return StartCoroutine(PlayStarPopRoutine(starObjects[i].transform, popScale, popDuration));
            }
        }

        public IEnumerator PlayUnlockTransition(
            float lockShakeDuration,
            float lockShakeStrength,
            float postLockShakeDelay,
            float postUnlockSpriteDelay,
            float lockFadeDuration)
        {
            Image image = GetLockImage();
            if (image == null)
            {
                yield break;
            }

            isUnlockAnimating = true;

            if (button != null)
            {
                button.interactable = false;
            }

            SetLockPresentation(true, lockedSprite, 1f);
            yield return StartCoroutine(PlayLockShakeRoutine(lockShakeDuration, lockShakeStrength));
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, postLockShakeDelay));

            SetLockSprite(unlockedSprite);
            SetLockAlpha(1f);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, postUnlockSpriteDelay));

            float safeDuration = Mathf.Max(0f, lockFadeDuration);

            if (safeDuration <= 0f)
            {
                SetLockPresentation(false, unlockedSprite, 0f);
                StageSession.MarkUnlockVisualShown(stageIndex);
                isUnlockAnimating = false;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float time = Mathf.Clamp01(elapsed / safeDuration);
                SetLockAlpha(Mathf.Lerp(1f, 0f, time));
                yield return null;
            }

            SetLockPresentation(false, unlockedSprite, 0f);
            StageSession.MarkUnlockVisualShown(stageIndex);
            isUnlockAnimating = false;
        }

        #endregion

        #region Private Methods

        private void PlayButtonClickSfx()
        {
            SceneAudioLibrary audioLibrary = GetSceneAudioLibrary();

            if (audioLibrary != null)
            {
                audioLibrary.PlayButtonClick();
            }
        }

        private IEnumerator PlayStarPopRoutine(Transform starTransform, float popScale, float popDuration)
        {
            starTransform.gameObject.SetActive(true);
            Vector3 baseScale = Vector3.one;
            Vector3 peakScale = baseScale * Mathf.Max(1f, popScale);
            float safeDuration = Mathf.Max(0f, popDuration);

            if (safeDuration <= 0f)
            {
                starTransform.localScale = baseScale;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float time = Mathf.Clamp01(elapsed / safeDuration);
                float scaleTime = time < 0.5f ? time / 0.5f : (1f - time) / 0.5f;
                starTransform.localScale = Vector3.Lerp(baseScale, peakScale, Mathf.Clamp01(scaleTime));
                yield return null;
            }

            starTransform.localScale = baseScale;
        }

        private void SetLockAlpha(float alpha)
        {
            CanvasGroup lockGroup = GetLockCanvasGroup();
            if (lockGroup != null)
            {
                lockGroup.alpha = Mathf.Clamp01(alpha);
                return;
            }

        }

        private int GetDisplayStars()
        {
            if (StageSession.HasPendingStageClearVisual &&
                StageSession.PendingStageStarsImproved &&
                StageSession.PendingClearedStageIndex == stageIndex)
            {
                return StageSession.PendingPreviousStars;
            }

            return Stars;
        }

        private bool GetDisplayUnlockedState()
        {
            if (StageSession.HasPendingStageClearVisual &&
                StageSession.PendingStageUnlockedNext &&
                StageSession.PendingUnlockedStageIndex == stageIndex)
            {
                return false;
            }

            return IsUnlocked;
        }

        private bool ShouldShowLockedPresentation(bool isUnlocked)
        {
            if (!isUnlocked)
            {
                return true;
            }

            return !StageSession.HasShownUnlockVisual(stageIndex);
        }

        private void SetLockPresentation(bool visible, Sprite sprite, float alpha)
        {
            Image image = GetLockImage();

            if (image != null)
            {
                image.gameObject.SetActive(visible);
            }

            SetLockSprite(sprite);
            SetLockAlpha(alpha);

        }

        private void SetLockSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Image image = GetLockImage();
            if (image != null)
            {
                image.sprite = sprite;
            }

        }

        private Image GetLockImage()
        {
            return lockImage;
        }

        private CanvasGroup GetLockCanvasGroup()
        {
            return lockCanvasGroup;
        }

        private IEnumerator PlayLockShakeRoutine(float duration, float strength)
        {
            Image image = GetLockImage();
            float safeDuration = Mathf.Max(0f, duration);
            float safeStrength = Mathf.Max(0f, strength);

            if (safeDuration <= 0f)
            {
                yield break;
            }

            Transform lockTransform = image != null ? image.transform : null;
            Vector3 originalLocalPosition = lockTransform != null ? lockTransform.localPosition : Vector3.zero;
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float shake = safeStrength > 0f ? Mathf.Sin(elapsed * 45f) * safeStrength : 0f;

                if (lockTransform != null)
                {
                    lockTransform.localPosition = originalLocalPosition + Vector3.right * shake;
                }

                yield return null;
            }

            if (lockTransform != null)
            {
                lockTransform.localPosition = originalLocalPosition;
            }
        }

        private SceneAudioLibrary GetSceneAudioLibrary()
        {
            if (sceneAudioLibrary != null)
            {
                return sceneAudioLibrary;
            }

            if (SceneAudioLibrary.Current != null)
            {
                sceneAudioLibrary = SceneAudioLibrary.Current;
                return sceneAudioLibrary;
            }

            if (hasSearchedSceneAudioLibrary)
            {
                return null;
            }

            hasSearchedSceneAudioLibrary = true;
            sceneAudioLibrary = FindFirstObjectByType<SceneAudioLibrary>();
            return sceneAudioLibrary;
        }

        #endregion
    }
}
