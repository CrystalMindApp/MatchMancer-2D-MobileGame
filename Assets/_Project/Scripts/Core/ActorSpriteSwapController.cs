using System.Collections;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class ActorSpriteSwapController : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField, Min(0f)] private float returnToIdleDelay = 1f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hitTintColor = new Color(1f, 0.55f, 0.55f, 1f);

        [Header("References")]
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite attackSprite;
        [SerializeField] private Sprite getHitSprite;
        [SerializeField] private Sprite skillSprite;

        // Cache
        private Coroutine returnRoutine;

        // State

        #endregion

        #region Unity Methods

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        #endregion

        #region Public Methods

        public void ShowIdle()
        {
            StopReturnRoutine();
            SetColor(normalColor);
            SetSprite(idleSprite);
        }

        public void ResetToIdle()
        {
            Show();
            ShowIdle();
        }

        public void PlayAttack()
        {
            PlayTemporarySprite(attackSprite);
        }

        public void PlayGetHit()
        {
            PlayTemporarySprite(getHitSprite, true);
        }

        public void PlaySkill()
        {
            PlayTemporarySprite(skillSprite);
        }

        public void ApplySprites(Sprite idle, Sprite attack, Sprite skill, Sprite getHit)
        {
            if (idle != null)
            {
                idleSprite = idle;
            }

            if (attack != null)
            {
                attackSprite = attack;
            }

            if (skill != null)
            {
                skillSprite = skill;
            }

            if (getHit != null)
            {
                getHitSprite = getHit;
            }

            if (idleSprite != null)
            {
                ShowIdle();
            }
        }

        public void Hide()
        {
            StopReturnRoutine();

            if (targetRenderer != null)
            {
                targetRenderer.enabled = false;
            }
        }

        public void Show()
        {
            if (targetRenderer != null)
            {
                targetRenderer.enabled = true;
            }
        }

        #endregion

        #region Private Methods

        private void PlayTemporarySprite(Sprite sprite)
        {
            PlayTemporarySprite(sprite, false);
        }

        private void PlayTemporarySprite(Sprite sprite, bool useHitTint)
        {
            StopReturnRoutine();
            SetColor(useHitTint ? hitTintColor : normalColor);
            SetSprite(sprite);
            returnRoutine = StartCoroutine(ReturnToIdleRoutine());
        }

        private IEnumerator ReturnToIdleRoutine()
        {
            yield return new WaitForSeconds(returnToIdleDelay);
            returnRoutine = null;
            SetColor(normalColor);
            SetSprite(idleSprite);
        }

        private void SetSprite(Sprite sprite)
        {
            if (targetRenderer == null || sprite == null)
            {
                return;
            }

            targetRenderer.sprite = sprite;
        }

        private void SetColor(Color color)
        {
            if (targetRenderer != null)
            {
                targetRenderer.color = color;
            }
        }

        private void StopReturnRoutine()
        {
            if (returnRoutine == null)
            {
                return;
            }

            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        #endregion
    }
}
