using RPGame.Core.Statuses;

namespace RPGame.Core.Spells
{
    public interface IStatusApplicationModifier : IRuntimeSpellBehavior
    {
        StatusContext ModifyStatusApplication(
            StatusDefinition status,
            StatusContext context);
    }
}
