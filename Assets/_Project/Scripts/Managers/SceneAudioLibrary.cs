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
        [SerializeField] private AudioClip combo1Sfx;
        [SerializeField] private AudioClip combo2Sfx;
        [SerializeField] private AudioClip combo3Sfx;
        [SerializeField] private AudioClip combo4Sfx;
        [SerializeField] private AudioClip combo5Sfx;
        [SerializeField] private AudioClip combo6Sfx;
        [SerializeField] private AudioClip combo7Sfx;
        [SerializeField] private AudioClip combo8Sfx;
        [SerializeField] private AudioClip failedSwapSfx;
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
            PlayComboLevel(1);
        }

        public void PlayComboLevel(int comboLevel)
        {
            AudioClip comboClip = GetComboClip(comboLevel);
            PlaySfx(comboClip != null ? comboClip : matchClearSfx, gameplaySfxMinInterval);
        }

        public void PlayFailedSwap()
        {
            PlaySfx(failedSwapSfx, gameplaySfxMinInterval);
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

        private AudioClip GetComboClip(int comboLevel)
        {
            int safeComboLevel = Mathf.Clamp(comboLevel, 1, 8);

            return safeComboLevel switch
            {
                1 => combo1Sfx,
                2 => combo2Sfx,
                3 => combo3Sfx,
                4 => combo4Sfx,
                5 => combo5Sfx,
                6 => combo6Sfx,
                7 => combo7Sfx,
                _ => combo8Sfx
            };
        }

        #endregion
    }
}
