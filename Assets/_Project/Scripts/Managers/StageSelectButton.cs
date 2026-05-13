using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrystalMind.MatchMancer
{
    public class StageSelectButton : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private string mainGameSceneName = "MainGame";

        [Header("References")]
        [SerializeField] private StageDefinition stageDefinition;

        // Cache

        // State

        #endregion

        #region Public Methods

        public void SelectStageAndLoad()
        {
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
            SceneManager.LoadScene(mainGameSceneName);
        }

        #endregion
    }
}
