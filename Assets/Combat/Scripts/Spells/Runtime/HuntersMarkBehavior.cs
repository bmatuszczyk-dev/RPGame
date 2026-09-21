using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    public sealed class HuntersMarkBehavior : ISpellBehavior, IMarkConsumer
    {
        private readonly MarkStatusDefinition markDefinition;
        private readonly PartialDamage bonusDamage;
        private readonly GameObject source;

        public HuntersMarkBehavior(
            MarkStatusDefinition markDefinition,
            PartialDamage bonusDamage,
            GameObject source)
        {
            this.markDefinition = markDefinition;
            this.bonusDamage = bonusDamage;
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

            context.AddExecutionState(new MarkActivationState());
            return true;
        }

        public void Execute(SpellBehaviorContext context)
        {
            if (!context.TryGetExecutionState(out MarkActivationState state)
                || !state.TryConsumePayoff()
                || context.Target == null)
            {
                return;
            }

            IDamageable damageable = context.Target.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                return;
            }

            DamageResult result = damageable.ApplyDamage(new DamageData(
                new[] { bonusDamage },
                source));
            context.AddResolveResult(result);
        }
    }
}
