using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Combat.Damage;
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
    public sealed class SuppressionBehaviorTests
    {
        private readonly List<Object> createdObjects = new();
        private GameObject caster;
        private GameObject suppressedTarget;
        private MarkStatusDefinition markDefinition;
        private SuppressionStatusDefinition suppressionStatus;
        private TestStatusReceiver statusReceiver;
        private DamageReceiver damageReceiver;

        [SetUp]
        public void SetUp()
        {
            caster = CreateGameObject("Caster");
            suppressedTarget = CreateGameObject("Suppressed Target");
            markDefinition = ScriptableObject.CreateInstance<MarkStatusDefinition>();
            suppressionStatus = ScriptableObject.CreateInstance<SuppressionStatusDefinition>();
            createdObjects.Add(markDefinition);
            createdObjects.Add(suppressionStatus);
            SetDamageReductionPercent(0.5f);

            statusReceiver = suppressedTarget.AddComponent<TestStatusReceiver>();
            statusReceiver.Initialize();
            damageReceiver = CreateDamageReceiver(CreateGameObject("Victim"), 500f);
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
        public void Resolve_WithoutMark_DoesNotApplySuppression()
        {
            ResolveSuppression();

            Assert.IsFalse(statusReceiver.HasStatus(suppressionStatus));
        }

        [Test]
        public void Resolve_WithMark_ConsumesMarkAndAppliesSuppression()
        {
            ApplyMark();

            ResolveSuppression();

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));
            Assert.IsTrue(statusReceiver.HasStatus(suppressionStatus));
        }

        [Test]
        public void Resolve_WhenAnotherConsumerUsesMark_AppliesSuppressionFromSharedConsumption()
        {
            ApplyMark();
            SharedMarkConsumer firstConsumer = new(markDefinition);

            ResolveSuppression(
                runtimeBehaviors: new IRuntimeSpellBehavior[]
                {
                    firstConsumer,
                    new SuppressionBehavior(markDefinition, suppressionStatus, 3f)
                });

            Assert.IsTrue(firstConsumer.ConsumedMark);
            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));
            Assert.IsTrue(statusReceiver.HasStatus(suppressionStatus));
        }

        [Test]
        public void Supports_OnlyEarthZoneSpell()
        {
            SuppressionBehavior behavior =
                new(markDefinition, suppressionStatus, 3f);
            EarthZoneSpell earthZoneSpell = ScriptableObject.CreateInstance<EarthZoneSpell>();
            ProjectileSpell projectileSpell = ScriptableObject.CreateInstance<ProjectileSpell>();
            createdObjects.Add(earthZoneSpell);
            createdObjects.Add(projectileSpell);

            Assert.IsTrue(behavior.Supports(earthZoneSpell));
            Assert.IsFalse(behavior.Supports(projectileSpell));
        }

        [Test]
        public void DamageData_SuppressionReducesAllDamageParts()
        {
            ApplyMark();
            ResolveSuppression();

            DamageResult result = damageReceiver.ApplyDamage(new DamageData(
                new[]
                {
                    new PartialDamage(60f, DamageType.Physical, DamageElement.None),
                    new PartialDamage(40f, DamageType.Magical, DamageElement.Fire)
                },
                suppressedTarget));

            Assert.AreEqual(50f, result.AppliedAmount, 0.0001f);
            Assert.AreEqual(30f, result.Data.Parts[0].Amount, 0.0001f);
            Assert.AreEqual(20f, result.Data.Parts[1].Amount, 0.0001f);
        }

        [Test]
        public void DamageData_WhenSuppressionExpires_DamageReturnsToNormal()
        {
            ApplyMark();
            ResolveSuppression(duration: 3f);
            statusReceiver.Tick(3f);

            DamageResult result = damageReceiver.ApplyDamage(new DamageData(
                new[] { new PartialDamage(100f, DamageType.Magical, DamageElement.None) },
                suppressedTarget));

            Assert.IsFalse(statusReceiver.HasStatus(suppressionStatus));
            Assert.AreEqual(100f, result.AppliedAmount, 0.0001f);
        }

        [Test]
        public void Resolve_ReapplyRefreshesSuppressionDurationWithoutStacking()
        {
            ApplyMark();
            ResolveSuppression(duration: 5f);
            statusReceiver.Tick(2f);
            ApplyMark();

            ResolveSuppression(duration: 5f);

            Assert.AreEqual(1, statusReceiver.CountStatusInstances(suppressionStatus));
            Assert.AreEqual(5f, statusReceiver.GetFirstStatus(suppressionStatus).RemainingDuration, 0.0001f);
            Assert.AreEqual(0.5f, statusReceiver.GetFirstStatus(suppressionStatus).Value, 0.0001f);
        }

        private void ApplyMark()
        {
            statusReceiver.ApplyStatus(
                markDefinition,
                5f,
                new StatusContext(new StatusSourceId("Test"), caster));
        }

        private void ResolveSuppression(
            float duration = 3f,
            IReadOnlyList<IRuntimeSpellBehavior> runtimeBehaviors = null)
        {
            runtimeBehaviors ??= new IRuntimeSpellBehavior[]
            {
                new SuppressionBehavior(markDefinition, suppressionStatus, duration)
            };

            CasterData casterData = new CasterDataBuilder(
                    caster,
                    caster.transform,
                    suppressedTarget.transform)
                .WithSpellId(new SpellId("earth_zone"))
                .WithRuntimeBehaviors(runtimeBehaviors)
                .Build();

            SpellBehaviorPipelineExecutor.Resolve(
                casterData,
                suppressedTarget,
                new SuccessfulResolveBehavior());
        }

        private DamageReceiver CreateDamageReceiver(GameObject target, float maxHealth)
        {
            StatisticsConfig statisticsConfig = ScriptableObject.CreateInstance<StatisticsConfig>();
            createdObjects.Add(statisticsConfig);

            StatisticsController statisticsController = target.AddComponent<StatisticsController>();
            SerializedObject serializedConfig = new(statisticsConfig);
            serializedConfig.FindProperty("maxHealth").floatValue = maxHealth;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedStatistics = new(statisticsController);
            serializedStatistics.FindProperty("config").objectReferenceValue = statisticsConfig;
            serializedStatistics.ApplyModifiedPropertiesWithoutUndo();
            statisticsController.ResetToConfig();

            DamageReceiver receiver = target.AddComponent<DamageReceiver>();
            SerializedObject serializedReceiver = new(receiver);
            serializedReceiver.FindProperty("loggingEnabled").boolValue = false;
            serializedReceiver.ApplyModifiedPropertiesWithoutUndo();
            return receiver;
        }

        private void SetDamageReductionPercent(float value)
        {
            SerializedObject serializedStatus = new(suppressionStatus);
            serializedStatus.FindProperty("damageReductionPercent").floatValue = value;
            serializedStatus.ApplyModifiedPropertiesWithoutUndo();
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class SuccessfulResolveBehavior : ISpellResolveBehavior
        {
            public SpellBehaviorPhase Phase => SpellBehaviorPhase.Resolve;

            public bool Resolve(SpellBehaviorContext context)
            {
                return true;
            }
        }

        private sealed class SharedMarkConsumer : IMarkConsumer
        {
            private readonly MarkStatusDefinition markDefinition;

            public SharedMarkConsumer(MarkStatusDefinition markDefinition)
            {
                this.markDefinition = markDefinition;
            }

            public bool ConsumedMark { get; private set; }

            public bool TryConsumeMark(SpellBehaviorContext context)
            {
                if (context.HasExecutionFlag(SpellExecutionFlag.MarkConsumed))
                {
                    return true;
                }

                if (context.StatusReceiver == null
                    || !context.StatusReceiver.TryConsumeStatus(markDefinition))
                {
                    return false;
                }

                context.SetExecutionFlag(SpellExecutionFlag.MarkConsumed);
                ConsumedMark = true;
                return true;
            }
        }

        private sealed class TestStatusReceiver : MonoBehaviour, IStatusReceiver, IStatusReader
        {
            private StatusContainer container;

            public IReadOnlyList<StatusInstance> Statuses => container.Statuses;

            public void Initialize()
            {
                container = new StatusContainer(default);
            }

            public bool ApplyStatus(StatusDefinition status, float duration, StatusContext context)
            {
                return container.Add(status, duration, context);
            }

            public bool HasStatus(StatusDefinition status)
            {
                return container.HasStatus(status);
            }

            public bool TryConsumeStatus(StatusDefinition status)
            {
                return container.TryConsumeStatus(status);
            }

            public int CountStatusInstances(StatusDefinition status)
            {
                return container.CountStatusInstances(status);
            }

            public StatusInstance GetFirstStatus(StatusDefinition status)
            {
                return container.GetFirstStatus(status);
            }

            public void Tick(float deltaTime)
            {
                container.Tick(deltaTime);
            }
        }
    }
}
