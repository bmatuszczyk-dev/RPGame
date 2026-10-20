using RPGame.Core.Pooling;
using UnityEngine;

namespace RPGame.Enemies
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class HealerBeamView : MonoBehaviour, IPooledEnemyResettable
    {
        [SerializeField] private LineRenderer lineRenderer;

        private Controller controller;

        private void Awake()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            controller = GetComponentInParent<Controller>();
            Hide();
        }

        private void LateUpdate()
        {
            HealerEnemyBehaviour behaviour = controller != null
                ? controller.Behaviour as HealerEnemyBehaviour
                : null;
            HealerTarget target = behaviour?.CurrentHealTarget;
            if (behaviour == null || !behaviour.IsHealing || target == null || !target.IsValid)
            {
                Hide();
                return;
            }

            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, target.Position);
        }

        private void OnDisable()
        {
            Hide();
        }

        public void ResetForSpawn()
        {
            Hide();
        }

        public void ResetForDespawn()
        {
            Hide();
        }

        private void Hide()
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }
    }
}
