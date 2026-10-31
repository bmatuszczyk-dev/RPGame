using UnityEngine;

namespace RPGame.UI
{
    public static class CursorController
    {
        private static bool isDrawingCursorActive;
        private static CursorLockMode previousCursorLockState;
        private static bool previousCursorVisible;

        // TODO Zmien nazwe na ShowCursor
        public static void ShowMenuCursor()
        {
            isDrawingCursorActive = false;
            SetCursor(CursorLockMode.None, true);
        }

        // TODO: Zmien nazwe na HideCursor 
        public static void ShowGameplayCursor()
        {
            isDrawingCursorActive = false;
            SetCursor(CursorLockMode.Locked, false);
        }

        public static void SetDrawingCursorActive(bool active)
        {
            if (isDrawingCursorActive == active)
            {
                return;
            }

            isDrawingCursorActive = active;
            if (active)
            {
                previousCursorLockState = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                SetCursor(CursorLockMode.None, true);
                return;
            }

            SetCursor(previousCursorLockState, previousCursorVisible);
        }

        private static void SetCursor(CursorLockMode lockState, bool visible)
        {
            Cursor.lockState = lockState;
            Cursor.visible = visible;
        }
    }
}
