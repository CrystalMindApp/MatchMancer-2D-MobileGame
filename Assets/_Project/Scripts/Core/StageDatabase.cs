using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "StageDatabase", menuName = "MatchMancer/Stage/Stage Database")]
    public class StageDatabase : ScriptableObject
    {
        #region Variables

        [Header("References")]
        [SerializeField] private StageDefinition[] stages;

        // Cache

        // State

        #endregion

        #region Public Methods

        public StageDefinition GetStageByIndex(int index)
        {
            if (stages == null || stages.Length == 0)
            {
                Debug.LogWarning("StageDatabase: No stages assigned.");
                return null;
            }

            if (index < 0 || index >= stages.Length)
            {
                Debug.LogWarning($"StageDatabase: Invalid stage index {index}. Valid range is 0-{stages.Length - 1}.");
                return null;
            }

            StageDefinition stage = stages[index];

            if (stage == null)
            {
                Debug.LogWarning($"StageDatabase: Stage at index {index} is null.");
            }

            return stage;
        }

        public int GetStageCount()
        {
            return stages != null ? stages.Length : 0;
        }

        #endregion
    }
}
