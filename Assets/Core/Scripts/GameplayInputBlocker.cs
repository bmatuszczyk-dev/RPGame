namespace RPGame.Core
{
    public static class GameplayInputBlocker
    {
        public static bool IsBlocked { get; private set; }

        public static void SetBlocked(bool blocked)
        {
            IsBlocked = blocked;
        }
    }
}
