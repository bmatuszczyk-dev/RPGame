using RPGame.Core.Statistics;
using UnityEngine;
using UnityEngine.UI;

namespace RPGame.UI
{
    public sealed class EnemyTargetHealthBar : MonoBehaviour
    {
        [SerializeField] private GameObject healthBar;
        [SerializeField] private Image fill;

        private StatisticsController statisticsController;
        private bool isSubscribed;

        private void Awake()
        {
            statisticsController = GetComponentInParent<StatisticsController>();
            Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        public void Show()
        {
            if (healthBar != null)
            {
                healthBar.SetActive(true);
            }

            Subscribe();
            RefreshFill();
        }

        public void Hide()
        {
            Unsubscribe();
            if (healthBar != null)
            {
                healthBar.SetActive(false);
            }
        }

        private void Subscribe()
        {
            if (isSubscribed || statisticsController == null)
            {
                return;
            }

            statisticsController.HealthChanged += HandleHealthChanged;
            statisticsController.Died += Hide;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || statisticsController == null)
            {
                return;
            }

            statisticsController.HealthChanged -= HandleHealthChanged;
            statisticsController.Died -= Hide;
            isSubscribed = false;
        }

        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            SetFill(currentHealth, maxHealth);
        }

        private void RefreshFill()
        {
            if (fill != null && statisticsController != null)
            {
                fill.fillAmount = Mathf.Clamp01(statisticsController.HealthNormalized);
            }
        }

        private void SetFill(float currentHealth, float maxHealth)
        {
            if (fill != null)
            {
                fill.fillAmount = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
            }
        }
    }
}
