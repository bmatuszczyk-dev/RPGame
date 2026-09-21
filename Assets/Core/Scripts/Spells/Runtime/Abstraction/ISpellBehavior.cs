namespace RPGame.Core.Spells
{
    public interface ISpellBehavior : IPhasedSpellBehavior
    {
        void Execute(SpellBehaviorContext context);
    }
}
