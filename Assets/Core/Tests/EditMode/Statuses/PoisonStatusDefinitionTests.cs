using NUnit.Framework;
using RPGame.Core.Damage;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Core.Tests.Statuses
{
    public sealed class PoisonStatusDefinitionTests
    {
        private PoisonStatusDefinition poison;

        [SetUp]
        public void SetUp()
        {
            poison = ScriptableObject.CreateInstance<PoisonStatusDefinition>();
            SerializedObject serializedPoison = new(poison);
            serializedPoison.FindProperty("damagePerTick").floatValue = 3f;
            serializedPoison.FindProperty("tickInterval").floatValue = 1f;
            serializedPoison.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(poison);
        }

        [Test]
        public void CanApply_RequiresDamageable()
        {
            Assert.IsFalse(poison.CanApply(new StatusTarget(null, null, null)));
            Assert.IsTrue(poison.CanApply(new StatusTarget(null, null, new FakeDamageable())));
        }

        [Test]
        public void Tick_DealsConfiguredDamageAtInterval()
        {
            FakeDamageable damageable = new();
            StatusContainer container = new(new StatusTarget(null, null, damageable));
            container.Add(poison, 3f, new StatusContext(new StatusSourceId("PoisonTest"), null));

            container.Tick(0.5f);
            Assert.That(damageable.TotalDamage, Is.Zero);

            container.Tick(0.5f);
            Assert.That(damageable.TotalDamage, Is.EqualTo(3f));
        }

        [Test]
        public void Poison_UsesSingleInstanceRefreshSemantics()
        {
            Assert.That(poison.ReapplyPolicy, Is.EqualTo(ReapplyPolicy.Refresh));
            Assert.That(poison.ConcurrentStatusPolicy, Is.EqualTo(ConcurrentStatusPolicy.SingleInstance));
        }

        private sealed class FakeDamageable : IDamageable
        {
            public bool CanReceiveDamage => true;
            public float TotalDamage { get; private set; }

            public DamageResult ApplyDamage(DamageData data)
            {
                TotalDamage += data.Amount;
                return DamageResult.Applied(data, data.Amount, 100f, 100f - data.Amount, false);
            }
        }
    }
}
