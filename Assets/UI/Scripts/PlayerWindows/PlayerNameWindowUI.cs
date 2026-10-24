using System;
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
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text errorText;

        private bool isSubmitting;
        private bool isDestroyed;
        private bool isChangingPlayerName;

        public bool IsPlayerNameConfirmed { get; private set; }
        public event Action PlayerNameConfirmed;
        public event Action PlayerNameChanged;

        public bool HandleEscape()
        {
            if (!isChangingPlayerName || isSubmitting)
            {
                return false;
            }

            playerNameContextWindow.SetActive(false);
            isChangingPlayerName = false;
            cancelButton.gameObject.SetActive(false);
            return true;
        }

        public void OpenForChange(string playerName)
        {
            if (isSubmitting)
            {
                return;
            }

            isChangingPlayerName = true;
            playerNameInputField.text = playerName == "Unavailable" ? string.Empty : playerName;
            HideError();
            playerNameContextWindow.SetActive(true);
            confirmButton.interactable = true;
            cancelButton.gameObject.SetActive(true);
            playerNameInputField.ActivateInputField();
        }

        private void Awake()
        {
            confirmButton.onClick.AddListener(ConfirmPlayerName);
            cancelButton.onClick.AddListener(CancelPlayerNameChange);
            HideError();
            cancelButton.gameObject.SetActive(false);
            playerNameContextWindow.SetActive(true);
            confirmButton.interactable = false;
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
                SetPlayerNameConfirmed();
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

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(CancelPlayerNameChange);
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
                isSubmitting = false;
                cancelButton.gameObject.SetActive(false);

                if (isChangingPlayerName)
                {
                    isChangingPlayerName = false;
                    PlayerNameChanged?.Invoke();
                    return;
                }

                SetPlayerNameConfirmed();
                return;
            }

            ShowWindow(result.ErrorMessage);
            isSubmitting = false;
        }

        private void ShowWindow(string errorMessage)
        {
            playerNameContextWindow.SetActive(true);
            confirmButton.interactable = true;
            cancelButton.gameObject.SetActive(isChangingPlayerName);

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

        private void CancelPlayerNameChange()
        {
            HandleEscape();
        }

        private void SetPlayerNameConfirmed()
        {
            if (IsPlayerNameConfirmed)
            {
                return;
            }

            IsPlayerNameConfirmed = true;
            PlayerNameConfirmed?.Invoke();
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
                && cancelButton != null
                && errorText != null;
        }
    }
}
