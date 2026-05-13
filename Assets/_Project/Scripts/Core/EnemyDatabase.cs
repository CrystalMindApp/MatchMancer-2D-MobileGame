using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "MatchMancer/Enemy/Enemy Database")]
    public class EnemyDatabase : ScriptableObject
    {
        #region Variables

        [Header("References")]
        [SerializeField] private EnemyDefinition[] enemies;

        // Cache

        // State

        #endregion

        #region Properties

        public int Count => enemies != null ? enemies.Length : 0;

        #endregion

        #region Public Methods

        public EnemyDefinition GetEnemyByIndex(int index)
        {
            if (enemies == null || enemies.Length == 0)
            {
                Debug.LogWarning("EnemyDatabase: No enemy definitions assigned.");
                return null;
            }

            if (index < 0 || index >= enemies.Length)
            {
                Debug.LogWarning($"EnemyDatabase: Invalid enemy index {index}. Valid range is 0-{enemies.Length - 1}.");
                return null;
            }

            EnemyDefinition enemy = enemies[index];

            if (enemy == null)
            {
                Debug.LogWarning($"EnemyDatabase: Enemy definition at index {index} is null.");
            }
            else if (!enemy.IsValid)
            {
                Debug.LogWarning($"EnemyDatabase: Enemy definition at index {index} is missing CharacterData or EnemyCombatProfile.");
            }

            return enemy;
        }

        public EnemyDefinition GetRandomEnemy()
        {
            if (enemies == null || enemies.Length == 0)
            {
                Debug.LogWarning("EnemyDatabase: Cannot select random enemy because no enemy definitions are assigned.");
                return null;
            }

            return GetEnemyByIndex(Random.Range(0, enemies.Length));
        }

        public int GetEnemyCount()
        {
            return Count;
        }

        #endregion
    }
}
