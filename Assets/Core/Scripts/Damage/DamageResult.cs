using RPGame.Core.Spells;

namespace RPGame.Core.Damage
{
    public readonly struct DamageResult : IResolveResult
    {
        private DamageResult(
            DamageData data,
            bool wasApplied,
            float appliedAmount,
            float previousHealth,
            float currentHealth,
            bool wasFatal)
        {
            Data = data;
            WasApplied = wasApplied;
            AppliedAmount = appliedAmount;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            WasFatal = wasFatal;
        }

        public DamageData Data { get; }
        public bool WasApplied { get; }
        public float AppliedAmount { get; }
        public float PreviousHealth { get; }
        public float CurrentHealth { get; }
        public bool WasFatal { get; }

        public static DamageResult Applied(
            DamageData data,
            float appliedAmount,
            float previousHealth,
            float currentHealth,
            bool wasFatal)
        {
            return new DamageResult(data, true, appliedAmount, previousHealth, currentHealth, wasFatal);
        }

        public static DamageResult Ignored(DamageData data, float currentHealth)
        {
            return new DamageResult(data, false, 0f, currentHealth, currentHealth, false);
        }
    }
}
