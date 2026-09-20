namespace RPGame.Core.Spells
{
    public enum RuntimeSpellBehaviorPhase
    {
        PreResolve,
        MarkConsumption,
        PrimaryEffect,
        PostEffect,
        MarkProgress,
        Aftermath
    }
}
