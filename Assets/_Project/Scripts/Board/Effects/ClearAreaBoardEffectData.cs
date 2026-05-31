using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "ClearAreaBoardEffect", menuName = "MatchMancer/Board Effects/Clear Area")]
    public class ClearAreaBoardEffectData : BoardEffectData
    {
        #region Variables

        [Header("Settings")]
        [SerializeField, Min(0)] private int radius = 1;

        #endregion

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
                ? BoardEffectTargetingUtility.GetAreaTiles(context.Board, context.SourceRow, context.SourceCol, radius)
                : new List<Tile>();
        }

        #endregion
    }
}
