using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using UnityEditor;
using UnityEngine;

namespace RPGame.Core.Tests.Effects
{
    public sealed class RuntimeBehaviorEffectTests
    {
        private RuntimeBehaviorEffectDefinition effect;
        private GameObject gameObject;
        private EffectAggregator aggregator;

        [SetUp]
        public void SetUp()
        {
            effect = ScriptableObject.CreateInstance<RuntimeBehaviorEffectDefinition>();
            gameObject = new GameObject("Runtime Behavior Effect Tests");
            aggregator = gameObject.AddComponent<EffectAggregator>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Add_StoresRuntimeBehaviorEffect()
        {
            aggregator.Add(effect);

            Assert.AreEqual(1, aggregator.Effects.Count);
            Assert.AreSame(effect, aggregator.Effects[0].Definition);
        }

        [Test]
        public void CreateRuntimeBehaviors_CreatesFreshBehaviorForEachCast()
        {
            SetBehavior(new TestRuntimeSpellBehaviorDefinition());
            aggregator.Add(effect);
            TestSpell spell = ScriptableObject.CreateInstance<TestSpell>();
            GameObject caster = new GameObject("Caster");

            IReadOnlyList<IRuntimeSpellBehavior> firstCast =
                aggregator.CreateRuntimeBehaviors(spell, caster);
            IReadOnlyList<IRuntimeSpellBehavior> secondCast =
                aggregator.CreateRuntimeBehaviors(spell, caster);

            Assert.AreEqual(1, firstCast.Count);
            Assert.AreEqual(1, secondCast.Count);
            Assert.AreNotSame(firstCast[0], secondCast[0]);
            Assert.AreSame(caster, ((TestRuntimeBehavior)firstCast[0]).Source);

            Object.DestroyImmediate(spell);
            Object.DestroyImmediate(caster);
        }

        [Test]
        public void CreateRuntimeBehaviors_AggregatesMultipleRuntimeEffects()
        {
            RuntimeBehaviorEffectDefinition secondEffect =
                ScriptableObject.CreateInstance<RuntimeBehaviorEffectDefinition>();
            SetBehavior(new TestRuntimeSpellBehaviorDefinition());
            SetBehavior(secondEffect, new TestRuntimeSpellBehaviorDefinition());
            aggregator.AddRange(new[] { effect, secondEffect });

            IReadOnlyList<IRuntimeSpellBehavior> behaviors =
                aggregator.CreateRuntimeBehaviors(null, null);

            Assert.AreEqual(2, behaviors.Count);
            Object.DestroyImmediate(secondEffect);
        }

        private static void SetBehavior(RuntimeBehaviorEffectDefinition target, RuntimeSpellBehaviorDefinition behavior)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty("behavior").managedReferenceValue = behavior;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetBehavior(RuntimeSpellBehaviorDefinition behavior)
        {
            SetBehavior(effect, behavior);
        }

        private sealed class TestSpell : Spell
        {
            public override void OnCast(CasterData casterData)
            {
            }
        }

        [System.Serializable]
        private sealed class TestRuntimeSpellBehaviorDefinition : RuntimeSpellBehaviorDefinition
        {
            public override bool TryCreate(
                Spell spell,
                GameObject casterObject,
                out IRuntimeSpellBehavior behavior)
            {
                behavior = new TestRuntimeBehavior(casterObject);
                return true;
            }
        }

        private sealed class TestRuntimeBehavior : IRuntimeSpellBehavior
        {
            public TestRuntimeBehavior(GameObject source)
            {
                Source = source;
            }

            public GameObject Source { get; }
        }
    }
}
