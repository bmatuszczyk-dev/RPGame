namespace RPGame.Core.Statuses
{
    public interface IStatusOwnerDeathHandler
    {
        void HandleOwnerDied(StatusTarget target, StatusInstance instance);
    }
}
