using System;
using TMPro;
using RPGame.Encounter;
using RPGame.UI;
using RPGame.UI.Leaderboard;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RPGame.UI.Encounter
{
    public sealed class EncounterEndUI : MonoBehaviour
    {
        [SerializeField] private EncounterRuntime runtime;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text reachedWaveText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button returnToMenuButton;
        [SerializeField] private Button scoreboardButton;
        [SerializeField] private LeaderboardScreen leaderboardScreen;

        private Action<int> loadScene = sceneBuildIndex => GameFlow.RestartGameplayScene(sceneBuildIndex);
        private bool hasDisplayedEnd;

        private void Start()
        {
            SetPanelVisible(false);
            leaderboardScreen?.Close();
        }

        private void OnEnable()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartCurrentScene);
                restartButton.onClick.AddListener(RestartCurrentScene);
            }

            if (scoreboardButton != null)
            {
                scoreboardButton.onClick.RemoveListener(ShowScoreboard);
                scoreboardButton.onClick.AddListener(ShowScoreboard);
            }

            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.RemoveListener(ReturnToMainMenu);
                returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
        }

        private void OnDisable()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartCurrentScene);
            }

            if (scoreboardButton != null)
            {
                scoreboardButton.onClick.RemoveListener(ShowScoreboard);
            }

            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            }

        }

        private void Update()
        {
            EncounterController encounterController = runtime != null ? runtime.EncounterController : null;
            if (encounterController == null || encounterController.State != EncounterState.Ended)
            {
                hasDisplayedEnd = false;
                SetPanelVisible(false);
                leaderboardScreen?.Close();
                return;
            }

            if (hasDisplayedEnd)
            {
                return;
            }

            SetPanelVisible(true);
            leaderboardScreen?.Close();
            GameFlow.ShowMenuCursor();

            ScoreSystem scoreSystem = runtime.ScoreSystem;
            if (finalScoreText != null && scoreSystem != null)
            {
                finalScoreText.text = $"Final Score: {scoreSystem.CurrentScore}";
            }

            if (reachedWaveText != null)
            {
                reachedWaveText.text = $"Waves Completed: {encounterController.CurrentWaveNumber - 1}";
            }

            hasDisplayedEnd = true;
        }

        public void RestartCurrentScene()
        {
            loadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ReturnToMainMenu()
        {
            GameFlow.LoadMainMenu();
        }

        private void ShowScoreboard()
        {
            leaderboardScreen?.Open();
        }

        private void SetPanelVisible(bool isVisible)
        {
            if (panel != null && panel.activeSelf != isVisible)
            {
                panel.SetActive(isVisible);
            }
        }

    }
}
