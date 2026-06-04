using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "SpecialTileDestroyEffectSet", menuName = "MatchMancer/Board/Special Tile Destroy Effect Set")]
    public class SpecialTileDestroyEffectSet : ScriptableObject
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private SpecialTileType specialTileType = SpecialTileType.Bomb;
        [SerializeField] private GameObject destroyEffectPrefab;

        #endregion

        #region Properties

        public SpecialTileType SpecialTileType => specialTileType;
        public GameObject DestroyEffectPrefab => destroyEffectPrefab;
        public bool IsValid => specialTileType != SpecialTileType.None && destroyEffectPrefab != null;

        #endregion
    }
}
