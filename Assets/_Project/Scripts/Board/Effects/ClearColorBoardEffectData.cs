using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "ClearColorBoardEffect", menuName = "MatchMancer/Board Effects/Clear Color")]
    public class ClearColorBoardEffectData : BoardEffectData
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private bool useContextTargetColor = true;
        [SerializeField] private TileType fallbackTileType = TileType.Red;

        #endregion

        #region Public Methods

        public override IEnumerator Execute(BoardEffectContext context, Action<BoardEffectResult> onComplete)
        {
            if (!HasValidBoard(context))
            {
                Complete(onComplete, BoardEffectResult.Empty());
                yield break;
            }

            TileType targetType = ResolveTargetType(context);
            List<Tile> targets = GetTargets(context);
            yield return context.Board.ClearTilesForBoardEffectRoutine(targets, context.WithTargetTileType(targetType), onComplete);
        }

        public override List<Tile> GetTargets(BoardEffectContext context)
        {
            if (!HasValidBoard(context))
            {
                return new List<Tile>();
            }

            TileType targetType = ResolveTargetType(context);
            if (context.HasSecondaryTargetTileType)
            {
                return BoardEffectTargetingUtility.GetTilesOfTypes(context.Board, targetType, context.SecondaryTargetTileType);
            }

            return BoardEffectTargetingUtility.GetTilesOfType(context.Board, targetType);
        }

        #endregion

        #region Private Methods

        private TileType ResolveTargetType(BoardEffectContext context)
        {
            return useContextTargetColor ? context.TargetTileType : fallbackTileType;
        }

        #endregion
    }
}
