using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class StageBackgroundController : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private Sprite fallbackBackground;

        [Header("References")]
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private StageDatabase stageDatabase;

        // Cache

        // State

        #endregion

        #region Unity Methods

        private void Start()
        {
            ApplySelectedStageBackground();
        }

        #endregion

        #region Public Methods

        public void ApplySelectedStageBackground()
        {
            if (backgroundRenderer == null)
            {
                Debug.LogWarning("StageBackgroundController: Background renderer is missing.");
                return;
            }

            Sprite selectedBackground = GetSelectedStageBackground();

            if (selectedBackground != null)
            {
                backgroundRenderer.sprite = selectedBackground;
            }
        }

        #endregion

        #region Private Methods

        private Sprite GetSelectedStageBackground()
        {
            StageDefinition selectedStage = GetSelectedStage();

            if (selectedStage != null && selectedStage.BackgroundSprite != null)
            {
                return selectedStage.BackgroundSprite;
            }

            if (selectedStage != null)
            {
                Debug.LogWarning($"StageBackgroundController: Stage '{selectedStage.StageDisplayName}' has no background sprite assigned.");
            }

            return fallbackBackground;
        }

        private StageDefinition GetSelectedStage()
        {
            if (StageSession.SelectedStage != null)
            {
                return StageSession.SelectedStage;
            }

            if (stageDatabase == null)
            {
                Debug.LogWarning("StageBackgroundController: StageDatabase is missing. Using fallback background.");
                return null;
            }

            int selectedStageIndex = StageSession.SelectedStageIndex;
            if (selectedStageIndex < 0)
            {
                Debug.LogWarning("StageBackgroundController: No selected stage index. Using fallback background.");
                return null;
            }

            return stageDatabase.GetStageByIndex(selectedStageIndex);
        }

        #endregion
    }
}
