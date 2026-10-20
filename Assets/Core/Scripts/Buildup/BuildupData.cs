namespace RPGame.Core.Buildup
{
    public sealed class BuildupData
    {
        public BuildupData(BuildupDefinition definition, float amount)
        {
            Definition = definition;
            Amount = amount;
        }

        public BuildupDefinition Definition { get; }
        public float Amount { get; }
    }
}
