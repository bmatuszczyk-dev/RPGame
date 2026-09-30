using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Combat.Damage;
using RPGame.Combat.Spells;
using RPGame.Core.Damage;
using RPGame.Core.Effects;
using RPGame.Core.Movement;
using RPGame.Core.Spells;
using RPGame.Core.Statistics;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Combat.Tests
{
    public sealed class ExposureBehaviorTests
    {
        private readonly List<Object> createdObjects = new();
        private GameObject target;
        private GameObject firstCaster;
        private GameObject secondCaster;
        private SlowStatusDefinition slowStatus;
        private WeaknessStatusDefinition weaknessStatus;
        private TestStatusReceiver statusReceiver;
        private DamageReceiver damageReceiver;

        [SetUp]
        public void SetUp()
        {
            target = CreateGameObject("Target");
            firstCaster = CreateGameObject("First Caster");
            secondCaster = CreateGameObject("Second Caster");
            slowStatus = CreateSlowStatus();
            weaknessStatus = CreateWeaknessStatus();

            target.AddComponent<TestMovement>();
            statusReceiver = target.AddComponent<TestStatusReceiver>();
            statusReceiver.Initialize();

            StatisticsConfig statisticsConfig = ScriptableObject.CreateInstance<StatisticsConfig>();
            createdObjects.Add(statisticsConfig);

            StatisticsController statisticsController = target.AddComponent<StatisticsController>();
            SerializedObject serializedStatistics = new(statisticsController);
            serializedStatistics.FindProperty("config").objectReferenceValue = statisticsConfig;
            serializedStatistics.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject serializedConfig = new(statisticsConfig);
            serializedConfig.FindProperty("maxHealth").floatValue = 200f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            statisticsController.ResetToConfig();

            damageReceiver = target.AddComponent<DamageReceiver>();
            SerializedObject serializedDamageReceiver = new(damageReceiver);
            serializedDamageReceiver.FindProperty("loggingEnabled").boolValue = false;
            serializedDamageReceiver.ApplyModifiedPropertiesWithoutUndo();
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
        public void Resolve_FirstExposure_AppliesOneWeaknessStack()
        {
            ExecuteSlowZoneResolve(firstCaster);

            Assert.AreEqual(1, statusReceiver.CountStatusInstances(weaknessStatus));
            Assert.AreEqual(0.1f, GetWeakness().Value, 0.0001f);
        }

        [Test]
        public void Resolve_ConsecutiveExposure_IncreasesWeakness()
        {
            ExecuteSlowZoneResolve(firstCaster);
            ExecuteSlowZoneResolve(firstCaster);

            Assert.AreEqual(0.2f, GetWeakness().Value, 0.0001f);
        }

        [Test]
        public void Resolve_Weakness_DoesNotExceedCap()
        {
            ExecuteSlowZoneResolve(firstCaster);
            ExecuteSlowZoneResolve(firstCaster);
            ExecuteSlowZoneResolve(firstCaster);
            ExecuteSlowZoneResolve(firstCaster);

            Assert.AreEqual(0.3f, GetWeakness().Value, 0.0001f);
        }

        [Test]
        public void Resolve_Weakness_ReappliesFullDuration()
        {
            ExecuteSlowZoneResolve(firstCaster, weaknessDuration: 5f);
            statusReceiver.Tick(2f);

            ExecuteSlowZoneResolve(firstCaster, weaknessDuration: 5f);

            Assert.AreEqual(5f, GetWeakness().RemainingDuration, 0.0001f);
        }

        [Test]
        public void Resolve_Weakness_IsSharedAcrossCasters()
        {
            ExecuteSlowZoneResolve(firstCaster);
            ExecuteSlowZoneResolve(secondCaster);

            Assert.AreEqual(1, statusReceiver.CountStatusInstances(weaknessStatus));
            Assert.AreEqual(0.2f, GetWeakness().Value, 0.0001f);
        }

        [Test]
        public void DamageReceiver_Weakness_IncreasesAnyIncomingDamage()
        {
            ExecuteSlowZoneResolve(firstCaster);
            ExecuteSlowZoneResolve(firstCaster);
            ExecuteSlowZoneResolve(firstCaster);

            DamageResult result = damageReceiver.ApplyDamage(new DamageData(
                new[]
                {
                    new PartialDamage(60f, DamageType.Physical, DamageElement.None),
                    new PartialDamage(40f, DamageType.Magical, DamageElement.Fire)
                },
                secondCaster));

            Assert.AreEqual(130f, result.AppliedAmount, 0.0001f);
            Assert.AreEqual(78f, result.Data.Parts[0].Amount, 0.0001f);
            Assert.AreEqual(52f, result.Data.Parts[1].Amount, 0.0001f);
            Assert.AreSame(secondCaster, result.Data.Source);
        }

        [Test]
        public void SlowZoneTechnicalReapply_DoesNotAddWeaknessStack()
        {
            ExecuteSlowZoneResolve(firstCaster);

            statusReceiver.ApplyStatus(
                slowStatus,
                2.5f,
                new StatusContext(new StatusSourceId(nameof(EarthZoneBehaviour)), firstCaster));

            Assert.AreEqual(0.1f, GetWeakness().Value, 0.0001f);
        }

        [Test]
        public void DeepExposure_IncreasesExistingExposureDuration()
        {
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviors(
                weaknessDuration: 3f,
                weaknessDurationBonus: 2f);

            ExposureBehavior exposure = GetExposureBehavior(behaviors);

            Assert.AreEqual(5f, exposure.WeaknessDuration, 0.0001f);
        }

        [Test]
        public void DeepExposure_DoesNotCreateSecondExposureBehavior()
        {
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviors(
                weaknessDuration: 3f,
                weaknessDurationBonus: 2f);

            Assert.AreEqual(1, behaviors.Count);
            Assert.AreEqual(1, CountExposureBehaviors(behaviors));
        }

        [Test]
        public void DeepExposure_WeaknessStillStacksNormally()
        {
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviors(
                weaknessDuration: 3f,
                weaknessDurationBonus: 2f);

            ExecuteSlowZoneResolve(firstCaster, behaviors);
            ExecuteSlowZoneResolve(firstCaster, behaviors);

            Assert.AreEqual(0.2f, GetWeakness().Value, 0.0001f);
        }

        [Test]
        public void DeepExposure_WeaknessCapIsUnchanged()
        {
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviors(
                weaknessDuration: 3f,
                weaknessDurationBonus: 2f);

            ExecuteSlowZoneResolve(firstCaster, behaviors);
            ExecuteSlowZoneResolve(firstCaster, behaviors);
            ExecuteSlowZoneResolve(firstCaster, behaviors);
            ExecuteSlowZoneResolve(firstCaster, behaviors);

            Assert.AreEqual(0.3f, GetWeakness().Value, 0.0001f);
        }

        [Test]
        public void DeepExposure_ReapplyRefreshesExtendedDuration()
        {
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviors(
                weaknessDuration: 3f,
                weaknessDurationBonus: 2f);
            ExecuteSlowZoneResolve(firstCaster, behaviors);
            statusReceiver.Tick(2f);

            ExecuteSlowZoneResolve(firstCaster, behaviors);

            Assert.AreEqual(5f, GetWeakness().RemainingDuration, 0.0001f);
        }

        private void ExecuteSlowZoneResolve(GameObject caster, float weaknessDuration = 3f)
        {
            ExecuteSlowZoneResolve(caster, new IRuntimeSpellBehavior[]
            {
                new ExposureBehavior(weaknessStatus, weaknessDuration)
            });
        }

        private void ExecuteSlowZoneResolve(
            GameObject caster,
            IReadOnlyList<IRuntimeSpellBehavior> runtimeBehaviors)
        {
            CasterData casterData = new CasterDataBuilder(caster, caster.transform, target.transform)
                .WithSpellId(new SpellId("earth_zone"))
                .WithRuntimeBehaviors(runtimeBehaviors)
                .Build();

            SpellBehaviorPipelineExecutor.Resolve(
                casterData,
                target,
                new SlowZoneResolveBehavior(slowStatus, 2.5f, caster));
        }

        private IReadOnlyList<IRuntimeSpellBehavior> CreateRuntimeBehaviors(
            float weaknessDuration,
            float weaknessDurationBonus)
        {
            RuntimeBehaviorEffectDefinition exposureEffect =
                ScriptableObject.CreateInstance<RuntimeBehaviorEffectDefinition>();
            DeepExposureEffectDefinition deepExposureEffect =
                ScriptableObject.CreateInstance<DeepExposureEffectDefinition>();
            EarthZoneSpell spell = ScriptableObject.CreateInstance<EarthZoneSpell>();
            createdObjects.Add(exposureEffect);
            createdObjects.Add(deepExposureEffect);
            createdObjects.Add(spell);

            SetBehavior(exposureEffect, new ExposureBehavior(weaknessStatus, weaknessDuration));
            SerializedObject serializedDeepExposure = new(deepExposureEffect);
            serializedDeepExposure.FindProperty("weaknessDurationBonus").floatValue = weaknessDurationBonus;
            serializedDeepExposure.ApplyModifiedPropertiesWithoutUndo();

            EffectAggregator aggregator = target.AddComponent<EffectAggregator>();
            aggregator.AddRange(new PassiveEffectDefinition[] { exposureEffect, deepExposureEffect });
            return aggregator.CreateRuntimeBehaviors(spell, firstCaster);
        }

        private static ExposureBehavior GetExposureBehavior(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors)
        {
            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is ExposureBehavior exposure)
                {
                    return exposure;
                }
            }

            return null;
        }

        private static int CountExposureBehaviors(IReadOnlyList<IRuntimeSpellBehavior> behaviors)
        {
            int count = 0;
            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is ExposureBehavior)
                {
                    count++;
                }
            }

            return count;
        }

        private static void SetBehavior(
            RuntimeBehaviorEffectDefinition target,
            IRuntimeSpellBehavior behavior)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty("behavior").managedReferenceValue = behavior;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private StatusInstance GetWeakness()
        {
            return statusReceiver.GetFirstStatus(weaknessStatus);
        }

        private SlowStatusDefinition CreateSlowStatus()
        {
            SlowStatusDefinition status = ScriptableObject.CreateInstance<SlowStatusDefinition>();
            createdObjects.Add(status);
            return status;
        }

        private WeaknessStatusDefinition CreateWeaknessStatus()
        {
            WeaknessStatusDefinition status = ScriptableObject.CreateInstance<WeaknessStatusDefinition>();
            createdObjects.Add(status);
            SerializedObject serializedStatus = new(status);
            serializedStatus.FindProperty("stackValue").floatValue = 0.1f;
            serializedStatus.FindProperty("maxStack").intValue = 3;
            serializedStatus.ApplyModifiedPropertiesWithoutUndo();
            return status;
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class SlowZoneResolveBehavior : ISpellResolveBehavior
        {
            private readonly SlowStatusDefinition slowStatus;
            private readonly float duration;
            private readonly GameObject caster;

            public SlowZoneResolveBehavior(
                SlowStatusDefinition slowStatus,
                float duration,
                GameObject caster)
            {
                this.slowStatus = slowStatus;
                this.duration = duration;
                this.caster = caster;
            }

            public SpellBehaviorPhase Phase => SpellBehaviorPhase.Resolve;

            public bool Resolve(SpellBehaviorContext context)
            {
                return context.StatusReceiver.ApplyStatus(
                    slowStatus,
                    duration,
                    new StatusContext(new StatusSourceId(nameof(EarthZoneBehaviour)), caster));
            }
        }

        private sealed class TestStatusReceiver : MonoBehaviour, IStatusReceiver, IStatusReader
        {
            private StatusContainer container;

            public IReadOnlyList<StatusInstance> Statuses => container.Statuses;

            public void Initialize()
            {
                container = new StatusContainer(new StatusTarget(
                    null,
                    GetComponent<IMovement>(),
                    GetComponent<IDamageable>()));
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

        private sealed class TestMovement : MonoBehaviour, IMovement
        {
            private readonly Dictionary<int, float> modifiers = new();
            private int nextModifierId;

            public void BlockMovement()
            {
            }

            public void UnblockMovement()
            {
            }

            public int AddMovementSpeedModifier(float multiplier)
            {
                nextModifierId++;
                modifiers.Add(nextModifierId, multiplier);
                return nextModifierId;
            }

            public void RemoveMovementSpeedModifier(int modifierId)
            {
                modifiers.Remove(modifierId);
            }
        }
    }
}
