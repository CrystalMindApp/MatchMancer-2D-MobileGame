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

        // State
        private int currentHp;
        private bool isCursed;
        private float curseMissChance;
        private int curseTurnsRemaining;

        #endregion

        #region Properties

        public CharacterData CharacterData => characterData;
        public EnemyCombatProfile CombatProfile => combatProfile;
        public int MaxHp => characterData != null ? characterData.MaxHP : 0;
        public int BaseSpeed => characterData != null ? characterData.BaseSpeed : 0;
        public int CurrentHp => Mathf.Max(0, currentHp);
        public int BaseAttackDamage => characterData != null ? characterData.BaseAttackDamage : 0;
        public float EnemySpecialDisruptChance => combatProfile != null ? combatProfile.EnemySpecialDisruptChance : 0f;
        public float EnemyApplyCurseChance => combatProfile != null ? combatProfile.EnemyApplyCurseChance : 0f;
        public float EnemyCritChance => combatProfile != null ? combatProfile.EnemyCritChance : 0f;
        public float EnemyCritMultiplier => combatProfile != null ? combatProfile.EnemyCritMultiplier : 1f;
        public float EnemySelfHealChance => combatProfile != null ? combatProfile.EnemySelfHealChance : 0f;
        public int EnemySelfHealAmount => combatProfile != null ? combatProfile.EnemySelfHealAmount : 0;
        public string EnemySkillAnnouncementText => combatProfile != null ? combatProfile.EnemySkillAnnouncementText : "Enemy Turn";
        public float EnemyActionDelay => combatProfile != null ? combatProfile.EnemyActionDelay : 0f;

        #endregion

        #region Public Methods

        public void InitializeRuntimeState()
        {
            currentHp = MaxHp;
            isCursed = false;
            curseMissChance = 0f;
            curseTurnsRemaining = 0;
        }

        public void TakeDamage(int amount)
        {
            currentHp = Mathf.Max(0, currentHp - Mathf.Max(0, amount));
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

        public void PlayGetHitVisual()
        {
            visualController?.PlayGetHit();
        }

        public void ShowIdleVisual()
        {
            visualController?.ShowIdle();
        }

        public void PlaySkillVisual()
        {
            visualController?.PlaySkill();
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
        }

        #endregion
    }
}
