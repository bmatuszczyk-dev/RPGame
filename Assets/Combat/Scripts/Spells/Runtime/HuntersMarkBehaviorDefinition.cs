using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [System.Serializable]
    public sealed class HuntersMarkBehaviorDefinition : RuntimeSpellBehaviorDefinition
    {
        [SerializeField] private MarkStatusDefinition markDefinition;
        [SerializeField, Min(0f)] private float bonusDamage = 1f;

        public override bool TryCreate(
            Spell spell,
            GameObject casterObject,
            out IRuntimeSpellBehavior behavior)
        {
            behavior = null;
            if (spell is not IProjectileCapability || markDefinition == null)
            {
                return false;
            }

            behavior = new HuntersMarkBehavior(
                markDefinition,
                new PartialDamage(bonusDamage, DamageType.Magical, DamageElement.None),
                casterObject);
            return true;
        }
    }
}
