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
        [SerializeField] private GameObject lockedVisual;
        [SerializeField] private CanvasGroup lockedVisualCanvasGroup;
        [SerializeField] private Graphic lockedVisualGraphic;
        [SerializeField] private GameObject unlockVisual;

        // Cache

        // State
        private bool hasSearchedSceneAudioLibrary;
        private bool inputBlocked;

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
            bool isUnlocked = IsUnlocked;
            int stars = Stars;

            if (button != null)
            {
                button.interactable = isUnlocked && !inputBlocked;
            }

            SetLockedVisual(!isUnlocked, 1f);
            SetStarsVisual(stars);
            SetUnlockVisual(isUnlocked);
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
            if (lockedVisual == null)
            {
                return;
            }

            lockedVisual.SetActive(locked);
            SetLockAlpha(alpha);
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

        public IEnumerator PlayUnlockTransition(float lockFadeDuration)
        {
            if (lockedVisual == null)
            {
                yield break;
            }

            lockedVisual.SetActive(true);
            SetUnlockVisual(false);
            float safeDuration = Mathf.Max(0f, lockFadeDuration);

            if (safeDuration <= 0f)
            {
                SetLockedVisual(false, 0f);
                SetUnlockVisual(true);
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

            SetLockedVisual(false, 0f);
            SetUnlockVisual(true);
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
            if (lockedVisualCanvasGroup != null)
            {
                lockedVisualCanvasGroup.alpha = Mathf.Clamp01(alpha);
                return;
            }

            if (lockedVisualGraphic != null)
            {
                SetGraphicAlpha(lockedVisualGraphic, alpha);
                return;
            }

            if (lockedVisual == null)
            {
                return;
            }

            CanvasGroup canvasGroup = lockedVisual.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(alpha);
                return;
            }

            Graphic graphic = lockedVisual.GetComponent<Graphic>();
            if (graphic == null)
            {
                graphic = lockedVisual.GetComponentInChildren<Graphic>(true);
            }

            if (graphic == null)
            {
                return;
            }

            SetGraphicAlpha(graphic, alpha);
        }

        private void SetUnlockVisual(bool visible)
        {
            if (unlockVisual != null)
            {
                unlockVisual.SetActive(visible);
            }
        }

        private void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            Color color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
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
