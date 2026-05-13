using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "MatchMancer/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private string displayName;

        [Header("References")]
        [SerializeField] private CharacterData characterData;
        [SerializeField] private EnemyCombatProfile combatProfile;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite attackSprite;
        [SerializeField] private Sprite skillSprite;
        [SerializeField] private Sprite getHitSprite;

        // Cache

        // State

        #endregion

        #region Properties

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    return displayName;
                }

                if (characterData != null)
                {
                    return characterData.CharacterName;
                }

                return name;
            }
        }

        public CharacterData CharacterData => characterData;
        public EnemyCombatProfile CombatProfile => combatProfile;
        public Sprite IdleSprite => idleSprite;
        public Sprite AttackSprite => attackSprite;
        public Sprite SkillSprite => skillSprite;
        public Sprite GetHitSprite => getHitSprite;
        public bool IsValid => characterData != null && combatProfile != null;
        public bool HasVisualSprites => idleSprite != null || attackSprite != null || skillSprite != null || getHitSprite != null;

        #endregion
    }
}
