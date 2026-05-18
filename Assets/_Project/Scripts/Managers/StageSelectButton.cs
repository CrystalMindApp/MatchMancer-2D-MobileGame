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
        [SerializeField] private GameObject lockedVisual;
        [SerializeField] private GameObject[] starObjects;
        [SerializeField] private SceneAudioLibrary sceneAudioLibrary;

        // Cache

        // State
        private bool hasSearchedSceneAudioLibrary;

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

        #endregion

        #region Public Methods

        public void SelectStageAndLoad()
        {
            PlayButtonClickSfx();

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
                button.interactable = isUnlocked;
            }

            if (lockedVisual != null)
            {
                lockedVisual.SetActive(!isUnlocked);
            }

            if (starObjects == null)
            {
                return;
            }

            for (int i = 0; i < starObjects.Length; i++)
            {
                if (starObjects[i] != null)
                {
                    starObjects[i].SetActive(stars >= i + 1);
                }
            }
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

        private SceneAudioLibrary GetSceneAudioLibrary()
        {
            if (sceneAudioLibrary != null)
            {
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
