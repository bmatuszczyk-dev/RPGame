using NUnit.Framework;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using UnityEditor;
using UnityEngine;

namespace RPGame.Core.Tests.Effects
{
    public sealed class SpellPropertyModifierTests
    {
        private GameObject gameObject;
        private EffectAggregator aggregator;
        private TestSpell spell;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Spell Property Modifier Tests");
            aggregator = gameObject.AddComponent<EffectAggregator>();
            spell = ScriptableObject.CreateInstance<TestSpell>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(spell);
        }

        [Test]
        public void ModifierEffect_StoresPropertyAndValue()
        {
            SpellPropertyModifierEffectDefinition effect =
                CreateModifier(SpellProperty.Radius, 1.5f);

            Assert.AreEqual(SpellProperty.Radius, effect.Property);
            Assert.AreEqual(1.5f, effect.Value);
            Assert.IsNull(effect.TargetSpell);
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void GenericModifier_AppliesToDifferentSpellsWithCapability()
        {
            SpellPropertyModifierEffectDefinition effect = CreateModifier(SpellProperty.Radius, 2f);
            aggregator.Add(effect);
            TestSpell otherSpell = ScriptableObject.CreateInstance<TestSpell>();

            Assert.AreEqual(2f, aggregator.CreateSpellPropertyModifiers(spell).GetFlatValue(SpellProperty.Radius));
            Assert.AreEqual(2f, aggregator.CreateSpellPropertyModifiers(otherSpell).GetFlatValue(SpellProperty.Radius));
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(otherSpell);
        }

        [Test]
        public void SpecificModifier_AppliesOnlyToTargetSpell()
        {
            TestSpell otherSpell = ScriptableObject.CreateInstance<TestSpell>();
            SpellPropertyModifierEffectDefinition effect =
                CreateModifier(SpellProperty.Radius, 2f, spell);
            aggregator.Add(effect);

            Assert.AreEqual(2f, aggregator.CreateSpellPropertyModifiers(spell).GetFlatValue(SpellProperty.Radius));
            Assert.AreEqual(0f, aggregator.CreateSpellPropertyModifiers(otherSpell).GetFlatValue(SpellProperty.Radius));
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(otherSpell);
        }

        [Test]
        public void GenericAndSpecificModifiers_StackForTargetSpellOnly()
        {
            TestSpell otherSpell = ScriptableObject.CreateInstance<TestSpell>();
            SpellPropertyModifierEffectDefinition generic = CreateModifier(SpellProperty.Radius, 1f);
            SpellPropertyModifierEffectDefinition specific = CreateModifier(SpellProperty.Radius, 2f, spell);
            aggregator.AddRange(new[] { generic, specific });

            Assert.AreEqual(3f, aggregator.CreateSpellPropertyModifiers(spell).GetFlatValue(SpellProperty.Radius));
            Assert.AreEqual(1f, aggregator.CreateSpellPropertyModifiers(otherSpell).GetFlatValue(SpellProperty.Radius));
            Object.DestroyImmediate(generic);
            Object.DestroyImmediate(specific);
            Object.DestroyImmediate(otherSpell);
        }

        [Test]
        public void SpecificModifier_StillRequiresCapability()
        {
            NoCapabilitySpell noCapabilitySpell = ScriptableObject.CreateInstance<NoCapabilitySpell>();
            SpellPropertyModifierEffectDefinition effect =
                CreateModifier(SpellProperty.Duration, 2f, noCapabilitySpell);
            aggregator.Add(effect);

            SpellPropertyModifiers modifiers = aggregator.CreateSpellPropertyModifiers(noCapabilitySpell);

            Assert.AreSame(SpellPropertyModifiers.Empty, modifiers);
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(noCapabilitySpell);
        }

        [Test]
        public void ModifierEffect_ToStringIncludesTargetSpellWhenScoped()
        {
            SpellPropertyModifierEffectDefinition effect =
                CreateModifier(SpellProperty.Radius, 2f, spell);

            StringAssert.Contains("TestSpell", effect.ToString());
            StringAssert.Contains("Radius +2", effect.ToString());
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void CreateSnapshot_SumsModifiersForSameProperty()
        {
            SpellPropertyModifierEffectDefinition first = CreateModifier(SpellProperty.Radius, 1f);
            SpellPropertyModifierEffectDefinition second = CreateModifier(SpellProperty.Radius, 2f);
            aggregator.AddRange(new[] { first, second });

            SpellPropertyModifiers modifiers = aggregator.CreateSpellPropertyModifiers(spell);

            Assert.AreEqual(3f, modifiers.GetFlatValue(SpellProperty.Radius));
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void CreateSnapshot_KeepsPropertiesIndependent()
        {
            SpellPropertyModifierEffectDefinition radius = CreateModifier(SpellProperty.Radius, 1f);
            SpellPropertyModifierEffectDefinition duration = CreateModifier(SpellProperty.Duration, 2f);
            aggregator.AddRange(new[] { radius, duration });

            SpellPropertyModifiers modifiers = aggregator.CreateSpellPropertyModifiers(spell);

            Assert.AreEqual(1f, modifiers.GetFlatValue(SpellProperty.Radius));
            Assert.AreEqual(2f, modifiers.GetFlatValue(SpellProperty.Duration));
            Assert.AreEqual(0f, modifiers.GetFlatValue(SpellProperty.ControlPower));
            Object.DestroyImmediate(radius);
            Object.DestroyImmediate(duration);
        }

        [Test]
        public void CreateSnapshot_WhenNoModifierExists_ReturnsZero()
        {
            SpellPropertyModifiers modifiers = aggregator.CreateSpellPropertyModifiers(null);

            Assert.AreSame(SpellPropertyModifiers.Empty, modifiers);
            Assert.AreEqual(0f, modifiers.GetFlatValue(SpellProperty.OrbCount));
        }

        [Test]
        public void CreateSnapshot_IsNotChangedByLaterEffectAdditions()
        {
            SpellPropertyModifierEffectDefinition first = CreateModifier(SpellProperty.Radius, 1f);
            SpellPropertyModifierEffectDefinition second = CreateModifier(SpellProperty.Radius, 2f);
            aggregator.Add(first);
            SpellPropertyModifiers snapshot = aggregator.CreateSpellPropertyModifiers(spell);

            aggregator.Add(second);

            Assert.AreEqual(1f, snapshot.GetFlatValue(SpellProperty.Radius));
            Assert.AreEqual(3f, aggregator.CreateSpellPropertyModifiers(spell).GetFlatValue(SpellProperty.Radius));
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void Resolver_UsesCapabilityValuesAndModifiers()
        {
            SpellPropertyModifierEffectDefinition radius = CreateModifier(SpellProperty.Radius, 2.5f);
            SpellPropertyModifierEffectDefinition duration = CreateModifier(SpellProperty.Duration, -2f);
            SpellPropertyModifierEffectDefinition controlPower = CreateModifier(SpellProperty.ControlPower, 3f);
            SpellPropertyModifierEffectDefinition orbCount = CreateModifier(SpellProperty.OrbCount, 1.6f);
            aggregator.AddRange(new[]
            {
                radius,
                duration,
                controlPower,
                orbCount
            });

            SpellPropertyModifiers modifiers = aggregator.CreateSpellPropertyModifiers(spell);

            Assert.AreEqual(3.5f, SpellPropertyModifierResolver.ResolveRadius(spell, modifiers));
            Assert.AreEqual(0f, SpellPropertyModifierResolver.ResolveDuration(spell, modifiers));
            Assert.AreEqual(4f, SpellPropertyModifierResolver.ResolveControlPower(spell, modifiers));
            Assert.AreEqual(3, SpellPropertyModifierResolver.ResolveOrbCount(spell, modifiers));
            Object.DestroyImmediate(radius);
            Object.DestroyImmediate(duration);
            Object.DestroyImmediate(controlPower);
            Object.DestroyImmediate(orbCount);
        }

        [Test]
        public void CreateSnapshot_IgnoresPropertiesUnsupportedBySpell()
        {
            SpellPropertyModifierEffectDefinition radius = CreateModifier(SpellProperty.Radius, 2f);
            SpellPropertyModifierEffectDefinition duration = CreateModifier(SpellProperty.Duration, 3f);
            aggregator.AddRange(new[] { radius, duration });
            NoCapabilitySpell noCapabilitySpell = ScriptableObject.CreateInstance<NoCapabilitySpell>();

            SpellPropertyModifiers modifiers = aggregator.CreateSpellPropertyModifiers(noCapabilitySpell);

            Assert.AreSame(SpellPropertyModifiers.Empty, modifiers);
            Object.DestroyImmediate(radius);
            Object.DestroyImmediate(duration);
            Object.DestroyImmediate(noCapabilitySpell);
        }

        private static SpellPropertyModifierEffectDefinition CreateModifier(
            SpellProperty property,
            float value,
            Spell targetSpell = null)
        {
            SpellPropertyModifierEffectDefinition effect =
                ScriptableObject.CreateInstance<SpellPropertyModifierEffectDefinition>();
            SerializedObject serializedEffect = new(effect);
            serializedEffect.FindProperty("property").enumValueIndex = (int)property;
            serializedEffect.FindProperty("value").floatValue = value;
            serializedEffect.FindProperty("targetSpell").objectReferenceValue = targetSpell;
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
            return effect;
        }

        private sealed class TestSpell : Spell, IAoECapability, IDurationCapability, IControlCapability, IOrbCapability
        {
            public float Radius => 1f;
            public float Duration => 1f;
            public float ControlPower => 1f;
            public int OrbCount => 1;

            public override bool SupportsProperty(SpellProperty property)
            {
                return property is SpellProperty.Radius
                    or SpellProperty.Area
                    or SpellProperty.Duration
                    or SpellProperty.ControlPower
                    or SpellProperty.OrbCount;
            }

            public override void OnCast(CasterData casterData)
            {
            }
        }

        private sealed class NoCapabilitySpell : Spell
        {
            public override bool SupportsProperty(SpellProperty property) => false;

            public override void OnCast(CasterData casterData)
            {
            }
        }
    }
}
