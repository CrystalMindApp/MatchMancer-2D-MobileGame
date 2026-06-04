using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "MatchMancer/Enemy/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private string displayName;
        [SerializeField] private EnemyArchetype archetype = EnemyArchetype.Standard;
        [SerializeField, TextArea] private string description;
        [SerializeField, TextArea] private string traitDescription;

        [Header("References")]
        [SerializeField] private CharacterData characterData;
        [SerializeField] private EnemyCombatProfile combatProfile;
        [SerializeField] private Sprite portrait;
        [SerializeField] private Sprite traitPreviewIcon;
        [SerializeField] private Sprite defaultIdleSprite;
        [SerializeField] private RuntimeAnimatorController animatorController;

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
        public EnemyArchetype Archetype => archetype;
        public string Description => string.IsNullOrWhiteSpace(description) ? string.Empty : description;
        public string TraitDescription => string.IsNullOrWhiteSpace(traitDescription) ? string.Empty : traitDescription;
        public Sprite Portrait => portrait;
        public Sprite TraitPreviewIcon => traitPreviewIcon;
        public Sprite DefaultIdleSprite => defaultIdleSprite;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public bool IsValid => characterData != null && combatProfile != null;
        public bool HasVisualSetup => defaultIdleSprite != null || animatorController != null;

        #endregion
    }
}
