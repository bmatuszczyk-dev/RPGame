using System.Collections.Generic;
using RPGame.Core.Spells;

namespace RPGame.Core.Effects
{
    public sealed class PermanentEffectContainer
    {
        private readonly List<EffectInstance> effects = new();
        private readonly Dictionary<EffectStat, float> flatEffectValues = new();
        private readonly Dictionary<EffectStat, float> percentEffectValues = new();

        public IReadOnlyList<EffectInstance> Effects => effects;

        public void Add(PassiveEffectDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            EffectInstance instance = CreateInstance(definition);
            if (instance != null)
            {
                effects.Add(instance);
                if (definition is StatEffectDefinition statEffect)
                {
                    AddEffectValue(statEffect.Stat, statEffect.ModifierType, statEffect.Value);
                }
            }
        }

        public void AddRange(IEnumerable<PassiveEffectDefinition> definitions)
        {
            if (definitions == null)
            {
                return;
            }

            foreach (PassiveEffectDefinition definition in definitions)
            {
                Add(definition);
            }
        }

        public float GetEffectValue(EffectStat stat, EffectModifierType modifierType)
        {
            Dictionary<EffectStat, float> values = modifierType == EffectModifierType.Flat
                ? flatEffectValues
                : percentEffectValues;
            return values.TryGetValue(stat, out float value) ? value : 0f;
        }

        public SpellPropertyModifiers CreateSpellPropertyModifiers(Spell spell)
        {
            Dictionary<SpellProperty, float> flatValues = new();
            Dictionary<SpellProperty, float> percentValues = new();

            foreach (EffectInstance effect in effects)
            {
                if (effect.Definition is not SpellPropertyModifierEffectDefinition modifier)
                {
                    continue;
                }

                if (spell == null || !spell.SupportsProperty(modifier.Property))
                {
                    continue;
                }

                if (modifier.TargetSpell != null && modifier.TargetSpell != spell)
                {
                    continue;
                }

                Dictionary<SpellProperty, float> values =
                    modifier.ModifierType == EffectModifierType.Flat
                        ? flatValues
                        : percentValues;
                values.TryGetValue(modifier.Property, out float currentValue);
                values[modifier.Property] = currentValue + modifier.Value;
            }

            return flatValues.Count == 0 && percentValues.Count == 0
                ? SpellPropertyModifiers.Empty
                : new SpellPropertyModifiers(flatValues, percentValues);
        }

        public IReadOnlyList<IRuntimeSpellBehavior> CreateRuntimeBehaviors(
            Spell spell,
            UnityEngine.GameObject casterObject)
        {
            List<RuntimeSpellBehaviorOrderEntry> behaviors = new();
            foreach (EffectInstance effect in effects)
            {
                if (effect.Definition is not RuntimeBehaviorEffectDefinition runtimeBehaviorEffect)
                {
                    continue;
                }

                IRuntimeSpellBehavior behavior =
                    runtimeBehaviorEffect.CreateRuntimeBehavior(spell, casterObject);
                if (behavior != null)
                {
                    behaviors.Add(new RuntimeSpellBehaviorOrderEntry(
                        behavior,
                        runtimeBehaviorEffect.ExecutionOrder));
                }
            }

            IReadOnlyList<IRuntimeSpellBehavior> orderedBehaviors =
                RuntimeSpellBehaviorOrderer.Order(behaviors);
            ApplyRuntimeBehaviorModifiers(spell, casterObject, orderedBehaviors);
            return orderedBehaviors;
        }

        private void AddEffectValue(
            EffectStat stat,
            EffectModifierType modifierType,
            float value)
        {
            Dictionary<EffectStat, float> values = modifierType == EffectModifierType.Flat
                ? flatEffectValues
                : percentEffectValues;
            values.TryGetValue(stat, out float currentValue);
            values[stat] = currentValue + value;
        }

        private static EffectInstance CreateInstance(PassiveEffectDefinition definition)
        {
            if (definition is StatEffectDefinition statEffect)
            {
                return new EffectInstance(
                    statEffect,
                    statEffect.Stat,
                    statEffect.ModifierType,
                    statEffect.Value);
            }

            return new EffectInstance(definition);
        }

        private void ApplyRuntimeBehaviorModifiers(
            Spell spell,
            UnityEngine.GameObject casterObject,
            IReadOnlyList<IRuntimeSpellBehavior> behaviors)
        {
            foreach (EffectInstance effect in effects)
            {
                if (effect.Definition is RuntimeBehaviorModifierEffectDefinition modifier
                    && modifier.Supports(spell))
                {
                    modifier.ModifyRuntimeBehaviors(behaviors, spell, casterObject);
                }
            }
        }
    }
}
