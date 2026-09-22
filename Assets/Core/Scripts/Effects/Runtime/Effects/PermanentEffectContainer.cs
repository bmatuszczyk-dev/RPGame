using System.Collections.Generic;
using RPGame.Core.Spells;

namespace RPGame.Core.Effects
{
    public sealed class PermanentEffectContainer
    {
        private readonly List<EffectInstance> effects = new();

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
            float value = 0f;

            foreach (EffectInstance effect in GetEffects(stat, modifierType))
            {
                value += effect.Value;
            }

            return value;
        }

        public SpellPropertyModifiers CreateSpellPropertyModifiers(Spell spell)
        {
            float radius = 0f;
            float duration = 0f;
            float controlPower = 0f;
            float orbCount = 0f;

            foreach (EffectInstance effect in effects)
            {
                if (effect.Definition is not SpellPropertyModifierEffectDefinition modifier)
                {
                    continue;
                }

                if (!SpellPropertyModifierResolver.Supports(spell, modifier.Property))
                {
                    continue;
                }

                if (modifier.TargetSpell != null && modifier.TargetSpell != spell)
                {
                    continue;
                }

                switch (modifier.Property)
                {
                    case SpellProperty.Radius:
                        radius += modifier.Value;
                        break;
                    case SpellProperty.Duration:
                        duration += modifier.Value;
                        break;
                    case SpellProperty.ControlPower:
                        controlPower += modifier.Value;
                        break;
                    case SpellProperty.OrbCount:
                        orbCount += modifier.Value;
                        break;
                }
            }

            return radius == 0f
                && duration == 0f
                && controlPower == 0f
                && orbCount == 0f
                ? SpellPropertyModifiers.Empty
                : new SpellPropertyModifiers(radius, duration, controlPower, orbCount);
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

            return RuntimeSpellBehaviorOrderer.Order(behaviors);
        }

        private IEnumerable<EffectInstance> GetEffects(
            EffectStat stat,
            EffectModifierType modifierType)
        {
            foreach (EffectInstance effect in effects)
            {
                if (effect.Stat == stat && effect.ModifierType == modifierType)
                {
                    yield return effect;
                }
            }
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
    }
}
