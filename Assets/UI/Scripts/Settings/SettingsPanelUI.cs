using RPGame.Core.Settings;
using RPGame.Leaderboard;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using RPGame.UI.PlayerWindows;
using System;

namespace RPGame.UI.Settings
{
    public sealed class SettingsPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private PlayerNameWindowUI playerNameWindow;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider soundVolumeSlider;
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private Slider drawingSensitivitySlider;
        [SerializeField] private TMP_Text masterVolumeValueText;
        [SerializeField] private TMP_Text musicVolumeValueText;
        [SerializeField] private TMP_Text soundVolumeValueText;
        [SerializeField] private TMP_Text mouseSensitivityValueText;
        [SerializeField] private TMP_Text drawingSensitivityValueText;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private Button changeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button backButton;

        private GameSettingsValues savedValues;
        private GameObject returnPanel;
        private bool isDestroyed;

        public event Action Closed;

        public void Open()
        {
            Open(mainMenuPanel);
        }

        public void Open(GameObject panelToRestore)
        {
            savedValues = GameSettings.Load();
            SetDraft(savedValues);
            returnPanel = panelToRestore != null ? panelToRestore : mainMenuPanel;
            returnPanel.SetActive(false);
            gameObject.SetActive(true);
            RefreshPlayerName();
        }

        private void Awake()
        {
            changeButton.onClick.AddListener(ShowPlayerNameWindow);
            saveButton.onClick.AddListener(Save);
            backButton.onClick.AddListener(CloseWithoutSaving);
            masterVolumeSlider.onValueChanged.AddListener(HandleDraftChanged);
            musicVolumeSlider.onValueChanged.AddListener(HandleDraftChanged);
            soundVolumeSlider.onValueChanged.AddListener(HandleDraftChanged);
            mouseSensitivitySlider.onValueChanged.AddListener(HandleDraftChanged);
            drawingSensitivitySlider.onValueChanged.AddListener(HandleDraftChanged);
            playerNameWindow.PlayerNameChanged += RefreshPlayerName;
        }

        private void OnDestroy()
        {
            isDestroyed = true;
            changeButton.onClick.RemoveListener(ShowPlayerNameWindow);
            saveButton.onClick.RemoveListener(Save);
            backButton.onClick.RemoveListener(CloseWithoutSaving);
            masterVolumeSlider.onValueChanged.RemoveListener(HandleDraftChanged);
            musicVolumeSlider.onValueChanged.RemoveListener(HandleDraftChanged);
            soundVolumeSlider.onValueChanged.RemoveListener(HandleDraftChanged);
            mouseSensitivitySlider.onValueChanged.RemoveListener(HandleDraftChanged);
            drawingSensitivitySlider.onValueChanged.RemoveListener(HandleDraftChanged);
            playerNameWindow.PlayerNameChanged -= RefreshPlayerName;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (!playerNameWindow.HandleEscape())
                {
                    CloseWithoutSaving();
                }
            }
        }

        private void Save()
        {
            GameSettings.SaveAndApply(GetDraft());
            Close();
        }

        private void CloseWithoutSaving()
        {
            SetDraft(savedValues);
            Close();
        }

        private void Close()
        {
            gameObject.SetActive(false);
            if (returnPanel != null)
            {
                returnPanel.SetActive(true);
            }

            returnPanel = mainMenuPanel;
            Closed?.Invoke();
        }

        private void ShowPlayerNameWindow()
        {
            playerNameWindow.OpenForChange(playerNameText.text);
        }

        private async void RefreshPlayerName()
        {
            if (isDestroyed)
            {
                return;
            }

            PlayerNameOperationResult result = await PlayerNameService.GetCurrentPlayerNameAsync();
            if (isDestroyed || !isActiveAndEnabled)
            {
                return;
            }

            playerNameText.text = result.IsSuccess && !string.IsNullOrWhiteSpace(result.PlayerName)
                ? result.PlayerName
                : "Unavailable";
        }

        private void SetDraft(GameSettingsValues values)
        {
            masterVolumeSlider.SetValueWithoutNotify(values.MasterVolume);
            musicVolumeSlider.SetValueWithoutNotify(values.MusicVolume);
            soundVolumeSlider.SetValueWithoutNotify(values.SoundVolume);
            mouseSensitivitySlider.SetValueWithoutNotify(values.MouseSensitivity);
            drawingSensitivitySlider.SetValueWithoutNotify(values.DrawingSensitivity);
            UpdateDraftUi();
        }

        private GameSettingsValues GetDraft()
        {
            return new GameSettingsValues(
                masterVolumeSlider.value,
                musicVolumeSlider.value,
                soundVolumeSlider.value,
                mouseSensitivitySlider.value,
                drawingSensitivitySlider.value);
        }

        private void HandleDraftChanged(float _)
        {
            drawingSensitivitySlider.SetValueWithoutNotify(Mathf.Round(drawingSensitivitySlider.value / 0.05f) * 0.05f);
            UpdateDraftUi();
        }

        private void UpdateDraftUi()
        {
            masterVolumeValueText.text = $"{masterVolumeSlider.value:0}%";
            musicVolumeValueText.text = $"{musicVolumeSlider.value:0}%";
            soundVolumeValueText.text = $"{soundVolumeSlider.value:0}%";
            mouseSensitivityValueText.text = mouseSensitivitySlider.value.ToString("0.00");
            drawingSensitivityValueText.text = drawingSensitivitySlider.value.ToString("0.00");

            GameSettingsValues draft = GetDraft();
            saveButton.interactable = !Mathf.Approximately(draft.MasterVolume, savedValues.MasterVolume)
                || !Mathf.Approximately(draft.MusicVolume, savedValues.MusicVolume)
                || !Mathf.Approximately(draft.SoundVolume, savedValues.SoundVolume)
                || !Mathf.Approximately(draft.MouseSensitivity, savedValues.MouseSensitivity)
                || !Mathf.Approximately(draft.DrawingSensitivity, savedValues.DrawingSensitivity);
        }
    }
}
