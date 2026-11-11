using System;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [Serializable]
    public sealed class MomentumBehavior :
        IKnockbackCompletedHandler,
        IInitializableRuntimeSpellBehavior
    {
        private static readonly StatusSourceId MomentumSourceId =
            new(nameof(MomentumBehavior));

        [SerializeField] private SlowStatusDefinition slowStatus;
        [SerializeField, Min(0f)] private float slowDuration = 1f;

        [NonSerialized] private GameObject source;

        public MomentumBehavior()
        {
        }

        public MomentumBehavior(
            SlowStatusDefinition slowStatus,
            float slowDuration,
            GameObject source = null)
        {
            this.slowStatus = slowStatus;
            this.slowDuration = Mathf.Max(0f, slowDuration);
            Initialize(source);
        }

        public bool Supports(Spell spell)
        {
            return spell is WaveSpell;
        }

        public void Initialize(GameObject caster)
        {
            source = caster;
            slowDuration = Mathf.Max(0f, slowDuration);
        }

        public void OnKnockbackCompleted(GameObject target)
        {
            if (target == null || slowStatus == null)
            {
                return;
            }

            IStatusReceiver statusReceiver = target.GetComponentInParent<IStatusReceiver>();
            if (statusReceiver == null)
            {
                return;
            }

            statusReceiver.ApplyStatus(
                slowStatus,
                slowDuration,
                new StatusContext(MomentumSourceId, source));
        }
    }
}
