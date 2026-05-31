using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public abstract class BoardEffectData : ScriptableObject
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private string effectDisplayName;

        #endregion

        #region Properties

        public string EffectDisplayName => string.IsNullOrWhiteSpace(effectDisplayName) ? name : effectDisplayName;

        #endregion

        #region Public Methods

        public abstract IEnumerator Execute(BoardEffectContext context, Action<BoardEffectResult> onComplete);

        public virtual List<Tile> GetTargets(BoardEffectContext context)
        {
            return new List<Tile>();
        }

        public virtual bool TryExecuteImmediate(BoardEffectContext context, out BoardEffectResult result)
        {
            result = BoardEffectResult.Empty();
            return false;
        }

        #endregion

        #region Protected Methods

        protected bool HasValidBoard(BoardEffectContext context)
        {
            return context.Board != null;
        }

        protected void Complete(Action<BoardEffectResult> onComplete, BoardEffectResult result)
        {
            onComplete?.Invoke(result);
        }

        #endregion
    }
}
