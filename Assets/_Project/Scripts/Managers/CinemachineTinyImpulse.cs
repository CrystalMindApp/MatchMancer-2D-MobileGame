using Unity.Cinemachine;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class CinemachineTinyImpulse : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField, Min(0f)] private float defaultForce = 0.08f;
        [SerializeField] private Vector3 defaultVelocity = new Vector3(0.06f, 0.04f, 0f);

        [Header("References")]
        [SerializeField] private CinemachineImpulseSource impulseSource;

        // Cache

        // State

        #endregion

        #region Unity Methods

        private void Awake()
        {
            if (impulseSource == null)
            {
                impulseSource = GetComponent<CinemachineImpulseSource>();
            }
        }

        #endregion

        #region Public Methods

        public void Shake()
        {
            Shake(defaultForce);
        }

        public void Shake(float force)
        {
            if (impulseSource == null || force <= 0f)
            {
                return;
            }

            impulseSource.GenerateImpulseWithVelocity(defaultVelocity.normalized * force);
        }

        #endregion
    }
}
