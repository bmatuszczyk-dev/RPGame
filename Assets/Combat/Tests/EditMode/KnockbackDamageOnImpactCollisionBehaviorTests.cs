using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Damage;
using RPGame.Core.Movement;
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
            Assert.AreEqual(DamageType.Physical, damageable.LastData.Parts[0].DamageType);
            Assert.AreEqual(DamageElement.None, damageable.LastData.Parts[0].DamageElement);
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
        public void Dispatch_WithKnockbackCollisionHandler_ExecutesHandler()
        {
            GameObject target = CreateObject("Target");
            Collider obstacle = CreateObject("Obstacle").AddComponent<BoxCollider>();
            TestDamageable damageable = target.AddComponent<TestDamageable>();
            KnockbackDamageOnImpactCollisionBehavior behavior = new(17f);
            List<IRuntimeSpellBehavior> behaviors = new() { behavior };

            KnockbackEndDispatcher.Dispatch(
                behaviors,
                target,
                new KnockbackEndContext(KnockbackEndReason.Collision, obstacle, Vector3.zero));

            Assert.AreEqual(1, damageable.CallCount);
        }

        [Test]
        public void Dispatch_WhenKnockbackCompletes_DoesNotExecuteHandler()
        {
            GameObject target = CreateObject("Target");
            TestDamageable damageable = target.AddComponent<TestDamageable>();
            KnockbackDamageOnImpactCollisionBehavior behavior = new(17f);
            List<IRuntimeSpellBehavior> behaviors = new() { behavior };

            KnockbackEndDispatcher.Dispatch(
                behaviors,
                target,
                new KnockbackEndContext(KnockbackEndReason.Completed));

            Assert.AreEqual(0, damageable.CallCount);
        }

        [Test]
        public void Supports_ForWaveSpell_ReturnsTrue()
        {
            KnockbackDamageOnImpactCollisionBehavior behavior = new(23f);
            WaveSpell spell = ScriptableObject.CreateInstance<WaveSpell>();

            Assert.IsTrue(behavior.Supports(spell));
            Object.DestroyImmediate(spell);
        }

        [Test]
        public void Supports_ForOtherSpell_ReturnsFalse()
        {
            KnockbackDamageOnImpactCollisionBehavior behavior = new(23f);
            Spell otherSpell = ScriptableObject.CreateInstance<TestSpell>();

            Assert.IsFalse(behavior.Supports(otherSpell));

            Object.DestroyImmediate(otherSpell);
        }

        [Test]
        public void Initialize_SetsDamageSource()
        {
            GameObject source = CreateObject("Source");
            GameObject target = CreateObject("Target");
            TestDamageable damageable = target.AddComponent<TestDamageable>();
            KnockbackDamageOnImpactCollisionBehavior behavior = new(23f);

            behavior.Initialize(source);
            behavior.OnKnockbackCollision(target, null, Vector3.zero);

            Assert.AreEqual(23f, damageable.LastData.Amount);
            Assert.AreSame(source, damageable.LastData.Source);
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
