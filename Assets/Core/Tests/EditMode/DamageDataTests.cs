using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Damage;
using RPGame.Core.Effects;
using UnityEditor;
using UnityEngine;

namespace RPGame.Core.Tests
{
    public sealed class DamageDataTests
    {
        private GameObject source;
        private EffectAggregator effects;
        private readonly List<StatEffectDefinition> effectDefinitions = new();

        [SetUp]
        public void SetUp()
        {
            source = new GameObject("Damage Source");
            effects = source.AddComponent<EffectAggregator>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (StatEffectDefinition definition in effectDefinitions)
            {
                Object.DestroyImmediate(definition);
            }

            Object.DestroyImmediate(source);
        }

        [Test]
        public void DamageData_WithPartialDamage_SumsAmount()
        {
            List<PartialDamage> parts = new()
            {
                new PartialDamage(3f, DamageType.Magical, DamageElement.None),
                new PartialDamage(5f, DamageType.Magical, DamageElement.Fire)
            };

            DamageData damageData = new DamageData(parts);

            Assert.AreEqual(8f, damageData.Amount);
            Assert.AreEqual(2, damageData.Parts.Count);
            Assert.IsTrue(damageData.HasDamage);
        }

        [Test]
        public void DamageData_WithZeroDamageParts_HasNoDamage()
        {
            List<PartialDamage> parts = new()
            {
                new PartialDamage(0f, DamageType.Physical, DamageElement.None)
            };

            DamageData damageData = new DamageData(parts);

            Assert.AreEqual(0f, damageData.Amount);
            Assert.AreEqual(0, damageData.Parts.Count);
            Assert.IsFalse(damageData.HasDamage);
        }

        [Test]
        public void SpellDamage_AppliesFlatBonus()
        {
            AddEffect(EffectStat.SpellDamage, EffectModifierType.Flat, 5f);

            Assert.AreEqual(25f, CreateSpellDamage(20f).Amount, 0.0001f);
        }

        [Test]
        public void SpellDamage_AppliesPercentBonus()
        {
            AddEffect(EffectStat.SpellDamage, EffectModifierType.Percent, 0.2f);

            Assert.AreEqual(24f, CreateSpellDamage(20f).Amount, 0.0001f);
        }

        [Test]
        public void SpellDamage_AppliesFlatAndPercentInOrder()
        {
            AddEffect(EffectStat.SpellDamage, EffectModifierType.Flat, 5f);
            AddEffect(EffectStat.SpellDamage, EffectModifierType.Percent, 0.2f);

            DamageData data = CreateSpellDamage(20f);

            Assert.AreEqual(30f, data.Amount, 0.0001f);
        }

        [Test]
        public void SpellDamage_SumsMultipleFlatAndPercentEffects()
        {
            AddEffect(EffectStat.SpellDamage, EffectModifierType.Flat, 2f);
            AddEffect(EffectStat.SpellDamage, EffectModifierType.Flat, 3f);
            AddEffect(EffectStat.SpellDamage, EffectModifierType.Percent, 0.1f);
            AddEffect(EffectStat.SpellDamage, EffectModifierType.Percent, 0.1f);

            DamageData data = CreateSpellDamage(20f);

            Assert.AreEqual(30f, data.Amount, 0.0001f);
        }

        [Test]
        public void SpellDamage_AppliesFlatBonusOnlyOnceForMultipleDamageParts()
        {
            AddEffect(EffectStat.SpellDamage, EffectModifierType.Flat, 5f);
            PartialDamage[] parts =
            {
                new(10f, DamageType.Magical, DamageElement.Fire),
                new(10f, DamageType.Physical, DamageElement.None)
            };

            DamageData data = new(parts, source, DamageApplicationType.Direct, DamageOrigin.Spell);

            Assert.AreEqual(25f, data.Amount, 0.0001f);
            Assert.AreEqual(12.5f, data.Parts[0].Amount, 0.0001f);
            Assert.AreEqual(DamageType.Magical, data.Parts[0].DamageType);
            Assert.AreEqual(DamageElement.Fire, data.Parts[0].DamageElement);
            Assert.AreEqual(12.5f, data.Parts[1].Amount, 0.0001f);
            Assert.AreEqual(DamageType.Physical, data.Parts[1].DamageType);
            Assert.AreEqual(DamageElement.None, data.Parts[1].DamageElement);
        }

        [Test]
        public void SpellDamage_DoesNotApplyFlatBonusToPeriodicOrNonSpellDamage()
        {
            AddEffect(EffectStat.SpellDamage, EffectModifierType.Flat, 5f);

            DamageData periodic = new(
                new[] { new PartialDamage(20f, DamageType.Magical, DamageElement.Fire) },
                source,
                DamageApplicationType.Periodic,
                DamageOrigin.Spell);
            DamageData collision = new(
                new[] { new PartialDamage(20f, DamageType.Magical, DamageElement.None) },
                source);

            Assert.AreEqual(20f, periodic.Amount, 0.0001f);
            Assert.AreEqual(20f, collision.Amount, 0.0001f);
        }

        [Test]
        public void SpellDamage_PreservesPeriodicScalingAndDoesNotReapplyWithParts()
        {
            AddEffect(EffectStat.SpellDamage, EffectModifierType.Flat, 5f);
            AddEffect(EffectStat.SpellDamage, EffectModifierType.Percent, 0.1f);
            AddEffect(EffectStat.PeriodicSpellDamage, EffectModifierType.Percent, 0.2f);
            DamageData direct = CreateSpellDamage(20f);
            DamageData periodic = new(
                new[] { new PartialDamage(20f, DamageType.Magical, DamageElement.Fire) },
                source,
                DamageApplicationType.Periodic,
                DamageOrigin.Spell);

            Assert.AreEqual(27.5f, direct.Amount, 0.0001f);
            Assert.AreEqual(26.4f, periodic.Amount, 0.0001f);
            Assert.AreEqual(27.5f, direct.WithParts(direct.Parts).Amount, 0.0001f);
        }

        private DamageData CreateSpellDamage(float amount)
        {
            return new DamageData(
                new[] { new PartialDamage(amount, DamageType.Magical, DamageElement.Fire) },
                source,
                DamageApplicationType.Direct,
                DamageOrigin.Spell);
        }

        private void AddEffect(EffectStat stat, EffectModifierType modifierType, float value)
        {
            StatEffectDefinition definition = ScriptableObject.CreateInstance<StatEffectDefinition>();
            SerializedObject serialized = new(definition);
            serialized.FindProperty("stat").enumValueIndex = GetEffectStatIndex(stat);
            serialized.FindProperty("modifierType").enumValueIndex = (int)modifierType;
            serialized.FindProperty("value").floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            effectDefinitions.Add(definition);
            effects.Add(definition);
        }

        private static int GetEffectStatIndex(EffectStat stat)
        {
            return System.Array.IndexOf((EffectStat[])System.Enum.GetValues(typeof(EffectStat)), stat);
        }
    }
}
