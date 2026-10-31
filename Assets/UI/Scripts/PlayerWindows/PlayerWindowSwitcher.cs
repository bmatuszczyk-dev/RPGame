using System.Collections.Generic;
using RPGame.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RPGame.UI.PlayerWindows
{
    public sealed class PlayerWindowSwitcher : MonoBehaviour
    {
        [SerializeField] private GameObject rootWindow;
        [SerializeField] private List<GameObject> windows = new();
        [SerializeField] private bool toggleRootWithTab = true;
        [SerializeField] private bool closeRootWithEscape = true;

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (toggleRootWithTab && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                ToggleRootWindow();
            }

            if (closeRootWithEscape && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseRootWindow();
            }
        }

        public void CloseRootWindow()
        {
            if (rootWindow != null)
            {
                SetRootWindowVisible(false);
            }
        }

        public void ToggleRootWindow()
        {
            if (rootWindow == null)
            {
                return;
            }

            SetRootWindowVisible(!rootWindow.activeSelf);
        }

        public void ShowWindow(GameObject window)
        {
            for (int i = 0; i < windows.Count; i++)
            {
                GameObject currentWindow = windows[i];
                if (currentWindow != null)
                {
                    currentWindow.SetActive(currentWindow == window);
                }
            }
        }

        private void SetRootWindowVisible(bool visible)
        {
            rootWindow.SetActive(visible);
            if (visible)
            {
                CursorController.ShowMenuCursor();
                return;
            }

            CursorController.ShowGameplayCursor();
        }
    }
}
