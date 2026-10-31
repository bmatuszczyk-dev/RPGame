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
                duration,
                new StatusContext(new StatusSourceId(nameof(ManaSurgeBehavior)), context.Source));
        }
    }
}
