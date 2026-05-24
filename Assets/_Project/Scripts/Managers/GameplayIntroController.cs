using System.Collections;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class GameplayIntroController : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private bool playGameplayIntro = true;
        [SerializeField, Min(0f)] private float playerRunInDuration = 0.8f;
        [SerializeField, Min(0f)] private float postRunInDelay = 0.1f;
        [SerializeField, Min(0f)] private float battleStartHoldDuration = 0.75f;
        [SerializeField, Min(1f)] private float enemyBattleStartPunchScale = 1.06f;
        [SerializeField, Min(0f)] private float battleStartShakeForce;
        [SerializeField, Min(0.01f)] private float battleStartShakeInterval = 0.1f;
        [SerializeField] private bool enableBoardManagerOnBattleStart = true;

        [Header("References")]
        [SerializeField] private PlayerActor playerActor;
        [SerializeField] private EnemyActor enemyActor;
        [SerializeField] private Transform playerIntroStartPoint;
        [SerializeField] private Transform playerBattlePoint;
        [SerializeField] private CinemachineTinyImpulse tinyImpulse;
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private GameObject enemyThreatIcon;

        // Cache

        // State
        private Vector3 fallbackPlayerBattlePosition;
        private bool hasFallbackPlayerBattlePosition;

        #endregion

        #region Public Methods

        public void PrepareIntro()
        {
            if (!playGameplayIntro || playerActor == null)
            {
                return;
            }

            CacheFallbackBattlePosition();

            if (playerIntroStartPoint != null)
            {
                playerActor.transform.position = playerIntroStartPoint.position;
            }

            playerActor.ShowIdleVisual();
            enemyActor?.ShowIdleVisual();
            SetEnemyThreatIconVisible(false);
            boardManager?.SetInputBlocked(true);
        }

        public IEnumerator PlayIntroRoutine()
        {
            if (!playGameplayIntro)
            {
                yield break;
            }

            yield return StartCoroutine(PlayPlayerRunInRoutine());
            yield return new WaitForSeconds(Mathf.Max(0f, postRunInDelay));
            yield return StartCoroutine(PlayBattleStartRoutine());
        }

        #endregion

        #region Private Methods

        private IEnumerator PlayPlayerRunInRoutine()
        {
            if (playerActor == null)
            {
                yield break;
            }

            CacheFallbackBattlePosition();

            Vector3 startPosition = playerIntroStartPoint != null
                ? playerIntroStartPoint.position
                : playerActor.transform.position;
            Vector3 targetPosition = playerBattlePoint != null
                ? playerBattlePoint.position
                : fallbackPlayerBattlePosition;
            float safeDuration = Mathf.Max(0f, playerRunInDuration);

            playerActor.transform.position = startPosition;
            playerActor.PlayRunVisual();

            if (safeDuration <= 0f)
            {
                playerActor.transform.position = targetPosition;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float time = Mathf.Clamp01(elapsed / safeDuration);
                playerActor.transform.position = Vector3.Lerp(startPosition, targetPosition, time);
                yield return null;
            }

            playerActor.transform.position = targetPosition;
        }

        private IEnumerator PlayBattleStartRoutine()
        {
            playerActor?.PlayBattleStartVisual();
            enemyActor?.PlayBattleStartVisual();
            SetEnemyThreatIconVisible(true);
            StartBoardForBattleBeat();

            float safeDuration = Mathf.Max(0f, battleStartHoldDuration);

            if (safeDuration > 0f)
            {
                yield return StartCoroutine(PlayBattleStartHoldRoutine(safeDuration));
            }

            playerActor?.ShowIdleVisual();
            enemyActor?.ShowIdleVisual();
            SetEnemyThreatIconVisible(false);
        }

        private IEnumerator PlayBattleStartHoldRoutine(float duration)
        {
            Transform enemyTransform = enemyActor != null ? enemyActor.transform : null;
            Vector3 originalEnemyScale = enemyTransform != null ? enemyTransform.localScale : Vector3.one;
            float elapsed = 0f;
            float nextShakeTime = 0f;

            if (enemyTransform != null)
            {
                enemyTransform.localScale = originalEnemyScale * enemyBattleStartPunchScale;
            }

            while (elapsed < duration)
            {
                if (tinyImpulse != null && elapsed >= nextShakeTime)
                {
                    tinyImpulse.Shake(battleStartShakeForce);
                    nextShakeTime += battleStartShakeInterval;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (enemyTransform != null)
            {
                enemyTransform.localScale = originalEnemyScale;
            }
        }

        private void StartBoardForBattleBeat()
        {
            if (!enableBoardManagerOnBattleStart || boardManager == null)
            {
                return;
            }

            if (!boardManager.enabled)
            {
                boardManager.enabled = true;
            }

            boardManager.SetInputBlocked(true);
        }

        private void CacheFallbackBattlePosition()
        {
            if (hasFallbackPlayerBattlePosition || playerActor == null)
            {
                return;
            }

            fallbackPlayerBattlePosition = playerActor.transform.position;
            hasFallbackPlayerBattlePosition = true;
        }

        private void SetEnemyThreatIconVisible(bool visible)
        {
            if (enemyThreatIcon != null)
            {
                enemyThreatIcon.SetActive(visible);
            }
        }

        #endregion
    }
}
