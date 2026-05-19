using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class SettingsInputController : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

        [Header("References")]
        [SerializeField] private GlobalSettingsPanel settingsPanel;

        // Cache

        // State
        private bool hasSearchedSettingsPanel;

        #endregion

        #region Unity Methods

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleSettingsPanel();
            }
        }

        #endregion

        #region Public Methods

        #endregion

        #region Private Methods

        private void ToggleSettingsPanel()
        {
            GlobalSettingsPanel panel = GetSettingsPanel();
            panel?.Toggle();
        }

        private GlobalSettingsPanel GetSettingsPanel()
        {
            if (settingsPanel != null)
            {
                return settingsPanel;
            }

            if (GlobalSettingsPanel.Instance != null)
            {
                settingsPanel = GlobalSettingsPanel.Instance;
                return settingsPanel;
            }

            if (hasSearchedSettingsPanel)
            {
                return null;
            }

            hasSearchedSettingsPanel = true;
            settingsPanel = FindFirstObjectByType<GlobalSettingsPanel>();
            return settingsPanel;
        }

        #endregion
    }
}
