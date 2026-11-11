using RPGame.Core.Statuses;
using UnityEngine;
using UnityEngine.UI;

namespace RPGame.UI
{
    public sealed class StatusIndicatorUI : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private Image icon;

        private StatusInstance status;

        private void Update()
        {
            RefreshFill();
        }

        public void Initialize(StatusInstance instance)
        {
            status = instance;
            ConfigureImages();
            icon.sprite = status?.Definition?.Icon;
            RefreshFill();
        }

        public void Refresh(StatusInstance instance)
        {
            Initialize(instance);
        }

        private void ConfigureImages()
        {
            if (fill != null)
            {
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Radial360;
                fill.raycastTarget = false;
            }

            if (icon != null)
            {
                icon.raycastTarget = false;
            }
        }

        private void RefreshFill()
        {
            if (fill == null)
            {
                return;
            }

            float duration = status?.Duration ?? 0f;
            fill.fillAmount = duration > 0f
                ? Mathf.Clamp01(status.RemainingDuration / duration)
                : 0f;
        }
    }
}
