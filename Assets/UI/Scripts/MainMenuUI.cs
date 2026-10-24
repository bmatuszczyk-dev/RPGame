using RPGame.UI.Leaderboard;
using RPGame.UI.PlayerWindows;
using UnityEngine;
using UnityEngine.UI;

namespace RPGame.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private PlayerNameWindowUI playerNameWindow;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button leaderboardsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private LeaderboardScreen leaderboardScreen;

        private void Awake()
        {
            GameFlow.ShowMenuCursor();
            newGameButton.onClick.AddListener(StartNewGame);
            leaderboardsButton.onClick.AddListener(OpenLeaderboards);
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
        }
    }
}
