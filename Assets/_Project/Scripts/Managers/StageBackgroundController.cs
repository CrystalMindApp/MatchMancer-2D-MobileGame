using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class StageBackgroundController : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private Sprite fallbackBackground;
        [SerializeField] private Sprite fallbackForeground;

        [Header("References")]
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private SpriteRenderer foregroundRenderer;
        [SerializeField] private StageDatabase stageDatabase;

        // Cache

        // State

        #endregion

        #region Unity Methods

        private System.Collections.IEnumerator Start()
        {
            ApplySelectedStageBackground();
            yield return null;
            ApplySelectedStageBgm();
        }

        #endregion

        #region Public Methods

        public void ApplySelectedStageBackground()
        {
            StageDefinition selectedStage = GetSelectedStage();

            if (backgroundRenderer != null)
            {
                ApplySprite(backgroundRenderer, GetSelectedStageBackground(selectedStage), false);
            }
            else
            {
                Debug.LogWarning("StageBackgroundController: Background renderer is missing.");
            }

            ApplySprite(foregroundRenderer, GetSelectedStageForeground(selectedStage), true);
        }

        public void ApplySelectedStageBgm()
        {
            StageDefinition selectedStage = GetSelectedStage();

            if (selectedStage == null || selectedStage.StageBgm == null)
            {
                return;
            }

            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("StageBackgroundController: AudioManager is missing. Stage BGM was not applied.");
                return;
            }

            AudioManager.Instance.PlayBgm(selectedStage.StageBgm);
        }

        #endregion

        #region Private Methods

        private Sprite GetSelectedStageBackground(StageDefinition selectedStage)
        {
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

        private Sprite GetSelectedStageForeground(StageDefinition selectedStage)
        {
            if (selectedStage != null && selectedStage.ForegroundSprite != null)
            {
                return selectedStage.ForegroundSprite;
            }

            return fallbackForeground;
        }

        private void ApplySprite(SpriteRenderer targetRenderer, Sprite sprite, bool hideWhenMissing)
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.sprite = sprite;

            if (hideWhenMissing)
            {
                targetRenderer.enabled = sprite != null;
            }
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
