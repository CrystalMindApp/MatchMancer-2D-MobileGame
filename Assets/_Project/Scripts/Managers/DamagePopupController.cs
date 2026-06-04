using System.Collections.Generic;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public enum DamagePopupType
    {
        NormalDamage,
        CriticalDamage,
        Heal
    }

    public class DamagePopupController : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField, Min(1)] private int initialPoolSize = 12;
        [SerializeField, Min(0.05f)] private float popupDuration = 0.75f;
        [SerializeField] private Vector2 startScreenOffset = new Vector2(0f, 32f);
        [SerializeField] private Vector2 floatScreenOffset = new Vector2(0f, 48f);
        [SerializeField, Min(0f)] private float startScale = 1f;
        [SerializeField, Min(0f)] private float punchScale = 1.25f;
        [SerializeField] private Color damageColor = Color.white;
        [SerializeField] private Color criticalDamageColor = new Color(1f, 0.35f, 0.05f, 1f);
        [SerializeField] private Color healColor = new Color(0.25f, 1f, 0.35f, 1f);
        [SerializeField] private Color missColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        [SerializeField, Min(0f)] private float criticalScaleMultiplier = 1.5f;
        [SerializeField, Min(0f)] private float healScaleMultiplier = 1.2f;

        [Header("References")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private DamagePopupItem popupPrefab;

        // Cache
        private readonly Queue<DamagePopupItem> availablePopups = new Queue<DamagePopupItem>();
        private readonly List<DamagePopupItem> activePopups = new List<DamagePopupItem>();

        // State
        private bool isInitialized;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            InitializePool();
        }

        #endregion

        #region Public Methods

        public void ShowDamage(int amount, Transform anchor)
        {
            ShowPopup(amount, anchor, DamagePopupType.NormalDamage);
        }

        public void ShowCriticalDamage(int amount, Transform anchor)
        {
            ShowPopup(amount, anchor, DamagePopupType.CriticalDamage);
        }

        public void ShowHeal(int amount, Transform anchor)
        {
            ShowPopup(amount, anchor, DamagePopupType.Heal);
        }

        public void ShowMiss(Transform anchor)
        {
            ShowTextPopup("MISS", anchor, missColor, 1f);
        }

        public void ShowPopup(int amount, Transform anchor, DamagePopupType popupType)
        {
            if (amount <= 0 || anchor == null)
            {
                return;
            }

            InitializePool();

            if (targetCanvas == null || popupPrefab == null)
            {
                return;
            }

            DamagePopupItem popup = GetPopup();
            activePopups.Add(popup);
            Color popupColor = GetPopupColor(popupType);
            float scaleMultiplier = GetPopupScaleMultiplier(popupType);

            PlayPopup(popup, GetPopupText(amount, popupType), anchor, popupColor, scaleMultiplier);
        }

        #endregion

        #region Private Methods

        private void InitializePool()
        {
            if (isInitialized)
            {
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (popupPrefab == null || targetCanvas == null)
            {
                isInitialized = true;
                return;
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                DamagePopupItem popup = CreatePopup();
                availablePopups.Enqueue(popup);
            }

            isInitialized = true;
        }

        private DamagePopupItem GetPopup()
        {
            if (availablePopups.Count > 0)
            {
                return availablePopups.Dequeue();
            }

            return CreatePopup();
        }

        private void ShowTextPopup(string text, Transform anchor, Color popupColor, float scaleMultiplier)
        {
            if (string.IsNullOrWhiteSpace(text) || anchor == null)
            {
                return;
            }

            InitializePool();

            if (targetCanvas == null || popupPrefab == null)
            {
                return;
            }

            DamagePopupItem popup = GetPopup();
            activePopups.Add(popup);
            PlayPopup(popup, text, anchor, popupColor, Mathf.Max(0f, scaleMultiplier));
        }

        private void PlayPopup(DamagePopupItem popup, string text, Transform anchor, Color popupColor, float scaleMultiplier)
        {
            if (popup == null)
            {
                return;
            }

            popup.Play(
                text,
                anchor,
                targetCanvas,
                GetWorldCamera(),
                startScreenOffset,
                floatScreenOffset,
                popupDuration,
                Vector3.one * startScale * scaleMultiplier,
                Vector3.one * punchScale * scaleMultiplier,
                popupColor,
                ReleasePopup);
        }

        private DamagePopupItem CreatePopup()
        {
            DamagePopupItem popup = Instantiate(popupPrefab, targetCanvas.transform);
            popup.gameObject.SetActive(false);
            return popup;
        }

        private void ReleasePopup(DamagePopupItem popup)
        {
            if (popup == null)
            {
                return;
            }

            popup.Stop();
            activePopups.Remove(popup);
            availablePopups.Enqueue(popup);
        }

        private Camera GetWorldCamera()
        {
            if (worldCamera != null)
            {
                return worldCamera;
            }

            worldCamera = Camera.main;
            return worldCamera;
        }

        private Color GetPopupColor(DamagePopupType popupType)
        {
            switch (popupType)
            {
                case DamagePopupType.CriticalDamage:
                    return criticalDamageColor;

                case DamagePopupType.Heal:
                    return healColor;

                default:
                    return damageColor;
            }
        }

        private float GetPopupScaleMultiplier(DamagePopupType popupType)
        {
            switch (popupType)
            {
                case DamagePopupType.CriticalDamage:
                    return criticalScaleMultiplier;

                case DamagePopupType.Heal:
                    return healScaleMultiplier;

                default:
                    return 1f;
            }
        }

        private string GetPopupText(int amount, DamagePopupType popupType)
        {
            return popupType == DamagePopupType.Heal ? $"+{amount}" : amount.ToString();
        }

        #endregion
    }
}
