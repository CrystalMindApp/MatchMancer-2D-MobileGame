using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "EnemyCombatProfile", menuName = "MatchMancer/Enemy/Enemy Combat Profile")]
    public class EnemyCombatProfile : ScriptableObject
    {
        #region Variables

        private const int DefaultEnemySkillCooldownTurns = 2;

        [SerializeField, Range(0f, 1f)] private float enemySpecialDisruptChance = 1f;
        [SerializeField, Range(0f, 1f)] private float enemyApplyCurseChance;
        [SerializeField, Range(0f, 1f)] private float enemyCritChance;
        [SerializeField, Min(1f)] private float enemyCritMultiplier = 1.5f;
        [SerializeField, Range(0f, 1f)] private float enemySelfHealChance;
        [SerializeField, Min(0)] private int enemySelfHealAmount;
        [SerializeField] private string enemySkillAnnouncementText = "Enemy Counter";
        [SerializeField, TextArea] private string enemyDisruptDescription = "Disrupts special tiles and pressures your passive setup.";
        [SerializeField] private BoardEffectData disruptBoardEffect;
        [SerializeField, Min(1)] private int enemySkillCooldownTurns = DefaultEnemySkillCooldownTurns;
        [SerializeField, Min(0f)] private float enemyActionDelay = 2f;

        #endregion

        #region Properties

        public float EnemySpecialDisruptChance => enemySpecialDisruptChance;
        public float EnemyApplyCurseChance => enemyApplyCurseChance;
        public float EnemyCritChance => enemyCritChance;
        public float EnemyCritMultiplier => enemyCritMultiplier;
        public float EnemySelfHealChance => enemySelfHealChance;
        public int EnemySelfHealAmount => enemySelfHealAmount;
        public string EnemySkillAnnouncementText => string.IsNullOrWhiteSpace(enemySkillAnnouncementText) ? name : enemySkillAnnouncementText;
        public string EnemyDisruptDescription => string.IsNullOrWhiteSpace(enemyDisruptDescription)
            ? "Disrupts special tiles and pressures your passive setup."
            : enemyDisruptDescription;
        public BoardEffectData DisruptBoardEffect => disruptBoardEffect;
        public int EnemySkillCooldownTurns => enemySkillCooldownTurns > 0 ? enemySkillCooldownTurns : DefaultEnemySkillCooldownTurns;
        public float EnemyActionDelay => enemyActionDelay;

        #endregion
    }
}
