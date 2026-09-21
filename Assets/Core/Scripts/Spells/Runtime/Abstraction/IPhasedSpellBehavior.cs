namespace RPGame.Core.Spells
{
    public interface IPhasedSpellBehavior : IRuntimeSpellBehavior
    {
        SpellBehaviorPhase Phase { get; }
    }
}
