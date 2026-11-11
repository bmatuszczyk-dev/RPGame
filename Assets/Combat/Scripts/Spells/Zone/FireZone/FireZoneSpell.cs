using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [CreateAssetMenu(fileName = "FireZoneSpell", menuName = "RPGame/Spells/Fire Zone")]
    public sealed class FireZoneSpell : ZoneSpell, IDurationCapability
    {
        public float Duration => ActiveDuration;

        public override bool SupportsProperty(SpellProperty property)
        {
            return base.SupportsProperty(property) || property == SpellProperty.Duration;
        }
    }
}
