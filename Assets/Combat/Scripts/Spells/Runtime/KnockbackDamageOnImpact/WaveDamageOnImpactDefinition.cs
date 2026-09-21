using RPGame.Core.Spells;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RPGame.Combat.Spells
{
    [System.Serializable]
    [MovedFrom(true, "RPGame.Combat.Spells", "RPGame.Combat", "WaveImpactBehaviorDefinition")]
    public sealed class WaveDamageOnImpactDefinition : RuntimeSpellBehaviorDefinition
    {
        [SerializeField, Min(0f)] private float impactDamage = 1f;

        public override bool TryCreate(
            Spell spell,
            GameObject casterObject,
            out IRuntimeSpellBehavior behavior)
        {
            behavior = null;
            if (spell is not WaveSpell)
            {
                return false;
            }

            behavior = new KnockbackDamageOnImpactCollisionBehavior(impactDamage, casterObject);
            return true;
        }

    }
}
