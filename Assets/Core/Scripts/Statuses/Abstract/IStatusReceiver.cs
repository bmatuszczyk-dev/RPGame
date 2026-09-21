namespace RPGame.Core.Statuses
{
    public interface IStatusReceiver
    {
        bool ApplyStatus(StatusDefinition status, float duration, StatusContext context);
        bool HasStatus(StatusDefinition status);
        bool TryConsumeStatus(StatusDefinition status);
    }
}
