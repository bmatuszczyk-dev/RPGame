using System;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [Serializable]
    public sealed class ConcussionOnImpactCollisionBehavior :
        IKnockbackCollisionHandler,
        IInitializableRuntimeSpellBehavior
    {
        private static readonly StatusSourceId ConcussionSourceId =
            new(nameof(ConcussionOnImpactCollisionBehavior));

        [SerializeField] private StunStatusDefinition stunStatus;
        [SerializeField, Min(0f)] private float stunDuration = 1f;

        [NonSerialized] private GameObject source;

        public ConcussionOnImpactCollisionBehavior()
        {
        }

        public ConcussionOnImpactCollisionBehavior(
            StunStatusDefinition stunStatus,
            float stunDuration,
            GameObject source = null)
        {
            this.stunStatus = stunStatus;
            this.stunDuration = Mathf.Max(0f, stunDuration);
            Initialize(source);
        }

        public bool Supports(Spell spell)
        {
            return spell is WaveSpell;
        }

        public void Initialize(GameObject caster)
        {
            source = caster;
            stunDuration = Mathf.Max(0f, stunDuration);
        }

        public void OnKnockbackCollision(GameObject target, Collider obstacle, Vector3 point)
        {
            if (target == null || stunStatus == null)
            {
                return;
            }

            IStatusReceiver statusReceiver = target.GetComponentInParent<IStatusReceiver>();
            if (statusReceiver == null)
            {
                return;
            }

            statusReceiver.ApplyStatus(
                stunStatus,
                stunDuration,
                new StatusContext(ConcussionSourceId, source));
        }
    }
}
