using RPGame.Leaderboard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RPGame.UI.PlayerWindows
{
    public sealed class PlayerNameWindowUI : MonoBehaviour
    {
        [SerializeField] private GameObject playerNameContextWindow;
        [SerializeField] private TMP_InputField playerNameInputField;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button quitButton;

        private bool isSubmitting;
        private bool isDestroyed;

        private void Awake()
        {
            GameFlow.ShowMenuCursor();
            confirmButton.onClick.AddListener(ConfirmPlayerName);
            HideError();
            playerNameContextWindow.SetActive(true);
            confirmButton.interactable = false;
            SetNewGameAvailable(false);

            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(StartNewGame);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        private async void Start()
        {
            PlayerNameOperationResult result = await PlayerNameService.GetCurrentPlayerNameAsync();
            if (!HasUiReferences())
            {
                return;
            }

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.PlayerName))
            {
                playerNameContextWindow.SetActive(false);
                SetNewGameAvailable(true);
                return;
            }

            ShowWindow(result.ErrorMessage);
        }

        private void OnDestroy()
        {
            isDestroyed = true;
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(ConfirmPlayerName);
            }

            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(StartNewGame);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
            }
        }

        private async void ConfirmPlayerName()
        {
            if (isSubmitting)
            {
                return;
            }

            isSubmitting = true;
            confirmButton.interactable = false;
            HideError();

            PlayerNameOperationResult result = await PlayerNameService.SetPlayerNameAsync(playerNameInputField.text);
            if (!HasUiReferences())
            {
                return;
            }

            if (result.IsSuccess)
            {
                playerNameContextWindow.SetActive(false);
                SetNewGameAvailable(true);
                isSubmitting = false;
                return;
            }

            ShowWindow(result.ErrorMessage);
            isSubmitting = false;
        }

        private void ShowWindow(string errorMessage)
        {
            playerNameContextWindow.SetActive(true);
            confirmButton.interactable = true;
            SetNewGameAvailable(false);

            if (string.IsNullOrEmpty(errorMessage))
            {
                HideError();
            }
            else
            {
                errorText.text = errorMessage;
                errorText.gameObject.SetActive(true);
            }

            playerNameInputField.ActivateInputField();
        }

        private void StartNewGame()
        {
            if (playerNameContextWindow == null || playerNameContextWindow.activeInHierarchy)
            {
                return;
            }

            GameFlow.LoadGameplay();
        }

        private static void QuitGame()
        {
            GameFlow.QuitGame();
        }

        private void SetNewGameAvailable(bool isAvailable)
        {
            if (newGameButton != null)
            {
                newGameButton.interactable = isAvailable;
            }
        }

        private void HideError()
        {
            errorText.text = string.Empty;
            errorText.gameObject.SetActive(false);
        }

        private bool HasUiReferences()
        {
            return !isDestroyed
                && isActiveAndEnabled
                && playerNameContextWindow != null
                && playerNameInputField != null
                && confirmButton != null
                && errorText != null;
        }
    }
}
