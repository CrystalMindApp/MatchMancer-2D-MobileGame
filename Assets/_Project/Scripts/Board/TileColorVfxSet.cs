using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "TileColorVfxSet", menuName = "MatchMancer/Board/Tile Color VFX Set")]
    public class TileColorVfxSet : ScriptableObject
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private TileType tileType = TileType.Red;
        [SerializeField] private GameObject effectPrefab;

        #endregion

        #region Properties

        public TileType TileType => tileType;
        public GameObject EffectPrefab => effectPrefab;
        public bool IsValid => effectPrefab != null;

        #endregion
    }
}
