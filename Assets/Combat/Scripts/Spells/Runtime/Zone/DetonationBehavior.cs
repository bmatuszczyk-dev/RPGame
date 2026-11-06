using System;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [Serializable]
    public sealed class DetonationBehavior :
        IInitializableRuntimeSpellBehavior,
        IStatusApplicationModifier
    {
        [SerializeField, Min(0f)] private float baseRadius = 3f;

        [NonSerialized] private float radiusMultiplier = 1f;
        [NonSerialized] private float damageMultiplier = 1f;

        public bool Supports(Spell spell)
        {
            return spell is FireZoneSpell;
        }

        public void Initialize(GameObject caster)
        {
            baseRadius = Mathf.Max(0f, baseRadius);
            radiusMultiplier = Mathf.Max(1f, radiusMultiplier);
            damageMultiplier = Mathf.Max(1f, damageMultiplier);
        }

        public void AddRadiusMultiplierBonus(float bonus)
        {
            radiusMultiplier = Mathf.Max(1f, radiusMultiplier + Mathf.Max(0f, bonus));
        }

        public void AddDamageMultiplierBonus(float bonus)
        {
            damageMultiplier = Mathf.Max(1f, damageMultiplier + Mathf.Max(0f, bonus));
        }

        public StatusContext ModifyStatusApplication(
            StatusDefinition status,
            StatusContext context)
        {
            return status is BurningStatusDefinition
                ? context.WithApplicationData(new DetonationApplication(
                    baseRadius * radiusMultiplier,
                    damageMultiplier))
                : context;
        }
    }

    internal sealed class DetonationApplication : IStatusOwnerDeathHandler
    {
        public DetonationApplication(float radius, float damageMultiplier)
        {
            Radius = Mathf.Max(0f, radius);
            DamageMultiplier = Mathf.Max(1f, damageMultiplier);
        }

        public float Radius { get; }
        public float DamageMultiplier { get; }

        public void HandleOwnerDied(StatusTarget target, StatusInstance instance)
        {
            if (target.Damageable is not Component originComponent
                || instance.Definition is not BurningStatusDefinition burn)
            {
                return;
            }

            float damage = burn.BaseDamagePerSecond
                * Mathf.Max(1, instance.CurrentStacks)
                * Mathf.Max(0f, instance.RemainingDuration)
                * EffectSourceValueResolver.GetMultiplier(
                    instance.LastApplicationContext.Source,
                    EffectStat.PeriodicSpellDamage)
                * DamageMultiplier;
            if (damage <= 0f)
            {
                return;
            }

            DetonationExplosionQueue.Enqueue(new ExplosionData(
                originComponent.transform.position,
                Radius,
                damage,
                instance.LastApplicationContext.Source,
                originComponent.GetEntityId()));
        }
    }
}
