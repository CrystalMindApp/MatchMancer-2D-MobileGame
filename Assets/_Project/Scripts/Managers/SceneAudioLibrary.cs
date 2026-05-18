using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class SceneAudioLibrary : MonoBehaviour
    {
        #region Variables

        [Header("BGM")]
        [SerializeField] private AudioClip sceneBgm;

        [Header("UI SFX")]
        [SerializeField] private AudioClip buttonClickSfx;
        [SerializeField, Min(0f)] private float buttonClickMinInterval = 0.05f;

        // Gameplay SFX section reserved for future scene-specific clips.

        #endregion

        #region Properties

        public AudioClip SceneBgm => sceneBgm;

        #endregion

        #region Unity Methods

        private void Start()
        {
            PlaySceneBgm();
        }

        #endregion

        #region Public Methods

        public void PlaySceneBgm()
        {
            if (AudioManager.Instance == null || sceneBgm == null)
            {
                return;
            }

            AudioManager.Instance.PlayBgm(sceneBgm);
        }

        public void PlayButtonClick()
        {
            if (AudioManager.Instance == null || buttonClickSfx == null)
            {
                return;
            }

            AudioManager.Instance.PlaySfx(buttonClickSfx, buttonClickMinInterval);
        }

        #endregion

        #region Private Methods

        #endregion
    }
}
