using System;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [Serializable]
    public sealed class KnockbackDamageOnImpactCollisionBehavior :
        IKnockbackCollisionHandler,
        IInitializableRuntimeSpellBehavior
    {
        [SerializeField, Min(0f)] private float impactDamage = 1f;

        [NonSerialized] private GameObject source;

        public KnockbackDamageOnImpactCollisionBehavior()
        {
        }

        public KnockbackDamageOnImpactCollisionBehavior(float impactDamage, GameObject source = null)
        {
            this.impactDamage = Mathf.Max(0f, impactDamage);
            Initialize(source);
        }

        public bool Supports(Spell spell)
        {
            return spell is WaveSpell;
        }

        public void Initialize(GameObject caster)
        {
            source = caster;
            impactDamage = Mathf.Max(0f, impactDamage);
        }

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
