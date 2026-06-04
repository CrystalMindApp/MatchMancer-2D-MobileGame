using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class EnemyTraitIconController : MonoBehaviour
    {
        #region Variables

        [Header("References")]
        [SerializeField] private StatusIconView traitIconView;

        [Header("Curse Trait Icons")]
        [SerializeField] private Sprite poisonIcon;
        [SerializeField] private Sprite blindIcon;

        #endregion

        #region Public Methods

        public void SetCurseTrait(bool active, CurseEffectData effect)
        {
            if (!active || effect == null)
            {
                HideTraitIcon();
                return;
            }

            Sprite icon = GetIcon(effect.CurseType);

            if (icon == null || traitIconView == null)
            {
                HideTraitIcon();
                return;
            }

            traitIconView.gameObject.SetActive(true);
            traitIconView.SetIconOnly(icon);
        }

        #endregion

        #region Private Methods

        private Sprite GetIcon(CurseType curseType)
        {
            return curseType switch
            {
                CurseType.Poison => poisonIcon,
                CurseType.Blind => blindIcon,
                _ => null
            };
        }

        private void HideTraitIcon()
        {
            if (traitIconView != null)
            {
                traitIconView.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
