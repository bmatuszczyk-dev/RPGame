using UnityEngine;
using UnityEngine.SceneManagement;

namespace RPGame.UI
{
    public static class GameFlow
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string GameplaySceneName = "TestScene";

        private static bool isTransitioning;

        public static bool LoadGameplay()
        {
            return LoadScene(GameplaySceneName, isGameplayScene: true);
        }

        public static bool LoadMainMenu()
        {
            return LoadScene(MainMenuSceneName, isGameplayScene: false);
        }

        public static bool RestartGameplayScene(int sceneBuildIndex)
        {
            return LoadScene(sceneBuildIndex, isGameplayScene: true);
        }

        public static void ShowMenuCursor()
        {
            CursorController.ShowMenuCursor();
        }

        public static void QuitGame()
        {
            Application.Quit();
        }

        private static bool LoadScene(string sceneName, bool isGameplayScene)
        {
            if (isTransitioning)
            {
                return false;
            }

            isTransitioning = true;
            SetCursorForScene(isGameplayScene);
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            operation.completed += _ => isTransitioning = false;
            return true;
        }

        private static bool LoadScene(int sceneBuildIndex, bool isGameplayScene)
        {
            if (isTransitioning)
            {
                return false;
            }

            isTransitioning = true;
            SetCursorForScene(isGameplayScene);
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneBuildIndex, LoadSceneMode.Single);
            operation.completed += _ => isTransitioning = false;
            return true;
        }

        private static void SetCursorForScene(bool isGameplayScene)
        {
            if (isGameplayScene)
            {
                CursorController.ShowGameplayCursor();
                return;
            }

            CursorController.ShowMenuCursor();
        }
    }
}
