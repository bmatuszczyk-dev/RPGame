using RPGame.Core.Statuses;

namespace RPGame.Core.Buildup
{
    public interface IBuildupReceiver
    {
        void AddBuildup(BuildupDefinition definition, float amount, StatusContext context);
    }
}
