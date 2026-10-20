using RPGame.Core.Targeting;
using RPGame.Player.Targeting;
using UnityEngine;

namespace RPGame.UI
{
    public sealed class EnemyTargetHealthBarController : MonoBehaviour
    {
        [SerializeField] private TargetingController targetingController;
        [SerializeField] private Camera playerCamera;

        private EnemyTargetHealthBar currentHealthBar;

        private void OnEnable()
        {
            if (targetingController == null)
            {
                return;
            }

            targetingController.TargetChanged += HandleTargetChanged;
            HandleTargetChanged(targetingController.CurrentTarget);
        }

        private void OnDisable()
        {
            if (targetingController != null)
            {
                targetingController.TargetChanged -= HandleTargetChanged;
            }

            HideCurrentHealthBar();
        }

        private void HandleTargetChanged(ITargetable target)
        {
            HideCurrentHealthBar();
            if (target == null || target.TargetPoint == null)
            {
                return;
            }

            currentHealthBar = target.TargetPoint.GetComponentInParent<EnemyTargetHealthBar>();
            currentHealthBar?.Show(playerCamera);
        }

        private void HideCurrentHealthBar()
        {
            if (currentHealthBar == null)
            {
                return;
            }

            currentHealthBar.Hide();
            currentHealthBar = null;
        }
    }
}
