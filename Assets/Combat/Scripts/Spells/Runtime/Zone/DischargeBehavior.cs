using System;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statistics.Attributes;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [Serializable]
    public sealed class DischargeBehavior :
        ISpellBehavior,
        IInitializableRuntimeSpellBehavior,
        IMarkConsumer
    {
        [SerializeField] private MarkStatusDefinition markDefinition;
        [SerializeField, Min(0f)] private float baseDamage = 10f;
        [SerializeField, Min(0f)] private float powerScaling = 0.15f;

        [NonSerialized] private ICharacterAttributes casterAttributes;
        [NonSerialized] private float damageMultiplier = 1f;

        public DischargeBehavior()
        {
        }

        public DischargeBehavior(
            MarkStatusDefinition markDefinition,
            float baseDamage,
            float powerScaling,
            GameObject caster)
        {
            this.markDefinition = markDefinition;
            this.baseDamage = Mathf.Max(0f, baseDamage);
            this.powerScaling = Mathf.Max(0f, powerScaling);
            Initialize(caster);
        }

        public SpellBehaviorPhase Phase => SpellBehaviorPhase.PostResolve;

        public void AddBaseDamage(float damageBonus)
        {
            baseDamage = Mathf.Max(0f, baseDamage + Mathf.Max(0f, damageBonus));
        }

        public void AddDamageMultiplierBonus(float bonus)
        {
            damageMultiplier = Mathf.Max(1f, damageMultiplier + Mathf.Max(0f, bonus));
        }

        public bool Supports(Spell spell)
        {
            return spell is LightningZoneSpell && markDefinition != null;
        }

        public void Initialize(GameObject caster)
        {
            casterAttributes = caster != null
                ? caster.GetComponentInParent<ICharacterAttributes>()
                : null;
            baseDamage = Mathf.Max(0f, baseDamage);
            powerScaling = Mathf.Max(0f, powerScaling);
            damageMultiplier = Mathf.Max(1f, damageMultiplier);
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
            if (!context.HasExecutionFlag(SpellExecutionFlag.MarkConsumed)
                || context.Target == null)
            {
                return;
            }

            float damage = (baseDamage + SpellDamageCalculator.CalculatePowerDamageBonus(
                casterAttributes != null ? casterAttributes.Power : 0,
                powerScaling)) * damageMultiplier;
            if (damage <= 0f)
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
                        damage,
                        DamageType.Magical,
                        DamageElement.Thunder)
                },
                context.Source,
                DamageApplicationType.Direct,
                DamageOrigin.Spell));
            context.AddResolveResult(result);
        }
    }
}
