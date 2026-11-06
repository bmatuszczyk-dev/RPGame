using System.Collections.Generic;
using RPGame.Core.Effects;

namespace RPGame.Core.Spells
{
    public sealed class SpellPropertyModifiers
    {
        public static SpellPropertyModifiers Empty { get; } = new(null, null);

        private readonly IReadOnlyDictionary<SpellProperty, float> flatValues;
        private readonly IReadOnlyDictionary<SpellProperty, float> percentValues;

        internal SpellPropertyModifiers(
            IReadOnlyDictionary<SpellProperty, float> flatValues,
            IReadOnlyDictionary<SpellProperty, float> percentValues)
        {
            this.flatValues = flatValues != null
                ? new Dictionary<SpellProperty, float>(flatValues)
                : new Dictionary<SpellProperty, float>();
            this.percentValues = percentValues != null
                ? new Dictionary<SpellProperty, float>(percentValues)
                : new Dictionary<SpellProperty, float>();
        }

        public float GetFlatValue(SpellProperty property)
        {
            return GetValue(property, EffectModifierType.Flat);
        }

        public float GetPercentValue(SpellProperty property)
        {
            return GetValue(property, EffectModifierType.Percent);
        }

        private float GetValue(SpellProperty property, EffectModifierType modifierType)
        {
            IReadOnlyDictionary<SpellProperty, float> values =
                modifierType == EffectModifierType.Flat ? flatValues : percentValues;
            return values.TryGetValue(property, out float value) ? value : 0f;
        }
    }
}
