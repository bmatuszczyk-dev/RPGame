using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statistics;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    public sealed class FinisherBehavior : ISpellBehavior, IMarkConsumer
    {
        private readonly MarkStatusDefinition markDefinition;
        private readonly float healthThreshold;
        private readonly GameObject source;

        public FinisherBehavior(
            MarkStatusDefinition markDefinition,
            float healthThreshold,
            GameObject source)
        {
            this.markDefinition = markDefinition;
            this.healthThreshold = Mathf.Clamp01(healthThreshold);
            this.source = source;
        }

        public SpellBehaviorPhase Phase => SpellBehaviorPhase.PostResolve;

        public bool TryConsumeMark(SpellBehaviorContext context)
        {
            if (markDefinition == null
                || context.StatusReceiver == null
                || !context.StatusReceiver.TryConsumeStatus(markDefinition))
            {
                return false;
            }

            context.SetExecutionFlag(SpellExecutionFlag.MarkConsumed);
            return true;
        }

        public void Execute(SpellBehaviorContext context)
        {
            if (!context.HasExecutionFlag(SpellExecutionFlag.MarkConsumed)
                || context.Target == null)
            {
                return;
            }

            IStatisticsController statistics =
                context.Target.GetComponentInParent<IStatisticsController>();
            if (statistics == null
                || statistics.CurrentHealth <= 0f
                || statistics.MaxHealth <= 0f
                || statistics.CurrentHealth / statistics.MaxHealth > healthThreshold)
            {
                return;
            }

            IDamageable damageable = context.Target.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                return;
            }

            DamageResult result = damageable.ApplyDamage(new DamageData(
                new[]
                {
                    new PartialDamage(
                        statistics.CurrentHealth,
                        DamageType.Magical,
                        DamageElement.None)
                },
                source));
            context.AddResolveResult(result);
        }
    }
}
