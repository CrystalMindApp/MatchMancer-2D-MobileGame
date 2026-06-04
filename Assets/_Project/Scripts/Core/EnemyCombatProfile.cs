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

        [Header("Rage Mode")]
        [Tooltip("Enables enemy-only Rage Mode. Rage is permanent once triggered and is not tied to StageDefinition yet.")]
        [SerializeField] private bool enableRageMode;
        [Tooltip("Enemy enters Rage when Current HP / Max HP is at or below this value.")]
        [SerializeField, Range(0f, 1f)] private float rageHpThreshold01 = 0.5f;
        [Tooltip("When enabled, Rage applies Rage Attack Damage Multiplier to enemy attack damage.")]
        [SerializeField] private bool rageModifyAttackDamage = true;
        [Tooltip("Multiplier applied to enemy attack damage while raging.")]
        [SerializeField, Min(0f)] private float rageAttackDamageMultiplier = 1f;
        [Tooltip("When enabled, Rage applies Rage Tile Curse Apply Chance Bonus to resolved tile curse chance.")]
        [SerializeField] private bool rageModifyTileCurseApplyChance;
        [Tooltip("Additive modifier applied to tile curse chance while raging. Final chance is clamped by EnemyActor.")]
        [SerializeField] private float rageTileCurseApplyChanceBonus;
        [Tooltip("When enabled, Rage applies Rage Tile Curse Apply Count Bonus to resolved tile curse count.")]
        [SerializeField] private bool rageModifyTileCurseApplyCount;
        [Tooltip("Additive modifier applied to tile curse count while raging. Final count is clamped by EnemyActor.")]
        [SerializeField] private int rageTileCurseApplyCountBonus;
        [Tooltip("Short debug/HUD-ready announcement text for Rage entry.")]
        [SerializeField] private string rageAnnouncementText = "RAGE";

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
        public bool EnableRageMode => enableRageMode;
        public float RageHpThreshold01 => Mathf.Clamp01(rageHpThreshold01);
        public bool RageModifyAttackDamage => rageModifyAttackDamage;
        public float RageAttackDamageMultiplier => Mathf.Max(0f, rageAttackDamageMultiplier);
        public bool RageModifyTileCurseApplyChance => rageModifyTileCurseApplyChance;
        public float RageTileCurseApplyChanceBonus => rageTileCurseApplyChanceBonus;
        public bool RageModifyTileCurseApplyCount => rageModifyTileCurseApplyCount;
        public int RageTileCurseApplyCountBonus => rageTileCurseApplyCountBonus;
        public string RageAnnouncementText => string.IsNullOrWhiteSpace(rageAnnouncementText) ? "RAGE" : rageAnnouncementText;
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
