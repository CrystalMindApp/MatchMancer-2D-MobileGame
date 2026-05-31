using System;
using System.Collections;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "CreateRandomSpecialTileBoardEffect", menuName = "MatchMancer/Board Effects/Create Random Special Tile")]
    public class CreateRandomSpecialTileBoardEffectData : BoardEffectData
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private SpecialTileType specialTileType = SpecialTileType.Bomb;
        [SerializeField] private bool playSpawnSfx = true;

        #endregion

        #region Public Methods

        public override IEnumerator Execute(BoardEffectContext context, Action<BoardEffectResult> onComplete)
        {
            TryExecuteImmediate(context, out BoardEffectResult result);
            Complete(onComplete, result);
            yield break;
        }

        public override bool TryExecuteImmediate(BoardEffectContext context, out BoardEffectResult result)
        {
            if (!HasValidBoard(context))
            {
                result = BoardEffectResult.Empty();
                return false;
            }

            bool created = context.Board.TryCreateRandomSpecialTile(specialTileType, playSpawnSfx);
            result = BoardEffectResult.Affected(created ? 1 : 0);
            return created;
        }

        #endregion
    }
}
