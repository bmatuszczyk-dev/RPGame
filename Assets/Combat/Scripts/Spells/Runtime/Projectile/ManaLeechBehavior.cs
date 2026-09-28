using System;
using System.Collections.Generic;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statistics;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [Serializable]
    public sealed class ManaLeechBehavior : ISpellBehavior, IInitializableRuntimeSpellBehavior
    {
        [SerializeField, Range(0f, 1f)] private float manaLeechPercent;

        [NonSerialized] private IStatisticsController casterStatistics;

        public ManaLeechBehavior()
        {
        }

        public ManaLeechBehavior(float manaLeechPercent, GameObject caster)
        {
            this.manaLeechPercent = Mathf.Clamp01(manaLeechPercent);
            Initialize(caster);
        }

        public SpellBehaviorPhase Phase => SpellBehaviorPhase.Effect;

        public bool Supports(Spell spell)
        {
            return spell is IProjectileCapability;
        }

        public void Initialize(GameObject caster)
        {
            casterStatistics = caster != null
                ? caster.GetComponentInParent<IStatisticsController>()
                : null;
            manaLeechPercent = Mathf.Clamp01(manaLeechPercent);
        }

        public void Execute(SpellBehaviorContext context)
        {
            if (casterStatistics == null
                || manaLeechPercent <= 0f
                || !context.TryGetResolveResults(out IReadOnlyList<DamageResult> results))
            {
                return;
            }

            float totalAppliedDamage = 0f;
            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                totalAppliedDamage += results[resultIndex].AppliedAmount;
            }

            float restoredMana = totalAppliedDamage * manaLeechPercent;
            if (restoredMana <= 0f)
            {
                return;
            }

            casterStatistics.RestoreMana(restoredMana);
        }
    }
}
