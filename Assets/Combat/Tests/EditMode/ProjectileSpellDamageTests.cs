using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statistics.Attributes;
using RPGame.Combat.Spells;
using UnityEditor;
using UnityEngine;

namespace RPGame.Combat.Tests
{
    public sealed class ProjectileSpellDamageTests
    {
        [Test]
        public void CalculatePowerDamageBonus_WhenPowerIsPositive_ReturnsScaledBonus()
        {
            float damageBonus = SpellDamageCalculator.CalculatePowerDamageBonus(6, 1.5f);

            Assert.AreEqual(9f, damageBonus);
        }

        [Test]
        public void GetDamageRanges_ReturnsBaseDamageWithPowerScaling()
        {
            ProjectileSpell spell = ScriptableObject.CreateInstance<ProjectileSpell>();
            try
            {
                SetProjectileSpellDamage(spell, 20f, 30f, DamageType.Magical, DamageElement.Fire, 1.5f);
                CasterData casterData = new CasterDataBuilder(null, null, null)
                    .WithAttributes(new TestCharacterAttributes(power: 6))
                    .Build();

                IReadOnlyList<PartialDamageRange> ranges = spell.GetDamageRanges(casterData);

                Assert.AreEqual(1, ranges.Count);
                Assert.AreEqual(29f, ranges[0].MinDamage);
                Assert.AreEqual(39f, ranges[0].MaxDamage);
                Assert.AreEqual(DamageType.Magical, ranges[0].DamageType);
                Assert.AreEqual(DamageElement.Fire, ranges[0].DamageElement);
            }
            finally
            {
                Object.DestroyImmediate(spell);
            }
        }

        [Test]
        public void DerivedCasterData_PreservesSpellId()
        {
            SpellId spellId = new("wave");
            CasterData casterData = new CasterDataBuilder(null, null, null)
                .WithSpellId(spellId)
                .Build();
            ProjectileSpell projectileSpell = ScriptableObject.CreateInstance<ProjectileSpell>();
            OrbitSpell orbitSpell = ScriptableObject.CreateInstance<OrbitSpell>();
            WaveSpell waveSpell = ScriptableObject.CreateInstance<WaveSpell>();

            try
            {
                CasterData projectileCasterData = InvokeCreateCasterData(
                    projectileSpell,
                    "CreateProjectileCasterData",
                    casterData);
                CasterData orbitCasterData = InvokeCreateCasterData(
                    orbitSpell,
                    "CreateOrbitCasterData",
                    casterData);
                CasterData waveCasterData = InvokeCreateCasterData(
                    waveSpell,
                    "CreateWaveCasterData",
                    casterData);

                Assert.AreEqual(spellId, projectileCasterData.SpellId);
                Assert.AreEqual(spellId, orbitCasterData.SpellId);
                Assert.AreEqual(spellId, waveCasterData.SpellId);
            }
            finally
            {
                Object.DestroyImmediate(projectileSpell);
                Object.DestroyImmediate(orbitSpell);
                Object.DestroyImmediate(waveSpell);
            }
        }

        private static void SetProjectileSpellDamage(
            ProjectileSpell spell,
            float minDamage,
            float maxDamage,
            DamageType damageType,
            DamageElement damageElement,
            float powerDamageScaling)
        {
            SerializedObject serializedSpell = new(spell);
            SerializedProperty baseDamageRange = serializedSpell.FindProperty("baseDamageRange");
            baseDamageRange.FindPropertyRelative("minDamage").floatValue = minDamage;
            baseDamageRange.FindPropertyRelative("maxDamage").floatValue = maxDamage;
            baseDamageRange.FindPropertyRelative("damageType").enumValueIndex = (int)damageType;
            baseDamageRange.FindPropertyRelative("damageElement").enumValueIndex = (int)damageElement;
            serializedSpell.FindProperty("powerDamageScaling").floatValue = powerDamageScaling;
            serializedSpell.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CasterData InvokeCreateCasterData(
            Spell spell,
            string methodName,
            CasterData casterData)
        {
            MethodInfo method = spell.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (CasterData)method.Invoke(spell, new object[] { casterData });
        }

        private sealed class TestCharacterAttributes : ICharacterAttributes
        {
            public TestCharacterAttributes(int power)
            {
                Power = power;
            }

            public int Strength => 0;
            public int Dexterity => 0;
            public int Endurance => 0;
            public int Vitality => 0;
            public int Intelligence => 0;
            public int Power { get; }

            public int GetValue(CharacterAttributeType attributeType)
            {
                return attributeType == CharacterAttributeType.Power ? Power : 0;
            }
        }
    }
}
