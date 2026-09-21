using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Combat.Tests
{
    public sealed class HuntersMarkBehaviorTests
    {
        private readonly List<Object> createdObjects = new();
        private MarkStatusDefinition markDefinition;
        private GameObject source;
        private GameObject target;
        private StatusAggregator statusReceiver;
        private TestDamageable damageable;

        [SetUp]
        public void SetUp()
        {
            markDefinition = ScriptableObject.CreateInstance<MarkStatusDefinition>();
            createdObjects.Add(markDefinition);
            source = CreateGameObject("Source");
            target = CreateGameObject("Target");
            statusReceiver = target.AddComponent<StatusAggregator>();
            InvokeAwake(statusReceiver);
            damageable = target.AddComponent<TestDamageable>();
            MarkProgressReceiver progressReceiver = target.AddComponent<MarkProgressReceiver>();
            Configure(progressReceiver, markDefinition);
        }

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
        public void Resolve_WhenTargetHasNoMark_DoesNotApplyHuntersMarkDamage()
        {
            DamageResultCaptureBehavior capture = new();
            MarkActivationCaptureBehavior markActivation = new();

            ExecuteProjectileHit(new IRuntimeSpellBehavior[]
            {
                CreateHuntersMarkBehavior(5f),
                capture,
                markActivation
            });

            Assert.AreEqual(1, damageable.Results.Count);
            Assert.AreEqual(10f, damageable.Results[0].AppliedAmount);
            Assert.AreEqual(1, capture.Results.Count);
            Assert.AreEqual(10f, capture.Results[0].AppliedAmount);
            Assert.IsFalse(markActivation.HasState);
        }

        [Test]
        public void Resolve_WhenTargetHasMark_ConsumesMarkAndAppliesBonusDamage()
        {
            statusReceiver.ApplyStatus(markDefinition, 5f, CreateStatusContext());
            DamageResultCaptureBehavior capture = new();
            MarkActivationCaptureBehavior markActivation = new();

            ExecuteProjectileHit(new IRuntimeSpellBehavior[]
            {
                CreateHuntersMarkBehavior(5f),
                capture,
                markActivation
            });

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));
            Assert.IsTrue(markActivation.HasState);
            Assert.AreEqual(2, damageable.Results.Count);
            Assert.AreEqual(10f, damageable.Results[0].AppliedAmount);
            Assert.AreEqual(5f, damageable.Results[1].AppliedAmount);
            Assert.AreEqual(2, capture.Results.Count);
            Assert.AreEqual(10f, capture.Results[0].AppliedAmount);
            Assert.AreEqual(5f, capture.Results[1].AppliedAmount);
        }

        [Test]
        public void Resolve_WhenCurrentHitCreatesMark_DoesNotUseCreatedMarkForHuntersMark()
        {
            IRuntimeSpellBehavior huntersMark = CreateHuntersMarkBehavior(5f);
            ExecuteProjectileHit(new IRuntimeSpellBehavior[0], new SpellId("wave"));
            ExecuteProjectileHit(new IRuntimeSpellBehavior[0], new SpellId("orbit"));
            damageable.Clear();

            ExecuteProjectileHit(new[] { huntersMark }, new SpellId("fire_zone"));

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
            Assert.AreEqual(1, damageable.Results.Count);
            Assert.AreEqual(10f, damageable.Results[0].AppliedAmount);
        }

        [Test]
        public void Resolve_WhenResolveFails_DoesNotApplyHuntersMarkDamage()
        {
            statusReceiver.ApplyStatus(markDefinition, 5f, CreateStatusContext());
            IRuntimeSpellBehavior huntersMark = CreateHuntersMarkBehavior(5f);
            CasterData casterData = CreateCasterData(
                new IRuntimeSpellBehavior[] { huntersMark },
                new SpellId("wave"));

            SpellBehaviorPipelineExecutor.Resolve(
                casterData,
                target,
                new FailingResolveBehavior());

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));
            Assert.AreEqual(0, damageable.Results.Count);
        }

        [Test]
        public void Resolve_WhenBehaviorIsReused_DoesNotLeakHuntersMarkStateBetweenExecutions()
        {
            IRuntimeSpellBehavior huntersMark = CreateHuntersMarkBehavior(5f);
            MarkActivationCaptureBehavior firstActivation = new();
            MarkActivationCaptureBehavior secondActivation = new();
            statusReceiver.ApplyStatus(markDefinition, 5f, CreateStatusContext());

            ExecuteProjectileHit(
                new IRuntimeSpellBehavior[] { huntersMark, firstActivation },
                new SpellId("wave"));
            damageable.Clear();

            ExecuteProjectileHit(
                new IRuntimeSpellBehavior[] { huntersMark, secondActivation },
                new SpellId("orbit"));

            Assert.IsTrue(firstActivation.HasState);
            Assert.IsFalse(secondActivation.HasState);
            Assert.AreEqual(1, damageable.Results.Count);
            Assert.AreEqual(10f, damageable.Results[0].AppliedAmount);
        }

        [Test]
        public void Resolve_WhenHuntersMarkExecutesTwiceInSameExecution_AppliesPayoffOnce()
        {
            IRuntimeSpellBehavior huntersMark = CreateHuntersMarkBehavior(5f);
            statusReceiver.ApplyStatus(markDefinition, 5f, CreateStatusContext());

            ExecuteProjectileHit(new[] { huntersMark, huntersMark }, new SpellId("wave"));

            Assert.AreEqual(2, damageable.Results.Count);
            Assert.AreEqual(10f, damageable.Results[0].AppliedAmount);
            Assert.AreEqual(5f, damageable.Results[1].AppliedAmount);
        }

        [Test]
        public void Definition_ForProjectileSpell_CreatesHuntersMarkBehavior()
        {
            HuntersMarkBehaviorDefinition definition = CreateDefinition(5f);
            ProjectileSpell projectileSpell = ScriptableObject.CreateInstance<ProjectileSpell>();
            createdObjects.Add(projectileSpell);

            bool created = definition.TryCreate(
                projectileSpell,
                source,
                out IRuntimeSpellBehavior behavior);

            Assert.IsTrue(created);
            Assert.IsInstanceOf<HuntersMarkBehavior>(behavior);
        }

        [Test]
        public void Definition_ForNonProjectileSpell_DoesNotCreateBehavior()
        {
            HuntersMarkBehaviorDefinition definition = CreateDefinition(5f);
            TestSpell spell = ScriptableObject.CreateInstance<TestSpell>();
            createdObjects.Add(spell);

            bool created = definition.TryCreate(spell, source, out IRuntimeSpellBehavior behavior);

            Assert.IsFalse(created);
            Assert.IsNull(behavior);
        }

        private IRuntimeSpellBehavior CreateHuntersMarkBehavior(float bonusDamage)
        {
            HuntersMarkBehaviorDefinition definition = CreateDefinition(bonusDamage);
            ProjectileSpell spell = ScriptableObject.CreateInstance<ProjectileSpell>();
            createdObjects.Add(spell);

            definition.TryCreate(spell, source, out IRuntimeSpellBehavior behavior);
            return behavior;
        }

        private HuntersMarkBehaviorDefinition CreateDefinition(float bonusDamage)
        {
            HuntersMarkBehaviorDefinition definition = new();
            SetPrivateField(definition, "markDefinition", markDefinition);
            SetPrivateField(definition, "bonusDamage", bonusDamage);
            return definition;
        }

        private void ExecuteProjectileHit(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            SpellId? spellId = null)
        {
            CasterData casterData = CreateCasterData(
                behaviors,
                spellId ?? new SpellId("wave"));
            SpellBehaviorPipelineExecutor.Resolve(
                casterData,
                target,
                new DamageResolveBehavior(damageable, casterData));
        }

        private CasterData CreateCasterData(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            SpellId spellId)
        {
            return new CasterDataBuilder(source, source.transform, target.transform)
                .WithSpellId(spellId)
                .WithRuntimeBehaviors(behaviors)
                .WithDamageRanges(new[]
                {
                    new PartialDamageRange(10f, 10f, DamageType.Magical, DamageElement.None)
                })
                .Build();
        }

        private StatusContext CreateStatusContext()
        {
            return new StatusContext(new StatusSourceId("Test"), source);
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void Configure(
            MarkProgressReceiver progressReceiver,
            MarkStatusDefinition markDefinition)
        {
            SerializedObject serializedReceiver = new(progressReceiver);
            serializedReceiver.FindProperty("markDefinition").objectReferenceValue = markDefinition;
            serializedReceiver.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokeAwake(StatusAggregator statusReceiver)
        {
            MethodInfo awake = typeof(StatusAggregator).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(statusReceiver, null);
        }

        private static void SetPrivateField<T>(
            object target,
            string fieldName,
            T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private sealed class DamageResultCaptureBehavior : ISpellBehavior
        {
            public SpellBehaviorPhase Phase => SpellBehaviorPhase.Effect;
            public List<DamageResult> Results { get; } = new();

            public void Execute(SpellBehaviorContext context)
            {
                if (!context.TryGetResolveResults(out IReadOnlyList<DamageResult> results))
                {
                    return;
                }

                Results.AddRange(results);
            }
        }

        private sealed class MarkActivationCaptureBehavior : ISpellBehavior
        {
            public SpellBehaviorPhase Phase => SpellBehaviorPhase.Effect;
            public bool HasState { get; private set; }

            public void Execute(SpellBehaviorContext context)
            {
                HasState = context.TryGetExecutionState<MarkActivationState>(out _);
            }
        }

        private sealed class FailingResolveBehavior : ISpellResolveBehavior
        {
            public SpellBehaviorPhase Phase => SpellBehaviorPhase.Resolve;

            public bool Resolve(SpellBehaviorContext context)
            {
                return false;
            }
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public List<DamageResult> Results { get; } = new();
            public bool CanReceiveDamage => true;

            public DamageResult ApplyDamage(DamageData data)
            {
                DamageResult result = DamageResult.Applied(
                    data,
                    data.Amount,
                    100f,
                    100f - data.Amount,
                    false);
                Results.Add(result);
                return result;
            }

            public void Clear()
            {
                Results.Clear();
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
