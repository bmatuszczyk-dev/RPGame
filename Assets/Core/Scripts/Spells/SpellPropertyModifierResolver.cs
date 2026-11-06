using RPGame.Core.Effects;
using UnityEngine;

namespace RPGame.Core.Spells
{
    public static class SpellPropertyModifierResolver
    {
        public static float ResolveRadius(IAoECapability capability, SpellPropertyModifiers modifiers)
        {
            return ResolveValue(capability.Radius, modifiers, SpellProperty.Radius);
        }

        public static float ResolveWaveRange(IWaveCapability capability, SpellPropertyModifiers modifiers)
        {
            return ResolveValue(
                capability.WaveRange,
                modifiers,
                SpellProperty.Radius,
                SpellProperty.WaveRange);
        }

        public static float ResolveWaveAngle(IWaveCapability capability, SpellPropertyModifiers modifiers)
        {
            float angle = ResolveValue(capability.WaveAngle, modifiers, SpellProperty.WaveAngle);
            return Mathf.Clamp(angle * ResolveAreaMultiplier(modifiers), 0f, 360f);
        }

        public static float ResolveZoneRadius(IZoneCapability capability, SpellPropertyModifiers modifiers)
        {
            float radius = ResolveValue(
                capability.Radius,
                modifiers,
                SpellProperty.Radius,
                SpellProperty.ZoneRadius);
            return radius * Mathf.Sqrt(ResolveAreaMultiplier(modifiers));
        }

        public static float ResolveDuration(IDurationCapability capability, SpellPropertyModifiers modifiers)
        {
            return ResolveValue(capability.Duration, modifiers, SpellProperty.Duration);
        }

        public static float ResolveControlPower(IControlCapability capability, SpellPropertyModifiers modifiers)
        {
            return ResolveValue(capability.ControlPower, modifiers, SpellProperty.ControlPower);
        }

        public static int ResolveOrbCount(IOrbCapability capability, SpellPropertyModifiers modifiers)
        {
            return Mathf.Max(1, Mathf.RoundToInt(
                ResolveValue(capability.OrbCount, modifiers, SpellProperty.OrbCount)));
        }

        public static float ResolveAreaMultiplier(SpellPropertyModifiers modifiers)
        {
            return Mathf.Max(0f, 1f + GetModifier(
                modifiers,
                SpellProperty.Area,
                EffectModifierType.Percent));
        }

        private static float ResolveValue(
            float baseValue,
            SpellPropertyModifiers modifiers,
            params SpellProperty[] properties)
        {
            float flat = 0f;
            float percent = 0f;
            for (int i = 0; i < properties.Length; i++)
            {
                flat += GetModifier(modifiers, properties[i], EffectModifierType.Flat);
                percent += GetModifier(modifiers, properties[i], EffectModifierType.Percent);
            }

            return Mathf.Max(0f, (baseValue + flat) * Mathf.Max(0f, 1f + percent));
        }

        private static float GetModifier(
            SpellPropertyModifiers modifiers,
            SpellProperty property,
            EffectModifierType modifierType)
        {
            if (modifiers == null)
            {
                return 0f;
            }

            return modifierType == EffectModifierType.Flat
                ? modifiers.GetFlatValue(property)
                : modifiers.GetPercentValue(property);
        }
    }
}
