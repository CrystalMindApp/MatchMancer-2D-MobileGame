using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public enum CharacterType
    {
        Actor,
        AI
    }

    [CreateAssetMenu(fileName = "CharacterData", menuName = "MatchMancer/Character Data")]
    public class CharacterData : ScriptableObject
    {
        #region Variables

        [SerializeField] private string characterName = "Actor";
        [SerializeField, Min(1)] private int maxHP = 100;
        [SerializeField, Min(0)] private int baseSpeed = 10;
        [SerializeField, Min(0f)] private float attackMultiplier = 1f;
        [SerializeField, Min(0)] private int baseAttackDamage = 10;
        [SerializeField] private CharacterType characterType = CharacterType.Actor;

        #endregion

        #region Properties

        public string CharacterName => string.IsNullOrWhiteSpace(characterName) ? name : characterName;
        public int MaxHP => maxHP;
        public int BaseSpeed => baseSpeed;
        public float AttackMultiplier => attackMultiplier;
        public int BaseAttackDamage => baseAttackDamage;
        public CharacterType CharacterType => characterType;

        #endregion
    }
}
