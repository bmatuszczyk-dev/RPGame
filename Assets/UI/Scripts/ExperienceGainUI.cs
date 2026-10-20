using RPGame.Progression;
using TMPro;
using UnityEngine;

namespace RPGame.UI
{
    public sealed class ExperienceGainUI : MonoBehaviour
    {
        [SerializeField] private CharacterProgression progression;
        [SerializeField] private TMP_Text experienceText;
        [SerializeField] private TMP_Text availableExperienceText;
        [SerializeField] private GameObject availableExperiencePanel;
        [SerializeField, Min(0f)] private float visibilityDuration = 2f;
        [SerializeField, Min(0f)] private float availableExperienceVisibilityDuration = 5f;

        private float remainingVisibility;
        private float remainingAvailableExperienceVisibility;

        private void Awake()
        {
            Hide();
            HideAvailableExperience();
        }

        private void OnEnable()
        {
            if (progression != null)
            {
                progression.ExperienceGained += Show;
                progression.AvailableExperienceChanged += RefreshAvailableExperience;
            }

            RefreshAvailableExperience();
        }

        private void OnDisable()
        {
            if (progression != null)
            {
                progression.ExperienceGained -= Show;
                progression.AvailableExperienceChanged -= RefreshAvailableExperience;
            }
        }

        private void Update()
        {
            if (remainingVisibility > 0f)
            {
                remainingVisibility -= Time.deltaTime;
                if (remainingVisibility <= 0f)
                {
                    Hide();
                }
            }

            if (remainingAvailableExperienceVisibility <= 0f)
            {
                return;
            }

            remainingAvailableExperienceVisibility -= Time.deltaTime;
            if (remainingAvailableExperienceVisibility <= 0f)
            {
                HideAvailableExperience();
            }
        }

        private void Show(int amount)
        {
            if (experienceText == null)
            {
                return;
            }

            experienceText.text = $"+ {amount}";
            experienceText.enabled = true;
            remainingVisibility = visibilityDuration;
        }

        private void Hide()
        {
            remainingVisibility = 0f;
            if (experienceText != null)
            {
                experienceText.enabled = false;
            }
        }

        private void RefreshAvailableExperience()
        {
            if (availableExperienceText != null)
            {
                availableExperienceText.text = progression != null ? $"{progression.AvailableExperience}" : string.Empty;
                SetAvailableExperienceVisible(progression != null);
                remainingAvailableExperienceVisibility = progression != null
                    ? availableExperienceVisibilityDuration
                    : 0f;
            }
        }

        private void HideAvailableExperience()
        {
            remainingAvailableExperienceVisibility = 0f;
            SetAvailableExperienceVisible(false);
        }

        private void SetAvailableExperienceVisible(bool isVisible)
        {
            if (availableExperiencePanel != null)
            {
                availableExperiencePanel.SetActive(isVisible);
                return;
            }

            if (availableExperienceText != null)
            {
                availableExperienceText.enabled = isVisible;
            }
        }
    }
}
