using System.Collections.Generic;
using RPGame.Core.Effects;
using UnityEngine;

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

        public float Resolve(float baseValue, params SpellProperty[] properties)
        {
            float flat = 0f;
            float percent = 0f;
            for (int i = 0; i < properties.Length; i++)
            {
                flat += GetFlatValue(properties[i]);
                percent += GetPercentValue(properties[i]);
            }

            return Mathf.Max(0f, (baseValue + flat) * Mathf.Max(0f, 1f + percent));
        }

        public float GetPercentMultiplier(SpellProperty property)
        {
            return Mathf.Max(0f, 1f + GetPercentValue(property));
        }

        private float GetValue(SpellProperty property, EffectModifierType modifierType)
        {
            IReadOnlyDictionary<SpellProperty, float> values =
                modifierType == EffectModifierType.Flat ? flatValues : percentValues;
            return values.TryGetValue(property, out float value) ? value : 0f;
        }
    }
}
