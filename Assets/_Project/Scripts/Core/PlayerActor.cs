using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class PlayerActor : MonoBehaviour
    {
        #region Variables

        [Header("References")]
        [SerializeField] private CharacterData characterData;
        [SerializeField] private PlayerCombatProfile combatProfile;
        [SerializeField] private ActorSpriteSwapController visualController;
        [SerializeField] private ActorTurnScaleHighlighter turnScaleHighlighter;
        [SerializeField] private ActorCombatMotionController combatMotionController;
        [SerializeField] private Transform damagePopupAnchor;

        // State
        private int currentHp;
        private bool isCursed;
        private float curseMissChance;
        private int curseTurnsRemaining;
        private int currentSkillGauge;
        private int purplePassiveStack;
        private int currentTurnSpeedBonus;
        private bool passiveReady;
        private bool passiveTriggeredThisTurn;

        #endregion

        #region Properties

        public CharacterData CharacterData => characterData;
        public PlayerCombatProfile CombatProfile => combatProfile;
        public ActiveSkillData ActiveSkill => combatProfile != null ? combatProfile.ActiveSkill : null;
        public PassiveSkillData PassiveSkill => combatProfile != null ? combatProfile.PassiveSkill : null;
        public int MaxHp => characterData != null ? characterData.MaxHP : 0;
        public int MaxHP => MaxHp;
        public int BaseSpeed => characterData != null ? characterData.BaseSpeed : 0;
        public int CurrentTurnSpeed => BaseSpeed + currentTurnSpeedBonus;
        public int CurrentHp => Mathf.Max(0, currentHp);
        public int CurrentHP => CurrentHp;
        public float AttackMultiplier => characterData != null ? characterData.AttackMultiplier : 1f;
        public int BaseDamagePerTile => combatProfile != null ? combatProfile.BaseDamagePerTile : 0;
        public float RedCritChancePerTile => combatProfile != null ? combatProfile.RedCritChancePerTile : 0f;
        public float RedCritDamageMultiplier => combatProfile != null ? combatProfile.RedCritDamageMultiplier : 1f;
        public int GreenHealPerTile => combatProfile != null ? combatProfile.GreenHealPerTile : 0;
        public int SpeedGainPerYellowTile => combatProfile != null ? combatProfile.SpeedGainPerYellowTile : 0;
        public int CurrentSkillGauge => currentSkillGauge;
        public int MaxSkillGauge => combatProfile != null ? combatProfile.MaxSkillGauge : 0;
        public int BaseGaugeGain => combatProfile != null ? combatProfile.BaseGaugeGain : 0;
        public int PurplePassiveStack => purplePassiveStack;
        public int CurrentTurnSpeedBonus => currentTurnSpeedBonus;
        public bool PassiveReady => passiveReady;
        public bool PassiveTriggeredThisTurn => passiveTriggeredThisTurn;
        public Transform DamagePopupAnchor => damagePopupAnchor != null ? damagePopupAnchor : transform;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            if (turnScaleHighlighter == null)
            {
                turnScaleHighlighter = GetComponentInChildren<ActorTurnScaleHighlighter>();
            }

            if (combatMotionController == null)
            {
                combatMotionController = GetComponentInChildren<ActorCombatMotionController>();
            }
        }

        #endregion

        #region Public Methods

        public void InitializeRuntimeState()
        {
            currentHp = MaxHp;
            isCursed = false;
            curseMissChance = 0f;
            curseTurnsRemaining = 0;
            currentSkillGauge = 0;
            purplePassiveStack = 0;
            currentTurnSpeedBonus = 0;
            passiveReady = false;
            passiveTriggeredThisTurn = false;
        }

        public void TakeDamage(int amount)
        {
            currentHp = Mathf.Max(0, currentHp - Mathf.Max(0, amount));
        }

        public void Heal(int amount)
        {
            currentHp = Mathf.Min(MaxHp, currentHp + Mathf.Max(0, amount));
        }

        public void AddSkillGauge(int amount)
        {
            currentSkillGauge = Mathf.Min(MaxSkillGauge, currentSkillGauge + Mathf.Max(0, amount));
        }

        public void SpendSkillGauge(ActiveSkillData activeSkill)
        {
            currentSkillGauge = Mathf.Max(0, currentSkillGauge - GetSkillGaugeCost(activeSkill));
        }

        public bool HasEnoughGauge(ActiveSkillData activeSkill)
        {
            if (activeSkill == null)
            {
                return false;
            }

            return currentSkillGauge >= GetRequiredSkillGauge(activeSkill);
        }

        public int GetRequiredSkillGauge(ActiveSkillData activeSkill)
        {
            if (activeSkill == null || activeSkill.RequiresFullGauge)
            {
                return MaxSkillGauge;
            }

            return GetSkillGaugeCost(activeSkill);
        }

        public void BeginPlayerAction()
        {
            passiveTriggeredThisTurn = false;
        }

        public void AddPassiveCharge(int amount)
        {
            PassiveSkillData passiveSkill = PassiveSkill;

            if (passiveSkill == null || passiveReady || passiveTriggeredThisTurn)
            {
                return;
            }

            purplePassiveStack += Mathf.Max(0, amount);

            if (purplePassiveStack >= passiveSkill.StackThreshold)
            {
                passiveReady = true;
            }
        }

        public bool CanExecutePassive(PassiveSkillTiming timing)
        {
            PassiveSkillData passiveSkill = PassiveSkill;
            return passiveSkill != null &&
                passiveReady &&
                !passiveTriggeredThisTurn &&
                passiveSkill.Timing == timing;
        }

        public void MarkPassiveExecuted()
        {
            PassiveSkillData passiveSkill = PassiveSkill;

            if (passiveSkill == null)
            {
                return;
            }

            if (passiveSkill.ResetStackOnTrigger)
            {
                purplePassiveStack = 0;
            }
            else
            {
                purplePassiveStack = Mathf.Max(0, purplePassiveStack - passiveSkill.StackThreshold);
            }

            passiveReady = false;
            passiveTriggeredThisTurn = true;
        }

        public void AddTurnSpeedBonus(int amount)
        {
            currentTurnSpeedBonus += Mathf.Max(0, amount);
        }

        public void ResetTurnSpeedBonus()
        {
            currentTurnSpeedBonus = 0;
        }

        public void ApplyCurse(float missChance, int duration)
        {
            isCursed = true;
            curseMissChance = Mathf.Clamp01(missChance);
            curseTurnsRemaining = Mathf.Max(1, duration);
        }

        public bool IsAttackMissed()
        {
            return isCursed && Random.value < curseMissChance;
        }

        public void TickCurseDuration()
        {
            if (!isCursed)
            {
                return;
            }

            curseTurnsRemaining--;

            if (curseTurnsRemaining > 0)
            {
                return;
            }

            isCursed = false;
            curseMissChance = 0f;
            curseTurnsRemaining = 0;
        }

        public void PlayAttackVisual()
        {
            visualController?.PlayAttack();
        }

        public void PlayAttackMotion(Vector3 targetWorldPosition)
        {
            combatMotionController?.PlayAttackMotion(targetWorldPosition);
        }

        public void PlayGetHitVisual()
        {
            visualController?.PlayGetHit();
        }

        public void PlayDeadVisual()
        {
            visualController?.PlayDead();
        }

        public void PlayHitMotion(Vector3 sourceWorldPosition)
        {
            combatMotionController?.PlayHitMotion(sourceWorldPosition);
        }

        public void ShowIdleVisual()
        {
            visualController?.ShowIdle();
        }

        public void PlaySkillVisual()
        {
            visualController?.PlaySkill();
        }

        public void PlayBattleStartVisual()
        {
            visualController?.PlayBattleStart();
        }

        public void PlayRunVisual()
        {
            visualController?.PlayRun();
        }

        public void HideVisual()
        {
            visualController?.Hide();
        }

        public void ShowVisual()
        {
            visualController?.Show();
        }

        public void ResetVisual()
        {
            visualController?.ResetToIdle();
            turnScaleHighlighter?.ResetHighlight();
            combatMotionController?.ResetMotion();
        }

        public void HighlightTurnVisual()
        {
            turnScaleHighlighter?.Highlight();
        }

        public void ClearTurnHighlightVisual()
        {
            turnScaleHighlighter?.ClearHighlight();
        }

        #endregion

        #region Private Methods

        private int GetSkillGaugeCost(ActiveSkillData activeSkill)
        {
            return activeSkill.RequiresFullGauge
                ? MaxSkillGauge
                : Mathf.Max(0, activeSkill.GaugeCost);
        }

        #endregion
    }
}
