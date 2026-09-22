using System;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statistics;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [Serializable]
    public sealed class FinisherBehavior :
        ISpellBehavior,
        IInitializableRuntimeSpellBehavior,
        IMarkConsumer
    {
        [SerializeField] private MarkStatusDefinition markDefinition;
        [SerializeField, Range(0f, 1f)] private float healthThreshold = 0.2f;

        [NonSerialized] private GameObject caster;

        public FinisherBehavior()
        {
        }

        public FinisherBehavior(
            MarkStatusDefinition markDefinition,
            float healthThreshold,
            GameObject caster)
        {
            this.markDefinition = markDefinition;
            this.healthThreshold = Mathf.Clamp01(healthThreshold);
            Initialize(caster);
        }

        public SpellBehaviorPhase Phase => SpellBehaviorPhase.PostResolve;

        public bool Supports(Spell spell)
        {
            return spell is IProjectileCapability && markDefinition != null;
        }

        public void Initialize(GameObject caster)
        {
            this.caster = caster;
            healthThreshold = Mathf.Clamp01(healthThreshold);
        }

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
                caster));
            context.AddResolveResult(result);
        }
    }
}
