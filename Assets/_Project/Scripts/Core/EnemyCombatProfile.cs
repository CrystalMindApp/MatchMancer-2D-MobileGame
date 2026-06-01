using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "EnemyCombatProfile", menuName = "MatchMancer/Enemy/Enemy Combat Profile")]
    public class EnemyCombatProfile : ScriptableObject
    {
        #region Variables

        private const int DefaultEnemySkillCooldownTurns = 2;

        [Header("Enemy Skill")]
        [SerializeField, Range(0f, 1f)] private float enemySpecialDisruptChance = 1f;
        [SerializeField] private string enemySkillAnnouncementText = "Enemy Counter";
        [SerializeField, TextArea] private string enemyDisruptDescription = "Disrupts special tiles and pressures your passive setup.";
        [SerializeField] private BoardEffectData disruptBoardEffect;
        [SerializeField, Min(1)] private int enemySkillCooldownTurns = DefaultEnemySkillCooldownTurns;

        [Header("Direct Attack Curse (Legacy)")]
        [Tooltip("Legacy direct attack curse chance. This is separate from Tile Curse and should not be used for tile-layer curse application.")]
        [SerializeField, Range(0f, 1f)] private float enemyApplyCurseChance;

        [Header("Tile Curse")]
        [Tooltip("Chance to apply a tile-layer curse near the end of this enemy's turn. This does not replace the legacy direct attack curse chance.")]
        [SerializeField, Range(0f, 1f)] private float tileCurseApplyChance;
        [Tooltip("Maximum number of currently uncursed board tiles to curse when Tile Curse Apply Chance succeeds.")]
        [SerializeField, Min(0)] private int tileCurseApplyCount;
        [Tooltip("CurseEffectData applied to selected board tiles. Leave empty to disable tile curse application.")]
        [SerializeField] private CurseEffectData tileCurseEffect;

        [Header("Combat")]
        [SerializeField, Range(0f, 1f)] private float enemyCritChance;
        [SerializeField, Min(1f)] private float enemyCritMultiplier = 1.5f;
        [SerializeField, Range(0f, 1f)] private float enemySelfHealChance;
        [SerializeField, Min(0)] private int enemySelfHealAmount;
        [SerializeField, Min(0f)] private float enemyActionDelay = 2f;

        #endregion

        #region Properties

        public float EnemySpecialDisruptChance => enemySpecialDisruptChance;
        public float EnemyApplyCurseChance => enemyApplyCurseChance;
        public float TileCurseApplyChance => tileCurseApplyChance;
        public int TileCurseApplyCount => Mathf.Max(0, tileCurseApplyCount);
        public CurseEffectData TileCurseEffect => tileCurseEffect;
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
