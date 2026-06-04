using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "CurseEffectData", menuName = "MatchMancer/Combat/Curse Effect Data")]
    public class CurseEffectData : ScriptableObject
    {
        #region Variables

        [Header("Identity")]
        [SerializeField] private string curseName = "Curse";
        [SerializeField] private CurseType curseType = CurseType.Poison;

        [Header("Shared Settings")]
        [Tooltip("Duration for this curse effect. Poison uses player turns. Blind uses attack attempts.")]
        [SerializeField, Min(1)] private int durationTurns = 2;

        [Header("Poison Settings")]
        [Tooltip("Used by Poison. Damage intended to be dealt to the player each poison tick in a future phase.")]
        [SerializeField, Min(0)] private int poisonDamagePerTurn = 3;

        [Header("Blind Settings")]
        [Tooltip("Used by Blind. Hit chance penalty in 0..1 form, for example 0.25 means -25% hit chance in a future phase.")]
        [SerializeField, Range(0f, 1f)] private float blindHitChancePenalty = 0.25f;

        #endregion

        #region Properties

        public string CurseName => string.IsNullOrWhiteSpace(curseName) ? name : curseName;
        public CurseType CurseType => curseType;
        public int DurationTurns => Mathf.Max(1, durationTurns);
        public int PoisonDamagePerTurn => Mathf.Max(0, poisonDamagePerTurn);
        public float BlindHitChancePenalty => Mathf.Clamp01(blindHitChancePenalty);

        #endregion

        #region Public Methods

        public void Apply(CurseEffectContext context)
        {
            switch (curseType)
            {
                case CurseType.Poison:
                    ApplyPoison(context);
                    return;

                case CurseType.Blind:
                    ApplyBlind(context);
                    return;
            }
        }

        #endregion

        #region Private Methods

        private void ApplyPoison(CurseEffectContext context)
        {
            if (context.Player == null)
            {
                Debug.LogWarning($"CurseEffectData '{CurseName}' could not apply Poison because PlayerActor is missing.");
                return;
            }

            context.Player.ApplyPoison(PoisonDamagePerTurn, DurationTurns);
        }

        private void ApplyBlind(CurseEffectContext context)
        {
            if (context.Player == null)
            {
                Debug.LogWarning($"CurseEffectData '{CurseName}' could not apply Blind because PlayerActor is missing.");
                return;
            }

            context.Player.ApplyBlind(BlindHitChancePenalty, DurationTurns);
        }

        #endregion
    }
}
