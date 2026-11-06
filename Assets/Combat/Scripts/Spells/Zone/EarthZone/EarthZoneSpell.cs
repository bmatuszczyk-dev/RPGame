using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [CreateAssetMenu(fileName = "EarthZoneSpell", menuName = "RPGame/Spells/Earth Zone")]
    public sealed class EarthZoneSpell : ZoneSpell, IDurationCapability
    {
        public float Duration => ActiveDuration;

        public override bool SupportsProperty(SpellProperty property)
        {
            return base.SupportsProperty(property) || property == SpellProperty.Duration;
        }
    }
}
