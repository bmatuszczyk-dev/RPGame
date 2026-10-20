using RPGame.Core.Statistics;

namespace RPGame.Enemies
{
    public sealed class ContinuousHeal
    {
        private readonly float healPerSecond;
        private readonly float manaCostPerSecond;

        public ContinuousHeal(float healPerSecond, float manaCostPerSecond)
        {
            this.healPerSecond = healPerSecond;
            this.manaCostPerSecond = manaCostPerSecond;
        }

        public bool TryHeal(
            IStatisticsController healerStatistics,
            HealerTarget target,
            float deltaTime)
        {
            if (healerStatistics == null || target == null || !target.IsValid || deltaTime <= 0f)
            {
                return false;
            }

            float manaCost = manaCostPerSecond * deltaTime;
            if (!healerStatistics.TrySpendMana(manaCost))
            {
                return false;
            }

            target.Statistics.Heal(healPerSecond * deltaTime);
            return true;
        }
    }
}
