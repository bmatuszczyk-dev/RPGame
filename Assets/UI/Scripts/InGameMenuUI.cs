using RPGame.Core;
using RPGame.Encounter;
using RPGame.UI.PlayerWindows;
using RPGame.UI.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RPGame.UI
{
    public sealed class InGameMenuUI : MonoBehaviour
    {
        [SerializeField] private EncounterRuntime runtime;
        [SerializeField] private PlayerWindowSwitcher playerWindowSwitcher;
        [SerializeField] private SettingsPanelUI settingsPanel;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject endRunConfirmationPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button endRunButton;
        [SerializeField] private Button confirmEndRunButton;
        [SerializeField] private Button cancelEndRunButton;

        private float previousTimeScale = 1f;
        private bool isPaused;
        private bool isEndingRun;
        private int settingsClosedFrame = -1;

        private void Awake()
        {
            SetActive(menuPanel, false);
            SetActive(endRunConfirmationPanel, false);
            resumeButton.onClick.AddListener(Resume);
            settingsButton.onClick.AddListener(OpenSettings);
            endRunButton.onClick.AddListener(OpenEndRunConfirmation);
            confirmEndRunButton.onClick.AddListener(ConfirmEndRun);
            cancelEndRunButton.onClick.AddListener(CancelEndRun);
            settingsPanel.Closed += HandleSettingsClosed;
        }

        private void OnDestroy()
        {
            resumeButton.onClick.RemoveListener(Resume);
            settingsButton.onClick.RemoveListener(OpenSettings);
            endRunButton.onClick.RemoveListener(OpenEndRunConfirmation);
            confirmEndRunButton.onClick.RemoveListener(ConfirmEndRun);
            cancelEndRunButton.onClick.RemoveListener(CancelEndRun);
            settingsPanel.Closed -= HandleSettingsClosed;

            if (isPaused)
            {
                RestoreGameplay();
            }
        }

        private void Update()
        {
            if (IsEncounterEnded() || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            if (settingsPanel.gameObject.activeSelf || settingsClosedFrame == Time.frameCount)
            {
                return;
            }

            if (endRunConfirmationPanel.activeSelf)
            {
                CancelEndRun();
                return;
            }

            if (menuPanel.activeSelf)
            {
                Resume();
                return;
            }

            OpenMenu();
        }

        private void OpenMenu()
        {
            if (isPaused || IsEncounterEnded())
            {
                return;
            }

            playerWindowSwitcher.CloseRootWindow();
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            GameplayInputBlocker.SetBlocked(true);
            CursorController.ShowMenuCursor();
            isPaused = true;
            SetActive(menuPanel, true);
        }

        private void Resume()
        {
            if (!isPaused || isEndingRun)
            {
                return;
            }

            SetActive(endRunConfirmationPanel, false);
            SetActive(menuPanel, false);
            RestoreGameplay();
        }

        private void OpenSettings()
        {
            if (!isPaused || isEndingRun)
            {
                return;
            }

            settingsPanel.Open(menuPanel);
        }

        private void OpenEndRunConfirmation()
        {
            if (!isPaused || isEndingRun)
            {
                return;
            }

            SetActive(menuPanel, false);
            SetActive(endRunConfirmationPanel, true);
        }

        private void CancelEndRun()
        {
            if (!endRunConfirmationPanel.activeSelf || isEndingRun)
            {
                return;
            }

            SetActive(endRunConfirmationPanel, false);
            SetActive(menuPanel, true);
        }

        private void ConfirmEndRun()
        {
            if (!endRunConfirmationPanel.activeSelf || isEndingRun)
            {
                return;
            }

            isEndingRun = true;
            confirmEndRunButton.interactable = false;
            RestoreGameplay();
            GameFlow.LoadMainMenu();
        }

        private void HandleSettingsClosed()
        {
            settingsClosedFrame = Time.frameCount;
        }

        private void RestoreGameplay()
        {
            Time.timeScale = previousTimeScale;
            GameplayInputBlocker.SetBlocked(false);
            CursorController.ShowGameplayCursor();
            isPaused = false;
        }

        private bool IsEncounterEnded()
        {
            return runtime != null
                && runtime.EncounterController != null
                && runtime.EncounterController.State == EncounterState.Ended;
        }

        private static void SetActive(GameObject panel, bool active)
        {
            if (panel != null && panel.activeSelf != active)
            {
                panel.SetActive(active);
            }
        }
    }
}
