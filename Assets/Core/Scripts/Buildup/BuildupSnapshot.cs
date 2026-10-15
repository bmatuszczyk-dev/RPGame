namespace RPGame.Core.Buildup
{
    public readonly struct BuildupSnapshot
    {
        public BuildupSnapshot(BuildupDefinition definition, float currentAmount, float threshold)
        {
            Definition = definition;
            CurrentAmount = currentAmount;
            Threshold = threshold;
        }

        public BuildupDefinition Definition { get; }
        public float CurrentAmount { get; }
        public float Threshold { get; }
    }
}
