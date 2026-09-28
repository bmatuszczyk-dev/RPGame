using System;
using System.Collections.Generic;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statistics;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [Serializable]
    public sealed class ResourceRecoveryBehavior :
        ISpellBehavior,
        IInitializableRuntimeSpellBehavior,
        IMarkConsumer
    {
        [SerializeField] private MarkStatusDefinition markDefinition;
        [SerializeField, Range(0f, 1f)] private float healthRecoveryPercent;

        [NonSerialized] private IStatisticsController casterStatistics;

        public ResourceRecoveryBehavior()
        {
        }

        public ResourceRecoveryBehavior(
            MarkStatusDefinition markDefinition,
            float healthRecoveryPercent,
            GameObject caster)
        {
            this.markDefinition = markDefinition;
            this.healthRecoveryPercent = Mathf.Clamp01(healthRecoveryPercent);
            Initialize(caster);
        }

        public SpellBehaviorPhase Phase => SpellBehaviorPhase.Effect;

        public bool Supports(Spell spell)
        {
            return spell is IProjectileCapability && markDefinition != null;
        }

        public void Initialize(GameObject caster)
        {
            casterStatistics = caster != null
                ? caster.GetComponentInParent<IStatisticsController>()
                : null;
            healthRecoveryPercent = Mathf.Clamp01(healthRecoveryPercent);
        }

        public bool TryConsumeMark(SpellBehaviorContext context)
        {
            if (context.HasExecutionFlag(SpellExecutionFlag.MarkConsumed))
            {
                return true;
            }

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
            if (casterStatistics == null
                || healthRecoveryPercent <= 0f
                || !context.HasExecutionFlag(SpellExecutionFlag.MarkConsumed)
                || !context.TryGetResolveResults(out IReadOnlyList<DamageResult> results))
            {
                return;
            }

            float totalAppliedDamage = 0f;
            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                totalAppliedDamage += results[resultIndex].AppliedAmount;
            }

            float recoveredHealth = totalAppliedDamage * healthRecoveryPercent;
            if (recoveredHealth <= 0f)
            {
                return;
            }

            casterStatistics.Heal(recoveredHealth);
        }
    }
}
