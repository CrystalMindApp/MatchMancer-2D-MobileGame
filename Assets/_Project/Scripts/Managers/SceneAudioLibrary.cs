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
        [SerializeField] private AudioClip panelOpenSfx;
        [SerializeField] private AudioClip panelCloseSfx;
        [SerializeField, Min(0f)] private float buttonClickMinInterval = 0.05f;

        [Header("Gameplay SFX")]
        [SerializeField] private AudioClip matchClearSfx;
        [SerializeField] private AudioClip specialTileSpawnSfx;
        [SerializeField] private AudioClip comboSfx;
        [SerializeField] private AudioClip winSfx;
        [SerializeField] private AudioClip loseSfx;
        [SerializeField, Min(0f)] private float gameplaySfxMinInterval = 0.05f;

        #endregion

        #region Properties

        public AudioClip SceneBgm => sceneBgm;
        public static SceneAudioLibrary Current { get; private set; }

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            Current = this;
        }

        private void OnDisable()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

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
            PlaySfx(buttonClickSfx, buttonClickMinInterval);
        }

        public void PlayPanelOpen()
        {
            PlaySfx(panelOpenSfx, buttonClickMinInterval);
        }

        public void PlayPanelClose()
        {
            PlaySfx(panelCloseSfx, buttonClickMinInterval);
        }

        public void PlayMatchClear()
        {
            PlaySfx(matchClearSfx, gameplaySfxMinInterval);
        }

        public void PlaySpecialTileSpawn()
        {
            PlaySfx(specialTileSpawnSfx, gameplaySfxMinInterval);
        }

        public void PlayCombo()
        {
            PlaySfx(comboSfx, gameplaySfxMinInterval);
        }

        public void PlayResult(bool isWin)
        {
            PlaySfx(isWin ? winSfx : loseSfx, gameplaySfxMinInterval);
        }

        #endregion

        #region Private Methods

        private void PlaySfx(AudioClip clip, float minInterval)
        {
            if (AudioManager.Instance == null || clip == null)
            {
                return;
            }

            AudioManager.Instance.PlaySfx(clip, minInterval);
        }

        #endregion
    }
}
