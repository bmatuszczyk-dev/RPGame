using System.Globalization;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Core.Effects
{
    [CreateAssetMenu(
        fileName = "SpellPropertyModifierEffect",
        menuName = "RPGame/Progression/Effects/Spell Property Modifier")]
    public sealed class SpellPropertyModifierEffectDefinition : PassiveEffectDefinition
    {
        [SerializeField] private SpellProperty property;
        [SerializeField] private EffectModifierType modifierType;
        [SerializeField] private float value;
        [SerializeField] private Spell targetSpell;

        public SpellProperty Property => property;
        public EffectModifierType ModifierType => modifierType;
        public float Value => value;
        public Spell TargetSpell => targetSpell;

        public override string ToString()
        {
            float displayValueValue = modifierType == EffectModifierType.Percent
                ? value * 100f
                : value;
            string displayValue = displayValueValue.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture);
            string suffix = modifierType == EffectModifierType.Percent ? "%" : string.Empty;
            return targetSpell == null
                ? $"{property} {displayValue}{suffix}"
                : $"{GetTargetSpellName()}: {property} {displayValue}{suffix}";
        }

        private string GetTargetSpellName()
        {
            return string.IsNullOrEmpty(targetSpell.name)
                ? targetSpell.GetType().Name
                : targetSpell.name;
        }
    }
}
