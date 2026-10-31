using RPGame.Core.Settings;
using RPGame.UI.Leaderboard;
using RPGame.UI.PlayerWindows;
using RPGame.UI.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace RPGame.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private PlayerNameWindowUI playerNameWindow;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button leaderboardsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private LeaderboardScreen leaderboardScreen;
        [SerializeField] private SettingsPanelUI settingsPanel;

        private void Awake()
        {
            GameSettings.ApplySavedSettings();
            GameFlow.ShowMenuCursor();
            newGameButton.onClick.AddListener(StartNewGame);
            leaderboardsButton.onClick.AddListener(OpenLeaderboards);
            settingsButton.onClick.AddListener(OpenSettings);
            quitButton.onClick.AddListener(QuitGame);
            playerNameWindow.PlayerNameConfirmed += EnableMenuActions;
            SetMenuActionsAvailable(playerNameWindow.IsPlayerNameConfirmed);
        }

        private void Start()
        {
            SetMenuActionsAvailable(playerNameWindow.IsPlayerNameConfirmed);
        }

        private void OnDestroy()
        {
            newGameButton.onClick.RemoveListener(StartNewGame);
            leaderboardsButton.onClick.RemoveListener(OpenLeaderboards);
            settingsButton.onClick.RemoveListener(OpenSettings);
            quitButton.onClick.RemoveListener(QuitGame);
            playerNameWindow.PlayerNameConfirmed -= EnableMenuActions;
        }

        private void StartNewGame()
        {
            if (playerNameWindow.IsPlayerNameConfirmed)
            {
                GameFlow.LoadGameplay();
            }
        }

        private void OpenLeaderboards()
        {
            if (playerNameWindow.IsPlayerNameConfirmed)
            {
                leaderboardScreen.Open();
            }
        }

        private void OpenSettings()
        {
            if (playerNameWindow.IsPlayerNameConfirmed)
            {
                settingsPanel.Open();
            }
        }

        private static void QuitGame()
        {
            GameFlow.QuitGame();
        }

        private void EnableMenuActions()
        {
            SetMenuActionsAvailable(true);
        }

        private void SetMenuActionsAvailable(bool isAvailable)
        {
            newGameButton.interactable = isAvailable;
            leaderboardsButton.interactable = isAvailable;
            settingsButton.interactable = isAvailable;
        }
    }
}
