using System;
using System.Collections;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "RemoveRandomSpecialTileBoardEffect", menuName = "MatchMancer/Board Effects/Remove Random Special Tile")]
    public class RemoveRandomSpecialTileBoardEffectData : BoardEffectData
    {
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

            bool removed = context.Board.TryRemoveRandomSpecialTileInternal();
            result = BoardEffectResult.Affected(removed ? 1 : 0);
            return removed;
        }

        #endregion
    }
}
