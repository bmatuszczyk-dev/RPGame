using System;
using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
        public void Asset_StoresConcreteRuntimeBehaviorTemplate()
        {
            TestRuntimeBehavior template = new("Template");
            SetBehavior(effect, template);
            SerializedObject serializedObject = new(effect);

            object storedTemplate =
                serializedObject.FindProperty("behavior").managedReferenceValue;

            Assert.IsInstanceOf<TestRuntimeBehavior>(storedTemplate);
        }

        [Test]
        public void CreateRuntimeBehaviors_CreatesFreshBehaviorForEachCast()
        {
            TestRuntimeBehavior template = new("Template");
            SetBehavior(effect, template);
            aggregator.Add(effect);
            TestSpell spell = ScriptableObject.CreateInstance<TestSpell>();
            GameObject caster = new("Caster");

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
        public void CreateRuntimeBehaviors_DoesNotInitializeTemplate()
        {
            TestRuntimeBehavior template = new("Template");
            SetBehavior(effect, template);
            TestRuntimeBehavior storedTemplate = GetStoredBehavior<TestRuntimeBehavior>(effect);
            aggregator.Add(effect);
            TestSpell spell = ScriptableObject.CreateInstance<TestSpell>();
            GameObject caster = new("Caster");

            aggregator.CreateRuntimeBehaviors(spell, caster);

            Assert.IsFalse(storedTemplate.Initialized);
            Assert.IsNull(storedTemplate.Source);

            Object.DestroyImmediate(spell);
            Object.DestroyImmediate(caster);
        }

        [Test]
        public void CreateRuntimeBehaviors_CopiesSerializedConfiguration()
        {
            GameObject referencedObject = new("Referenced Object");
            TestRuntimeBehavior template = new("Configured")
            {
                AssetReference = referencedObject
            };
            SetBehavior(effect, template);
            aggregator.Add(effect);
            TestSpell spell = ScriptableObject.CreateInstance<TestSpell>();
            GameObject caster = new("Caster");

            IReadOnlyList<IRuntimeSpellBehavior> behaviors =
                aggregator.CreateRuntimeBehaviors(spell, caster);

            TestRuntimeBehavior runtimeBehavior = (TestRuntimeBehavior)behaviors[0];
            Assert.AreEqual("Configured", runtimeBehavior.Name);
            Assert.AreSame(referencedObject, runtimeBehavior.AssetReference);

            Object.DestroyImmediate(spell);
            Object.DestroyImmediate(caster);
            Object.DestroyImmediate(referencedObject);
        }

        [Test]
        public void CreateRuntimeBehaviors_DoesNotShareNonSerializedRuntimeState()
        {
            TestRuntimeBehavior template = new("Template");
            SetBehavior(effect, template);
            TestRuntimeBehavior storedTemplate = GetStoredBehavior<TestRuntimeBehavior>(effect);
            object templateRuntimeState = storedTemplate.RuntimeState;
            aggregator.Add(effect);
            TestSpell spell = ScriptableObject.CreateInstance<TestSpell>();
            GameObject caster = new("Caster");

            IReadOnlyList<IRuntimeSpellBehavior> behaviors =
                aggregator.CreateRuntimeBehaviors(spell, caster);

            TestRuntimeBehavior runtimeBehavior = (TestRuntimeBehavior)behaviors[0];
            Assert.AreNotSame(templateRuntimeState, runtimeBehavior.RuntimeState);

            Object.DestroyImmediate(spell);
            Object.DestroyImmediate(caster);
        }

        [Test]
        public void CreateRuntimeBehaviors_WhenBehaviorDoesNotSupportSpell_FiltersBehavior()
        {
            TestRuntimeBehavior template = new("Template")
            {
                Supported = false
            };
            SetBehavior(effect, template);
            aggregator.Add(effect);
            TestSpell spell = ScriptableObject.CreateInstance<TestSpell>();
            GameObject caster = new("Caster");

            IReadOnlyList<IRuntimeSpellBehavior> behaviors =
                aggregator.CreateRuntimeBehaviors(spell, caster);

            Assert.IsEmpty(behaviors);

            Object.DestroyImmediate(spell);
            Object.DestroyImmediate(caster);
        }

        [Test]
        public void CreateRuntimeBehaviors_AggregatesMultipleRuntimeEffects()
        {
            RuntimeBehaviorEffectDefinition secondEffect =
                ScriptableObject.CreateInstance<RuntimeBehaviorEffectDefinition>();
            SetBehavior(effect, new TestRuntimeBehavior("First"));
            SetBehavior(secondEffect, new TestRuntimeBehavior("Second"), 1);
            aggregator.AddRange(new[] { effect, secondEffect });

            IReadOnlyList<IRuntimeSpellBehavior> behaviors =
                aggregator.CreateRuntimeBehaviors(null, null);

            Assert.AreEqual(2, behaviors.Count);
            Object.DestroyImmediate(secondEffect);
        }

        [Test]
        public void CreateRuntimeBehaviors_OrdersSpellBehaviorsByPhaseAndExecutionOrder()
        {
            RuntimeBehaviorEffectDefinition firstEffect =
                ScriptableObject.CreateInstance<RuntimeBehaviorEffectDefinition>();
            RuntimeBehaviorEffectDefinition secondEffect =
                ScriptableObject.CreateInstance<RuntimeBehaviorEffectDefinition>();
            SetBehavior(firstEffect, new TestRuntimeBehavior("Order 1"), 1);
            SetBehavior(secondEffect, new TestRuntimeBehavior("Order 0"), 0);
            aggregator.AddRange(new[] { firstEffect, secondEffect });

            IReadOnlyList<IRuntimeSpellBehavior> behaviors =
                aggregator.CreateRuntimeBehaviors(
                    ScriptableObject.CreateInstance<TestSpell>(),
                    new GameObject("Caster"));

            Assert.AreEqual("Order 0", ((TestRuntimeBehavior)behaviors[0]).Name);
            Assert.AreEqual("Order 1", ((TestRuntimeBehavior)behaviors[1]).Name);
            Object.DestroyImmediate(firstEffect);
            Object.DestroyImmediate(secondEffect);
        }

        [Test]
        public void CreateRuntimeBehaviors_WhenExecutionOrderHasGap_AllowsGap()
        {
            RuntimeBehaviorEffectDefinition firstEffect =
                ScriptableObject.CreateInstance<RuntimeBehaviorEffectDefinition>();
            RuntimeBehaviorEffectDefinition secondEffect =
                ScriptableObject.CreateInstance<RuntimeBehaviorEffectDefinition>();
            SetBehavior(firstEffect, new TestRuntimeBehavior("Order 2"), 2);
            SetBehavior(secondEffect, new TestRuntimeBehavior("Order 0"), 0);
            aggregator.AddRange(new[] { firstEffect, secondEffect });

            IReadOnlyList<IRuntimeSpellBehavior> behaviors =
                aggregator.CreateRuntimeBehaviors(
                    ScriptableObject.CreateInstance<TestSpell>(),
                    new GameObject("Caster"));

            Assert.AreEqual("Order 0", ((TestRuntimeBehavior)behaviors[0]).Name);
            Assert.AreEqual("Order 2", ((TestRuntimeBehavior)behaviors[1]).Name);
            Object.DestroyImmediate(firstEffect);
            Object.DestroyImmediate(secondEffect);
        }

        [Test]
        public void CreateRuntimeBehaviors_WhenPhaseAndExecutionOrderConflict_Throws()
        {
            RuntimeBehaviorEffectDefinition secondEffect =
                ScriptableObject.CreateInstance<RuntimeBehaviorEffectDefinition>();
            SetBehavior(effect, new TestRuntimeBehavior("First"), 0);
            SetBehavior(secondEffect, new TestRuntimeBehavior("Second"), 0);
            aggregator.AddRange(new[] { effect, secondEffect });

            Assert.Throws<InvalidOperationException>(
                () => aggregator.CreateRuntimeBehaviors(
                    ScriptableObject.CreateInstance<TestSpell>(),
                    new GameObject("Caster")));

            Object.DestroyImmediate(secondEffect);
        }

        [Test]
        public void CreateRuntimeBehaviors_WhenSameOrderIsUsedByCollisionHandlers_AllowsOrder()
        {
            RuntimeBehaviorEffectDefinition firstEffect =
                ScriptableObject.CreateInstance<RuntimeBehaviorEffectDefinition>();
            RuntimeBehaviorEffectDefinition secondEffect =
                ScriptableObject.CreateInstance<RuntimeBehaviorEffectDefinition>();
            SetBehavior(firstEffect, new TestCollisionBehavior(), 0);
            SetBehavior(secondEffect, new TestCollisionBehavior(), 0);
            aggregator.AddRange(new[] { firstEffect, secondEffect });

            IReadOnlyList<IRuntimeSpellBehavior> behaviors =
                aggregator.CreateRuntimeBehaviors(
                    ScriptableObject.CreateInstance<TestSpell>(),
                    new GameObject("Caster"));

            Assert.AreEqual(2, behaviors.Count);
            Object.DestroyImmediate(firstEffect);
            Object.DestroyImmediate(secondEffect);
        }

        [Test]
        public void Clone_CopiesPrimitiveConfig()
        {
            TestRuntimeBehavior template = new("Template")
            {
                PrimitiveValue = 17
            };

            TestRuntimeBehavior clone =
                (TestRuntimeBehavior)RuntimeSpellBehaviorCloner.Clone(template);

            Assert.AreEqual(17, clone.PrimitiveValue);
        }

        [Test]
        public void Clone_KeepsUnityObjectReference()
        {
            GameObject referencedObject = new("Referenced Object");
            TestRuntimeBehavior template = new("Template")
            {
                AssetReference = referencedObject
            };

            TestRuntimeBehavior clone =
                (TestRuntimeBehavior)RuntimeSpellBehaviorCloner.Clone(template);

            Assert.AreSame(referencedObject, clone.AssetReference);
            Object.DestroyImmediate(referencedObject);
        }

        [Test]
        public void Clone_DoesNotCopyNonSerializedRuntimeState()
        {
            TestRuntimeBehavior template = new("Template");
            object templateRuntimeState = template.RuntimeState;

            TestRuntimeBehavior clone =
                (TestRuntimeBehavior)RuntimeSpellBehaviorCloner.Clone(template);

            Assert.AreNotSame(templateRuntimeState, clone.RuntimeState);
        }

        [Test]
        public void Clone_CopiesListAndElements()
        {
            NestedConfig nested = new()
            {
                Name = "Nested"
            };
            TestRuntimeBehavior template = new("Template")
            {
                Configs = new List<NestedConfig> { nested }
            };

            TestRuntimeBehavior clone =
                (TestRuntimeBehavior)RuntimeSpellBehaviorCloner.Clone(template);

            Assert.AreNotSame(template.Configs, clone.Configs);
            Assert.AreNotSame(template.Configs[0], clone.Configs[0]);
            Assert.AreEqual("Nested", clone.Configs[0].Name);
        }

        [Test]
        public void Clone_CopiesArrayAndElements()
        {
            NestedConfig nested = new()
            {
                Name = "Nested"
            };
            TestRuntimeBehavior template = new("Template")
            {
                ConfigArray = new[] { nested }
            };

            TestRuntimeBehavior clone =
                (TestRuntimeBehavior)RuntimeSpellBehaviorCloner.Clone(template);

            Assert.AreNotSame(template.ConfigArray, clone.ConfigArray);
            Assert.AreNotSame(template.ConfigArray[0], clone.ConfigArray[0]);
            Assert.AreEqual("Nested", clone.ConfigArray[0].Name);
        }

        [Test]
        public void Clone_CopiesNestedSerializableObjectIndependently()
        {
            TestRuntimeBehavior template = new("Template")
            {
                Nested = new NestedConfig
                {
                    Name = "Nested"
                }
            };

            TestRuntimeBehavior clone =
                (TestRuntimeBehavior)RuntimeSpellBehaviorCloner.Clone(template);

            Assert.AreNotSame(template.Nested, clone.Nested);
            Assert.AreEqual("Nested", clone.Nested.Name);
        }

        [Test]
        public void Clone_PreservesSharedReferencesInsideClone()
        {
            NestedConfig shared = new()
            {
                Name = "Shared"
            };
            TestRuntimeBehavior template = new("Template")
            {
                FirstShared = shared,
                SecondShared = shared
            };

            TestRuntimeBehavior clone =
                (TestRuntimeBehavior)RuntimeSpellBehaviorCloner.Clone(template);

            Assert.AreSame(clone.FirstShared, clone.SecondShared);
            Assert.AreNotSame(shared, clone.FirstShared);
        }

        [Test]
        public void Clone_HandlesCircularReferences()
        {
            CyclicConfig cyclic = new()
            {
                Name = "Cycle"
            };
            cyclic.Self = cyclic;
            TestRuntimeBehavior template = new("Template")
            {
                Cyclic = cyclic
            };

            TestRuntimeBehavior clone =
                (TestRuntimeBehavior)RuntimeSpellBehaviorCloner.Clone(template);

            Assert.AreSame(clone.Cyclic, clone.Cyclic.Self);
            Assert.AreNotSame(cyclic, clone.Cyclic);
        }

        [Test]
        public void Clone_WhenManagedTypeCannotBeCreated_Throws()
        {
            TestRuntimeBehavior template = new("Template")
            {
                Unsupported = new UnsupportedConfig("Unsupported")
            };

            Assert.Throws<InvalidOperationException>(
                () => RuntimeSpellBehaviorCloner.Clone(template));
        }

        [Test]
        public void Clone_WhenArrayIsMultidimensional_ThrowsInvalidOperationException()
        {
            TestRuntimeBehavior template = new("Template")
            {
                MultidimensionalArray = new NestedConfig[1, 1]
            };

            Assert.Throws<InvalidOperationException>(
                () => RuntimeSpellBehaviorCloner.Clone(template));
        }

        [Test]
        public void Clone_CopiesStructManagedFieldsIndependently()
        {
            NestedConfig nested = new()
            {
                Name = "Nested"
            };
            TestRuntimeBehavior template = new("Template")
            {
                StructConfig = new StructConfig
                {
                    Amount = 7,
                    Nested = nested
                }
            };

            TestRuntimeBehavior clone =
                (TestRuntimeBehavior)RuntimeSpellBehaviorCloner.Clone(template);

            Assert.AreEqual(7, clone.StructConfig.Amount);
            Assert.AreNotSame(template.StructConfig.Nested, clone.StructConfig.Nested);
            Assert.AreEqual("Nested", clone.StructConfig.Nested.Name);
        }

        [Test]
        public void Clone_WhenCalledTwice_CreatesIndependentRuntimeGraphs()
        {
            NestedConfig shared = new()
            {
                Name = "Shared"
            };
            TestRuntimeBehavior template = new("Template")
            {
                FirstShared = shared,
                SecondShared = shared
            };

            TestRuntimeBehavior firstClone =
                (TestRuntimeBehavior)RuntimeSpellBehaviorCloner.Clone(template);
            TestRuntimeBehavior secondClone =
                (TestRuntimeBehavior)RuntimeSpellBehaviorCloner.Clone(template);

            Assert.AreSame(firstClone.FirstShared, firstClone.SecondShared);
            Assert.AreSame(secondClone.FirstShared, secondClone.SecondShared);
            Assert.AreNotSame(firstClone.FirstShared, secondClone.FirstShared);
        }

        private static void SetBehavior(
            RuntimeBehaviorEffectDefinition target,
            IRuntimeSpellBehavior behavior,
            int executionOrder = 0)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty("behavior").managedReferenceValue = behavior;
            serializedObject.FindProperty("executionOrder").intValue = executionOrder;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T GetStoredBehavior<T>(RuntimeBehaviorEffectDefinition target)
            where T : class, IRuntimeSpellBehavior
        {
            SerializedObject serializedObject = new(target);
            return serializedObject.FindProperty("behavior").managedReferenceValue as T;
        }

        private sealed class TestSpell : Spell
        {
            public override void OnCast(CasterData casterData)
            {
            }
        }

        [Serializable]
        private sealed class TestRuntimeBehavior :
            ISpellBehavior,
            IInitializableRuntimeSpellBehavior
        {
            public string Name;
            public UnityEngine.Object AssetReference;
            public int PrimitiveValue;
            public bool Supported = true;
            public SpellBehaviorPhase TestPhase = SpellBehaviorPhase.PostResolve;
            public List<NestedConfig> Configs;
            public NestedConfig[] ConfigArray;
            public NestedConfig Nested;
            public NestedConfig FirstShared;
            public NestedConfig SecondShared;
            public CyclicConfig Cyclic;
            [SerializeReference] public UnsupportedConfig Unsupported;
            public NestedConfig[,] MultidimensionalArray;
            public StructConfig StructConfig;

            [NonSerialized] public GameObject Source;
            [NonSerialized] public bool Initialized;
            [NonSerialized] public object RuntimeState = new();

            public TestRuntimeBehavior()
            {
            }

            public TestRuntimeBehavior(string name)
            {
                Name = name;
            }

            public SpellBehaviorPhase Phase => TestPhase;

            public bool Supports(Spell spell)
            {
                return Supported;
            }

            public void Initialize(GameObject caster)
            {
                Source = caster;
                Initialized = true;
            }

            public void Execute(SpellBehaviorContext context)
            {
            }
        }

        [Serializable]
        private sealed class TestCollisionBehavior :
            IKnockbackCollisionHandler,
            IInitializableRuntimeSpellBehavior
        {
            public bool Supports(Spell spell)
            {
                return true;
            }

            public void Initialize(GameObject caster)
            {
            }

            public void OnKnockbackCollision(GameObject target, Collider obstacle, Vector3 point)
            {
            }
        }

        [Serializable]
        private sealed class NestedConfig
        {
            public string Name;
            public NestedConfig Child;
        }

        [Serializable]
        private sealed class CyclicConfig
        {
            public string Name;
            public CyclicConfig Self;
        }

        [Serializable]
        private sealed class UnsupportedConfig
        {
            public string Name;

            public UnsupportedConfig(string name)
            {
                Name = name;
            }
        }

        [Serializable]
        private struct StructConfig
        {
            public int Amount;
            public NestedConfig Nested;
        }
    }
}
