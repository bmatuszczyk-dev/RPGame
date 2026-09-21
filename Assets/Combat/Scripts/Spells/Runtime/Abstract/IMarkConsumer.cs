using RPGame.Core.Spells;

namespace RPGame.Combat.Spells
{
    public interface IMarkConsumer : IRuntimeSpellBehavior
    {
        bool TryConsumeMark(SpellBehaviorContext context);
    }
}
