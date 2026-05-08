using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public enum PassiveSkillTiming
    {
        ImmediateBoardEffect,
        BeforeAttack,
        AfterAttack
    }

    public enum PassiveSkillEffectType
    {
        CreateRandomBomb
    }

    [CreateAssetMenu(fileName = "PassiveSkillData", menuName = "MatchMancer/Passive Skill Data")]
    public class PassiveSkillData : ScriptableObject
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private string passiveName = "Purple Bomb";
        [SerializeField, TextArea] private string passiveDescription = "Charge with Purple tiles to create one Bomb.";
        [SerializeField] private PassiveSkillTiming timing = PassiveSkillTiming.ImmediateBoardEffect;
        [SerializeField] private PassiveSkillEffectType effectType = PassiveSkillEffectType.CreateRandomBomb;
        [SerializeField, Min(1)] private int stackThreshold = 3;
        [SerializeField] private bool resetStackOnTrigger = true;

        #endregion

        #region Properties

        public string PassiveName => string.IsNullOrWhiteSpace(passiveName) ? name : passiveName;
        public string PassiveDescription => passiveDescription;
        public PassiveSkillTiming Timing => timing;
        public PassiveSkillEffectType EffectType => effectType;
        public int StackThreshold => stackThreshold;
        public bool ResetStackOnTrigger => resetStackOnTrigger;

        #endregion
    }
}
