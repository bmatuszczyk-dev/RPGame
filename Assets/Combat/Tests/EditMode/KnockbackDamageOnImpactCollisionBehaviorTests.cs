using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Tests
{
    public sealed class KnockbackDamageOnImpactCollisionBehaviorTests
    {
        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void OnKnockbackCollision_WithDamageableTarget_AppliesConfiguredDamageOnce()
        {
            GameObject target = CreateObject("Target");
            TestDamageable damageable = target.AddComponent<TestDamageable>();
            GameObject source = CreateObject("Source");
            KnockbackDamageOnImpactCollisionBehavior behavior = new(17f, source);

            behavior.OnKnockbackCollision(target, null, Vector3.zero);

            Assert.AreEqual(1, damageable.CallCount);
            Assert.AreEqual(17f, damageable.LastData.Amount);
            Assert.AreSame(source, damageable.LastData.Source);
        }

        [Test]
        public void OnKnockbackCollision_WithoutDamageableTarget_DoesNothing()
        {
            GameObject target = CreateObject("Target");
            KnockbackDamageOnImpactCollisionBehavior behavior = new(17f);

            Assert.DoesNotThrow(() => behavior.OnKnockbackCollision(target, null, Vector3.zero));
        }

        [Test]
        public void Phase_IsAftermath()
        {
            KnockbackDamageOnImpactCollisionBehavior behavior = new(17f);

            Assert.AreEqual(RuntimeSpellBehaviorPhase.Aftermath, behavior.Phase);
        }

        [Test]
        public void WaveImpactDefinition_ForWaveSpell_CreatesBehaviorWithCaster()
        {
            WaveDamageOnImpactDefinition definition = new();
            WaveSpell spell = ScriptableObject.CreateInstance<WaveSpell>();
            GameObject source = CreateObject("Source");
            GameObject target = CreateObject("Target");
            TestDamageable damageable = target.AddComponent<TestDamageable>();
            SetImpactDamage(definition, 23f);

            bool created = definition.TryCreate(spell, source, out IRuntimeSpellBehavior behavior);
            ((IKnockbackCollisionHandler)behavior).OnKnockbackCollision(target, null, Vector3.zero);

            Assert.IsTrue(created);
            Assert.IsInstanceOf<KnockbackDamageOnImpactCollisionBehavior>(behavior);
            Assert.AreEqual(23f, damageable.LastData.Amount);
            Assert.AreSame(source, damageable.LastData.Source);
            Object.DestroyImmediate(spell);
        }

        [Test]
        public void WaveImpactDefinition_ForOtherSpell_DoesNotCreateBehavior()
        {
            WaveDamageOnImpactDefinition definition = new();
            Spell otherSpell = ScriptableObject.CreateInstance<TestSpell>();

            bool created = definition.TryCreate(otherSpell, null, out IRuntimeSpellBehavior behavior);

            Assert.IsFalse(created);
            Assert.IsNull(behavior);
            Object.DestroyImmediate(otherSpell);
        }

        private static void SetImpactDamage(WaveDamageOnImpactDefinition definition, float value)
        {
            FieldInfo field = typeof(WaveDamageOnImpactDefinition).GetField(
                "impactDamage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(definition, value);
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public int CallCount { get; private set; }
            public DamageData LastData { get; private set; }
            public bool CanReceiveDamage => true;

            public DamageResult ApplyDamage(DamageData data)
            {
                CallCount++;
                LastData = data;
                return DamageResult.Applied(data, data.Amount, 100f, 100f - data.Amount, false);
            }
        }

        private sealed class TestSpell : Spell
        {
            public override void OnCast(CasterData casterData)
            {
            }
        }
    }
}
