namespace CrystalMind.MatchMancer
{
    public readonly struct CurseEffectContext
    {
        #region Properties

        public PlayerActor Player { get; }
        public EnemyActor Enemy { get; }

        #endregion

        #region Public Methods

        public CurseEffectContext(PlayerActor player, EnemyActor enemy)
        {
            Player = player;
            Enemy = enemy;
        }

        #endregion
    }
}
