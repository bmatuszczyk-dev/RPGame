namespace RPGame.Core.Buildup
{
    public readonly struct BuildupSnapshot
    {
        public BuildupSnapshot(
            BuildupDefinition definition,
            float currentAmount,
            float threshold,
            BuildupState state)
        {
            Definition = definition;
            CurrentAmount = currentAmount;
            Threshold = threshold;
            State = state;
        }

        public BuildupDefinition Definition { get; }
        public float CurrentAmount { get; }
        public float Threshold { get; }
        public BuildupState State { get; }
    }
}
