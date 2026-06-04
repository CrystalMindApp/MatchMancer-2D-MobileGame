namespace CrystalMind.MatchMancer
{
    public readonly struct AttackResult
    {
        #region Properties

        public int Damage { get; }
        public bool IsHit { get; }
        public bool IsCritical { get; }
        public bool IsMiss => !IsHit;

        #endregion

        #region Public Methods

        public AttackResult(int damage, bool isHit, bool isCritical)
        {
            Damage = damage;
            IsHit = isHit;
            IsCritical = isCritical;
        }

        public static AttackResult Miss()
        {
            return new AttackResult(0, false, false);
        }

        public static AttackResult Hit(int damage, bool isCritical)
        {
            return new AttackResult(damage, true, isCritical);
        }

        #endregion
    }
}
