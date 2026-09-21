using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statistics;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Combat.Tests
{
    public sealed class FinisherBehaviorTests
    {
        private readonly List<Object> createdObjects = new();
        private MarkStatusDefinition markDefinition;
        private GameObject source;
        private GameObject target;
        private StatusAggregator statusReceiver;
        private TestTarget testTarget;

        [SetUp]
        public void SetUp()
        {
            markDefinition = ScriptableObject.CreateInstance<MarkStatusDefinition>();
            createdObjects.Add(markDefinition);
            source = CreateGameObject("Source");
            target = CreateGameObject("Target");
            statusReceiver = target.AddComponent<StatusAggregator>();
            InvokeAwake(statusReceiver);
            testTarget = target.AddComponent<TestTarget>();
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
        public void Resolve_WhenTargetHasNoMark_DoesNotExecuteFinisher()
        {
            testTarget.SetHealth(30f, 100f);
            DamageResultCaptureBehavior capture = new();
            MarkConsumedFlagCaptureBehavior markConsumed = new();

            ExecuteProjectileHit(new IRuntimeSpellBehavior[]
            {
                CreateFinisherBehavior(0.25f),
                capture,
                markConsumed
            });

            Assert.IsFalse(markConsumed.HasFlag);
            Assert.AreEqual(1, testTarget.Results.Count);
            Assert.AreEqual(10f, testTarget.Results[0].AppliedAmount);
            Assert.AreEqual(20f, testTarget.CurrentHealth);
            Assert.AreEqual(1, capture.Results.Count);
        }

        [Test]
        public void Resolve_WhenTargetHasMarkAndHealthIsAboveThreshold_ConsumesMarkWithoutExecute()
        {
            testTarget.SetHealth(60f, 100f);
            statusReceiver.ApplyStatus(markDefinition, 5f, CreateStatusContext());
            DamageResultCaptureBehavior capture = new();
            MarkConsumedFlagCaptureBehavior markConsumed = new();

            ExecuteProjectileHit(new IRuntimeSpellBehavior[]
            {
                CreateFinisherBehavior(0.25f),
                capture,
                markConsumed
            });

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));
            Assert.IsTrue(markConsumed.HasFlag);
            Assert.AreEqual(1, testTarget.Results.Count);
            Assert.AreEqual(10f, testTarget.Results[0].AppliedAmount);
            Assert.AreEqual(50f, testTarget.CurrentHealth);
            Assert.AreEqual(1, capture.Results.Count);
        }

        [Test]
        public void Resolve_WhenTargetHasMarkAndHealthIsBelowThreshold_ExecutesRemainingHealth()
        {
            testTarget.SetHealth(30f, 100f);
            statusReceiver.ApplyStatus(markDefinition, 5f, CreateStatusContext());
            DamageResultCaptureBehavior capture = new();

            ExecuteProjectileHit(new IRuntimeSpellBehavior[]
            {
                CreateFinisherBehavior(0.25f),
                capture
            });

            Assert.AreEqual(2, testTarget.Results.Count);
            Assert.AreEqual(10f, testTarget.Results[0].AppliedAmount);
            Assert.AreEqual(20f, testTarget.Results[1].AppliedAmount);
            Assert.IsTrue(testTarget.Results[1].WasFatal);
            Assert.AreEqual(0f, testTarget.CurrentHealth);
            Assert.AreEqual(2, capture.Results.Count);
            Assert.AreEqual(20f, capture.Results[1].AppliedAmount);
            Assert.IsTrue(capture.Results[1].WasFatal);
        }

        [Test]
        public void Resolve_WhenHealthIsExactlyThreshold_ExecutesFinisher()
        {
            testTarget.SetHealth(35f, 100f);
            statusReceiver.ApplyStatus(markDefinition, 5f, CreateStatusContext());

            ExecuteProjectileHit(new[]
            {
                CreateFinisherBehavior(0.25f)
            });

            Assert.AreEqual(2, testTarget.Results.Count);
            Assert.AreEqual(25f, testTarget.Results[1].AppliedAmount);
            Assert.IsTrue(testTarget.Results[1].WasFatal);
            Assert.AreEqual(0f, testTarget.CurrentHealth);
        }

        [Test]
        public void Resolve_WhenCurrentHitCreatesMark_DoesNotUseCreatedMarkForFinisher()
        {
            IRuntimeSpellBehavior finisher = CreateFinisherBehavior(0.25f);
            testTarget.SetHealth(100f, 100f);
            ExecuteProjectileHit(new IRuntimeSpellBehavior[0], new SpellId("wave"));
            ExecuteProjectileHit(new IRuntimeSpellBehavior[0], new SpellId("orbit"));
            testTarget.SetHealth(30f, 100f);

            ExecuteProjectileHit(new[] { finisher }, new SpellId("fire_zone"));

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
            Assert.AreEqual(1, testTarget.Results.Count);
            Assert.AreEqual(10f, testTarget.Results[0].AppliedAmount);
            Assert.AreEqual(20f, testTarget.CurrentHealth);
        }

        [Test]
        public void Resolve_WhenResolveFails_DoesNotExecuteFinisher()
        {
            testTarget.SetHealth(30f, 100f);
            statusReceiver.ApplyStatus(markDefinition, 5f, CreateStatusContext());
            IRuntimeSpellBehavior finisher = CreateFinisherBehavior(0.25f);
            CasterData casterData = CreateCasterData(
                new IRuntimeSpellBehavior[] { finisher },
                new SpellId("wave"));

            SpellBehaviorPipelineExecutor.Resolve(
                casterData,
                target,
                new FailingResolveBehavior());

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));
            Assert.AreEqual(0, testTarget.Results.Count);
            Assert.AreEqual(30f, testTarget.CurrentHealth);
        }

        [Test]
        public void Resolve_WhenBehaviorIsReused_DoesNotLeakMarkConsumedFlagBetweenExecutions()
        {
            IRuntimeSpellBehavior finisher = CreateFinisherBehavior(0.25f);
            MarkConsumedFlagCaptureBehavior firstMarkConsumed = new();
            MarkConsumedFlagCaptureBehavior secondMarkConsumed = new();
            testTarget.SetHealth(30f, 100f);
            statusReceiver.ApplyStatus(markDefinition, 5f, CreateStatusContext());

            ExecuteProjectileHit(
                new IRuntimeSpellBehavior[] { finisher, firstMarkConsumed },
                new SpellId("wave"));
            testTarget.SetHealth(30f, 100f);

            ExecuteProjectileHit(
                new IRuntimeSpellBehavior[] { finisher, secondMarkConsumed },
                new SpellId("orbit"));

            Assert.IsTrue(firstMarkConsumed.HasFlag);
            Assert.IsFalse(secondMarkConsumed.HasFlag);
            Assert.AreEqual(1, testTarget.Results.Count);
            Assert.AreEqual(10f, testTarget.Results[0].AppliedAmount);
            Assert.AreEqual(20f, testTarget.CurrentHealth);
        }

        [Test]
        public void Effect_WhenFinisherExecutes_SeesResolveAndFinisherResults()
        {
            testTarget.SetHealth(30f, 100f);
            statusReceiver.ApplyStatus(markDefinition, 5f, CreateStatusContext());
            DamageResultCaptureBehavior capture = new();

            ExecuteProjectileHit(new IRuntimeSpellBehavior[]
            {
                CreateFinisherBehavior(0.25f),
                capture
            });

            Assert.AreEqual(2, capture.Results.Count);
            Assert.AreEqual(10f, capture.Results[0].AppliedAmount);
            Assert.AreEqual(20f, capture.Results[1].AppliedAmount);
            Assert.IsTrue(capture.Results[1].WasFatal);
        }

        [Test]
        public void Definition_ForProjectileSpell_CreatesFinisherBehavior()
        {
            FinisherBehaviorDefinition definition = CreateDefinition(0.25f);
            ProjectileSpell projectileSpell = ScriptableObject.CreateInstance<ProjectileSpell>();
            createdObjects.Add(projectileSpell);

            bool created = definition.TryCreate(
                projectileSpell,
                source,
                out IRuntimeSpellBehavior behavior);

            Assert.IsTrue(created);
            Assert.IsInstanceOf<FinisherBehavior>(behavior);
        }

        [Test]
        public void Definition_ForNonProjectileSpell_DoesNotCreateBehavior()
        {
            FinisherBehaviorDefinition definition = CreateDefinition(0.25f);
            TestSpell spell = ScriptableObject.CreateInstance<TestSpell>();
            createdObjects.Add(spell);

            bool created = definition.TryCreate(spell, source, out IRuntimeSpellBehavior behavior);

            Assert.IsFalse(created);
            Assert.IsNull(behavior);
        }

        private IRuntimeSpellBehavior CreateFinisherBehavior(float healthThreshold)
        {
            FinisherBehaviorDefinition definition = CreateDefinition(healthThreshold);
            ProjectileSpell spell = ScriptableObject.CreateInstance<ProjectileSpell>();
            createdObjects.Add(spell);

            definition.TryCreate(spell, source, out IRuntimeSpellBehavior behavior);
            return behavior;
        }

        private FinisherBehaviorDefinition CreateDefinition(float healthThreshold)
        {
            FinisherBehaviorDefinition definition = new();
            SetPrivateField(definition, "markDefinition", markDefinition);
            SetPrivateField(definition, "healthThreshold", healthThreshold);
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
                new DamageResolveBehavior(testTarget, casterData));
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

        private sealed class MarkConsumedFlagCaptureBehavior : ISpellBehavior
        {
            public SpellBehaviorPhase Phase => SpellBehaviorPhase.Effect;
            public bool HasFlag { get; private set; }

            public void Execute(SpellBehaviorContext context)
            {
                HasFlag = context.HasExecutionFlag(SpellExecutionFlag.MarkConsumed);
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

        private sealed class TestTarget : MonoBehaviour, IDamageable, IStatisticsController
        {
            public event Action<float, float> HealthChanged;
            public event Action<float, float> StaminaChanged;
            public event Action<float, float> OnManaChanged;
            public event Action Died;

            public List<DamageResult> Results { get; } = new();
            public bool CanReceiveDamage => IsAlive;
            public float CurrentHealth { get; private set; }
            public float CurrentStamina => 0f;
            public float CurrentMana => 0f;
            public float MaxHealth { get; private set; }
            public float MaxStamina => 0f;
            public float MaxMana => 0f;
            public float HealthRegenerationPerSecond => 0f;
            public float StaminaRegenerationPerSecond => 0f;
            public float StaminaRegenerationDelay => 0f;
            public float ManaRegenerationPerSecond => 0f;
            public float ManaRegenerationDelay => 0f;
            public float HealthNormalized => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
            public float StaminaNormalized => 0f;
            public float ManaNormalized => 0f;
            public bool IsAlive => CurrentHealth > 0f;

            public void SetHealth(float currentHealth, float maxHealth)
            {
                CurrentHealth = currentHealth;
                MaxHealth = maxHealth;
                Results.Clear();
            }

            public DamageResult ApplyDamage(DamageData data)
            {
                if (!CanReceiveDamage || !data.HasDamage)
                {
                    DamageResult ignored = DamageResult.Ignored(data, CurrentHealth);
                    Results.Add(ignored);
                    return ignored;
                }

                float previousHealth = CurrentHealth;
                TakeDamage(data.Amount);
                float appliedAmount = Mathf.Max(0f, previousHealth - CurrentHealth);
                DamageResult result = appliedAmount > 0f
                    ? DamageResult.Applied(
                        data,
                        appliedAmount,
                        previousHealth,
                        CurrentHealth,
                        !IsAlive)
                    : DamageResult.Ignored(data, CurrentHealth);
                Results.Add(result);
                return result;
            }

            public void TakeDamage(float amount)
            {
                if (amount <= 0f || !IsAlive)
                {
                    return;
                }

                float previousHealth = CurrentHealth;
                CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, MaxHealth);
                HealthChanged?.Invoke(CurrentHealth, MaxHealth);
                if (previousHealth > 0f && CurrentHealth <= 0f)
                {
                    Died?.Invoke();
                }
            }

            public void Heal(float amount)
            {
                CurrentHealth = Mathf.Clamp(CurrentHealth + Mathf.Max(0f, amount), 0f, MaxHealth);
                HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            }

            public bool CanSpendStamina(float amount)
            {
                return true;
            }

            public bool TrySpendStamina(float amount)
            {
                return true;
            }

            public void RestoreStamina(float amount)
            {
                StaminaChanged?.Invoke(CurrentStamina, MaxStamina);
            }

            public bool CanSpendMana(float amount)
            {
                return true;
            }

            public bool TrySpendMana(float amount)
            {
                return true;
            }

            public void RestoreMana(float amount)
            {
                OnManaChanged?.Invoke(CurrentMana, MaxMana);
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
