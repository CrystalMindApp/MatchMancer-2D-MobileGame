using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class EnemyActor : MonoBehaviour
    {
        #region Variables

        [Header("References")]
        [SerializeField] private CharacterData characterData;
        [SerializeField] private EnemyCombatProfile combatProfile;
        [SerializeField] private ActorSpriteSwapController visualController;
        [SerializeField] private ActorTurnScaleHighlighter turnScaleHighlighter;
        [SerializeField] private ActorCombatMotionController combatMotionController;
        [SerializeField] private Transform damagePopupAnchor;
        [SerializeField] private Transform bloodHitAnchor;

        // State
        private int currentHp;
        private bool isCursed;
        private float curseMissChance;
        private int curseTurnsRemaining;
        private EnemyState currentState = EnemyState.Normal;
        private bool hasEnteredRage;
        private bool rageTransitionPending;

        #endregion

        #region Properties

        public CharacterData CharacterData => characterData;
        public EnemyCombatProfile CombatProfile => combatProfile;
        public int MaxHp => characterData != null ? characterData.MaxHP : 0;
        public int BaseSpeed => characterData != null ? characterData.BaseSpeed : 0;
        public int CurrentHp => Mathf.Max(0, currentHp);
        public int BaseAttackDamage => characterData != null ? characterData.BaseAttackDamage : 0;
        public int CurrentAttackDamage => IsRaging && combatProfile != null
            ? Mathf.Max(0, Mathf.RoundToInt(BaseAttackDamage * combatProfile.RageAttackDamageMultiplier))
            : BaseAttackDamage;
        public int CurrentAttackDamageAddition => Mathf.Max(0, CurrentAttackDamage - BaseAttackDamage);
        public float EnemySpecialDisruptChance => combatProfile != null ? combatProfile.EnemySpecialDisruptChance : 0f;
        public float EnemyApplyCurseChance => combatProfile != null ? combatProfile.EnemyApplyCurseChance : 0f;
        public float TileCurseApplyChance => combatProfile != null
            ? Mathf.Clamp01(combatProfile.TileCurseApplyChance + (IsRaging ? combatProfile.RageTileCurseApplyChanceBonus : 0f))
            : 0f;
        public int TileCurseApplyCount => combatProfile != null
            ? Mathf.Max(0, combatProfile.TileCurseApplyCount + (IsRaging ? combatProfile.RageTileCurseApplyCountBonus : 0))
            : 0;
        public CurseEffectData TileCurseEffect => combatProfile != null ? combatProfile.TileCurseEffect : null;
        public float EnemyCritChance => combatProfile != null ? combatProfile.EnemyCritChance : 0f;
        public float EnemyCritMultiplier => combatProfile != null ? combatProfile.EnemyCritMultiplier : 1f;
        public float EnemySelfHealChance => combatProfile != null ? combatProfile.EnemySelfHealChance : 0f;
        public int EnemySelfHealAmount => combatProfile != null ? combatProfile.EnemySelfHealAmount : 0;
        public string EnemySkillAnnouncementText => combatProfile != null ? combatProfile.EnemySkillAnnouncementText : "Enemy Turn";
        public string EnemyDisruptDescription => combatProfile != null ? combatProfile.EnemyDisruptDescription : string.Empty;
        public int EnemySkillCooldownTurns => combatProfile != null ? combatProfile.EnemySkillCooldownTurns : 0;
        public float EnemyActionDelay => combatProfile != null ? combatProfile.EnemyActionDelay : 0f;
        public EnemyState CurrentState => currentState;
        public bool IsRaging => currentState == EnemyState.Rage;
        public bool HasEnteredRage => hasEnteredRage;
        public bool IsRageTransitionPending => rageTransitionPending;
        public string RageAnnouncementText => combatProfile != null ? combatProfile.RageAnnouncementText : "RAGE";
        public Transform DamagePopupAnchor => damagePopupAnchor != null ? damagePopupAnchor : transform;
        public Transform BloodHitAnchor => bloodHitAnchor != null ? bloodHitAnchor : DamagePopupAnchor;

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
            ResetRageState();
        }

        public void SetCombatProfile(EnemyCombatProfile profile)
        {
            if (profile == null)
            {
                Debug.LogWarning("EnemyActor: Ignored null combat profile assignment.");
                return;
            }

            combatProfile = profile;
        }

        public bool ApplyEnemyDefinition(EnemyDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogWarning("EnemyActor: Cannot apply a null EnemyDefinition.");
                return false;
            }

            if (!definition.IsValid)
            {
                Debug.LogWarning($"EnemyActor: EnemyDefinition '{definition.name}' is missing CharacterData or EnemyCombatProfile.");
                return false;
            }

            characterData = definition.CharacterData;
            combatProfile = definition.CombatProfile;
            InitializeRuntimeState();
            ApplyDefinitionVisuals(definition);
            ResetVisual();
            return true;
        }

        public void TakeDamage(int amount)
        {
            int damageAmount = Mathf.Max(0, amount);
            currentHp = Mathf.Max(0, currentHp - damageAmount);

            if (damageAmount > 0)
            {
                EvaluateRageState();
            }
        }

        public void Heal(int amount)
        {
            currentHp = Mathf.Min(MaxHp, currentHp + Mathf.Max(0, amount));
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

        public void PlayRageThreatPresentation()
        {
            PlayBattleStartVisual();
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

        public bool ConsumeRageTransitionPending()
        {
            if (!rageTransitionPending || hasEnteredRage || currentHp <= 0)
            {
                return false;
            }

            rageTransitionPending = false;
            return true;
        }

        public bool EnterRageMode()
        {
            if (combatProfile == null || !combatProfile.EnableRageMode || hasEnteredRage || currentHp <= 0)
            {
                rageTransitionPending = false;
                return false;
            }

            currentState = EnemyState.Rage;
            hasEnteredRage = true;
            rageTransitionPending = false;
            return true;
        }

        #endregion

        #region Private Methods

        private void ResetRageState()
        {
            currentState = EnemyState.Normal;
            hasEnteredRage = false;
            rageTransitionPending = false;
        }

        private void EvaluateRageState()
        {
            if (combatProfile == null || !combatProfile.EnableRageMode || hasEnteredRage || rageTransitionPending || currentHp <= 0 || MaxHp <= 0)
            {
                return;
            }

            float hpRatio = Mathf.Clamp01((float)currentHp / MaxHp);

            if (hpRatio > combatProfile.RageHpThreshold01)
            {
                return;
            }

            rageTransitionPending = true;
        }

        private void ApplyDefinitionVisuals(EnemyDefinition definition)
        {
            if (definition == null || !definition.HasVisualSetup)
            {
                return;
            }

            if (visualController == null)
            {
                Debug.LogWarning($"EnemyActor: Missing ActorSpriteSwapController. Enemy visual setup was not applied for {definition.DisplayName}.");
                return;
            }

            if (definition.DefaultIdleSprite == null)
            {
                Debug.LogWarning($"EnemyActor: EnemyDefinition '{definition.DisplayName}' has visual setup but no default idle sprite.");
            }

            if (definition.AnimatorController == null)
            {
                Debug.LogWarning($"EnemyActor: EnemyDefinition '{definition.DisplayName}' has visual setup but no RuntimeAnimatorController.");
            }

            visualController.ApplyVisualSetup(definition.DefaultIdleSprite, definition.AnimatorController);

            Debug.Log($"Enemy visual setup applied: {definition.DisplayName}");
        }

        #endregion
    }
}
