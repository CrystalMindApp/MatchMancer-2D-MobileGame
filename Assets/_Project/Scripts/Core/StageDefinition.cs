using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "StageDefinition", menuName = "MatchMancer/Stage/Stage Definition")]
    public class StageDefinition : ScriptableObject
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private string stageId;
        [SerializeField] private string stageDisplayName = "Stage";
        [SerializeField, Min(0)] private int recommendedLevel;
        [SerializeField, TextArea] private string description;

        [Header("References")]
        [SerializeField] private EnemyDefinition[] enemyRoundSequence;

        // Cache

        // State

        #endregion

        #region Properties

        public string StageId => string.IsNullOrWhiteSpace(stageId) ? name : stageId;
        public string StageDisplayName => string.IsNullOrWhiteSpace(stageDisplayName) ? name : stageDisplayName;
        public int RecommendedLevel => recommendedLevel;
        public string Description => description;
        public EnemyDefinition[] EnemyRoundSequence => enemyRoundSequence;
        public bool HasEnemyRounds => enemyRoundSequence != null && enemyRoundSequence.Length > 0;

        #endregion
    }
}
