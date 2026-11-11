using System;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [Serializable]
    public sealed class ManaSurgeBehavior : ISpellBehavior, IInitializableRuntimeSpellBehavior
    {
        [SerializeField] private ManaSurgeStatusDefinition manaSurgeStatus;
        [SerializeField, Min(0f)] private float duration = 5f;

        [NonSerialized] private IStatusReceiver casterStatusReceiver;
        [NonSerialized] private float manaRegenerationMultiplier = 1f;
        [NonSerialized] private float durationMultiplier = 1f;

        public ManaSurgeBehavior()
        {
        }

        public ManaSurgeBehavior(ManaSurgeStatusDefinition manaSurgeStatus, float duration, GameObject caster)
        {
            this.manaSurgeStatus = manaSurgeStatus;
            this.duration = Mathf.Max(0f, duration);
            Initialize(caster);
        }

        public SpellBehaviorPhase Phase => SpellBehaviorPhase.PostResolve;

        public void AddManaRegenerationMultiplierBonus(float bonus)
        {
            manaRegenerationMultiplier = Mathf.Max(
                1f,
                manaRegenerationMultiplier + Mathf.Max(0f, bonus));
        }

        public void AddDurationMultiplierBonus(float bonus)
        {
            durationMultiplier = Mathf.Max(1f, durationMultiplier + Mathf.Max(0f, bonus));
        }

        public bool Supports(Spell spell)
        {
            return spell is LightningZoneSpell && manaSurgeStatus != null;
        }

        public void Initialize(GameObject caster)
        {
            casterStatusReceiver = caster != null
                ? caster.GetComponentInParent<IStatusReceiver>()
                : null;
            duration = Mathf.Max(0f, duration);
            manaRegenerationMultiplier = Mathf.Max(1f, manaRegenerationMultiplier);
            durationMultiplier = Mathf.Max(1f, durationMultiplier);
        }

        public void Execute(SpellBehaviorContext context)
        {
            if (!context.HasExecutionFlag(SpellExecutionFlag.StunApplied)
                || manaSurgeStatus == null
                || duration <= 0f
                || casterStatusReceiver == null)
            {
                return;
            }

            casterStatusReceiver.ApplyStatus(
                manaSurgeStatus,
                duration * durationMultiplier,
                new StatusContext(
                    new StatusSourceId(nameof(ManaSurgeBehavior)),
                    context.Source,
                    valueOverride: manaSurgeStatus.ManaRegenerationPerStack
                        * manaRegenerationMultiplier));
        }
    }
}
