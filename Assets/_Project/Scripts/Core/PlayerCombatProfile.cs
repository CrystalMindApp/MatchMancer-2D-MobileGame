using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "PlayerCombatProfile", menuName = "MatchMancer/Player/Player Combat Profile")]
    public class PlayerCombatProfile : ScriptableObject
    {
        #region Variables

        [Header("Tile Color Effects")]
        [SerializeField, Min(0)] private int baseDamagePerTile = 2;
        [SerializeField, Range(0f, 1f)] private float redCritChancePerTile = 0.03f;
        [SerializeField, Min(1f)] private float redCritDamageMultiplier = 1.5f;
        [SerializeField, Min(0)] private int greenHealPerTile = 2;
        [SerializeField, Min(0)] private int speedGainPerYellowTile = 3;

        [Header("Active Skill")]
        [SerializeField, Min(1)] private int maxSkillGauge = 100;
        [SerializeField, Min(0)] private int baseGaugeGain = 10;
        [SerializeField] private ActiveSkillData activeSkill;

        [Header("Passive Skill")]
        [SerializeField] private PassiveSkillData passiveSkill;

        #endregion

        #region Properties

        public int BaseDamagePerTile => baseDamagePerTile;
        public float RedCritChancePerTile => redCritChancePerTile;
        public float RedCritDamageMultiplier => redCritDamageMultiplier;
        public int GreenHealPerTile => greenHealPerTile;
        public int SpeedGainPerYellowTile => speedGainPerYellowTile;
        public int MaxSkillGauge => maxSkillGauge;
        public int BaseGaugeGain => baseGaugeGain;
        public ActiveSkillData ActiveSkill => activeSkill;
        public PassiveSkillData PassiveSkill => passiveSkill;

        #endregion
    }
}
