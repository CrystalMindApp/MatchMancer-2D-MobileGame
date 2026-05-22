using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrystalMind.MatchMancer
{
    public class TransitionOverlayController : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private string fadeOutStateName = "FadeOut";
        [SerializeField] private string fadeInStateName = "FadeIn";
        [SerializeField, Min(0f)] private float fallbackFadeDuration = 1f;
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool playFadeInOnAwake = true;
        [SerializeField] private bool waitForSceneReadyBeforeFadeIn = true;

        [Header("References")]
        [SerializeField] private CanvasGroup transitionCanvasGroup;
        [SerializeField] private Animator transitionAnimator;

        // Cache
        private Coroutine fadeRoutine;
        private Coroutine sceneTransitionRoutine;

        // State
        private bool isFading;
        private bool isWaitingForSceneReady;
        private bool sceneReadyReleased;

        #endregion

        #region Properties

        public static TransitionOverlayController Instance { get; private set; }
        public bool IsFading => isFading;
        public bool IsTransitionActive => isFading || IsCanvasBlocking();
        public bool IsWaitingForSceneReady => isWaitingForSceneReady;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(transform.root.gameObject);
                return;
            }

            Instance = this;
            CacheReferences();

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(transform.root.gameObject);
            }

            if (playFadeInOnAwake)
            {
                SetCanvasState(1f, true);
                StartFade(fadeInStateName, false, null);
            }
            else
            {
                SetTransparentInputState();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Public Methods

        public void PlayFadeOut(Action onComplete = null)
        {
            if (sceneTransitionRoutine != null)
            {
                return;
            }

            StartFade(fadeOutStateName, true, onComplete);
        }

        public void PlayFadeIn(Action onComplete = null)
        {
            if (sceneTransitionRoutine != null)
            {
                return;
            }

            StartFade(fadeInStateName, false, onComplete);
        }

        public void TransitionToScene(string sceneName)
        {
            TransitionToScene(sceneName, null);
        }

        public void TransitionToScene(string sceneName, Action beforeLoadWhenBlack)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("TransitionOverlayController: Scene name is missing.");
                return;
            }

            StartSceneTransition(() =>
            {
                beforeLoadWhenBlack?.Invoke();
                SceneManager.LoadScene(sceneName);
            });
        }

        public void TransitionToScene(int sceneBuildIndex)
        {
            TransitionToScene(sceneBuildIndex, null);
        }

        public void TransitionToScene(int sceneBuildIndex, Action beforeLoadWhenBlack)
        {
            StartSceneTransition(() =>
            {
                beforeLoadWhenBlack?.Invoke();
                SceneManager.LoadScene(sceneBuildIndex);
            });
        }

        public IEnumerator WaitForTransitionComplete()
        {
            while (IsTransitionActive)
            {
                yield return null;
            }
        }

        public void ReleaseSceneReady()
        {
            sceneReadyReleased = true;
        }

        public void SetFadeOutImmediate()
        {
            StopFadeRoutine();
            SetCanvasState(1f, true);
            isFading = false;
        }

        public void SetFadeInImmediate()
        {
            StopFadeRoutine();
            SetTransparentInputState();
            isFading = false;
        }

        #endregion

        #region Private Methods

        private void CacheReferences()
        {
            if (transitionCanvasGroup == null)
            {
                transitionCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
            }

            if (transitionAnimator == null)
            {
                transitionAnimator = GetComponentInChildren<Animator>(true);
            }

            if (transitionAnimator != null)
            {
                transitionAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }

        private void StartFade(string stateName, bool endBlocking, Action onComplete)
        {
            CacheReferences();
            StopFadeRoutine();
            fadeRoutine = StartCoroutine(FadeRoutine(stateName, endBlocking, onComplete));
        }

        private void StartSceneTransition(Action loadSceneAction)
        {
            if (sceneTransitionRoutine != null)
            {
                return;
            }

            CacheReferences();
            StopFadeRoutine();
            sceneTransitionRoutine = StartCoroutine(SceneTransitionRoutine(loadSceneAction));
        }

        private IEnumerator SceneTransitionRoutine(Action loadSceneAction)
        {
            yield return FadeRoutine(fadeOutStateName, true, null);

            SetCanvasState(1f, true);
            isWaitingForSceneReady = waitForSceneReadyBeforeFadeIn;
            sceneReadyReleased = !waitForSceneReadyBeforeFadeIn;

            loadSceneAction?.Invoke();
            yield return null;

            while (isWaitingForSceneReady && !sceneReadyReleased)
            {
                yield return null;
            }

            isWaitingForSceneReady = false;
            yield return FadeRoutine(fadeInStateName, false, null);
            sceneTransitionRoutine = null;
        }

        private IEnumerator FadeRoutine(string stateName, bool endBlocking, Action onComplete)
        {
            isFading = true;
            SetCanvasInputBlocking(true);

            if (transitionAnimator != null && !string.IsNullOrWhiteSpace(stateName))
            {
                transitionAnimator.Play(stateName, 0, 0f);
            }
            else
            {
                Debug.LogWarning("TransitionOverlayController: Missing Animator or fade state name. Using fallback timing.");
            }

            float waitDuration = GetFadeDuration(stateName);
            yield return WaitUnscaled(waitDuration);

            if (endBlocking)
            {
                SetCanvasState(1f, true);
            }
            else
            {
                SetTransparentInputState();
            }

            isFading = false;
            fadeRoutine = null;
            onComplete?.Invoke();
        }

        private float GetFadeDuration(string stateName)
        {
            if (transitionAnimator == null || transitionAnimator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName))
            {
                return fallbackFadeDuration;
            }

            AnimationClip[] clips = transitionAnimator.runtimeAnimatorController.animationClips;

            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];

                if (clip != null && clip.name == stateName)
                {
                    return Mathf.Max(0f, clip.length);
                }
            }

            Debug.LogWarning($"TransitionOverlayController: Could not find animation clip '{stateName}'. Using fallback timing.");
            return fallbackFadeDuration;
        }

        private IEnumerator WaitUnscaled(float duration)
        {
            float safeDuration = Mathf.Max(0f, duration);

            if (safeDuration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void SetTransparentInputState()
        {
            SetCanvasState(0f, false);
        }

        private void SetCanvasState(float alpha, bool blocksRaycasts)
        {
            if (transitionCanvasGroup == null)
            {
                return;
            }

            transitionCanvasGroup.alpha = Mathf.Clamp01(alpha);
            transitionCanvasGroup.interactable = false;
            transitionCanvasGroup.blocksRaycasts = blocksRaycasts;
        }

        private void SetCanvasInputBlocking(bool blocksRaycasts)
        {
            if (transitionCanvasGroup == null)
            {
                return;
            }

            transitionCanvasGroup.interactable = false;
            transitionCanvasGroup.blocksRaycasts = blocksRaycasts;
        }

        private bool IsCanvasBlocking()
        {
            return transitionCanvasGroup != null &&
                (transitionCanvasGroup.blocksRaycasts || transitionCanvasGroup.alpha > 0.001f);
        }

        private void StopFadeRoutine()
        {
            if (fadeRoutine == null)
            {
                return;
            }

            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        #endregion
    }
}
