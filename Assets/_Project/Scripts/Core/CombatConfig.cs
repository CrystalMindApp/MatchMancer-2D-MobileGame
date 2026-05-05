using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "MatchMancer/Combat Config")]
    public class CombatConfig : ScriptableObject
    {
        #region Variables

        [SerializeField, Min(1)] private int playerMaxHp = 100;
        [SerializeField, Min(1)] private int enemyMaxHp = 100;
        [SerializeField, Min(0)] private int enemyAttackDamage = 10;
        [SerializeField, Min(0)] private int baseDamagePerTile = 2;

        #endregion

        #region Properties

        public int PlayerMaxHp => playerMaxHp;
        public int EnemyMaxHp => enemyMaxHp;
        public int EnemyAttackDamage => enemyAttackDamage;
        public int BaseDamagePerTile => baseDamagePerTile;

        #endregion
    }
}
