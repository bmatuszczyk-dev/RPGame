using System;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [Serializable]
    public sealed class HeavyConcussionBehavior :
        IKnockbackCollisionHandler,
        IInitializableRuntimeSpellBehavior
    {
        private static readonly StatusSourceId HeavyConcussionSourceId =
            new(nameof(HeavyConcussionBehavior));

        [SerializeField] private StunStatusDefinition stunStatus;
        [SerializeField, Min(0f)] private float stunDuration = 2f;

        [NonSerialized] private GameObject source;

        public HeavyConcussionBehavior()
        {
        }

        public HeavyConcussionBehavior(
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
            ConcussionOnImpactCollisionBehavior.ApplyStunOnImpact(
                target,
                stunStatus,
                stunDuration,
                new StatusContext(
                    HeavyConcussionSourceId,
                    source,
                    ReapplyPolicy.Stack));
        }
    }
}
