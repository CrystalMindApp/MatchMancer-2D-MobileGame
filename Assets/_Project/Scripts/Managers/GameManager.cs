using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public enum GameState
    {
        Start,
        Playing,
        Win,
        Lose
    }

    public class GameManager : MonoBehaviour
    {
        #region Variables

        [Header("Timing")]
        [SerializeField, Min(0f)] private float actorActionDelay = 1f;
        [SerializeField, Min(0f)] private float nextEnemyRoundDelay = 0.75f;
        [SerializeField, Min(0f)] private float enemyTurnStartDelay = 0.4f;
        [SerializeField, Min(0f)] private float enemyAbilityDelay = 0.5f;
        [SerializeField, Min(0f)] private float enemyTurnEndDelay = 0.4f;
        [SerializeField, Min(0f)] private float attackImpactDelay = 0.5f;
        [SerializeField, Min(0f)] private float postImpactHoldDelay = 0.5f;
        [SerializeField, Min(0f)] private float deathHoldDelay = 0.5f;
        [SerializeField, Min(0f)] private float enemyRageTransitionDuration = 0.75f;
        [SerializeField, Range(0.05f, 1f)] private float critSlowMotionScale = 0.35f;
        [SerializeField, Min(0f)] private float critSlowMotionDuration = 0.08f;

        [Header("Scene Settings")]
        [SerializeField] private string homeSceneName = "Home";

        [Header("Production References")]
        [SerializeField] private PlayerActor playerActor;
        [SerializeField] private EnemyActor enemyActor;
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private GameHUD gameHUD;

        [Header("Debug / Direct Play Fallback")]
        // Used only when MainGame is played directly without a selected StageDefinition.
        [SerializeField] private EnemyDefinition[] enemyRoundSequence;

        [Header("Curse Settings")]
        [SerializeField, Range(0f, 1f)] private float defaultCurseMissChance = 0.25f;
        [SerializeField, Min(1)] private int defaultCurseDuration = 2;

        [Header("Polish References")]
        [SerializeField] private DamagePopupController damagePopupController;
        [SerializeField] private CinemachineTinyImpulse tinyImpulse;
        [SerializeField] private GameplayIntroController gameplayIntroController;

        [Header("Blood Hit Effects")]
        [SerializeField] private GameObject bloodHitEffectPrefab;
        [SerializeField] private Transform bloodHitEffectParent;
        [SerializeField, Min(0)] private int bloodHitMinCount = 1;
        [SerializeField, Min(0)] private int bloodHitMaxCount = 3;
        [SerializeField, Min(0f)] private float bloodHitSpawnRadius = 0.25f;
        [SerializeField, Min(0f)] private float bloodHitLifetime = 1f;
        [SerializeField, Min(1f)] private float criticalBloodScaleMultiplier = 1.5f;
        [SerializeField] private Color bloodHitTint = new Color(1f, 0.05f, 0.02f, 1f);

        [Header("Debug")]
        [SerializeField] private bool enableCombatDebugLogs = true;

        // State
        private GameState currentState;
        private float pendingCritChance;
        private int currentHeroHealAddition;
        private bool isTurnResolving;
        private bool isActiveSkillResolving;
        private string turnStatusText = "Player Turn";
        private int currentEnemyRoundIndex;
        private int enemySkillTurnsRemaining;
        private bool enemyRoundSequenceActive;
        private bool isEnemyRoundTransitioning;
        private bool isEnemyActionResolving;
        private bool enemyAttackUsedSkillThisAction;
        private Coroutine critSlowMotionRoutine;
        private float critSlowMotionOriginalTimeScale = 1f;
        private float critSlowMotionOriginalFixedDeltaTime = 0.02f;
        private bool isCritSlowMotionActive;
        private EnemyDefinition[] activeEnemyRoundSequence;
        private string currentStageName = "Stage";

        private struct AttackDamageResult
        {
            public int Damage;
            public bool IsCritical;
        }

        #endregion

        #region Properties

        public int PlayerMaxHp => playerActor != null ? playerActor.MaxHp : 0;
        public int PlayerCurrentHp => playerActor != null ? playerActor.CurrentHp : 0;
        public int EnemyMaxHp => enemyActor != null ? enemyActor.MaxHp : 0;
        public int EnemyCurrentHp => enemyActor != null ? enemyActor.CurrentHp : 0;
        public int CurrentSkillGauge => playerActor != null ? playerActor.CurrentSkillGauge : 0;
        public int MaxSkillGauge => playerActor != null ? playerActor.MaxSkillGauge : 0;
        public int PurplePassiveStack => playerActor != null ? playerActor.PurplePassiveStack : 0;
        public bool PlayerHasPoison => playerActor != null && playerActor.HasPoison;
        public int PlayerPoisonTurnsRemaining => playerActor != null ? playerActor.PoisonTurnsRemaining : 0;
        public int PlayerPoisonDamagePerTurn => playerActor != null ? playerActor.PoisonDamagePerTurn : 0;
        public bool EnemyHasTileCurseTrait => enemyActor != null &&
            enemyActor.TileCurseEffect != null &&
            enemyActor.TileCurseApplyChance > 0f &&
            enemyActor.TileCurseApplyCount > 0;
        public CurseEffectData EnemyTileCurseTraitEffect => EnemyHasTileCurseTrait ? enemyActor.TileCurseEffect : null;
        public GameState CurrentState => currentState;
        public bool IsPlaying => currentState == GameState.Playing;
        public bool IsActiveSkillReady => playerActor != null && playerActor.HasEnoughGauge(playerActor.ActiveSkill);
        public bool CanUseActiveSkillNow => IsPlaying &&
            IsActiveSkillReady &&
            !isTurnResolving &&
            !isActiveSkillResolving &&
            !isEnemyRoundTransitioning &&
            !isEnemyActionResolving &&
            boardManager != null &&
            !boardManager.IsResolving;
        public string TurnStatusText => turnStatusText;
        public string SpeedInfoText => $"P: {(playerActor != null ? playerActor.CurrentTurnSpeed : 0)}  E: {(enemyActor != null ? enemyActor.BaseSpeed : 0)}";
        public string HeroSpeedText => (playerActor != null ? playerActor.CurrentTurnSpeed : 0).ToString();
        public string EnemySpeedText => (enemyActor != null ? enemyActor.BaseSpeed : 0).ToString();
        public string HeroAttackStatText => $"{(playerActor != null ? playerActor.BaseDamagePerTile : 0)}+0";
        public string HeroHealStatText => $"{(playerActor != null ? playerActor.GreenHealPerTile : 0)}+{currentHeroHealAddition}";
        public string HeroCritChanceText => FormatPercent(pendingCritChance);
        public string HeroCritMultiplierText => $"{(playerActor != null ? playerActor.RedCritDamageMultiplier : 1f):0.##}x";
        public string HeroActiveSkillText => playerActor != null && playerActor.ActiveSkill != null ? playerActor.ActiveSkill.SkillDescription : string.Empty;
        public string HeroPassiveSkillText => playerActor != null && playerActor.PassiveSkill != null ? playerActor.PassiveSkill.PassiveDescription : string.Empty;
        public string EnemyAttackStatText => $"{(enemyActor != null ? enemyActor.BaseAttackDamage : 0)}+{(enemyActor != null ? enemyActor.CurrentAttackDamageAddition : 0)}";
        public string EnemyHealStatText => $"{(enemyActor != null ? enemyActor.EnemySelfHealAmount : 0)}+0";
        public string EnemyCritChanceText => FormatPercent(enemyActor != null ? enemyActor.EnemyCritChance : 0f);
        public string EnemyCritMultiplierText => $"{(enemyActor != null ? enemyActor.EnemyCritMultiplier : 1f):0.##}x";
        public string EnemyDisruptChanceText => FormatPercent(enemyActor != null ? enemyActor.EnemySpecialDisruptChance : 0f);
        public string EnemyDisruptDescriptionText => enemyActor != null ? enemyActor.EnemyDisruptDescription : string.Empty;
        public string EnemyPassiveSkillText => string.Empty;
        public bool IsMatchingPhase => IsPlaying && boardManager != null && boardManager.CanReceiveInput;
        public int PassiveStackThreshold => playerActor != null && playerActor.PassiveSkill != null ? playerActor.PassiveSkill.StackThreshold : 0;
        public int EnemySkillTurnsRemaining => Mathf.Max(0, enemySkillTurnsRemaining);
        public int EnemySkillCooldownTurns => enemyActor != null ? enemyActor.EnemySkillCooldownTurns : 0;
        public bool IsEnemySkillReady => EnemySkillTurnsRemaining <= 0;
        public float EnemyIntentFill01 => EnemySkillCooldownTurns > 0
            ? 1f - Mathf.Clamp01((float)EnemySkillTurnsRemaining / EnemySkillCooldownTurns)
            : 1f;
        public string EnemySkillCooldownText => enemySkillTurnsRemaining <= 0 ? "Enemy Skill: Ready" : $"Enemy Skill: {enemySkillTurnsRemaining}";
        public string CurrentRoundText => GetRoundDisplayText();
        public string CurrentStageText => currentStageName;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            SetState(GameState.Start);
        }

        private void Start()
        {
            StartCoroutine(StartGameRoutine());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        private void OnDisable()
        {
            RestoreCriticalSlowMotion();
        }

        #endregion

        #region Public Methods

        public void StartGame()
        {
            ApplyStartingEnemy();

            if (!HasRequiredActorReferences())
            {
                SetState(GameState.Start);
                return;
            }

            ResetGameData();
            ResetActorVisuals();
            gameHUD?.HideResult();
            SetState(GameState.Playing);
        }

        public void RestartGame()
        {
            StopAllCoroutines();
            RestoreCriticalSlowMotion();
            ApplyStartingEnemy();

            if (!HasRequiredActorReferences())
            {
                SetState(GameState.Start);
                return;
            }

            ResetGameData();
            ResetActorVisuals();
            SetBoardInputBlocked(false);
            gameHUD?.ClearPlayerSkillText();
            gameHUD?.ClearEnemySkillText();
            gameHUD?.HideResult();

            if (boardManager != null)
            {
                boardManager.RestartBoard();
            }
            else
            {
                LogSystem("GameManager: BoardManager reference is missing.");
            }

            SetState(GameState.Playing);
        }

        public void OnValidMoveUsed()
        {
            if (!IsPlaying)
            {
                return;
            }

            playerActor?.BeginPlayerAction();
            SetTurnStatus("Player Turn");
            LogSystem("Player action started.");
        }

        public void OnTilesCleared(int amount)
        {
            if (!IsPlaying || amount <= 0)
            {
                return;
            }

            LogSystem($"Tiles cleared: {amount}");
        }

        public void OnTileColorsCleared(IReadOnlyDictionary<TileType, int> colorCounts, int comboCount)
        {
            if (!IsPlaying || colorCounts == null || playerActor == null)
            {
                return;
            }

            int safeComboCount = Mathf.Max(1, comboCount);

            foreach (KeyValuePair<TileType, int> colorCount in colorCounts)
            {
                ApplyTileColorEffect(colorCount.Key, colorCount.Value, safeComboCount);
            }
        }

        public void OnTileCursesTriggered(IReadOnlyList<CurseEffectData> triggeredCurses)
        {
            if (!IsPlaying || triggeredCurses == null || playerActor == null)
            {
                return;
            }

            foreach (CurseEffectData curseEffect in triggeredCurses)
            {
                ApplyTriggeredTileCurse(curseEffect);
            }
        }

        public void OnPlayerMoveResolved(int clearedTileCount)
        {
            if (!IsPlaying || !HasRequiredActorReferences())
            {
                return;
            }

            if (isTurnResolving)
            {
                LogSystem("Turn resolve ignored because another turn is already resolving.");
                return;
            }

            StartCoroutine(PlayerTurnResolvedRoutine(clearedTileCount));
        }

        public void TryUseActiveSkill()
        {
            ActiveSkillData activeSkill = playerActor != null ? playerActor.ActiveSkill : null;
            TileType targetType = activeSkill != null ? activeSkill.TargetTileColor : TileType.Red;

            if (!CanUseActiveSkill(activeSkill, targetType))
            {
                return;
            }

            StartCoroutine(ActiveSkillRoutine(activeSkill));
        }

        public bool TryActivateActiveSkill(TileType targetType)
        {
            ActiveSkillData activeSkill = playerActor != null ? playerActor.ActiveSkill : null;

            if (!CanUseActiveSkill(activeSkill, targetType))
            {
                return false;
            }

            StartCoroutine(ActiveSkillRoutine(activeSkill, targetType));
            return true;
        }

        public bool TryActivateRedSkill()
        {
            return TryActivateActiveSkill(TileType.Red);
        }

        public bool TryActivateGreenSkill()
        {
            return TryActivateActiveSkill(TileType.Green);
        }

        public bool TryActivateBlueSkill()
        {
            return TryActivateActiveSkill(TileType.Blue);
        }

        public bool TryActivateYellowSkill()
        {
            return TryActivateActiveSkill(TileType.Yellow);
        }

        public bool TryActivatePurpleSkill()
        {
            return TryActivateActiveSkill(TileType.Purple);
        }

        public void ApplyCurseToPlayer()
        {
            ApplyCurseToActor(true, defaultCurseMissChance, defaultCurseDuration);
        }

        public void ApplyCurseToEnemy()
        {
            ApplyCurseToActor(false, defaultCurseMissChance, defaultCurseDuration);
        }

        public void ApplyCurseToActor(bool affectsPlayer, float missChance, int duration)
        {
            float safeMissChance = Mathf.Clamp01(missChance);
            int safeDuration = Mathf.Max(1, duration);

            if (affectsPlayer)
            {
                playerActor?.ApplyCurse(safeMissChance, safeDuration);
                LogCurse($"Player is cursed for {safeDuration} turn(s).");
                return;
            }

            enemyActor?.ApplyCurse(safeMissChance, safeDuration);
            LogCurse($"Enemy is cursed for {safeDuration} turn(s).");
        }

        public void EvaluateGameResult()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (enemyActor != null && enemyActor.CurrentHp <= 0)
            {
                StartCoroutine(HandleEnemyDefeatedRoutine());
                return;
            }

            if (playerActor != null && playerActor.CurrentHp <= 0)
            {
                HandlePlayerDefeated();
                return;
            }

        }

        public void RetryCurrentStage()
        {
            LogSystem("Retry current stage.");
            StageSession.ClearPendingStageClearVisual();
            LoadSceneWithTransition(SceneManager.GetActiveScene().name);
        }

        public void BackToHome()
        {
            if (string.IsNullOrWhiteSpace(homeSceneName))
            {
                LogSystem("Back to home failed: home scene name is missing.");
                return;
            }

            LogSystem("Back to home/menu.");
            StageSession.ClearSelectedStage();
            LoadSceneWithTransition(homeSceneName);
        }

        #endregion

        #region Private Methods

        private IEnumerator StartGameRoutine()
        {
            SetBoardInputBlocked(true);
            StartGame();
            SetBoardInputBlocked(true);
            gameplayIntroController?.PrepareIntro();

            if (TransitionOverlayController.Instance != null)
            {
                TransitionOverlayController.Instance.ReleaseSceneReady();
                yield return TransitionOverlayController.Instance.WaitForTransitionComplete();
            }

            if (gameplayIntroController != null)
            {
                yield return StartCoroutine(gameplayIntroController.PlayIntroRoutine());
            }

            if (IsPlaying)
            {
                SetBoardInputBlocked(false);
            }
        }

        private IEnumerator ActiveSkillRoutine(ActiveSkillData activeSkill, TileType? overrideTargetType = null)
        {
            isActiveSkillResolving = true;
            SetBoardInputBlocked(true);

            TileType targetType = overrideTargetType ?? activeSkill.TargetTileColor;
            string skillName = activeSkill.SkillName;

            LogSkill($"Active Skill: {skillName} ({targetType})");
            gameHUD?.ShowPlayerSkillText(skillName);
            yield return new WaitForSeconds(activeSkill.DelayBeforeApply);
            gameHUD?.ClearPlayerSkillText();

            playerActor.SpendSkillGauge(activeSkill);

            if (activeSkill.ConsumesTurn)
            {
                OnValidMoveUsed();
            }

            playerActor.PlaySkillVisual();
            HighlightPlayerTurn();

            bool activated = boardManager != null &&
                (activeSkill.BoardEffect != null
                    ? boardManager.TryActivateBoardEffectSkill(activeSkill.BoardEffect, targetType)
                    : activeSkill.SkillEffectType == ActiveSkillEffectType.ClearTileColor &&
                        boardManager.TryActivateColorClearSkill(targetType));

            if (!activated)
            {
                LogSkillWarning($"Active Skill failed: no valid {targetType} tiles to clear.");
                isActiveSkillResolving = false;
                ClearTurnHighlight();
                SetBoardInputBlocked(false);
            }
        }

        private IEnumerator PlayerTurnResolvedRoutine(int clearedTileCount)
        {
            isTurnResolving = true;
            SetBoardInputBlocked(true);

            LogSystem($"Cleared tile count this turn: {clearedTileCount}");
            ResolvePlayerPassive(PassiveSkillTiming.ImmediateBoardEffect);

            bool playerActsFirst = ShouldPlayerActFirst();
            LogTurnPriority(playerActsFirst);

            if (playerActsFirst)
            {
                SetTurnStatus("Player Acts First");
                yield return StartCoroutine(ExecutePlayerAttackRoutine(clearedTileCount));

                if (enemyActor != null && enemyActor.CurrentHp <= 0)
                {
                    yield return StartCoroutine(HandleEnemyDefeatedRoutine());
                    if (TickPlayerPoisonAtTurnStart())
                    {
                        FinishTurnResolve();
                        yield break;
                    }

                    FinishTurnResolve();
                    yield break;
                }

                yield return StartCoroutine(EnemyActionRoutine());
                yield return new WaitForSeconds(actorActionDelay);
            }
            else
            {
                SetTurnStatus("Enemy Acts First");
                yield return StartCoroutine(EnemyActionRoutine());
                yield return new WaitForSeconds(actorActionDelay);

                if (playerActor != null && playerActor.CurrentHp <= 0)
                {
                    HandlePlayerDefeated();
                    FinishTurnResolve();
                    yield break;
                }

                yield return StartCoroutine(ExecutePlayerAttackRoutine(clearedTileCount));
            }

            if (enemyActor != null && enemyActor.CurrentHp <= 0)
            {
                yield return StartCoroutine(HandleEnemyDefeatedRoutine());
                if (TickPlayerPoisonAtTurnStart())
                {
                    FinishTurnResolve();
                    yield break;
                }

                FinishTurnResolve();
                yield break;
            }

            if (playerActor != null && playerActor.CurrentHp <= 0)
            {
                HandlePlayerDefeated();
                FinishTurnResolve();
                yield break;
            }

            yield return StartCoroutine(TryProcessEnemyRageTransitionRoutine());

            if (enemyActor != null && enemyActor.CurrentHp <= 0)
            {
                yield return StartCoroutine(HandleEnemyDefeatedRoutine());
                FinishTurnResolve();
                yield break;
            }

            TickCurseDurations();

            if (TickPlayerPoisonAtTurnStart())
            {
                FinishTurnResolve();
                yield break;
            }

            FinishTurnResolve();
        }

        private IEnumerator EnemyActionRoutine()
        {
            if (isEnemyActionResolving)
            {
                yield break;
            }

            isEnemyActionResolving = true;
            string actionName = enemyActor != null ? enemyActor.EnemySkillAnnouncementText : "Enemy Turn";
            SetTurnStatus("Enemy Turn");
            HighlightEnemyTurn();
            gameHUD?.ShowEnemySkillText(actionName);
            LogEnemy($"Enemy Action: {actionName}");
            yield return new WaitForSeconds(enemyTurnStartDelay);
            gameHUD?.ClearEnemySkillText();

            bool enemyWasReadyAtTurnStart = IsEnemySkillReady;
            yield return StartCoroutine(EnemyAttackRoutine(enemyWasReadyAtTurnStart));
            bool enemyUsedSkill = enemyAttackUsedSkillThisAction;

            if (playerActor != null && playerActor.CurrentHp <= 0)
            {
                if (enemyWasReadyAtTurnStart)
                {
                    ResetEnemySkillCounter();
                }

                ClearTurnHighlight();
                isEnemyActionResolving = false;
                yield break;
            }

            if (enemyUsedSkill)
            {
                yield return new WaitForSeconds(enemyAbilityDelay);
            }

            if (enemyWasReadyAtTurnStart && TryEnemySelfHeal())
            {
                yield return new WaitForSeconds(enemyAbilityDelay);
            }

            if (enemyWasReadyAtTurnStart && TryEnemyDisruption())
            {
                yield return new WaitForSeconds(enemyAbilityDelay);
            }

            if (TryEnemyTileCurse())
            {
                yield return new WaitForSeconds(enemyAbilityDelay);
            }

            if (enemyWasReadyAtTurnStart)
            {
                ResetEnemySkillCounter();
            }
            else
            {
                AdvanceEnemySkillCounter();
            }

            yield return new WaitForSeconds(enemyTurnEndDelay);
            ClearTurnHighlight();
            isEnemyActionResolving = false;
        }

        private void FinishTurnResolve()
        {
            isTurnResolving = false;
            isActiveSkillResolving = false;
            pendingCritChance = 0f;
            currentHeroHealAddition = 0;
            playerActor?.ResetTurnSpeedBonus();
            playerActor?.ResetPassiveTurnState();
            gameHUD?.ClearPlayerSkillText();
            gameHUD?.ClearEnemySkillText();
            ClearTurnHighlight();

            if (IsPlaying)
            {
                SetBoardInputBlocked(false);
                turnStatusText = "Player Turn";
            }
        }

        private bool CanUseActiveSkill(ActiveSkillData activeSkill, TileType targetType)
        {
            if (!IsPlaying)
            {
                LogSkillWarning("Active Skill unavailable: game is not playing.");
                return false;
            }

            if (activeSkill == null)
            {
                LogSkillWarning("Active Skill unavailable: no ActiveSkillData assigned.");
                return false;
            }

            if (isTurnResolving || isActiveSkillResolving || isEnemyRoundTransitioning || isEnemyActionResolving || boardManager == null || boardManager.IsResolving)
            {
                LogSkillWarning("Active Skill unavailable: board or turn is resolving.");
                return false;
            }

            if (playerActor == null || !playerActor.HasEnoughGauge(activeSkill))
            {
                int requiredGauge = playerActor != null ? playerActor.GetRequiredSkillGauge(activeSkill) : 0;
                LogSkillWarning($"Active Skill unavailable: gauge {CurrentSkillGauge}/{requiredGauge}.");
                return false;
            }

            if (activeSkill.SkillEffectType == ActiveSkillEffectType.ClearTileColor &&
                boardManager != null &&
                !boardManager.HasTileOfType(targetType))
            {
                LogSkillWarning($"Active Skill unavailable: no {targetType} tiles on board.");
                return false;
            }

            return true;
        }

        private void ResetGameData()
        {
            playerActor?.InitializeRuntimeState();
            enemyActor?.InitializeRuntimeState();
            pendingCritChance = 0f;
            isTurnResolving = false;
            isActiveSkillResolving = false;
            turnStatusText = "Player Turn";
            ResetEnemySkillCounter();
            isEnemyRoundTransitioning = false;
            isEnemyActionResolving = false;
        }

        private void ApplyStartingEnemy()
        {
            enemyRoundSequenceActive = false;
            currentEnemyRoundIndex = 0;
            activeEnemyRoundSequence = ResolveActiveEnemyRoundSequence();
            enemyRoundSequenceActive = activeEnemyRoundSequence != null && activeEnemyRoundSequence.Length > 0;

            if (TryApplyEnemyRound(currentEnemyRoundIndex))
            {
                return;
            }

            enemyRoundSequenceActive = false;
            ApplyFallbackEnemy();
        }

        private void ApplyTileColorEffect(TileType tileType, int tileCount, int comboCount)
        {
            if (tileCount <= 0)
            {
                return;
            }

            switch (tileType)
            {
                case TileType.Red:
                    AddCritChance(tileCount, comboCount);
                    break;

                case TileType.Green:
                    HealPlayer(tileCount, comboCount);
                    break;

                case TileType.Blue:
                    AddSkillGauge(tileCount, comboCount);
                    break;

                case TileType.Yellow:
                    AddYellowSpeedBonus(tileCount, comboCount);
                    break;

                case TileType.Purple:
                    AddPurplePassiveStack(tileCount, comboCount);
                    break;
            }
        }

        private void AddCritChance(int tileCount, int comboCount)
        {
            float addedChance = tileCount * playerActor.RedCritChancePerTile * comboCount;
            pendingCritChance = Mathf.Clamp01(pendingCritChance + addedChance);
            LogPlayer($"Red effect: crit chance this turn is {pendingCritChance:P0}.");
        }

        private void HealPlayer(int tileCount, int comboCount)
        {
            int healAmount = tileCount * playerActor.GreenHealPerTile * comboCount;
            currentHeroHealAddition = Mathf.Max(0, healAmount - playerActor.GreenHealPerTile);
            playerActor.Heal(healAmount);
            ShowHealPopup(healAmount, playerActor.DamagePopupAnchor);
            LogPlayer($"Green effect: healed Player for {healAmount}. Player HP: {playerActor.CurrentHp}");
        }

        private void AddSkillGauge(int tileCount, int comboCount)
        {
            int gaugeGain = tileCount * playerActor.BaseGaugeGain * comboCount;
            playerActor.AddSkillGauge(gaugeGain);
            LogSkill($"Blue effect: gained {gaugeGain} skill gauge. Gauge: {playerActor.CurrentSkillGauge}/{playerActor.MaxSkillGauge}");
        }

        private void AddYellowSpeedBonus(int tileCount, int comboCount)
        {
            int speedGain = tileCount * playerActor.SpeedGainPerYellowTile * comboCount;
            playerActor.AddTurnSpeedBonus(speedGain);
            LogSpeed($"Yellow effect: gained {speedGain} turn speed. Player speed: {playerActor.CurrentTurnSpeed}");
        }

        private void AddPurplePassiveStack(int tileCount, int comboCount)
        {
            PassiveSkillData passiveSkill = playerActor.PassiveSkill;

            if (passiveSkill == null)
            {
                return;
            }

            int chargeAmount = tileCount * comboCount;
            playerActor.AddPassiveCharge(chargeAmount);
            LogPassive($"Purple effect: passive stack {playerActor.PurplePassiveStack}/{passiveSkill.StackThreshold}. Ready: {playerActor.PassiveReady}");
        }

        private void ApplyTriggeredTileCurse(CurseEffectData curseEffect)
        {
            if (curseEffect == null || playerActor == null)
            {
                return;
            }

            switch (curseEffect.CurseType)
            {
                case CurseType.Poison:
                    playerActor.ApplyPoison(curseEffect.PoisonDamagePerTurn, curseEffect.DurationTurns);
                    LogCurse($"Tile curse triggered: {curseEffect.CurseName} applied Poison for {curseEffect.DurationTurns} turn(s).");
                    break;

                case CurseType.Blind:
                    LogCurse($"Tile curse triggered: {curseEffect.CurseName} is Blind, but Blind is not implemented in D2.");
                    break;
            }
        }

        private IEnumerator TryProcessEnemyRageTransitionRoutine()
        {
            if (enemyActor == null || !enemyActor.ConsumeRageTransitionPending())
            {
                yield break;
            }

            if (!IsPlaying || enemyActor.CurrentHp <= 0)
            {
                yield break;
            }

            string announcementText = enemyActor.RageAnnouncementText;
            SetTurnStatus(announcementText);
            HighlightEnemyTurn();
            gameHUD?.ShowEnemySkillText(announcementText);
            playerActor?.PlayBattleStartVisual();
            enemyActor.PlayRageThreatPresentation();
            tinyImpulse?.Shake();
            LogEnemy($"Enemy Rage transition started: {announcementText}");

            if (enemyRageTransitionDuration > 0f)
            {
                yield return new WaitForSeconds(enemyRageTransitionDuration);
            }

            bool enteredRage = enemyActor.EnterRageMode();
            gameHUD?.ClearEnemySkillText();
            playerActor?.ShowIdleVisual();
            enemyActor.ShowIdleVisual();
            ClearTurnHighlight();

            if (enteredRage)
            {
                LogEnemy($"Enemy entered Rage Mode: {announcementText}");
            }
        }

        private bool TickPlayerPoisonAtTurnStart()
        {
            if (!IsPlaying || playerActor == null || !playerActor.HasPoison)
            {
                return false;
            }

            int poisonDamage = playerActor.TickPoisonAtPlayerTurnStart();

            if (poisonDamage <= 0)
            {
                return false;
            }

            PlayPlayerDamageFeedback(poisonDamage, false, false, Vector3.zero, true, true);
            LogCurse($"Poison deals {poisonDamage} damage. Player HP: {playerActor.CurrentHp}");

            if (playerActor.CurrentHp > 0)
            {
                return false;
            }

            HandlePlayerDefeated();
            return true;
        }

        private void ResolvePlayerPassive(PassiveSkillTiming timing)
        {
            if (playerActor == null || !playerActor.CanExecutePassive(timing))
            {
                return;
            }

            PassiveSkillData passiveSkill = playerActor.PassiveSkill;
            bool executed = ExecutePlayerPassiveEffect(passiveSkill);
            playerActor.ConsumePassiveTriggerAttempt();

            if (executed)
            {
                playerActor.PlaySkillVisual();
            }

            LogPassive(executed
                ? $"Passive Skill: {passiveSkill.PassiveName} executed at {passiveSkill.Timing}."
                : $"Passive Skill: {passiveSkill.PassiveName} attempted at {passiveSkill.Timing} but found no valid target.");
        }

        private bool ExecutePlayerPassiveEffect(PassiveSkillData passiveSkill)
        {
            if (passiveSkill == null)
            {
                return false;
            }

            switch (passiveSkill.EffectType)
            {
                case PassiveSkillEffectType.CreateRandomBomb:
                    if (boardManager == null)
                    {
                        return false;
                    }

                    bool createdBomb = passiveSkill.BoardEffect != null
                        ? boardManager.TryExecuteImmediateBoardEffect(passiveSkill.BoardEffect)
                        : boardManager.TryCreateRandomBomb();
                    LogPassive(createdBomb
                        ? "Purple passive: created one Bomb special tile."
                        : "Purple passive: no valid normal tile found for Bomb creation.");
                    return createdBomb;
            }

            return false;
        }

        private IEnumerator ApplyPlayerDamageRoutine(int clearedTileCount)
        {
            HighlightPlayerTurn();
            playerActor.PlayAttackVisual();
            playerActor.PlayAttackMotion(enemyActor.transform.position);

            yield return new WaitForSeconds(attackImpactDelay);

            AttackDamageResult damageResult = CalculatePlayerDamage(clearedTileCount);
            enemyActor.TakeDamage(damageResult.Damage);

            if (damageResult.Damage > 0)
            {
                enemyActor.PlayGetHitVisual();
                enemyActor.PlayHitMotion(playerActor.transform.position);
                ShowDamagePopup(damageResult.Damage, enemyActor.DamagePopupAnchor, damageResult.IsCritical);
                SpawnBloodHitEffects(enemyActor.BloodHitAnchor, damageResult.IsCritical);
                tinyImpulse?.Shake();
                TryPlayCriticalSlowMotion(damageResult.IsCritical);
            }

            pendingCritChance = 0f;

            LogEnemy($"Player deals {damageResult.Damage} damage. Enemy HP: {enemyActor.CurrentHp}");

            yield return new WaitForSeconds(postImpactHoldDelay);

            if (enemyActor.CurrentHp <= 0)
            {
                enemyActor.PlayDeadVisual();
                yield return new WaitForSeconds(deathHoldDelay);
                yield break;
            }

            playerActor.ShowIdleVisual();
            enemyActor.ShowIdleVisual();
        }

        private IEnumerator ExecutePlayerAttackRoutine(int clearedTileCount)
        {
            ResolvePlayerPassive(PassiveSkillTiming.BeforeAttack);
            yield return StartCoroutine(ApplyPlayerDamageRoutine(clearedTileCount));
            ResolvePlayerPassive(PassiveSkillTiming.AfterAttack);
        }

        private AttackDamageResult CalculatePlayerDamage(int clearedTileCount)
        {
            float damage = clearedTileCount * playerActor.BaseDamagePerTile * playerActor.AttackMultiplier;
            bool isCritical = Random.value < pendingCritChance;

            if (playerActor.IsAttackMissed())
            {
                LogCurse("Player attack missed due to Curse.");
                return new AttackDamageResult();
            }

            if (isCritical)
            {
                damage *= playerActor.RedCritDamageMultiplier;
                LogPlayer("Red effect: critical hit!");
            }

            return new AttackDamageResult
            {
                Damage = Mathf.Max(0, Mathf.RoundToInt(damage)),
                IsCritical = isCritical
            };
        }

        private IEnumerator EnemyAttackRoutine(bool canAttemptSpecial)
        {
            enemyAttackUsedSkillThisAction = false;
            HighlightEnemyTurn();
            enemyActor.PlayAttackVisual();
            enemyActor.PlayAttackMotion(playerActor.transform.position);

            yield return new WaitForSeconds(attackImpactDelay);

            AttackDamageResult damageResult = CalculateEnemyDamage();

            if (damageResult.Damage <= 0)
            {
                LogCurse("Enemy attack missed due to Curse.");
            }

            playerActor.TakeDamage(damageResult.Damage);

            if (damageResult.Damage > 0)
            {
                PlayPlayerDamageFeedback(damageResult.Damage, damageResult.IsCritical, true, enemyActor.transform.position, true, true);
                TryPlayCriticalSlowMotion(damageResult.IsCritical);
            }

            LogEnemy($"Enemy attacks for {damageResult.Damage}. Player HP: {playerActor.CurrentHp}");

            if (canAttemptSpecial && damageResult.Damage > 0 && Random.value < enemyActor.EnemyApplyCurseChance)
            {
                enemyActor.PlaySkillVisual();
                ApplyCurseToPlayer();
                enemyAttackUsedSkillThisAction = true;
            }

            yield return new WaitForSeconds(postImpactHoldDelay);

            if (playerActor.CurrentHp <= 0)
            {
                playerActor.PlayDeadVisual();
                yield return new WaitForSeconds(deathHoldDelay);
                yield break;
            }

            enemyActor.ShowIdleVisual();
            playerActor.ShowIdleVisual();
        }

        private bool TryEnemySelfHeal()
        {
            if (Random.value > enemyActor.EnemySelfHealChance)
            {
                return false;
            }

            int healAmount = enemyActor.EnemySelfHealAmount;

            if (healAmount <= 0)
            {
                return false;
            }

            enemyActor.Heal(healAmount);
            enemyActor.PlaySkillVisual();
            ShowHealPopup(healAmount, enemyActor.DamagePopupAnchor);
            LogEnemy($"Enemy heals for {healAmount}. Enemy HP: {enemyActor.CurrentHp}");
            return true;
        }

        private bool TryEnemyDisruption()
        {
            if (boardManager == null || Random.value > enemyActor.EnemySpecialDisruptChance)
            {
                return false;
            }

            BoardEffectData disruptEffect = enemyActor != null && enemyActor.CombatProfile != null
                ? enemyActor.CombatProfile.DisruptBoardEffect
                : null;
            bool disrupted = disruptEffect != null
                ? boardManager.TryExecuteImmediateBoardEffect(disruptEffect)
                : boardManager.TryRemoveRandomSpecialTile();
            if (disrupted)
            {
                enemyActor.PlaySkillVisual();
            }

            LogEnemy(disrupted
                ? "Enemy disruption: removed one special tile."
                : "Enemy disruption: no special tile available.");
            return disrupted;
        }

        private bool TryEnemyTileCurse()
        {
            if (enemyActor == null || boardManager == null)
            {
                return false;
            }

            CurseEffectData curseEffect = enemyActor.TileCurseEffect;
            int curseCount = enemyActor.TileCurseApplyCount;

            if (curseEffect == null || curseCount <= 0 || Random.value > enemyActor.TileCurseApplyChance)
            {
                return false;
            }

            bool applied = boardManager.TryApplyRandomTileCurse(curseEffect, curseCount);

            if (applied)
            {
                enemyActor.PlaySkillVisual();
            }

            LogCurse(applied
                ? $"Enemy tile curse: applied {curseEffect.CurseName} to up to {curseCount} tile(s)."
                : "Enemy tile curse: no valid uncursed tile available.");
            return applied;
        }

        private void TickCurseDurations()
        {
            playerActor?.TickCurseDuration();
            enemyActor?.TickCurseDuration();
        }

        private void ResetActorVisuals()
        {
            playerActor?.ResetVisual();
            enemyActor?.ResetVisual();
            ClearTurnHighlight();
        }

        private void ResetEnemySkillCounter()
        {
            enemySkillTurnsRemaining = EnemySkillCooldownTurns;
        }

        private void AdvanceEnemySkillCounter()
        {
            enemySkillTurnsRemaining = Mathf.Max(0, enemySkillTurnsRemaining - 1);
        }

        private AttackDamageResult CalculateEnemyDamage()
        {
            if (enemyActor.IsAttackMissed())
            {
                return new AttackDamageResult();
            }

            int damage = enemyActor.CurrentAttackDamage;
            bool isCritical = Random.value < enemyActor.EnemyCritChance;

            if (isCritical)
            {
                damage = Mathf.RoundToInt(damage * enemyActor.EnemyCritMultiplier);
                LogEnemy("Enemy critical hit!");
            }

            return new AttackDamageResult
            {
                Damage = Mathf.Max(0, damage),
                IsCritical = isCritical
            };
        }

        private void ShowDamagePopup(int amount, Transform anchor, bool isCritical)
        {
            if (damagePopupController == null)
            {
                return;
            }

            if (isCritical)
            {
                damagePopupController.ShowCriticalDamage(amount, anchor);
                return;
            }

            damagePopupController.ShowDamage(amount, anchor);
        }

        private void PlayPlayerDamageFeedback(
            int amount,
            bool isCritical,
            bool playHitMotion,
            Vector3 hitSourceWorldPosition,
            bool playBlood,
            bool playShake)
        {
            if (playerActor == null || amount <= 0)
            {
                return;
            }

            playerActor.PlayGetHitVisual();

            if (playHitMotion)
            {
                playerActor.PlayHitMotion(hitSourceWorldPosition);
            }

            ShowDamagePopup(amount, playerActor.DamagePopupAnchor, isCritical);

            if (playBlood)
            {
                SpawnBloodHitEffects(playerActor.BloodHitAnchor, isCritical);
            }

            if (playShake)
            {
                tinyImpulse?.Shake();
            }
        }

        private void ShowHealPopup(int amount, Transform anchor)
        {
            damagePopupController?.ShowHeal(amount, anchor);
        }

        private void SpawnBloodHitEffects(Transform anchor, bool isCritical)
        {
            if (bloodHitEffectPrefab == null)
            {
                return;
            }

            Transform effectAnchor = anchor != null ? anchor : transform;
            int minCount = Mathf.Max(0, bloodHitMinCount);
            int maxCount = Mathf.Max(minCount, bloodHitMaxCount);
            int effectCount = UnityEngine.Random.Range(minCount, maxCount + 1);
            float scaleMultiplier = isCritical ? criticalBloodScaleMultiplier : 1f;

            for (int i = 0; i < effectCount; i++)
            {
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * bloodHitSpawnRadius;
                Vector3 spawnPosition = effectAnchor.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
                GameObject effectInstance = Instantiate(bloodHitEffectPrefab, spawnPosition, Quaternion.identity, bloodHitEffectParent);
                effectInstance.transform.localScale *= scaleMultiplier;
                ApplyBloodHitTint(effectInstance);

                if (bloodHitLifetime > 0f)
                {
                    Destroy(effectInstance, bloodHitLifetime);
                }
            }
        }

        private void ApplyBloodHitTint(GameObject effectInstance)
        {
            if (effectInstance == null)
            {
                return;
            }

            VfxMaterialTintApplier[] materialTintAppliers = effectInstance.GetComponentsInChildren<VfxMaterialTintApplier>(true);

            foreach (VfxMaterialTintApplier materialTintApplier in materialTintAppliers)
            {
                if (materialTintApplier != null)
                {
                    materialTintApplier.ApplyTint(bloodHitTint);
                }
            }
        }

        private void TryPlayCriticalSlowMotion(bool isCritical)
        {
            if (!isCritical || critSlowMotionDuration <= 0f || Time.timeScale <= 0f)
            {
                return;
            }

            if (critSlowMotionRoutine != null)
            {
                return;
            }

            critSlowMotionRoutine = StartCoroutine(CriticalSlowMotionRoutine());
        }

        private IEnumerator CriticalSlowMotionRoutine()
        {
            critSlowMotionOriginalTimeScale = Time.timeScale;
            critSlowMotionOriginalFixedDeltaTime = Time.fixedDeltaTime;
            isCritSlowMotionActive = true;

            float safeScale = Mathf.Clamp(critSlowMotionScale, 0.05f, 1f);

            Time.timeScale = Mathf.Min(critSlowMotionOriginalTimeScale, safeScale);
            Time.fixedDeltaTime = critSlowMotionOriginalFixedDeltaTime * (Time.timeScale / Mathf.Max(0.0001f, critSlowMotionOriginalTimeScale));

            yield return new WaitForSecondsRealtime(critSlowMotionDuration);

            RestoreCriticalSlowMotion();
            critSlowMotionRoutine = null;
        }

        private void RestoreCriticalSlowMotion()
        {
            if (!isCritSlowMotionActive)
            {
                critSlowMotionRoutine = null;
                return;
            }

            Time.timeScale = critSlowMotionOriginalTimeScale;
            Time.fixedDeltaTime = critSlowMotionOriginalFixedDeltaTime;
            isCritSlowMotionActive = false;
            critSlowMotionRoutine = null;
        }

        private bool ShouldPlayerActFirst()
        {
            if (isActiveSkillResolving)
            {
                return true;
            }

            return playerActor.CurrentTurnSpeed >= enemyActor.BaseSpeed;
        }

        private void LogTurnPriority(bool playerActsFirst)
        {
            string firstActor = playerActsFirst ? "Player" : "Enemy";

            if (isActiveSkillResolving)
            {
                LogSpeed($"Turn priority: Active Skill used. Player acts first. Player Speed: {playerActor.CurrentTurnSpeed}, Enemy Speed: {enemyActor.BaseSpeed}, Yellow Bonus: {playerActor.CurrentTurnSpeedBonus}");
                return;
            }

            LogSpeed($"Turn priority: {firstActor} acts first. Player Speed: {playerActor.CurrentTurnSpeed}, Enemy Speed: {enemyActor.BaseSpeed}, Yellow Bonus: {playerActor.CurrentTurnSpeedBonus}");
        }

        private void SetTurnStatus(string status)
        {
            turnStatusText = status;
            LogSystem(status);
        }

        private void HighlightPlayerTurn()
        {
            playerActor?.HighlightTurnVisual();
            enemyActor?.ClearTurnHighlightVisual();
        }

        private void HighlightEnemyTurn()
        {
            enemyActor?.HighlightTurnVisual();
            playerActor?.ClearTurnHighlightVisual();
        }

        private void ClearTurnHighlight()
        {
            playerActor?.ClearTurnHighlightVisual();
            enemyActor?.ClearTurnHighlightVisual();
        }

        private void SetBoardInputBlocked(bool blocked)
        {
            if (boardManager != null)
            {
                boardManager.SetInputBlocked(blocked);
            }
        }

        private void LoadSceneWithTransition(string sceneName)
        {
            RestoreCriticalSlowMotion();

            if (TransitionOverlayController.Instance != null)
            {
                TransitionOverlayController.Instance.TransitionToScene(sceneName);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private bool HasRequiredActorReferences()
        {
            bool hasReferences = playerActor != null &&
                enemyActor != null &&
                playerActor.CharacterData != null &&
                playerActor.CombatProfile != null &&
                enemyActor.CharacterData != null &&
                enemyActor.CombatProfile != null;

            if (!hasReferences)
            {
                LogSystem("GameManager requires PlayerActor and EnemyActor with assigned CharacterData and combat profiles.");
            }

            return hasReferences;
        }

        private IEnumerator HandleEnemyDefeatedRoutine()
        {
            if (isEnemyRoundTransitioning)
            {
                yield break;
            }

            isEnemyRoundTransitioning = true;
            SetBoardInputBlocked(true);
            LogEnemyDefeated();

            if (TryGetNextEnemyRoundIndex(out int nextRoundIndex))
            {
                enemyActor?.HideVisual();
                LogSystem($"Waiting before next enemy round: {nextEnemyRoundDelay:0.##} seconds.");
                yield return new WaitForSeconds(nextEnemyRoundDelay);

                if (TryApplyEnemyRound(nextRoundIndex))
                {
                    enemyActor?.ShowVisual();
                    isEnemyRoundTransitioning = false;

                    if (!isTurnResolving && IsPlaying)
                    {
                        SetBoardInputBlocked(false);
                    }

                    yield break;
                }
            }

            SetState(GameState.Win);
            enemyActor?.HideVisual();
            isEnemyRoundTransitioning = false;
            int stars = CalculateStageStars();
            ApplyStageWinProgression(stars);
            ShowResult(true, stars);
            LogSystem("Final enemy defeated, battle won.");
        }

        private void HandlePlayerDefeated()
        {
            SetState(GameState.Lose);
            SetBoardInputBlocked(true);
            playerActor?.HideVisual();
            ShowResult(false, 0);
            LogSystem("Game Result: LOSE - Player defeated.");
        }

        private bool TryGetNextEnemyRoundIndex(out int nextRoundIndex)
        {
            nextRoundIndex = -1;

            if (!enemyRoundSequenceActive || activeEnemyRoundSequence == null || activeEnemyRoundSequence.Length == 0)
            {
                return false;
            }

            for (int index = currentEnemyRoundIndex + 1; index < activeEnemyRoundSequence.Length; index++)
            {
                if (activeEnemyRoundSequence[index] == null || !activeEnemyRoundSequence[index].IsValid)
                {
                    LogSystem($"Enemy round {index + 1}/{activeEnemyRoundSequence.Length} is not valid and will be skipped.");
                    continue;
                }

                nextRoundIndex = index;
                LogSystem("Advancing to next enemy round.");
                return true;
            }

            return false;
        }

        private bool TryApplyEnemyRound(int roundIndex)
        {
            if (activeEnemyRoundSequence == null || activeEnemyRoundSequence.Length == 0)
            {
                return false;
            }

            if (roundIndex < 0 || roundIndex >= activeEnemyRoundSequence.Length)
            {
                LogSystem($"Enemy round index {roundIndex} is outside the configured sequence.");
                return false;
            }

            EnemyDefinition definition = activeEnemyRoundSequence[roundIndex];

            if (!TryApplyEnemyDefinition(definition, $"enemy round {roundIndex + 1}/{activeEnemyRoundSequence.Length}"))
            {
                return false;
            }

            currentEnemyRoundIndex = roundIndex;
            ResetEnemySkillCounter();
            LogSystem($"Battle enemy round started: {roundIndex + 1}/{activeEnemyRoundSequence.Length} - {definition.DisplayName}");
            LogSystem($"Enemy round started with visual: {definition.DisplayName}");
            LogSystem($"Round UI updated: {GetRoundDisplayText()}");
            return true;
        }

        private void ApplyFallbackEnemy()
        {
            if (enemyActor == null)
            {
                LogSystem("GameManager: EnemyActor reference is missing.");
                return;
            }

            if (enemyActor.CharacterData != null && enemyActor.CombatProfile != null)
            {
                ResetEnemySkillCounter();
                LogSystem($"Fallback enemy used: {GetCurrentEnemyDisplayName()}");
                LogSystem($"Round UI updated: {GetRoundDisplayText()}");
                return;
            }

            LogSystem("GameManager: No Enemy round sequence or fallback EnemyActor setup is assigned.");
        }

        private EnemyDefinition[] ResolveActiveEnemyRoundSequence()
        {
            StageDefinition selectedStage = StageSession.SelectedStage;

            if (selectedStage != null)
            {
                currentStageName = selectedStage.StageDisplayName;
                LogSystem($"Stage selected: {currentStageName}");

                if (selectedStage.HasEnemyRounds)
                {
                    return selectedStage.EnemyRoundSequence;
                }

                LogSystem($"Selected stage '{currentStageName}' has no enemy rounds. Falling back to GameManager inspector sequence.");
            }

            currentStageName = "Direct Battle";

            if (enemyRoundSequence != null && enemyRoundSequence.Length > 0)
            {
                LogSystem("Using GameManager inspector enemy round sequence.");
                return enemyRoundSequence;
            }

            return null;
        }

        private bool TryApplyEnemyDefinition(EnemyDefinition definition, string source)
        {
            if (enemyActor == null || definition == null)
            {
                return false;
            }

            if (!enemyActor.ApplyEnemyDefinition(definition))
            {
                LogSystem($"GameManager: Failed to apply {source}.");
                return false;
            }

            return true;
        }

        private void LogEnemyDefeated()
        {
            if (enemyRoundSequenceActive && activeEnemyRoundSequence != null && currentEnemyRoundIndex >= 0 && currentEnemyRoundIndex < activeEnemyRoundSequence.Length)
            {
                EnemyDefinition defeatedEnemy = activeEnemyRoundSequence[currentEnemyRoundIndex];
                string defeatedName = defeatedEnemy != null ? defeatedEnemy.DisplayName : "Unknown Enemy";
                LogSystem($"Enemy round defeated: {currentEnemyRoundIndex + 1}/{activeEnemyRoundSequence.Length} - {defeatedName}");
                return;
            }

            LogSystem($"Enemy round defeated: {GetCurrentEnemyDisplayName()}");
        }

        private string GetCurrentEnemyDisplayName()
        {
            if (enemyActor == null)
            {
                return "Enemy";
            }

            if (enemyActor.CharacterData != null)
            {
                return enemyActor.CharacterData.CharacterName;
            }

            if (enemyActor.CombatProfile != null)
            {
                return enemyActor.CombatProfile.name;
            }

            return "Enemy";
        }

        private string GetRoundDisplayText()
        {
            if (!enemyRoundSequenceActive || activeEnemyRoundSequence == null || activeEnemyRoundSequence.Length <= 1)
            {
                return "Final Round";
            }

            if (currentEnemyRoundIndex >= activeEnemyRoundSequence.Length - 1)
            {
                return "Final Round";
            }

            return $"Round {currentEnemyRoundIndex + 1}";
        }

        private void ShowResult(bool isWin, int stars)
        {
            SetBoardInputBlocked(true);
            string resultStageName = StageSession.SelectedStage != null ? currentStageName : string.Empty;
            gameHUD?.ShowResult(isWin, resultStageName, stars);
            LogSystem($"Result shown: {(isWin ? "win" : "lose")}");
        }

        private void ApplyStageWinProgression(int stars)
        {
            int stageIndex = StageSession.SelectedStageIndex;

            if (stageIndex < 0)
            {
                LogSystem("Stage progression skipped: no selected stage index.");
                return;
            }

            int previousStars = StageProgression.GetStageStars(stageIndex);
            int nextStageIndex = stageIndex + 1;
            bool nextStageWasLocked = !StageProgression.IsStageUnlocked(nextStageIndex);

            StageProgression.MarkStageCleared(stageIndex);
            StageProgression.SetStageStars(stageIndex, stars);
            StageProgression.UnlockStage(nextStageIndex);
            StageSession.SetPendingStageClearVisual(stageIndex, previousStars, stars, stars > previousStars, nextStageWasLocked);
            LogSystem($"Stage cleared: {stageIndex} with {stars} star(s).");
            StageProgression.LogProgressionState();
        }

        private int CalculateStageStars()
        {
            if (playerActor == null || playerActor.MaxHP <= 0)
            {
                return 0;
            }

            float hpPercent = (float)playerActor.CurrentHP / playerActor.MaxHP;

            if (hpPercent > 0.7f)
            {
                return 3;
            }

            if (hpPercent > 0.3f)
            {
                return 2;
            }

            return hpPercent > 0f ? 1 : 0;
        }

        private void LogPlayer(string message)
        {
            LogCombat(message, "green");
        }

        private void LogEnemy(string message)
        {
            LogCombat(message, "red");
        }

        private void LogSkill(string message)
        {
            LogCombat(message, "cyan");
        }

        private void LogSkillWarning(string message)
        {
            LogCombat(message, "cyan", true);
        }

        private void LogPassive(string message)
        {
            LogCombat(message, "magenta");
        }

        private void LogCurse(string message)
        {
            LogCombat(message, "orange");
        }

        private void LogSpeed(string message)
        {
            LogCombat(message, "yellow");
        }

        private void LogSystem(string message)
        {
            LogCombat(message, "gray");
        }

        private void LogCombat(string message, string color, bool isWarning = false)
        {
            if (!enableCombatDebugLogs)
            {
                return;
            }

            string richMessage = $"<color={color}>{message}</color>";

            if (isWarning)
            {
                Debug.LogWarning(richMessage);
                return;
            }

            Debug.Log(richMessage);
        }

        private string FormatPercent(float value)
        {
            return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
        }

        private void SetState(GameState newState)
        {
            currentState = newState;
            switch (currentState)
            {
                case GameState.Playing:
                    turnStatusText = "Player Turn";
                    break;

                case GameState.Win:
                    turnStatusText = "Player Wins";
                    break;

                case GameState.Lose:
                    turnStatusText = "Player Loses";
                    break;
            }

            LogSystem($"Game State: {currentState}");
        }

        #endregion
    }
}
