using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "ClearRowBoardEffect", menuName = "MatchMancer/Board Effects/Clear Row")]
    public class ClearRowBoardEffectData : BoardEffectData
    {
        #region Public Methods

        public override IEnumerator Execute(BoardEffectContext context, Action<BoardEffectResult> onComplete)
        {
            if (!HasValidBoard(context))
            {
                Complete(onComplete, BoardEffectResult.Empty());
                yield break;
            }

            List<Tile> targets = GetTargets(context);
            yield return context.Board.ClearTilesForBoardEffectRoutine(targets, context, onComplete);
        }

        public override List<Tile> GetTargets(BoardEffectContext context)
        {
            return HasValidBoard(context)
                ? BoardEffectTargetingUtility.GetRowTiles(context.Board, context.SourceRow)
                : new List<Tile>();
        }

        #endregion
    }
}
