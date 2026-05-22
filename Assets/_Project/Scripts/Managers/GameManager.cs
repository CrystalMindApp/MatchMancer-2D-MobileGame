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

        [Header("Debug")]
        [SerializeField] private bool enableCombatDebugLogs = true;

        // State
        private GameState currentState;
        private float pendingCritChance;
        private bool isTurnResolving;
        private bool isActiveSkillResolving;
        private string turnStatusText = "Player Turn";
        private int currentEnemyRoundIndex;
        private int enemySkillTurnsRemaining;
        private bool enemyRoundSequenceActive;
        private bool isEnemyRoundTransitioning;
        private bool isEnemyActionResolving;
        private bool enemyAttackUsedSkillThisAction;
        private EnemyDefinition[] activeEnemyRoundSequence;
        private string currentStageName = "Stage";

        #endregion

        #region Properties

        public int PlayerMaxHp => playerActor != null ? playerActor.MaxHp : 0;
        public int PlayerCurrentHp => playerActor != null ? playerActor.CurrentHp : 0;
        public int EnemyMaxHp => enemyActor != null ? enemyActor.MaxHp : 0;
        public int EnemyCurrentHp => enemyActor != null ? enemyActor.CurrentHp : 0;
        public int CurrentSkillGauge => playerActor != null ? playerActor.CurrentSkillGauge : 0;
        public int MaxSkillGauge => playerActor != null ? playerActor.MaxSkillGauge : 0;
        public int PurplePassiveStack => playerActor != null ? playerActor.PurplePassiveStack : 0;
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
            StartGame();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
            SceneManager.LoadScene(homeSceneName);
        }

        #endregion

        #region Private Methods

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

            bool activated = activeSkill.SkillEffectType == ActiveSkillEffectType.ClearTileColor &&
                boardManager != null &&
                boardManager.TryActivateColorClearSkill(targetType);

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
                FinishTurnResolve();
                yield break;
            }

            if (playerActor != null && playerActor.CurrentHp <= 0)
            {
                HandlePlayerDefeated();
                FinishTurnResolve();
                yield break;
            }

            TickCurseDurations();

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
            playerActor?.ResetTurnSpeedBonus();
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
            playerActor.Heal(healAmount);
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

        private void ResolvePlayerPassive(PassiveSkillTiming timing)
        {
            if (playerActor == null || !playerActor.CanExecutePassive(timing))
            {
                return;
            }

            PassiveSkillData passiveSkill = playerActor.PassiveSkill;
            bool executed = ExecutePlayerPassiveEffect(passiveSkill);

            if (!executed)
            {
                return;
            }

            playerActor.MarkPassiveExecuted();
            playerActor.PlaySkillVisual();
            LogPassive($"Passive Skill: {passiveSkill.PassiveName} executed at {passiveSkill.Timing}.");
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

                    bool createdBomb = boardManager.TryCreateRandomBomb();
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

            int damage = CalculatePlayerDamage(clearedTileCount);
            enemyActor.TakeDamage(damage);

            if (damage > 0)
            {
                enemyActor.PlayGetHitVisual();
                enemyActor.PlayHitMotion(playerActor.transform.position);
                ShowDamagePopup(damage, enemyActor.DamagePopupAnchor);
                tinyImpulse?.Shake();
            }

            pendingCritChance = 0f;

            LogEnemy($"Player deals {damage} damage. Enemy HP: {enemyActor.CurrentHp}");

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

        private int CalculatePlayerDamage(int clearedTileCount)
        {
            float damage = clearedTileCount * playerActor.BaseDamagePerTile * playerActor.AttackMultiplier;
            bool isCritical = Random.value < pendingCritChance;

            if (playerActor.IsAttackMissed())
            {
                LogCurse("Player attack missed due to Curse.");
                return 0;
            }

            if (isCritical)
            {
                damage *= playerActor.RedCritDamageMultiplier;
                LogPlayer("Red effect: critical hit!");
            }

            return Mathf.Max(0, Mathf.RoundToInt(damage));
        }

        private IEnumerator EnemyAttackRoutine(bool canAttemptSpecial)
        {
            enemyAttackUsedSkillThisAction = false;
            HighlightEnemyTurn();
            enemyActor.PlayAttackVisual();
            enemyActor.PlayAttackMotion(playerActor.transform.position);

            yield return new WaitForSeconds(attackImpactDelay);

            int damage = enemyActor.IsAttackMissed() ? 0 : enemyActor.BaseAttackDamage;

            if (damage <= 0)
            {
                LogCurse("Enemy attack missed due to Curse.");
            }
            else if (Random.value < enemyActor.EnemyCritChance)
            {
                damage = Mathf.RoundToInt(damage * enemyActor.EnemyCritMultiplier);
                LogEnemy("Enemy critical hit!");
            }

            playerActor.TakeDamage(damage);

            if (damage > 0)
            {
                playerActor.PlayGetHitVisual();
                playerActor.PlayHitMotion(enemyActor.transform.position);
                ShowDamagePopup(damage, playerActor.DamagePopupAnchor);
                tinyImpulse?.Shake();
            }

            LogEnemy($"Enemy attacks for {damage}. Player HP: {playerActor.CurrentHp}");

            if (canAttemptSpecial && damage > 0 && Random.value < enemyActor.EnemyApplyCurseChance)
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
            LogEnemy($"Enemy heals for {healAmount}. Enemy HP: {enemyActor.CurrentHp}");
            return true;
        }

        private bool TryEnemyDisruption()
        {
            if (boardManager == null || Random.value > enemyActor.EnemySpecialDisruptChance)
            {
                return false;
            }

            bool disrupted = boardManager.TryRemoveRandomSpecialTile();
            if (disrupted)
            {
                enemyActor.PlaySkillVisual();
            }

            LogEnemy(disrupted
                ? "Enemy disruption: removed one special tile."
                : "Enemy disruption: no special tile available.");
            return disrupted;
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

        private void ShowDamagePopup(int amount, Transform anchor)
        {
            damagePopupController?.ShowDamage(amount, anchor);
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
