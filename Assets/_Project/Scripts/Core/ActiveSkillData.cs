using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public enum ActiveSkillEffectType
    {
        ClearTileColor
    }

    [CreateAssetMenu(fileName = "ActiveSkillData", menuName = "MatchMancer/Player/Active Skill Data")]
    public class ActiveSkillData : ScriptableObject
    {
        #region Variables

        [SerializeField] private string skillName = "Clear Tiles";
        [SerializeField, TextArea] private string skillDescription = "Clear all tiles of the selected color.";
        [SerializeField] private ActiveSkillEffectType skillEffectType = ActiveSkillEffectType.ClearTileColor;
        [SerializeField] private TileType targetTileColor = TileType.Red;
        [SerializeField, Min(0f)] private float delayBeforeApply = 2f;
        [SerializeField] private bool consumesTurn = true;
        [SerializeField, Min(0)] private int gaugeCost = 100;
        [SerializeField] private bool requiresFullGauge = true;

        #endregion

        #region Properties

        public string SkillName => string.IsNullOrWhiteSpace(skillName) ? name : skillName;
        public string SkillDescription => skillDescription;
        public ActiveSkillEffectType SkillEffectType => skillEffectType;
        public TileType TargetTileColor => targetTileColor;
        public float DelayBeforeApply => delayBeforeApply;
        public bool ConsumesTurn => consumesTurn;
        public int GaugeCost => gaugeCost;
        public bool RequiresFullGauge => requiresFullGauge;

        #endregion
    }
}
