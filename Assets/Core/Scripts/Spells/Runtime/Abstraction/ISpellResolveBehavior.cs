namespace RPGame.Core.Spells
{
    public interface ISpellResolveBehavior : IPhasedSpellBehavior
    {
        bool Resolve(SpellBehaviorContext context);
    }
}
