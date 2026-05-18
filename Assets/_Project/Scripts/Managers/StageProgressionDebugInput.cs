using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class StageProgressionDebugInput : MonoBehaviour
    {
        #region Variables

        [Header("References")]
        [SerializeField] private StageSelectPanel stageSelectPanel;

        // Cache

        // State

        #endregion

        #region Unity Methods

        private void Awake()
        {
            if (stageSelectPanel == null)
            {
                stageSelectPanel = GetComponent<StageSelectPanel>();
            }
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Backspace))
            {
                StageProgression.ResetProgression();
                stageSelectPanel?.RefreshAllStageButtons();
                Debug.Log("Stage progression debug reset complete.");
            }
#endif
        }

        #endregion
    }
}
