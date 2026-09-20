using RPGame.Core.Damage;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    public sealed class KnockbackDamageOnImpactCollisionBehavior : IKnockbackCollisionHandler
    {
        private readonly float impactDamage;
        private readonly GameObject source;

        public KnockbackDamageOnImpactCollisionBehavior(float impactDamage, GameObject source = null)
        {
            this.impactDamage = Mathf.Max(0f, impactDamage);
            this.source = source;
        }

        public RuntimeSpellBehaviorPhase Phase => RuntimeSpellBehaviorPhase.Aftermath;

        public void OnKnockbackCollision(GameObject target, Collider obstacle, Vector3 point)
        {
            if (target == null)
            {
                return;
            }

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                return;
            }

            damageable.ApplyDamage(new DamageData(
                new[]
                {
                    new PartialDamage(impactDamage, DamageType.Physical, DamageElement.None)
                },
                source));
        }
    }
}
