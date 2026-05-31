using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "ClearColumnBoardEffect", menuName = "MatchMancer/Board Effects/Clear Column")]
    public class ClearColumnBoardEffectData : BoardEffectData
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
                ? BoardEffectTargetingUtility.GetColumnTiles(context.Board, context.SourceCol)
                : new List<Tile>();
        }

        #endregion
    }
}
