using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class GlobalSettingsPanelBootstrap : MonoBehaviour
    {
        #region Variables

        [Header("References")]
        [SerializeField] private GlobalSettingsPanel settingsPanelPrefab;

        // Cache

        // State

        #endregion

        #region Unity Methods

        private void Awake()
        {
            EnsureSettingsPanelExists();
        }

        #endregion

        #region Public Methods

        #endregion

        #region Private Methods

        private void EnsureSettingsPanelExists()
        {
            if (GlobalSettingsPanel.Instance != null)
            {
                return;
            }

            GlobalSettingsPanel existingPanel = FindFirstObjectByType<GlobalSettingsPanel>();
            if (existingPanel != null)
            {
                return;
            }

            if (settingsPanelPrefab == null)
            {
                return;
            }

            Instantiate(settingsPanelPrefab);
        }

        #endregion
    }
}
